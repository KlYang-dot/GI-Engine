using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;

public class AvatarStoreMenu : MonoBehaviour
{
    [Header("Server Settings")]
    [SerializeField] private string serverRoot = "https://paimonbyte.duckdns.org:800/vrmstore/";

    [Header("UI Item References")]
    public GameObject storeItemPrefab;
    public Transform contentParent;
    public GameObject storePanel;

    // 修改：程序运行目录下的 CustomModels 文件夹
    private string saveDir => Path.Combine(GetExecutableDirectory(), "CustomModels");
    private List<VrmModel> storeModels = new List<VrmModel>();
    private List<Texture2D> downloadedTextures = new List<Texture2D>();

    // —— 宏编译控制：仅在编辑器下声明 MessageBox ——
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
    private const uint MB_YESNO = 0x00000004;
    private const uint MB_ICONQUESTION = 0x00000020;
    private const uint MB_ICONWARNING = 0x00000030;
    private const uint MB_ICONERROR = 0x00000010;
    private const int IDYES = 6;
#endif

    private int ShowNativeMessageBox(string text, string caption, uint type)
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        return MessageBox(IntPtr.Zero, text, caption, type);
#else
        Debug.Log($"[弹窗模拟] {caption}: {text}");
        return IDYES; // 非Windows环境自动确认
#endif
    }

    // 获取可执行文件所在目录（程序运行目录）
    private string GetExecutableDirectory()
    {
        // Windows 独立平台：Application.dataPath 是 GIEngine_Data 文件夹，上一级即为 .exe 所在目录
        return Path.GetDirectoryName(Application.dataPath);
    }

    [System.Serializable]
    public class VrmModel
    {
        public string title;
        public string author;
        public string fileName;
        public long fileSizeMB;
        public string downloadUrl;
        public string thumbnailUrl;
        public string version;
        [HideInInspector] public bool isDownloaded;
    }

    private class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }

    private void Start()
    {
        // 确保 CustomModels 文件夹存在
        if (!Directory.Exists(saveDir))
            Directory.CreateDirectory(saveDir);

        FetchStoreModels();
    }

    public void FetchStoreModels() => StartCoroutine(FetchModelsCoroutine());

    private IEnumerator FetchModelsCoroutine()
    {
        using (UnityWebRequest uwr = UnityWebRequest.Get(serverRoot + "get_models.php"))
        {
            uwr.certificateHandler = new BypassCertificateHandler();
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success) yield break;
            storeModels = JsonConvert.DeserializeObject<List<VrmModel>>(uwr.downloadHandler.text);
            foreach (var m in storeModels) m.isDownloaded = File.Exists(Path.Combine(saveDir, m.fileName));
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);
        foreach (var model in storeModels) SetupStoreItem(Instantiate(storeItemPrefab, contentParent), model);
    }

    private void SetupStoreItem(GameObject item, VrmModel model)
    {
        var allTexts = item.GetComponentsInChildren<TMP_Text>(true);
        var allButtons = item.GetComponentsInChildren<Button>(true);

        foreach (var t in allTexts)
        {
            if (t.name == "Title") t.text = model.title;
            if (t.name == "Details") t.text = $"作者: {model.author}\n大小: {model.fileSizeMB} MB";
            if (t.name == "Status")
            {
                t.text = model.isDownloaded ? "已下载" : "未下载";
                t.color = model.isDownloaded ? Color.green : Color.red;
            }
        }

        Button downloadBtn = null;
        Button deleteBtn = null;
        foreach (var b in allButtons)
        {
            if (b.name == "BtnDownload") downloadBtn = b;
            if (b.name == "BtnDelete") deleteBtn = b;
        }

        TMP_Text statusText = null;
        foreach (var t in allTexts) if (t.name == "Status") statusText = t;

        if (downloadBtn != null)
            downloadBtn.onClick.AddListener(() => OnCardDownloadClick(model, statusText, downloadBtn, deleteBtn));

        if (deleteBtn != null)
            deleteBtn.onClick.AddListener(() => OnCardDeleteClick(model, statusText, downloadBtn, deleteBtn));

        if (deleteBtn != null) deleteBtn.interactable = model.isDownloaded;
    }

    private void UpdateCardUIState(TMP_Text statusText, Button dBtn, Button delBtn, bool isDown)
    {
        if (statusText) { statusText.text = isDown ? "已下载" : "未下载"; statusText.color = isDown ? Color.green : Color.red; }
        if (delBtn) delBtn.interactable = isDown;
    }

    private void OnCardDownloadClick(VrmModel model, TMP_Text statusText, Button dBtn, Button delBtn)
    {
        if (model.isDownloaded && ShowNativeMessageBox($"覆盖 '{model.title}'？", "确认", MB_YESNO | MB_ICONQUESTION) != IDYES) return;
        StartCoroutine(DownloadModelCoroutine(model, statusText, dBtn, delBtn));
    }

    private IEnumerator DownloadModelCoroutine(VrmModel m, TMP_Text s, Button db, Button delb)
    {
        string path = Path.Combine(saveDir, m.fileName);
        using (UnityWebRequest uwr = UnityWebRequest.Get(serverRoot + m.downloadUrl))
        {
            uwr.certificateHandler = new BypassCertificateHandler();
            uwr.downloadHandler = new DownloadHandlerFile(path);
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                m.isDownloaded = true;
                UpdateCardUIState(s, db, delb, true);
            }
            else ShowNativeMessageBox($"下载失败: {uwr.error}", "错误", MB_ICONERROR);
        }
    }

    private void OnCardDeleteClick(VrmModel m, TMP_Text s, Button db, Button delb)
    {
        if (ShowNativeMessageBox($"删除 {m.title}?", "警告", MB_YESNO | MB_ICONWARNING) == IDYES)
        {
            string fullPath = Path.Combine(saveDir, m.fileName);
            if (File.Exists(fullPath)) File.Delete(fullPath);
            m.isDownloaded = false;
            UpdateCardUIState(s, db, delb, false);
        }
    }
}