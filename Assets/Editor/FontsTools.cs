#if UNITY_EDITOR // 确保在打包游戏时不会报错
using UnityEngine;
using UnityEditor;
using TMPro;

// 类名已修改为 FontsTools，与文件名保持一致
public class FontsTools : EditorWindow
{
    private TMP_FontAsset targetFont;

    [MenuItem("Tools/全局字体替换工具")]
    public static void ShowWindow()
    {
        // 这里的泛型类型也同步修改为了 FontsTools
        GetWindow<FontsTools>("字体替换");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("将新字体拖入下方，一键替换当前场景所有 TMP 字体：", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // 字体选择框
        targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField("目标字体 (TMP)", targetFont, typeof(TMP_FontAsset), false);

        GUILayout.Space(15);

        // 点击按钮执行替换
        if (GUILayout.Button("开始替换当前场景", GUILayout.Height(30)))
        {
            if (targetFont == null)
            {
                Debug.LogError("【字体工具】: 请先指定目标字体！");
                return;
            }

            // 获取场景中所有的 TMP_Text 组件（包含隐藏物体）
            TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            int count = 0;

            foreach (TMP_Text textComponent in allTexts)
            {
                // 确保只修改当前层级视图（Hierarchy）中的物体，不污染 Project 里的预制体原始资源
                if (textComponent.gameObject.scene.name != null)
                {
                    // 记录撤销操作，按下 Ctrl+Z 可以撤销这次字体替换
                    Undo.RecordObject(textComponent, "Change Global Font");

                    // 替换字体
                    textComponent.font = targetFont;

                    // 标记物体已被修改，确保 Unity 会保存更改
                    EditorUtility.SetDirty(textComponent);
                    count++;
                }
            }

            // 在控制台打印绿色成功提示
            Debug.Log($"<color=green>【字体工具】: 替换完成！当前场景共修改了 {count} 个文本组件。</color>");
        }
    }
}
#endif