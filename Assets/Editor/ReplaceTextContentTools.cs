#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public class ReplaceTextContentTools : EditorWindow
{
    private string sourceContent = ""; // 要被替换的旧内容
    private string targetContent = ""; // 替换后的新内容
    private bool includeTMP = true;
    private bool includeOldText = false;
    private bool onlyActiveObjects = false;
    private bool matchExact = true; // 是否精准匹配

    [MenuItem("Tools/全局文本内容替换工具")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceTextContentTools>("精确文本替换");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("全局「查找并替换」UI 控件内容", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 🎯 框一：源文本
        GUILayout.Label("1. 查找包含以下内容的控件 (旧文本):");
        sourceContent = EditorGUILayout.TextArea(sourceContent, GUILayout.Height(50));

        GUILayout.Space(10);

        // 🎯 框二：目标文本
        GUILayout.Label("2. 一键替换为以下内容 (新文本):");
        targetContent = EditorGUILayout.TextArea(targetContent, GUILayout.Height(50));

        GUILayout.Space(15);
        GUILayout.Label("高级筛选设置:", EditorStyles.boldLabel);

        matchExact = EditorGUILayout.Toggle("必须完全一致 (精准匹配)", matchExact);
        includeTMP = EditorGUILayout.Toggle("包含 TextMeshPro (TMP)", includeTMP);
        includeOldText = EditorGUILayout.Toggle("包含 旧版 UI Text", includeOldText);
        onlyActiveObjects = EditorGUILayout.Toggle("仅修改当前激活的物体", onlyActiveObjects);

        GUILayout.Space(20);

        if (GUILayout.Button("开始查找并替换", GUILayout.Height(35)))
        {
            if (string.IsNullOrEmpty(sourceContent))
            {
                EditorUtility.DisplayDialog("提示", "请输入你要查找的旧文本内容！", "确定");
                return;
            }

            string matchTypeStr = matchExact ? "完全等于" : "包含";
            if (EditorUtility.DisplayDialog("二次确认", $"确定要将场景中所有文本 {matchTypeStr} \"{sourceContent}\" 的控件，替换为 \"{targetContent}\" 吗？", "确定替换", "取消"))
            {
                ExecuteReplace();
            }
        }
    }

    private void ExecuteReplace()
    {
        int tmpCount = 0;
        int oldTextCount = 0;

        if (includeTMP)
        {
            TMP_Text[] allTMPs = Resources.FindObjectsOfTypeAll<TMP_Text>();
            foreach (TMP_Text tmp in allTMPs)
            {
                if (tmp.gameObject.scene.name == null) continue;
                if (onlyActiveObjects && !tmp.gameObject.activeInHierarchy) continue;

                // 检查是否匹配旧内容
                if (IsMatch(tmp.text, sourceContent))
                {
                    Undo.RecordObject(tmp, "Replace TMP Content");
                    tmp.text = matchExact ? targetContent : tmp.text.Replace(sourceContent, targetContent);
                    EditorUtility.SetDirty(tmp);
                    tmpCount++;
                }
            }
        }

        if (includeOldText)
        {
            Text[] allOldTexts = Resources.FindObjectsOfTypeAll<Text>();
            foreach (Text oldText in allOldTexts)
            {
                if (oldText.gameObject.scene.name == null) continue;
                if (onlyActiveObjects && !oldText.gameObject.activeInHierarchy) continue;

                // 检查是否匹配旧内容
                if (IsMatch(oldText.text, sourceContent))
                {
                    Undo.RecordObject(oldText, "Replace Old Text Content");
                    oldText.text = matchExact ? targetContent : oldText.text.Replace(sourceContent, targetContent);
                    EditorUtility.SetDirty(oldText);
                    oldTextCount++;
                }
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        string resultMessage = $"替换完成！\n";
        resultMessage += $"修改了 {tmpCount} 个 TMP 控件， {oldTextCount} 个旧版 Text 控件。";

        EditorUtility.DisplayDialog("成功", resultMessage, "知道了");
    }

    private bool IsMatch(string currentText, string searchPattern)
    {
        if (currentText == null) return false;
        if (matchExact)
        {
            return currentText.Trim() == searchPattern.Trim();
        }
        else
        {
            return currentText.Contains(searchPattern);
        }
    }
}
#endif