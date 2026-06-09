using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices; // 用于引入 Win32 API
using WinForms = System.Windows.Forms;   // 别名避免与 Unity 类型冲突
using UnityEngine;
using UnityEngine.UI;
using VRM;
using UniGLTF;
using UniVRM10;
using Newtonsoft.Json;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class VRMLoader : MonoBehaviour
{
    // === 引入 Win32 API 用以获取 Unity 窗口句柄，解决对话框二次打开挂起锁死的问题 ===
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    // 实现 IWin32Window 接口用于包裹句柄
    public class UnityWindowWrapper : WinForms.IWin32Window
    {
        public IntPtr Handle { get; }
        public UnityWindowWrapper(IntPtr handle) { Handle = handle; }
    }

    // 全局静态事件
    public static event Action<Animator> OnAvatarReady;

    [System.Serializable]
    public class MToon10ParameterOverride
    {
        [Header("颜色")]
        public bool overrideColor = false;
        public Color color = Color.white;

        [Header("阴影色")]
        public bool overrideShadeColor = false;
        public Color shadeColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        [Header("轮廓线 (范围已放宽)")]
        public bool overrideOutlineWidth = false;
        [Range(0, 5.0f)] public float outlineWidth = 0.02f;
        public bool overrideOutlineColor = false;
        public Color outlineColor = Color.black;

        [Header("光照/阴影混合")]
        public bool overrideShadingShift = false;
        [Range(-1, 1)] public float shadingShift = 0.0f;
        public bool overrideShadingToony = false;
        [Range(0, 1)] public float shadingToony = 0.9f;

        [Header("自发光")]
        public bool overrideEmission = false;
        public Color emissionColor = Color.black;

        [Header("边缘光")]
        public bool overrideRimWidth = false;
        [Range(0, 1)] public float rimWidth = 0.5f;
        public bool overrideRimStrength = false;
        [Range(0, 1)] public float rimStrength = 0.5f;
        public bool overrideRimColor = false;
        public Color rimColor = Color.white;
    }

    [Header("配置与预设")]
    public MToon10ParameterOverride mtoon10Preset;
    public Button loadVRMButton;

    [Header("模型挂载点与引用")]
    public GameObject mainModel;
    public GameObject customModelOutput;
    public RuntimeAnimatorController animatorController;
    public GameObject componentTemplatePrefab;
    public GameObject customDefaultPrefab;

    [Header("状态监控")]
    public string defaultmodel = "";
    private GameObject currentModel;
    private bool isLoading = false;
    private const string LegacyModelPathKey = "SavedPathModel";
    private RuntimeGltfInstance currentGltf;
    private AssetBundle currentBundle;
    private bool mtoonApplied = false;
    private HashSet<string> loggedShaders = new HashSet<string>();

    void Start()
    {
        if (customDefaultPrefab == null)
        {
            Debug.LogError("[VRMLoader] ❌ customDefaultPrefab 未分配！请在 Inspector 中设置");
            return;
        }

        string savedPath = SaveLoadHandler.Instance != null
            ? SaveLoadHandler.Instance.data.selectedModelPath
            : null;

        if (string.IsNullOrEmpty(savedPath) && PlayerPrefs.HasKey(LegacyModelPathKey))
        {
            savedPath = PlayerPrefs.GetString(LegacyModelPathKey);
            if (SaveLoadHandler.Instance != null)
            {
                SaveLoadHandler.Instance.data.selectedModelPath = savedPath;
                SaveLoadHandler.Instance.SaveToDisk();
            }
            PlayerPrefs.DeleteKey(LegacyModelPathKey);
            PlayerPrefs.Save();
        }

        if (SaveLoadHandler.Instance != null && SaveLoadHandler.Instance.data.enableRandomAvatar)
        {
            TryLoadRandomAvatar();
            return;
        }

        if (!string.IsNullOrEmpty(savedPath))
        {
            LoadVRM(savedPath);
        }
        else
        {
            GameObject model = Instantiate(customDefaultPrefab);
            FinalizeLoadedModel(model, customDefaultPrefab.name);
        }
    }

    void LateUpdate()
    {
        if (mtoonApplied)
            return;

        if (currentModel == null || mtoon10Preset == null)
            return;

        var allRenderers = currentModel.GetComponentsInChildren<Renderer>(true);

        foreach (var renderer in allRenderers)
        {
            if (renderer == null)
                continue;

            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat != null)
                    ApplyMToonXPipeline(mat);
            }
        }

        mtoonApplied = true;
    }

    private void TryLoadRandomAvatar()
    {
        var options = new List<string>();
        if (mainModel != null) options.Add("__DEFAULT__");

        var lib = FindFirstObjectByType<AvatarLibraryMenu>();
        if (lib != null && lib.dlcAvatars != null)
        {
            for (int i = 0; i < lib.dlcAvatars.Count; i++)
            {
                var p = lib.dlcAvatars[i]?.prefab;
                if (p != null) options.Add(p.name);
            }
        }

        try
        {
            string avatarsPath = Path.Combine(Application.persistentDataPath, "avatars.json");
            if (File.Exists(avatarsPath))
            {
                var entries = JsonConvert.DeserializeObject<List<AvatarLibraryMenu.AvatarEntry>>(File.ReadAllText(avatarsPath));
                if (entries != null)
                {
                    for (int i = 0; i < entries.Count; i++)
                    {
                        var fp = entries[i].filePath;
                        if (!string.IsNullOrEmpty(fp)) options.Add(fp);
                    }
                }
            }
        }
        catch { }

        if (options.Count == 0)
        {
            ActivateDefaultModel();
            return;
        }

        int idx = UnityEngine.Random.Range(0, options.Count);
        string pick = options[idx];
        if (pick == "__DEFAULT__") ActivateDefaultModel();
        else LoadVRM(pick);
    }

    // ===================== 最终修复：使用具有主从绑定(Owner)的 Windows Forms 对话框（STA线程） =====================
    public void OpenFileDialogAndLoadVRM()
    {
        if (isLoading)
        {
            Debug.LogWarning("[VRMLoader] ⚠️ 上一次加载尚未结束或对话框已打开，请勿重复点击！");
            return;
        }
        isLoading = true;

        // 在主线程提前锁定 Unity 当前的活动窗口句柄
        IntPtr unityWindowHandle = GetActiveWindow();

        Thread staThread = new Thread(() =>
        {
            try
            {
                using (var dialog = new WinForms.OpenFileDialog())
                {
                    dialog.Title = "Select Model File";
                    dialog.Filter = "Model Files|*.vrm;*.me;*.prefab|All Files|*.*";
                    dialog.FilterIndex = 1;
                    dialog.RestoreDirectory = true;

                    // 🛠️ 关键修复：通过 Owner 强行让文件对话框贴在 Unity 窗口身前。防止失焦挂起、不走 finally 的死锁问题。
                    var owner = new UnityWindowWrapper(unityWindowHandle);
                    WinForms.DialogResult result = dialog.ShowDialog(owner);

                    if (result == WinForms.DialogResult.OK)
                    {
                        string path = dialog.FileName;
                        Debug.Log($"[VRMLoader] 📁 已成功选中文件，准备投递主线程加载: {path}");
                        MainThreadDispatcher.Enqueue(() => LoadVRM(path));
                    }
                    else
                    {
                        Debug.Log("[VRMLoader] 👥 用户取消了文件选择对话框。");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VRMLoader] ❌ 文件对话框发生严重线程异常: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                // 🛠️ 稳健修复：确保无论用户选没选、崩没崩，都在主线程解锁 isLoading 状态，保证第二次一定能打开
                MainThreadDispatcher.Enqueue(() => isLoading = false);
            }
        });

        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
    }
    // =====================================================================

    public async void LoadVRM(string path)
    {
        Debug.Log($"[VRMLoader] 🚀 开始载入模型流程，目标路径: {path}");

        if (path.EndsWith(".me", StringComparison.OrdinalIgnoreCase))
        {
            LoadAssetBundleModel(path);
            if (SaveLoadHandler.Instance != null)
            {
                SaveLoadHandler.Instance.data.selectedModelPath = path;
                SaveLoadHandler.Instance.SaveToDisk();
            }
            return;
        }

        if (IsDLCReference(path))
        {
            GameObject prefab = FindDLCByName(path);
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab);
                FinalizeLoadedModel(instance, path);
                if (SaveLoadHandler.Instance != null)
                {
                    SaveLoadHandler.Instance.data.selectedModelPath = path;
                    SaveLoadHandler.Instance.SaveToDisk();
                }
            }
            return;
        }

        if (!File.Exists(path))
        {
            Debug.LogError($"[VRMLoader] ❌ 加载失败：本地磁盘未找到对应的文件。路径: {path}");
            return;
        }

        try
        {
            byte[] fileData = await Task.Run(() => File.ReadAllBytes(path));
            if (fileData == null || fileData.Length == 0)
            {
                Debug.LogError($"[VRMLoader] ❌ 读取文件失败，字节码为空或受损。路径: {path}");
                return;
            }

            GameObject loadedModel = null;

            // 优先尝试以 VRM 1.0 格式进行解析
            try
            {
                var glbData = new GlbFileParser(path).Parse();
                var vrm10Data = Vrm10Data.Parse(glbData);
                if (vrm10Data != null)
                {
                    using var importer10 = new Vrm10Importer(vrm10Data);
                    var instance10 = await importer10.LoadAsync(new ImmediateCaller());
                    if (instance10.Root != null)
                    {
                        loadedModel = instance10.Root;
                        currentGltf = instance10;
                        loadedModel.AddComponent<GltfInstanceDisposer>().Bind(instance10);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VRMLoader] 诊断提示：该模型非原生 VRM 1.0 (已自动准备降级兼容解析)。解析提示: {ex.Message}");
            }

            // 1.0 解析失败则降级到 VRM 0.x 兼容格式进行解析
            if (loadedModel == null)
            {
                try
                {
                    using var gltfData = new GlbBinaryParser(fileData, path).Parse();
                    VRMImporterContext importer = null;
                    try
                    {
                        importer = new VRMImporterContext(new VRMData(gltfData));
                        var instance = await importer.LoadAsync(new ImmediateCaller());
                        if (instance.Root != null)
                        {
                            loadedModel = instance.Root;
                            currentGltf = instance;
                            loadedModel.AddComponent<GltfInstanceDisposer>().Bind(instance);
                        }
                    }
                    finally
                    {
                        importer?.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[VRMLoader] ❌ 降级解析 VRM 0.x 同样遭遇崩溃: {ex.Message}");
                }
            }

            // 🛠️ 关键修复：当 1.0 和 0.x 都无法解析模型时，不再静默死掉，给出精准红字报警！
            if (loadedModel == null)
            {
                Debug.LogError($"[VRMLoader] ❌ 模型文件损坏或不属于可识别的标准 VRM 0.x/1.x 格式。路径: {path}");
                return;
            }

            FinalizeLoadedModel(loadedModel, path);
            if (SaveLoadHandler.Instance != null)
            {
                SaveLoadHandler.Instance.data.selectedModelPath = path;
                SaveLoadHandler.Instance.SaveToDisk();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[VRMLoader] ❌ 反序列化/加载流程中遭遇致命错误: " + ex.Message);
        }
    }

    private void LoadAssetBundleModel(string path)
    {
        var bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null) return;

        var prefab = bundle.LoadAllAssets<GameObject>().FirstOrDefault();
        if (prefab == null)
        {
            bundle.Unload(true);
            return;
        }

        var instance = Instantiate(prefab);
        FinalizeLoadedModel(instance, path, bundle);
    }

    private void FinalizeLoadedModel(GameObject loadedModel, string path, AssetBundle bundle = null)
    {
        DisableMainModel();
        ClearPreviousCustomModel();

        currentBundle = bundle;

        loadedModel.transform.SetParent(customModelOutput.transform, false);
        loadedModel.transform.localPosition = Vector3.zero;
        loadedModel.transform.localRotation = Quaternion.identity;
        loadedModel.transform.localScale = Vector3.one;
        currentModel = loadedModel;

        loggedShaders.Clear();

        EnableSkinnedMeshRenderers(currentModel);
        AssignAnimatorController(currentModel);

        StartCoroutine(DelayedInjectComponents(componentTemplatePrefab, currentModel));

        var changer = FindFirstObjectByType<MEValueChanger>();
        if (changer != null) changer.SendMessage("TryAttachCustomVRM", SendMessageOptions.DontRequireReceiver);

        string displayName = Path.GetFileNameWithoutExtension(path);
        string author = "Unknown";
        string version = "Unknown";
        string fileType = "Unknown";
        Texture2D thumbnail = null;
        bool isME = path.EndsWith(".me", StringComparison.OrdinalIgnoreCase);

        var vrm10Instance = loadedModel.GetComponent<UniVRM10.Vrm10Instance>();
        if (vrm10Instance != null && vrm10Instance.Vrm != null && vrm10Instance.Vrm.Meta != null)
        {
            displayName = vrm10Instance.Vrm.Meta.Name ?? displayName;
            author = (vrm10Instance.Vrm.Meta.Authors != null && vrm10Instance.Vrm.Meta.Authors.Count > 0) ? vrm10Instance.Vrm.Meta.Authors[0] : "Unknown";
            version = vrm10Instance.Vrm.Meta.Version ?? "Unknown";
            fileType = isME ? ".ME (VRM1.X)" : "VRM1.X";
            thumbnail = vrm10Instance.Vrm.Meta.Thumbnail;
        }
        else
        {
            var vrmMeta = loadedModel.GetComponent<VRM.VRMMeta>();
            if (vrmMeta != null && vrmMeta.Meta != null)
            {
                var meta = vrmMeta.Meta;
                displayName = !string.IsNullOrEmpty(meta.Title) ? meta.Title : displayName;
                author = !string.IsNullOrEmpty(meta.Author) ? meta.Author : "Unknown";
                version = !string.IsNullOrEmpty(meta.Version) ? meta.Version : "Unknown";
                fileType = isME ? ".ME (VRM0.X)" : "VRM0.X";
                thumbnail = meta.Thumbnail;
            }
        }

        Texture2D safeThumbnail = MakeReadableCopy(thumbnail);
        int polyCount = GetTotalPolygons(loadedModel);

        if (!IsDLCReference(path))
            AvatarLibraryMenu.AddAvatarToLibrary(displayName, author, version, fileType, path, safeThumbnail, polyCount);

        if (safeThumbnail != null) Destroy(safeThumbnail);

        var libraryMenu = FindFirstObjectByType<AvatarLibraryMenu>();
        if (libraryMenu != null) libraryMenu.ReloadAvatars();

        StartCoroutine(DelayedRefreshStats());

        if (MEModLoader.Instance != null)
            MEModLoader.Instance.AssignHandlersForCurrentAvatar(loadedModel);

        StartCoroutine(ReleaseRamAndUnloadAssetsCo());
        SettingsHandlerUtility.ReloadAllSettingsHandlers();
    }

    private IEnumerator DelayedInjectComponents(GameObject prefabTemplate, GameObject targetModel)
    {
        var animator = targetModel.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        yield return null;
        yield return null;

        InjectComponentsFromPrefab(prefabTemplate, targetModel);

        var allComps = targetModel.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var comp in allComps)
        {
            if (!comp.enabled && !(comp is Animator) && !(comp is SkinnedMeshRenderer))
            {
                comp.enabled = true;
            }
        }

        if (animator != null && animator.avatar != null)
        {
            OnAvatarReady?.Invoke(animator);
        }
    }

    private void ApplyMToonXPipeline(Material material)
    {
        if (material == null || mtoon10Preset == null) return;

        string shaderName = material.shader.name;

        if (!loggedShaders.Contains(shaderName))
        {
            loggedShaders.Add(shaderName);
            Debug.Log($"<color=cyan>[VRM渲染诊断]</color> 材质 {material.name} 当前正在执行的 Shader 是: <b>{shaderName}</b>");
        }

        if (shaderName.Contains("Standard") || shaderName.Contains("Unlit") || shaderName.Contains("MToon10") || shaderName.Contains("Error"))
        {
            Shader legacyMToon = Shader.Find("VRM/MToon");
            if (legacyMToon != null && shaderName != "VRM/MToon")
            {
                Texture baseMap = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : (material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null);
                Texture shadeMap = material.HasProperty("_ShadeMap") ? material.GetTexture("_ShadeMap") : null;
                Color baseColor = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : (material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white);

                material.shader = legacyMToon;

                if (baseMap != null) material.SetTexture("_MainTex", baseMap);
                if (shadeMap != null) material.SetTexture("_ShadeTexture", shadeMap);
                material.SetColor("_Color", baseColor);

                shaderName = "VRM/MToon";
            }
        }

        bool isLegacyMToon = shaderName == "VRM/MToon";

        if (mtoon10Preset.overrideColor)
        {
            if (material.HasProperty("_Color")) material.SetColor("_Color", mtoon10Preset.color);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", mtoon10Preset.color);
        }

        if (mtoon10Preset.overrideShadeColor && material.HasProperty("_ShadeColor"))
            material.SetColor("_ShadeColor", mtoon10Preset.shadeColor);

        if (mtoon10Preset.overrideOutlineWidth)
        {
            if (material.HasProperty("_OutlineWidth"))
                material.SetFloat("_OutlineWidth", mtoon10Preset.outlineWidth);

            if (isLegacyMToon)
            {
                if (material.HasProperty("_OutlineWidthMode")) material.SetInt("_OutlineWidthMode", 1);
                if (material.HasProperty("_OutlineColorMode")) material.SetInt("_OutlineColorMode", 0);
                material.EnableKeyword("_MTOON_OUTLINE_VERTEXNORMAL");
            }
            else
            {
                if (material.HasProperty("_OutlineMode"))
                {
                    material.SetInt("_OutlineMode", 1);
                    material.EnableKeyword("_OUTLINE_WIDTH_WORLD");
                    material.DisableKeyword("_OUTLINE_NONE");
                    if (material.HasProperty("_OutlineCullMode")) material.SetInt("_OutlineCullMode", 1);
                }
            }

            material.SetShaderPassEnabled("Outline", mtoon10Preset.outlineWidth > 0);
            material.SetShaderPassEnabled("VRM10/MToon10/Outline", mtoon10Preset.outlineWidth > 0);
        }

        if (mtoon10Preset.overrideOutlineColor && material.HasProperty("_OutlineColor"))
            material.SetColor("_OutlineColor", mtoon10Preset.outlineColor);

        if (mtoon10Preset.overrideShadingShift)
        {
            if (material.HasProperty("_ShadingShift")) material.SetFloat("_ShadingShift", mtoon10Preset.shadingShift);
            if (material.HasProperty("_ShadingShiftFactor")) material.SetFloat("_ShadingShiftFactor", mtoon10Preset.shadingShift);
        }
        if (mtoon10Preset.overrideShadingToony)
        {
            if (material.HasProperty("_ShadingToony")) material.SetFloat("_ShadingToony", mtoon10Preset.shadingToony);
            if (material.HasProperty("_ShadingToonyFactor")) material.SetFloat("_ShadingToonyFactor", mtoon10Preset.shadingToony);
        }

        if (mtoon10Preset.overrideEmission)
        {
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", mtoon10Preset.emissionColor);
            if (material.HasProperty("_EmissiveFactor")) material.SetColor("_EmissiveFactor", mtoon10Preset.emissionColor);
        }

        if (mtoon10Preset.overrideRimWidth && material.HasProperty("_RimWidth")) material.SetFloat("_RimWidth", mtoon10Preset.rimWidth);
        if (mtoon10Preset.overrideRimStrength && material.HasProperty("_RimLightingMixFactor")) material.SetFloat("_RimLightingMixFactor", mtoon10Preset.rimStrength);
        if (mtoon10Preset.overrideRimColor && material.HasProperty("_RimColor")) material.SetColor("_RimColor", mtoon10Preset.rimColor);
    }

    public Texture2D MakeReadableCopy(Texture texture)
    {
        if (texture == null) return null;
        RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0);
        Graphics.Blit(texture, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readable.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return readable;
    }

    public void ResetModel()
    {
        string vrmFolder = Path.Combine(Application.persistentDataPath, "VRM");
        if (Directory.Exists(vrmFolder))
            Directory.Delete(vrmFolder, true);

        ClearPreviousCustomModel(skipRawImageCleanup: true);
        GameObject model = Instantiate(customDefaultPrefab);
        FinalizeLoadedModel(model, customDefaultPrefab.name);

        if (SaveLoadHandler.Instance != null)
        {
            SaveLoadHandler.Instance.data.selectedModelPath = "";
            SaveLoadHandler.Instance.SaveToDisk();
        }
        StartCoroutine(ReleaseRamAndUnloadAssetsCo());
        SettingsHandlerUtility.ReloadAllSettingsHandlers();
    }

    private void DisableMainModel()
    {
        if (mainModel != null) mainModel.SetActive(false);
    }

    private void EnableMainModel()
    {
        if (mainModel != null) mainModel.SetActive(true);
    }

    private void ClearPreviousCustomModel(bool skipRawImageCleanup = false)
    {
        if (customModelOutput != null)
        {
            foreach (Transform child in customModelOutput.transform)
            {
                if (child.gameObject == mainModel) continue;
                CleanupRawImages(child.gameObject);
                Destroy(child.gameObject);
            }
        }

        if (currentBundle != null)
        {
            currentBundle.Unload(true);
            currentBundle = null;
        }

        currentGltf = null;

        if (!skipRawImageCleanup)
            CleanupAllRawImagesInScene();
    }

    private void EnableSkinnedMeshRenderers(GameObject model)
    {
        foreach (var skinnedMesh in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            skinnedMesh.enabled = true;
    }

    private void AssignAnimatorController(GameObject model)
    {
        var animator = model.GetComponentInChildren<Animator>();
        if (animator != null && animatorController != null)
            animator.runtimeAnimatorController = animatorController;
    }

    private void InjectComponentsFromPrefab(GameObject prefabTemplate, GameObject targetModel)
    {
        if (prefabTemplate == null || targetModel == null) return;

        var templateObj = Instantiate(prefabTemplate);
        var animator = targetModel.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        foreach (var templateComp in templateObj.GetComponents<MonoBehaviour>())
        {
            var type = templateComp.GetType();
            if (targetModel.GetComponent(type) != null) continue;
            var newComp = targetModel.AddComponent(type);
            CopyComponentValues(templateComp, newComp);

            if (animator != null && animator.avatar != null)
            {
                var setAnimMethod = type.GetMethod("SetAnimator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (setAnimMethod != null)
                {
                    try
                    {
                        setAnimMethod.Invoke(newComp, new object[] { animator });
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[VRMLoader] 调用 SetAnimator 失败: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }

                var animatorField = type.GetField("animator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (animatorField != null && animatorField.FieldType == typeof(Animator))
                    animatorField.SetValue(newComp, animator);
            }
            else if (newComp is MonoBehaviour mb)
            {
                mb.enabled = false;
                Debug.LogWarning($"[VRMLoader] 组件 {type.Name} 被禁用，因为 Animator 或 Avatar 未就绪");
            }
        }
        Destroy(templateObj);
    }

    private void CopyComponentValues(Component source, Component destination)
    {
        var type = source.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.IsDefined(typeof(SerializeField), true) || field.IsPublic)
                field.SetValue(destination, field.GetValue(source));
        }
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(p => p.CanWrite && p.GetSetMethod(true) != null);
        foreach (var prop in props)
        {
            try { prop.SetValue(destination, prop.GetValue(source)); }
            catch { }
        }
    }

    private IEnumerator DelayedRefreshStats()
    {
        yield return null;
        var stats = FindFirstObjectByType<RuntimeModelStats>();
        if (stats != null) stats.RefreshNow();
    }

    public int GetTotalPolygons(GameObject model)
    {
        int total = 0;
        foreach (var meshFilter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh = meshFilter.sharedMesh;
            if (mesh != null) total += mesh.triangles.Length / 3;
        }
        foreach (var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh = skinned.sharedMesh;
            if (mesh != null) total += mesh.triangles.Length / 3;
        }
        return total;
    }

    public void ActivateDefaultModel()
    {
        ClearPreviousCustomModel(skipRawImageCleanup: true);
        GameObject model = Instantiate(customDefaultPrefab);
        FinalizeLoadedModel(model, customDefaultPrefab.name);
        if (SaveLoadHandler.Instance != null)
        {
            SaveLoadHandler.Instance.data.selectedModelPath = "";
            SaveLoadHandler.Instance.SaveToDisk();
        }

        StartCoroutine(ReleaseRamAndUnloadAssetsCo());
        SettingsHandlerUtility.ReloadAllSettingsHandlers();
    }

    private IEnumerator ReleaseRamAndUnloadAssetsCo()
    {
        yield return Resources.UnloadUnusedAssets();
        yield return null;
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
    }

    private void CleanupRawImages(GameObject obj)
    {
        if (obj == null) return;
        var rawImages = obj.GetComponentsInChildren<RawImage>(true);
        foreach (var rawImage in rawImages) rawImage.texture = null;
    }

    private void CleanupAllRawImagesInScene()
    {
        var rawImages = GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var rawImage in rawImages) rawImage.texture = null;
    }

    private bool IsDLCReference(string path)
    {
#if UNITY_EDITOR
        if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) return true;
#endif
        if (!File.Exists(path) && !path.EndsWith(".vrm") && !path.EndsWith(".me")) return true;
        return false;
    }

    private GameObject FindDLCByName(string name)
    {
        var library = FindFirstObjectByType<AvatarLibraryMenu>();
        if (library == null) return null;
        foreach (var dlc in library.dlcAvatars)
        {
#if UNITY_EDITOR
            string assetPath = AssetDatabase.GetAssetPath(dlc.prefab);
            if (assetPath == name) return dlc.prefab;
#endif
            if (dlc.prefab != null && dlc.prefab.name == name) return dlc.prefab;
        }
        return null;
    }

    public GameObject GetCurrentModel()
    {
        return currentModel;
    }
}

public sealed class GltfInstanceDisposer : MonoBehaviour
{
    private UniGLTF.RuntimeGltfInstance inst;

    public void Bind(UniGLTF.RuntimeGltfInstance i)
    {
        inst = i;
    }

    private void OnDestroy()
    {
        try { inst?.Dispose(); } catch { }
    }
}