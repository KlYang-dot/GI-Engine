using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NAudio.CoreAudioApi;
using UnityEngine;

/// <summary>
/// 纯净版：完全不碰 IAudioSessionManager2，不注册任何通知，只用最基础的采集
/// </summary>
public class AudioSessionService : MonoBehaviour
{
    private static AudioSessionService _instance;
    private SynchronizationContext mainThreadContext;

    private AutoResetEvent requestEvent = new AutoResetEvent(false);
    private Action<List<string>> pendingCallback;
    private bool isRunning = true;
    private Thread workerStaThread;

    public static AudioSessionService Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[AudioSessionService]");
                _instance = go.AddComponent<AudioSessionService>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        mainThreadContext = SynchronizationContext.Current ?? new SynchronizationContext();

        workerStaThread = new Thread(PureWindowsAudioListener);
        workerStaThread.SetApartmentState(ApartmentState.STA);
        workerStaThread.IsBackground = true;
        workerStaThread.Start();
    }

    public void GetRunningAudioAppsAsync(Action<List<string>> onCompleted)
    {
        pendingCallback = onCompleted;
        requestEvent.Set();
    }

    /// <summary>
    /// 【完全避开 IAudioSessionManager2】的扫描函数
    /// </summary>
    private void PureWindowsAudioListener()
    {
        MMDeviceEnumerator enumerator = null;
        MMDevice device = null;

        try
        {
            enumerator = new MMDeviceEnumerator();
            device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[PureAudio] 初始化基础音频端点失败: {ex.Message}");
        }

        while (isRunning)
        {
            requestEvent.WaitOne();
            if (!isRunning) break;

            List<string> result = new List<string>();
            var callback = pendingCallback;

            if (device != null && callback != null)
            {
                try
                {
                    // 1. 优先使用最基础的 AudioMeterInformation（这属于 IAudioMeterInformation 接口，不属于 IAudioSessionManager2）
                    float globalVolume = device.AudioMeterInformation.MasterPeakValue;

                    // 2. 如果全局有声音
                    if (globalVolume > 0.005f)
                    {
                        // 3. 【避开Manager的替代策略】：直接获取当前系统所有正在运行的、带有窗口的、或者常见的播放器进程
                        // 因为无法从底层进程音频流中安全提取（为了不碰那个炸弹接口），
                        // 我们采用“快照过滤法”：只要这些进程存在，且系统当前有总音量，就判定为它们在发声。
                        var allProcesses = Process.GetProcesses();
                        var names = new HashSet<string>();

                        foreach (var proc in allProcesses)
                        {
                            try
                            {
                                // 过滤掉没有界面的后台死进程，只保留可能在放歌放视频的活跃进程
                                if (proc.MainWindowHandle != IntPtr.Zero || proc.ProcessName.Contains("music") || proc.ProcessName.Contains("player"))
                                {
                                    names.Add(proc.ProcessName.ToLowerInvariant());
                                }
                            }
                            catch { }
                            finally { proc.Dispose(); }
                        }

                        result = names.OrderBy(n => n).ToList();
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[PureAudio] 基础检测异常: {ex.Message}");
                    try
                    {
                        device?.Dispose(); enumerator?.Dispose();
                        enumerator = new MMDeviceEnumerator();
                        device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    }
                    catch { }
                }
            }

            if (callback != null)
            {
                mainThreadContext.Post(_ => callback.Invoke(result), null);
            }
        }

        try { device?.Dispose(); enumerator?.Dispose(); } catch { }
    }

    private void OnDestroy()
    {
        isRunning = false;
        requestEvent.Set();
    }
}