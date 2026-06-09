using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json.Linq;

/// <summary>
/// 外部 VRM 模型文件夹扫描与资产注册器。
/// 负责在不完全加载模型实例的前提下，通过解析 GLB/VRM 头部二进制数据提取元数据，并同步至本地模型库。
/// </summary>
public class VRMFolderScanner : MonoBehaviour
{
    [Header("Scanner Configuration")]
    [Tooltip("需要扫描的外部自定义模型文件夹名称（位于游戏根目录下）")]
    [SerializeField] private string folderName = "CustomModels";

    [Header("UI & Notification Events")]
    [Tooltip("当扫描或解析发生致命错误时触发的异常弹窗事件。参数1：标题，参数2：错误详细信息。")]
    public UnityEvent<string, string> OnScanErrorOccurred = new UnityEvent<string, string>();

    /// <summary>
    /// 模型资产库 UI 控制器引用。
    /// </summary>
    private AvatarLibraryMenu libraryMenu;

    /// <summary>
    /// Unity 生命周期：初始化与自动扫描。
    /// </summary>
    private void Start()
    {
        libraryMenu = FindFirstObjectByType<AvatarLibraryMenu>();
        ScanAndRegisterModels();
    }

    /// <summary>
    /// 执行指定文件夹内所有 VRM 模型的扫描、解析与库注册流程。
    /// </summary>
    public void ScanAndRegisterModels()
    {
        string targetFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderName);

        try
        {
            // 若目标文件夹不存在，则进行初始化创建
            if (!Directory.Exists(targetFolderPath))
            {
                Directory.CreateDirectory(targetFolderPath);
                return;
            }

            string[] vrmFiles = Directory.GetFiles(targetFolderPath, "*.vrm", SearchOption.TopDirectoryOnly);
            if (vrmFiles.Length == 0) return;

            bool discoveredNewModel = false;

            foreach (string filePath in vrmFiles)
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) continue;

                // 检查当前模型路径是否已被注册至 avatars.json
                if (IsModelAlreadyInLibrary(filePath)) continue;

                // 解析元数据并执行注册
                ParseVrmHeadersAndRegister(filePath);
                discoveredNewModel = true;
            }

            // 若存在新注册的资产，则触发本地模型库的 UI 刷新机制
            if (discoveredNewModel && libraryMenu != null)
            {
                libraryMenu.ReloadAvatars();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VRMFolderScanner] Error occurred during scanning/registration: {ex.Message}");

            // 触发致命错误弹窗通知
            OnScanErrorOccurred?.Invoke("模型库扫描失败", $"在扫描外部目录时发生错误：\n{ex.Message}");
        }
    }

    /// <summary>
    /// 校验指定的外部模型路径是否已存在于本地持久化资产库中。
    /// </summary>
    /// <param name="filePath">需要校验的外部模型绝对路径。</param>
    /// <returns>若已存在返回 true；否则返回 false。</returns>
    private bool IsModelAlreadyInLibrary(string filePath)
    {
        string avatarsJsonPath = Path.Combine(Application.persistentDataPath, "avatars.json");
        if (!File.Exists(avatarsJsonPath)) return false;

        try
        {
            string json = File.ReadAllText(avatarsJsonPath);
            if (json.Contains(filePath.Replace("\\", "\\\\")) || json.Contains(filePath))
            {
                return true;
            }
        }
        catch (Exception)
        {
            // 隐式吞没基础校验异常
        }
        return false;
    }

    /// <summary>
    /// 通过解析 GLB 容器的 JSON 文本块，提取 VRM 0.x 或 1.x 的元数据并将其注册至资产库。
    /// </summary>
    /// <param name="filePath">外部 VRM 模型的绝对路径。</param>
    private void ParseVrmHeadersAndRegister(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        string displayName = Path.GetFileNameWithoutExtension(filePath);
        string author = "Unknown";
        string version = "1.0";
        string fileType = "VRM";
        Texture2D thumbnail = null;
        int polyCount = 0;

        try
        {
            using (FileStream stream = File.OpenRead(filePath))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                uint magic = reader.ReadUInt32();       // 必须为 0x46546C67 ("glTF")
                uint glbVersion = reader.ReadUInt32();
                uint length = reader.ReadUInt32();

                if (magic != 0x46546C67)
                {
                    throw new InvalidDataException("该文件不是合法的 GLB/VRM 格式二进制文件。");
                }

                uint chunkLength = reader.ReadUInt32();
                uint chunkType = reader.ReadUInt32();   // 必须为 0x4E4F534A ("JSON")

                if (chunkType != 0x4E4F534A)
                {
                    throw new InvalidDataException("未能从模型中检索到合法的 JSON 描述文本块。");
                }

                byte[] jsonBytes = reader.ReadBytes((int)chunkLength);
                string jsonString = Encoding.UTF8.GetString(jsonBytes);

                JObject gltfRoot = JObject.Parse(jsonString);

                // 分支1：检测并解析 VRM 1.x (扩展键名为 VRMC_vrm)
                if (gltfRoot["extensions"]?["VRMC_vrm"]?["meta"] != null)
                {
                    JToken meta10 = gltfRoot["extensions"]["VRMC_vrm"]["meta"];
                    displayName = meta10["name"]?.ToString() ?? displayName;
                    version = meta10["version"]?.ToString() ?? version;
                    fileType = "VRM 1.x";

                    JArray authorsArray = meta10["authors"] as JArray;
                    if (authorsArray != null && authorsArray.Count > 0)
                    {
                        author = authorsArray[0].ToString();
                    }
                }
                // 分支2：检测并解析 VRM 0.x (扩展键名为 VRM)
                else if (gltfRoot["extensions"]?["VRM"]?["meta"] != null)
                {
                    JToken meta0x = gltfRoot["extensions"]["VRM"]["meta"];
                    displayName = meta0x["title"]?.ToString() ?? displayName;
                    author = meta0x["author"]?.ToString() ?? author;
                    version = meta0x["version"]?.ToString() ?? version;
                    fileType = "VRM 0.x";
                }
                else
                {
                    throw new NotSupportedException("该文件属于标准 glTF，但未包含 VRM 0.x 或 1.x 规范的扩展元数据。");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[VRMFolderScanner] Metadata extraction failed for {fileName}: {ex.Message}");

            // 当单个模型解析失败时，直接触发 UI 弹窗提示用户该文件损坏或不支持
            OnScanErrorOccurred?.Invoke("外部模型解析失败", $"文件 [{fileName}] 解析时发生错误：\n{ex.Message}\n\n该资产将降级使用标准配置载入。");
        }

        // 调用持久化模型库的核心静态方法进行数据登记
        AvatarLibraryMenu.AddAvatarToLibrary(
            displayName,
            author,
            version,
            fileType,
            filePath,
            thumbnail,
            polyCount
        );
    }
}