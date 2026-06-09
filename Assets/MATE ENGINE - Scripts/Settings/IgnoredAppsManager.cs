using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NAudio.CoreAudioApi;
using System.Threading;

public class AllowedAppsManager : MonoBehaviour
{
    public TMP_Dropdown runningAppsDropdown;
    public Button addToAllowedListButton;
    public Transform allowedAppsListContent;
    public GameObject allowedAppItemPrefab;

    private MMDeviceEnumerator enumerator;
    private MMDevice defaultDevice;
    private SynchronizationContext mainThreadContext;   // 主线程同步上下文

    private List<string> currentRunningAppNames = new List<string>();
    private List<string> allowedApps => SaveLoadHandler.Instance.data.allowedApps;

    private void Start()
    {
        // 保存主线程的 SynchronizationContext，用于从工作线程回调 UI 更新
        mainThreadContext = SynchronizationContext.Current;
        if (mainThreadContext == null)
            mainThreadContext = new SynchronizationContext(); // 保底

        // 初始化 NAudio 枚举器（主线程创建，但不调用敏感 COM）
        enumerator = new MMDeviceEnumerator();
        UpdateDefaultDevice();

        // 绑定按钮事件
        addToAllowedListButton.onClick.AddListener(() =>
        {
            if (runningAppsDropdown.options.Count == 0) return;

            string selectedApp = runningAppsDropdown.options[runningAppsDropdown.value].text;
            if (!allowedApps.Contains(selectedApp))
            {
                allowedApps.Add(selectedApp);
                UpdateAllowedListUI();
                RefreshRunningAppsDropdownAsync();   // 异步刷新下拉列表
                SaveLoadHandler.Instance.SaveToDisk();
                SaveLoadHandler.SyncAllowedAppsToAllAvatars();
            }
        });

        // 初始异步加载一次
        RefreshRunningAppsDropdownAsync();
        UpdateAllowedListUI();
        SaveLoadHandler.SyncAllowedAppsToAllAvatars(); // 初始同步
    }

    private void UpdateDefaultDevice()
    {
        defaultDevice?.Dispose();
        defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    /// <summary>
    /// 异步刷新正在播放音频的应用程序列表（在 STA 线程中执行，避免 COM 崩溃）
    /// </summary>
    private void RefreshRunningAppsDropdownAsync()
    {
        Thread staThread = new Thread(() =>
        {
            try
            {
                // 强制设置为 STA 单元
                Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

                // 在 STA 线程内重新创建 NAudio 对象，不可跨线程使用主线程创建的 COM 对象
                using (var localEnumerator = new MMDeviceEnumerator())
                using (var localDevice = localEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
                {
                    var appNames = GetRunningAudioAppNamesOnDevice(localDevice);
                    // 回到主线程更新 UI
                    mainThreadContext.Post(_ => UpdateRunningAppsDropdownUI(appNames), null);
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Audio session enumeration failed: {ex.Message}\n{ex.StackTrace}");
                // 出错时清空下拉列表
                mainThreadContext.Post(_ =>
                {
                    runningAppsDropdown.ClearOptions();
                    runningAppsDropdown.value = 0;
                }, null);
            }
        });
        staThread.Start();
    }

    /// <summary>
    /// 在指定音频设备上获取当前播放音频的进程名（此方法必须在 STA 线程内调用）
    /// </summary>
    private List<string> GetRunningAudioAppNamesOnDevice(MMDevice device)
    {
        var appNames = new HashSet<string>();
        try
        {
            var sessionManager = device.AudioSessionManager;
            var sessions = sessionManager.Sessions;
            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                int processId = (int)session.GetProcessID;
                if (processId == 0) continue;

                try
                {
                    var process = Process.GetProcessById(processId);
                    string name = process.ProcessName.ToLowerInvariant();
                    appNames.Add(name);
                }
                catch
                {
                    // 进程可能已退出，忽略
                    continue;
                }
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogWarning($"Error enumerating audio sessions: {ex.Message}");
        }
        return appNames.OrderBy(n => n).ToList();
    }

    /// <summary>
    /// 在主线程更新下拉列表 UI
    /// </summary>
    private void UpdateRunningAppsDropdownUI(List<string> runningAppNames)
    {
        currentRunningAppNames = runningAppNames;

        var filteredAppNames = currentRunningAppNames
            .Where(app => !allowedApps.Contains(app))
            .OrderBy(app => app)
            .ToList();

        runningAppsDropdown.ClearOptions();
        runningAppsDropdown.AddOptions(
            filteredAppNames.Select(app => new TMP_Dropdown.OptionData(app)).ToList()
        );

        if (filteredAppNames.Count == 0)
            runningAppsDropdown.value = 0;
    }

    // 同步刷新方法（对外接口，内部调用异步版本）
    public void RefreshRunningAppsDropdown()
    {
        RefreshRunningAppsDropdownAsync();
    }

    public void OnDropdownOpened()
    {
        RefreshRunningAppsDropdownAsync();
    }

    private void UpdateAllowedListUI()
    {
        foreach (Transform child in allowedAppsListContent)
            Destroy(child.gameObject);

        foreach (var app in allowedApps)
        {
            var item = Instantiate(allowedAppItemPrefab, allowedAppsListContent);

            var label = item.GetComponentsInChildren<TextMeshProUGUI>()
                            .FirstOrDefault(t => t.transform.parent == item.transform);
            if (label != null) label.text = app;

            var button = item.transform.Find("Button")?.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    allowedApps.Remove(app);
                    UpdateAllowedListUI();
                    SaveLoadHandler.Instance.SaveToDisk();
                    SaveLoadHandler.SyncAllowedAppsToAllAvatars();
                });
            }
        }
    }

    private void OnDestroy()
    {
        enumerator?.Dispose();
        defaultDevice?.Dispose();
    }

    public void RefreshAppListOnMenuOpen()
    {
        RefreshRunningAppsDropdownAsync();
        UpdateAllowedListUI();
        SaveLoadHandler.SyncAllowedAppsToAllAvatars();
    }

    public void RefreshUI()
    {
        UpdateDefaultDevice();
        RefreshRunningAppsDropdownAsync();
        UpdateAllowedListUI();
        SaveLoadHandler.SyncAllowedAppsToAllAvatars();
    }
}