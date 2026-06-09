using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 管理 UDP 端口绑定，检测占用进程，弹窗询问是否终止
/// </summary>
public class UDPPortManager : MonoBehaviour
{
    public static UDPPortManager Instance { get; private set; }

    [Header("UDP 配置")]
    public int port = 32145;
    public int maxRetries = 3;
    public float retryDelayMs = 100f;

    private UdpClient udpClient;
    private bool isBinding = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        if (!isBinding)
        {
            isBinding = true;
            TryBindUDP();
        }
    }

    void OnDisable()
    {
        ReleaseUDP();
    }

    /// <summary>
    /// 尝试绑定 UDP 端口，如果失败则检测占用进程并弹窗
    /// </summary>
    private void TryBindUDP()
    {
        for (int retries = 0; retries < maxRetries; retries++)
        {
            try
            {
                udpClient = new UdpClient(port);
                UnityEngine.Debug.Log($"[UDPPortManager] 成功绑定 UDP 端口 {port}");
                return;
            }
            catch (SocketException ex)
            {
                UnityEngine.Debug.LogWarning($"[UDPPortManager] 绑定端口失败 (尝试 {retries + 1}/{maxRetries}): {ex.Message}");

                if (retries == maxRetries - 1)
                {
                    // 最后一次尝试失败，检测占用进程
                    HandlePortConflict();
                }
                else
                {
                    System.Threading.Thread.Sleep((int)retryDelayMs);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[UDPPortManager] 未知错误: {ex.Message}");
                return;
            }
        }
    }

    /// <summary>
    /// 处理端口冲突：检测占用进程并弹窗询问
    /// </summary>
    private void HandlePortConflict()
    {
        List<Process> occupyingProcesses = GetProcessesUsingPort(port);

        if (occupyingProcesses.Count == 0)
        {
            UnityEngine.Debug.LogError($"[UDPPortManager] 无法确定哪个进程占用了端口 {port}");
            ShowErrorDialog($"端口 {port} 已被占用，但无法确定占用进程。\n请手动释放该端口。");
            return;
        }

        string processNames = string.Join("\n", occupyingProcesses.ConvertAll(p => $"• {p.ProcessName} (PID: {p.Id})"));

        ShowConfirmDialog(
            $"端口 {port} 已被占用\n\n占用进程:\n{processNames}\n\n是否终止这些进程?",
            () => TerminateProcesses(occupyingProcesses),
            () => UnityEngine.Debug.LogWarning($"[UDPPortManager] 用户选择不终止进程，UDP 功能将禁用")
        );
    }

    /// <summary>
    /// 获取占用指定端口的进程列表（仅 Windows）
    /// </summary>
    private List<Process> GetProcessesUsingPort(int port)
    {
        List<Process> result = new List<Process>();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                string[] lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    if (line.Contains("UDP") && line.Contains($":{port}"))
                    {
                        string[] parts = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5 && int.TryParse(parts[parts.Length - 1], out int pid))
                        {
                            try
                            {
                                Process p = Process.GetProcessById(pid);
                                if (!result.Contains(p))
                                    result.Add(p);
                            }
                            catch { }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[UDPPortManager] 获取占用进程失败: {ex.Message}");
        }
#else
        UnityEngine.Debug.LogWarning("[UDPPortManager] 仅支持 Windows 系统检测占用进程");
#endif

        return result;
    }

    /// <summary>
    /// 终止指定进程列表
    /// </summary>
    private void TerminateProcesses(List<Process> processes)
    {
        foreach (var p in processes)
        {
            try
            {
                p.Kill(); // 在 .NET 6+ 中使用 Kill() 无参版本
                UnityEngine.Debug.Log($"[UDPPortManager] 已终止进程: {p.ProcessName} (PID: {p.Id})");
                System.Threading.Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[UDPPortManager] 终止进程失败: {ex.Message}");
            }
        }

        // 终止后重新尝试绑定
        System.Threading.Thread.Sleep(1000);
        isBinding = false;
        TryBindUDP();
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    private void ShowConfirmDialog(string message, Action onConfirm, Action onCancel)
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            UnityEngine.Debug.LogError("[UDPPortManager] 场景中没有 Canvas，无法显示对话框");
            onCancel?.Invoke();
            return;
        }

        // 创建对话框 GameObject
        GameObject dialogGO = new GameObject("PortConflictDialog");
        dialogGO.transform.SetParent(canvas.transform, false);

        RectTransform rect = dialogGO.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bgImage = dialogGO.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);

        // 创建内容面板
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(dialogGO.transform);
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(500, 300);
        contentRect.anchoredPosition = Vector2.zero;

        Image contentImage = contentGO.AddComponent<Image>();
        contentImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // 标题
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(contentGO.transform);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(480, 50);
        titleRect.anchoredPosition = new Vector2(0, 110);

        Text titleText = titleGO.AddComponent<Text>();
        titleText.text = "端口冲突警告";
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.8f, 0.2f, 1f);

        // 消息文本
        GameObject msgGO = new GameObject("Message");
        msgGO.transform.SetParent(contentGO.transform);
        RectTransform msgRect = msgGO.AddComponent<RectTransform>();
        msgRect.sizeDelta = new Vector2(480, 150);
        msgRect.anchoredPosition = new Vector2(0, 30);

        Text msgText = msgGO.AddComponent<Text>();
        msgText.text = message;
        msgText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        msgText.fontSize = 14;
        msgText.alignment = TextAnchor.MiddleCenter;
        msgText.color = Color.white;
        msgText.horizontalOverflow = HorizontalWrapMode.Wrap;
        msgText.verticalOverflow = VerticalWrapMode.Truncate;

        // 确认按钮
        GameObject confirmBtnGO = new GameObject("ConfirmButton");
        confirmBtnGO.transform.SetParent(contentGO.transform);
        RectTransform confirmRect = confirmBtnGO.AddComponent<RectTransform>();
        confirmRect.sizeDelta = new Vector2(150, 50);
        confirmRect.anchoredPosition = new Vector2(-100, -80);

        Image confirmImage = confirmBtnGO.AddComponent<Image>();
        confirmImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);

        Button confirmBtn = confirmBtnGO.AddComponent<Button>();
        confirmBtn.targetGraphic = confirmImage;
        confirmBtn.onClick.AddListener(() =>
        {
            Destroy(dialogGO);
            onConfirm?.Invoke();
        });

        Text confirmText = confirmBtnGO.AddComponent<Text>();
        confirmText.text = "是";
        confirmText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        confirmText.fontSize = 18;
        confirmText.alignment = TextAnchor.MiddleCenter;
        confirmText.color = Color.white;

        // 取消按钮
        GameObject cancelBtnGO = new GameObject("CancelButton");
        cancelBtnGO.transform.SetParent(contentGO.transform);
        RectTransform cancelRect = cancelBtnGO.AddComponent<RectTransform>();
        cancelRect.sizeDelta = new Vector2(150, 50);
        cancelRect.anchoredPosition = new Vector2(100, -80);

        Image cancelImage = cancelBtnGO.AddComponent<Image>();
        cancelImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        Button cancelBtn = cancelBtnGO.AddComponent<Button>();
        cancelBtn.targetGraphic = cancelImage;
        cancelBtn.onClick.AddListener(() =>
        {
            Destroy(dialogGO);
            onCancel?.Invoke();
        });

        Text cancelText = cancelBtnGO.AddComponent<Text>();
        cancelText.text = "否";
        cancelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        cancelText.fontSize = 18;
        cancelText.alignment = TextAnchor.MiddleCenter;
        cancelText.color = Color.white;
    }

    /// <summary>
    /// 显示错误对话框
    /// </summary>
    private void ShowErrorDialog(string message)
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            UnityEngine.Debug.LogError($"[UDPPortManager] {message}");
            return;
        }

        GameObject dialogGO = new GameObject("ErrorDialog");
        dialogGO.transform.SetParent(canvas.transform, false);

        RectTransform rect = dialogGO.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bgImage = dialogGO.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);

        // 内容面板
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(dialogGO.transform);
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(500, 250);
        contentRect.anchoredPosition = Vector2.zero;

        Image contentImage = contentGO.AddComponent<Image>();
        contentImage.color = new Color(0.3f, 0.1f, 0.1f, 1f);

        // 消息文本
        GameObject msgGO = new GameObject("Message");
        msgGO.transform.SetParent(contentGO.transform);
        RectTransform msgRect = msgGO.AddComponent<RectTransform>();
        msgRect.sizeDelta = new Vector2(480, 150);
        msgRect.anchoredPosition = new Vector2(0, 30);

        Text msgText = msgGO.AddComponent<Text>();
        msgText.text = message;
        msgText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        msgText.fontSize = 14;
        msgText.alignment = TextAnchor.MiddleCenter;
        msgText.color = new Color(1f, 0.8f, 0.8f, 1f);
        msgText.horizontalOverflow = HorizontalWrapMode.Wrap;

        // 关闭按钮
        GameObject closeBtnGO = new GameObject("CloseButton");
        closeBtnGO.transform.SetParent(contentGO.transform);
        RectTransform closeRect = closeBtnGO.AddComponent<RectTransform>();
        closeRect.sizeDelta = new Vector2(150, 50);
        closeRect.anchoredPosition = new Vector2(0, -80);

        Image closeImage = closeBtnGO.AddComponent<Image>();
        closeImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        Button closeBtn = closeBtnGO.AddComponent<Button>();
        closeBtn.targetGraphic = closeImage;
        closeBtn.onClick.AddListener(() => Destroy(dialogGO));

        Text closeText = closeBtnGO.AddComponent<Text>();
        closeText.text = "确定";
        closeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        closeText.fontSize = 18;
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.color = Color.white;
    }

    /// <summary>
    /// 释放 UDP 资源
    /// </summary>
    private void ReleaseUDP()
    {
        if (udpClient != null)
        {
            try
            {
                udpClient.Close();
                udpClient.Dispose();
                UnityEngine.Debug.Log($"[UDPPortManager] 已释放 UDP 端口 {port}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[UDPPortManager] 释放 UDP 失败: {ex.Message}");
            }
            udpClient = null;
        }
    }

    /// <summary>
    /// 获取 UDP 客户端（供外部使用）
    /// </summary>
    public UdpClient GetUdpClient()
    {
        return udpClient;
    }

    /// <summary>
    /// 检查是否成功绑定
    /// </summary>
    public bool IsBindingSuccessful()
    {
        return udpClient != null;
    }
}