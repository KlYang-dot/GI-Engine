using System;
using System.IO;
using UnityEngine;
using VRM;
using UniVRM10;

public static class VRMMetaReader
{
    // 定义一个结构体/类来统一接收返回的数据
    public class ModelMetaResult
    {
        public string displayName;
        public string author;
        public string version;
        public string fileType;
        public Texture2D thumbnail; // 这是经过安全复制、可读写的纹理
        public int polygonCount;
    }

    /// <summary>
    /// 使用和 VRMLoader 相同的方法，从已载入的 GameObject 中提取模型名称、缩略图等元数据
    /// </summary>
    /// <param name="loadedModel">已经生成的模型 GameObject 根节点</param>
    /// <param name="path">模型的完整文件路径（用于备用名称和格式判断）</param>
    public static ModelMetaResult ReadMeta(GameObject loadedModel, string path)
    {
        if (loadedModel == null)
        {
            Debug.LogError("[VRMMetaReader] 传入的模型 GameObject 为空！");
            return null;
        }

        ModelMetaResult result = new ModelMetaResult();

        // 1. 初始化默认兜底数据
        result.displayName = Path.GetFileNameWithoutExtension(path);
        result.author = "Unknown";
        result.version = "Unknown";
        result.fileType = "Unknown";

        Texture2D rawThumbnail = null;
        bool isME = !string.IsNullOrEmpty(path) && path.EndsWith(".me", StringComparison.OrdinalIgnoreCase);

        // 2. 尝试解析 VRM 1.0 元数据
        var vrm10Instance = loadedModel.GetComponent<UniVRM10.Vrm10Instance>();
        if (vrm10Instance != null && vrm10Instance.Vrm != null && vrm10Instance.Vrm.Meta != null)
        {
            var meta10 = vrm10Instance.Vrm.Meta;
            result.displayName = meta10.Name ?? result.displayName;
            result.author = (meta10.Authors != null && meta10.Authors.Count > 0) ? meta10.Authors[0] : "Unknown";
            result.version = meta10.Version ?? "Unknown";
            result.fileType = isME ? ".ME (VRM1.X)" : "VRM1.X";
            rawThumbnail = meta10.Thumbnail;
        }
        // 3. 降级解析 VRM 0.x 元数据
        else
        {
            var vrmMeta0X = loadedModel.GetComponent<VRM.VRMMeta>();
            if (vrmMeta0X != null && vrmMeta0X.Meta != null)
            {
                var meta0X = vrmMeta0X.Meta;
                result.displayName = !string.IsNullOrEmpty(meta0X.Title) ? meta0X.Title : result.displayName;
                result.author = !string.IsNullOrEmpty(meta0X.Author) ? meta0X.Author : "Unknown";
                result.version = !string.IsNullOrEmpty(meta0X.Version) ? meta0X.Version : "Unknown";
                result.fileType = isME ? ".ME (VRM0.X)" : "VRM0.X";
                rawThumbnail = meta0X.Thumbnail;
            }
        }

        // 4. 使用和 VRMLoader 相同的底层 Blit 像素解锁逻辑复制缩略图
        result.thumbnail = MakeReadableCopy(rawThumbnail);

        // 5. 顺便统计面数
        result.polygonCount = CalculateTotalPolygons(loadedModel);

        return result;
    }

    // ==================== 核心辅助方法（完全同步 VRMLoader） ====================

    private static Texture2D MakeReadableCopy(Texture texture)
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

    private static int CalculateTotalPolygons(GameObject model)
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
}