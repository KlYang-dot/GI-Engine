using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro; // 完美支持 TextMeshPro

[Serializable]
public class VrmModelData
{
    // 🔥 精准对齐 PHP 返回的大驼峰 JSON 字段名
    public string FileName;
    public long FileSizeMB;
    public string DownloadUrl;
    public string ThumbnailUrl;
    public string Title;
    public string Author;
    public string Version;
    public string LicenseType;
    public int MaterialCount;
    public int BoneCount;
    public int BlendShapeCount;

    [NonSerialized] public bool isDownloaded;
}

[Serializable]
public class VrmModelListWrapper
{
    public List<VrmModelData> models;
}

public class UnityVRMStorePanel : MonoBehaviour
{
    [Header("服务器配置")]
    public string serverRoot = "https://paimonbyte.duckdns.org:800/vrmstore/";
    private string saveDir;

    [Header("UI 元素引用 (UGUI)")]
    public Transform contentParent;       // ScrollView -> Viewport -> Content
    public GameObject modelItemPrefab;    // 列表单项的按钮预制体 (场景里的 List 物体)

    [Space]
    public TMP_Text txtDetails;           // 右侧详情文本 (TextMeshPro)
    public RawImage imgThumb;             // 右侧缩略图
    public Button btnDownload;            // 下载按钮
    public Slider pbDownload;             // 下载进度条
    public TMP_Text lblSpeed;             // 下载速度文本 (TextMeshPro)

    private List<VrmModelData> modelList = new List<VrmModelData>();
    private VrmModelData selectedModel;

    private void OnEnable()
    {
        // 游戏目录/../custommodel 完美定位
        saveDir = Path.Combine(Application.dataPath, "../custommodel");
        if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);

        if (pbDownload) pbDownload.gameObject.SetActive(false);
        if (btnDownload)
        {
            btnDownload.onClick.RemoveAllListeners();
            btnDownload.onClick.AddListener(OnDownloadClick);
        }

        // 每次打开面板时自动从 PHP 拉取最新列表
        StartCoroutine(LoadModelsRoutine());
    }

    private IEnumerator LoadModelsRoutine()
    {
        string url = serverRoot + "get_models.php";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.certificateHandler = new BypassCertificateHandler();
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[原生商店] 获取列表失败: " + webRequest.error);
                yield break;
            }

            string json = webRequest.downloadHandler.text;

            // 自动规避标准 JSON 数组无法解析的问题
            if (json.StartsWith("[")) json = "{\"models\":" + json + "}";

            VrmModelListWrapper wrapper = JsonUtility.FromJson<VrmModelListWrapper>(json);
            if (wrapper == null || wrapper.models == null)
            {
                Debug.LogError("[原生商店] JSON 数据解析失败，请检查格式！");
                yield break;
            }

            modelList = wrapper.models;

            // 检查本地文件是否已经下载，使用大驼峰 FileName 安全拼接
            foreach (var m in modelList)
            {
                m.isDownloaded = !string.IsNullOrEmpty(m.FileName) && File.Exists(Path.Combine(saveDir, m.FileName));
            }

            PopulateListView();
        }
    }

    private void PopulateListView()
    {
        // 1. 安全清空旧的克隆体
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // 2. 遍历网络列表，动态实例化 UGUI 列表项
        foreach (var model in modelList)
        {
            GameObject item = Instantiate(modelItemPrefab, contentParent);
            item.SetActive(true); // 确保它是显示状态

            // 3. 动态刷新预制体内部的 TextMeshPro 文字组件

            // 刷新标题 (Title)
            Transform titleTrans = item.transform.Find("Title");
            if (titleTrans != null)
            {
                TMP_Text itemText = titleTrans.GetComponent<TMP_Text>();
                if (itemText != null)
                {
                    string displayTitle = string.IsNullOrWhiteSpace(model.Title) ? model.FileName : model.Title;
                    itemText.text = model.isDownloaded ? $"[已下载] {displayTitle}" : displayTitle;
                }
            }

            // 刷新作者 (Author)
            Transform authorTrans = item.transform.Find("Author");
            if (authorTrans != null)
            {
                TMP_Text authorText = authorTrans.GetComponent<TMP_Text>();
                if (authorText != null && !string.IsNullOrWhiteSpace(model.Author))
                {
                    authorText.text = model.Author;
                }
            }

            // 4. 绑定列表项点击选择事件
            Button btn = item.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SelectModel(model));
            }
        }
    }

    private void SelectModel(VrmModelData model)
    {
        selectedModel = model;

        // 刷新右侧详情看版
        if (txtDetails != null)
        {
            txtDetails.text = $"模型: {model.Title}\n作者: {model.Author}\n大小: {model.FileSizeMB} MB\n网格面数: {model.BoneCount}";
        }

        // 裁剪并拼接缩略图的相对路径
        string thumbUrl = model.ThumbnailUrl;
        if (!string.IsNullOrEmpty(thumbUrl) && thumbUrl.StartsWith("./"))
            thumbUrl = thumbUrl.Substring(2);

        StartCoroutine(LoadThumbnail(serverRoot + thumbUrl));
    }

    private IEnumerator LoadThumbnail(string url)
    {
        if (string.IsNullOrEmpty(url) || imgThumb == null) yield break;

        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
        {
            webRequest.certificateHandler = new BypassCertificateHandler();
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                imgThumb.texture = DownloadHandlerTexture.GetContent(webRequest);
            }
        }
    }

    private void OnDownloadClick()
    {
        if (selectedModel == null || string.IsNullOrEmpty(selectedModel.FileName)) return;
        string savePath = Path.Combine(saveDir, selectedModel.FileName);
        StartCoroutine(DownloadModelRoutine(selectedModel, savePath));
    }

    private IEnumerator DownloadModelRoutine(VrmModelData model, string savePath)
    {
        if (btnDownload) btnDownload.interactable = false;
        if (pbDownload) pbDownload.gameObject.SetActive(true);

        // 裁剪并拼接下载相对路径
        string dlUrl = model.DownloadUrl;
        if (!string.IsNullOrEmpty(dlUrl) && dlUrl.StartsWith("./"))
            dlUrl = dlUrl.Substring(2);

        string url = serverRoot + dlUrl;
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.certificateHandler = new BypassCertificateHandler();
            webRequest.downloadHandler = new DownloadHandlerFile(savePath);

            var operation = webRequest.SendWebRequest();
            float startTime = Time.time;

            while (!operation.isDone)
            {
                float progress = webRequest.downloadProgress;
                if (pbDownload) pbDownload.value = progress;

                float elapsed = Time.time - startTime;
                if (elapsed > 0.1f)
                {
                    double downloadedMB = progress * model.FileSizeMB;
                    double speed = downloadedMB / elapsed;
                    if (lblSpeed) lblSpeed.text = $"下载速度: {speed:F2} MB/s";
                }
                yield return null;
            }

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                model.isDownloaded = true;
                if (lblSpeed) lblSpeed.text = "下载完成！";
                PopulateListView(); // 刷新列表状态
            }
            else
            {
                if (lblSpeed) lblSpeed.text = "下载失败";
            }
            if (btnDownload) btnDownload.interactable = true;
        }
    }
}

// 🔐 强行跳过私服 HTTPS 证书验证，放在类最外层，绝不报错
public class BypassCertificateHandler : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData) => true;
}