using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AvatarLibraryMenu : MonoBehaviour
{
    [Header("Default Model")]
    public GameObject defaultAvatarPrefab;
    public Texture2D defaultAvatarThumbnail;
    public string defaultAvatarDisplayName = "Nahida";
    public string defaultAvatarAuthor = "mihoyo";
    public string defaultAvatarVersion = "1.0";
    public string defaultAvatarFileType = "Built-in";

    [Header("UI References")]
    public GameObject avatarItemPrefab;
    public GameObject avatarItemPrefabDLC;
    public Transform contentParent;
    public GameObject libraryPanel;

    [Header("DLC Avatars")]
    public List<DLCEntry> dlcAvatars = new List<DLCEntry>();

    private Coroutine liveUpdateRoutine;
    [SerializeField] private float liveUpdateInterval = 3f;

    private string avatarsJsonPath => Path.Combine(Application.persistentDataPath, "avatars.json");
    private string thumbnailsFolder => Path.Combine(Application.persistentDataPath, "Thumbnails");

    private List<AvatarEntry> avatarEntries = new List<AvatarEntry>();

    // 🛠️ 用于追踪动态生成的本地缩略图纹理，防止销毁UI时产生显存残留泄露
    private List<Texture2D> generatedTexturesCache = new List<Texture2D>();

    [System.Serializable]
    public class DLCEntry
    {
        public GameObject prefab;
        public string displayName;
        public string author;
        public string version;
        public string fileType;
        public Texture2D thumbnail;
    }

    [System.Serializable]
    public class AvatarEntry
    {
        public string displayName;
        public string author;
        public string version;
        public string fileType;
        public string filePath;
        public string thumbnailPath;
        public int polygonCount;
        public bool isSteamWorkshop = false;
        public ulong steamFileId = 0;
        public bool isNSFW = false;
        public bool isOwner = false;
    }

    private void Start()
    {
        if (!Directory.Exists(thumbnailsFolder))
            Directory.CreateDirectory(thumbnailsFolder);

        LoadAvatarList();
        RefreshUI();
    }

    public void OpenLibrary()
    {
        libraryPanel.SetActive(true);

        var auto = FindFirstObjectByType<SteamWorkshopAutoLoader>();
        if (auto != null)
        {
            auto.RefreshWorkshopAvatars();
        }

        if (liveUpdateRoutine != null) StopCoroutine(liveUpdateRoutine);
        liveUpdateRoutine = StartCoroutine(LiveUpdateWhileOpen());
    }

    public void CloseLibrary()
    {
        libraryPanel.SetActive(false);
        if (liveUpdateRoutine != null) { StopCoroutine(liveUpdateRoutine); liveUpdateRoutine = null; }
    }

    private IEnumerator LiveUpdateWhileOpen()
    {
        var auto = FindFirstObjectByType<SteamWorkshopAutoLoader>();
        while (libraryPanel != null && libraryPanel.activeInHierarchy)
        {
            if (auto != null)
            {
                auto.RefreshWorkshopAvatars();
                if (auto.hadChangesLastRun)
                {
                    ReloadAvatars();
                }
            }

            yield return new WaitForSeconds(liveUpdateInterval);
        }
    }

    private void LoadAvatarList()
    {
        avatarEntries.Clear();

        if (File.Exists(avatarsJsonPath))
        {
            try
            {
                string json = File.ReadAllText(avatarsJsonPath);
                avatarEntries = JsonConvert.DeserializeObject<List<AvatarEntry>>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[AvatarLibraryMenu] Failed to load avatars.json: " + e.Message);
            }
        }

        try
        {
            string workshopDir = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "Steam Workshop"));
            bool changed = false;

            foreach (var e in avatarEntries)
            {
                if (!e.isOwner)
                {
                    string full = string.IsNullOrEmpty(e.filePath) ? "" : Path.GetFullPath(e.filePath);
                    bool isLocal = !string.IsNullOrEmpty(full) && File.Exists(full) &&
                                   !full.StartsWith(workshopDir, StringComparison.OrdinalIgnoreCase);

                    if (!e.isSteamWorkshop && e.steamFileId == 0 && isLocal)
                    {
                        e.isOwner = true;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                string newJson = JsonConvert.SerializeObject(avatarEntries, Formatting.Indented);
                File.WriteAllText(avatarsJsonPath, newJson);
            }
        }
        catch { }
    }

    private void RefreshUI()
    {
        // 🛠️ 性能保护：每次刷新列表前，彻底销毁旧的缩略图纹理，防止显存泄漏
        for (int i = 0; i < generatedTexturesCache.Count; i++)
        {
            if (generatedTexturesCache[i] != null) Destroy(generatedTexturesCache[i]);
        }
        generatedTexturesCache.Clear();

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        if (defaultAvatarPrefab != null)
        {
            GameObject item = Instantiate(avatarItemPrefabDLC != null ? avatarItemPrefabDLC : avatarItemPrefab, contentParent);
            SetupDefaultAvatarItem(item);
        }

        foreach (var dlc in dlcAvatars)
        {
            if (dlc.prefab == null) continue;
            GameObject item = Instantiate(avatarItemPrefabDLC != null ? avatarItemPrefabDLC : avatarItemPrefab, contentParent);
            SetupDLCItem(item, dlc);
        }

        foreach (var entry in avatarEntries)
        {
            GameObject item = Instantiate(avatarItemPrefab, contentParent);
            SetupAvatarItem(item, entry);
        }
    }

    private void SetupDefaultAvatarItem(GameObject item)
    {
        RawImage thumbnail = item.transform.Find("RawImage").GetComponent<RawImage>();
        TMP_Text titleText = item.transform.Find("Title").GetComponent<TMP_Text>();
        TMP_Text authorText = item.transform.Find("Author").GetComponent<TMP_Text>();
        TMP_Text versionText = item.transform.Find("Version").GetComponent<TMP_Text>();
        TMP_Text fileTypeText = item.transform.Find("File Type").GetComponent<TMP_Text>();
        TMP_Text polygonText = item.transform.Find("Polygons")?.GetComponent<TMP_Text>();
        Button loadButton = item.transform.Find("Button").GetComponent<Button>();

        if (thumbnail != null && defaultAvatarThumbnail != null)
            thumbnail.texture = defaultAvatarThumbnail;
        if (titleText != null) titleText.text = "Name: " + defaultAvatarDisplayName;
        if (authorText != null) authorText.text = "Author: " + defaultAvatarAuthor;
        if (versionText != null) versionText.text = "Version: " + defaultAvatarVersion;
        if (fileTypeText != null) fileTypeText.text = "Format: " + defaultAvatarFileType;
        if (polygonText != null && defaultAvatarPrefab != null)
        {
            int polyCount = 0;
            foreach (var mesh in defaultAvatarPrefab.GetComponentsInChildren<MeshFilter>(true))
                if (mesh.sharedMesh != null) polyCount += mesh.sharedMesh.triangles.Length / 3;
            foreach (var smr in defaultAvatarPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (smr.sharedMesh != null) polyCount += smr.sharedMesh.triangles.Length / 3;
            polygonText.text = "Polygons: " + polyCount;
        }
        loadButton.onClick.RemoveAllListeners();
        loadButton.onClick.AddListener(() => {
            var loader = FindFirstObjectByType<VRMLoader>();
            if (loader != null) loader.ActivateDefaultModel();
        });
    }

    private void SetupDLCItem(GameObject item, DLCEntry dlc)
    {
        RawImage thumbnail = item.transform.Find("RawImage").GetComponent<RawImage>();
        TMP_Text titleText = item.transform.Find("Title").GetComponent<TMP_Text>();
        TMP_Text authorText = item.transform.Find("Author").GetComponent<TMP_Text>();
        TMP_Text versionText = item.transform.Find("Version").GetComponent<TMP_Text>();
        TMP_Text fileTypeText = item.transform.Find("File Type").GetComponent<TMP_Text>();
        TMP_Text polygonText = item.transform.Find("Polygons")?.GetComponent<TMP_Text>();
        Button loadButton = item.transform.Find("Button").GetComponent<Button>();
        Slider uploadSlider = item.transform.Find("UploadBar")?.GetComponent<Slider>();

        if (titleText != null) titleText.text = "Name: " + (!string.IsNullOrEmpty(dlc.displayName) ? dlc.displayName : dlc.prefab.name);
        if (authorText != null) authorText.text = "Author: " + dlc.author;
        if (versionText != null) versionText.text = "Version: " + dlc.version;
        if (fileTypeText != null) fileTypeText.text = "Format: " + dlc.fileType;

        int polyCount = 0;
        foreach (var mesh in dlc.prefab.GetComponentsInChildren<MeshFilter>(true))
            if (mesh.sharedMesh != null) polyCount += mesh.sharedMesh.triangles.Length / 3;
        foreach (var smr in dlc.prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (smr.sharedMesh != null) polyCount += smr.sharedMesh.triangles.Length / 3;
        if (polygonText != null) polygonText.text = "Polygons: " + polyCount;

        if (thumbnail != null && dlc.thumbnail != null) thumbnail.texture = dlc.thumbnail;

        loadButton.onClick.RemoveAllListeners();
        loadButton.onClick.AddListener(() => LoadAvatar(dlc.prefab.name));
        if (uploadSlider != null) uploadSlider.gameObject.SetActive(false);
    }

    private void SetupAvatarItem(GameObject item, AvatarEntry entry)
    {
        RawImage thumbnail = item.transform.Find("RawImage").GetComponent<RawImage>();
        TMP_Text titleText = item.transform.Find("Title").GetComponent<TMP_Text>();
        TMP_Text authorText = item.transform.Find("Author").GetComponent<TMP_Text>();
        TMP_Text versionText = item.transform.Find("Version").GetComponent<TMP_Text>();
        TMP_Text fileTypeText = item.transform.Find("File Type").GetComponent<TMP_Text>();
        TMP_Text polygonText = item.transform.Find("Polygons")?.GetComponent<TMP_Text>();
        Button loadButton = item.transform.Find("Button").GetComponent<Button>();
        Button removeButton = item.transform.Find("Remove").GetComponent<Button>();
        Button uploadButton = item.transform.Find("Upload")?.GetComponent<Button>();
        Slider uploadSlider = item.transform.Find("UploadBar")?.GetComponent<Slider>();
        Toggle nsfwToggle = item.transform.Find("NSFW")?.GetComponent<Toggle>();

        if (nsfwToggle != null)
        {
            nsfwToggle.isOn = entry.isNSFW;
            nsfwToggle.onValueChanged.RemoveAllListeners();
            nsfwToggle.onValueChanged.AddListener((val) =>
            {
                entry.isNSFW = val;
                var match = avatarEntries.FirstOrDefault(e => e.filePath == entry.filePath);
                if (match != null) { match.isNSFW = val; SaveAvatars(); }
            });
        }

        if (titleText != null) titleText.text = "Name: " + entry.displayName;
        if (authorText != null) authorText.text = "Author: " + entry.author;
        if (versionText != null) versionText.text = "Version: " + entry.version;
        if (fileTypeText != null) fileTypeText.text = "Format: " + entry.fileType;
        if (polygonText != null) polygonText.text = "Polygons: " + entry.polygonCount;

        if (thumbnail != null && File.Exists(entry.thumbnailPath))
        {
            byte[] imageBytes = File.ReadAllBytes(entry.thumbnailPath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imageBytes);
            thumbnail.texture = tex;
            // 记录该纹理以便后续安全销毁
            generatedTexturesCache.Add(tex);
        }

        loadButton.onClick.RemoveAllListeners();
        loadButton.onClick.AddListener(() => LoadAvatar(entry.filePath));

        var holdHandler = removeButton.GetComponent<DeleteButtonHoldHandler>();
        if (holdHandler == null) holdHandler = removeButton.gameObject.AddComponent<DeleteButtonHoldHandler>();
        holdHandler.entry = entry;
        holdHandler.labelText = removeButton.GetComponentInChildren<TMP_Text>();
        holdHandler.audioSource = item.GetComponentInChildren<AudioSource>();

        if (uploadButton != null) uploadButton.gameObject.SetActive(entry.isOwner);
        if (uploadSlider != null) uploadSlider.gameObject.SetActive(false);

        if (uploadButton != null && uploadButton.gameObject.activeSelf)
        {
            uploadButton.onClick.RemoveAllListeners();
            uploadButton.onClick.AddListener(() =>
            {
                if (nsfwToggle != null) entry.isNSFW = nsfwToggle.isOn;
                var match = avatarEntries.FirstOrDefault(e => e.filePath == entry.filePath);
                if (match != null)
                {
                    match.isNSFW = entry.isNSFW;
                    SaveAvatars();
                    SteamWorkshopHandler.Instance.UploadToWorkshop(match, uploadSlider);
                }
            });

            var handler = uploadButton.GetComponent<UploadButtonHoldHandler>();
            if (handler != null)
            {
                var match = avatarEntries.FirstOrDefault(e => e.filePath == entry.filePath);
                if (match != null) handler.entry = match;
                handler.progressSlider = uploadSlider;
                handler.labelText = uploadButton.GetComponentInChildren<TMP_Text>();
            }
        }
    }

    private void LoadAvatar(string path)
    {
        var loader = FindFirstObjectByType<VRMLoader>();
        if (loader != null) loader.LoadVRM(path);
        else Debug.LogError("[AvatarLibraryMenu] VRMLoader not found in scene!");
    }

    // ==================== 🛠️ 核心入库功能 ====================

    /// <summary>
    /// [重载方法] 直接传入加载好的 GameObject，自动利用 VRMMetaReader 提取并入库
    /// </summary>
    /// <param name="loadedModel">已生成的VRM模型根节点GameObject</param>
    /// <param name="filePath">模型的本地绝对路径</param>
    public static void AddAvatarToLibrary(GameObject loadedModel, string filePath)
    {
        if (loadedModel == null)
        {
            Debug.LogError("[AvatarLibraryMenu] 无法提取元数据：传入的 GameObject 为空。");
            return;
        }

        // 调用刚才写好的统一提取器
        VRMMetaReader.ModelMetaResult metaData = VRMMetaReader.ReadMeta(loadedModel, filePath);

        if (metaData == null)
        {
            Debug.LogError($"[AvatarLibraryMenu] 解析模型元数据失败: {filePath}");
            return;
        }

        // 将提取出来的安全数据，直接喂给底层的静态入库方法
        AddAvatarToLibrary(
            metaData.displayName,
            metaData.author,
            metaData.version,
            metaData.fileType,
            filePath,
            metaData.thumbnail,
            metaData.polygonCount
        );

        // 内存回收：由于 thumbnail 已经落地成了硬盘图片，
        // 此时生成的临时纹理应该立刻从内存中卸载掉，防止显存泄漏。
        if (metaData.thumbnail != null)
        {
            Destroy(metaData.thumbnail);
        }
    }

    /// <summary>
    /// 底层数据持久化入库方法 (包含保存本地缩略图和更新 json)
    /// </summary>
    public static void AddAvatarToLibrary(string displayName, string author, string version, string fileType, string filePath, Texture2D thumbnail, int polygonCount)
    {
        string avatarsJsonPath = Path.Combine(Application.persistentDataPath, "avatars.json");
        string thumbnailsFolder = Path.Combine(Application.persistentDataPath, "Thumbnails");

        if (!Directory.Exists(thumbnailsFolder)) Directory.CreateDirectory(thumbnailsFolder);

        List<AvatarEntry> entries = new List<AvatarEntry>();
        if (File.Exists(avatarsJsonPath))
        {
            try
            {
                string json = File.ReadAllText(avatarsJsonPath);
                entries = JsonConvert.DeserializeObject<List<AvatarEntry>>(json);
            }
            catch { }
        }

        if (entries.Exists(e => e.filePath == filePath))
        {
            Debug.Log($"[AvatarLibraryMenu] VRM already exists in library, skipping: {displayName}");
            return;
        }

        string thumbnailFileName = Path.GetFileNameWithoutExtension(filePath) + "_thumb.png";
        string thumbnailPath = Path.Combine(thumbnailsFolder, thumbnailFileName);

        if (thumbnail != null)
        {
            File.WriteAllBytes(thumbnailPath, thumbnail.EncodeToPNG());
        }

        AvatarEntry newEntry = new AvatarEntry
        {
            displayName = displayName,
            author = author,
            version = version,
            fileType = fileType,
            filePath = filePath,
            thumbnailPath = thumbnailPath,
            polygonCount = polygonCount,
            isNSFW = false,
            isOwner = true
        };

        entries.Add(newEntry);
        string newJson = JsonConvert.SerializeObject(entries, Formatting.Indented);
        File.WriteAllText(avatarsJsonPath, newJson);
    }

    public void ReloadAvatars()
    {
        LoadAvatarList();
        RefreshUI();
    }

    // 已修复权限为 public
    public void RemoveAvatar(AvatarEntry entryToRemove)
    {
        string avatarsJsonPath = Path.Combine(Application.persistentDataPath, "avatars.json");
        if (!File.Exists(avatarsJsonPath)) return;

        List<AvatarEntry> entries = new List<AvatarEntry>();
        try
        {
            string json = File.ReadAllText(avatarsJsonPath);
            entries = JsonConvert.DeserializeObject<List<AvatarEntry>>(json);
        }
        catch { }

        entries = entries.Where(e => e.filePath != entryToRemove.filePath).ToList();

        if (entryToRemove.isSteamWorkshop && File.Exists(entryToRemove.filePath))
        {
            try
            {
                File.Delete(entryToRemove.filePath);
                Debug.Log("[AvatarLibraryMenu] Deleted Workshop model file: " + entryToRemove.filePath);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[AvatarLibraryMenu] Could not delete Workshop model file: " + e.Message);
            }
        }

        if (File.Exists(entryToRemove.thumbnailPath))
        {
            try { File.Delete(entryToRemove.thumbnailPath); } catch { }
        }

        string newJson = JsonConvert.SerializeObject(entries, Formatting.Indented);
        File.WriteAllText(avatarsJsonPath, newJson);
        ReloadAvatars();
    }

    private void SaveAvatars()
    {
        string avatarsJsonPath = Path.Combine(Application.persistentDataPath, "avatars.json");
        string newJson = JsonConvert.SerializeObject(avatarEntries, Formatting.Indented);
        File.WriteAllText(avatarsJsonPath, newJson);
    }
}