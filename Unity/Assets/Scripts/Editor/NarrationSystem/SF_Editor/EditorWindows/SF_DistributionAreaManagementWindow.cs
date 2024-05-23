using ET.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;


//本地支持等级。
public enum LocalizationSupportLevelTag
{
    暂时不提供支持,
    即将提供支持,
    提供支持,
    内容定制级支持,
}

public class SF_DistributionAreaSelectionWindow : EditorWindow
{
    private Vector2 scrollPosition;

    public static SF_DistributionAreaSelectionWindow window;

    private  Texture2D itemConfigImage;
    private Texture2D switchImage;

    public  List<LocalizationSupportLevelTag> SupportLevelTags;

    private string[] options;

    private int selectedOption = 1;

    private GUIStyle labelBigStyle;

    public static void Open()
    {
        window = (SF_DistributionAreaSelectionWindow)GetWindow(typeof(SF_DistributionAreaSelectionWindow), true);
        window.titleContent = UpdateWindowTitle.UpdateAs("SF_全球化发行", "山水", "SF_DistributionArea");
    }

    private void OnEnable()
    {
        itemConfigImage = EditorGUIUtility.IconContent("d_ScriptableObject Icon").image as Texture2D;
        switchImage = EditorGUIUtility.IconContent("火箭").image as Texture2D;

        // 创建一个新的GUIStyle，并指定字体大小
        labelBigStyle = new GUIStyle(EditorStyles.label);
        labelBigStyle.fontSize = 25;

        if (SupportLevelTags != null)
            SupportLevelTags.Clear();
        else
            SupportLevelTags = new();

        // 获取枚举的所有命名值
        options = Enum.GetNames(typeof(EnumOfDistributionArea));

        foreach (string o in options)
        {
            SupportLevelTags.Add(LocalizationSupportLevelTag.暂时不提供支持);
        }
    }

    void OnGUI()
    {
        GUI.backgroundColor = SF_Conductor.lightblue;

        EditorGUILayout.BeginVertical();
        /// 开始绘制滚动区域的内容
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        for (int i = 0; i < options.Length; i++)
        {
            bool isSelected = selectedOption == i;
            EditorGUI.BeginChangeCheck();
            if(options[i].Contains("发行") || options[i].Contains("市场"))
            {
                GUILayout.Label(options[i], labelBigStyle);
            }
            else
            {
                GUILayout.BeginHorizontal();
                isSelected = GUILayout.Toggle(isSelected, options[i], "Button", GUILayout.Width(position.width / 2-7));
                GUI.backgroundColor = SF_Conductor.darkBlueColor;
                EditorGUILayout.EnumPopup(SupportLevelTags[i], GUILayout.Width(position.width / 2 -15));
                GUI.backgroundColor = SF_Conductor.lightblue;
                GUILayout.EndHorizontal();
            }
            if (EditorGUI.EndChangeCheck() && isSelected)
            {
                selectedOption = i;
            }
        }

        EditorGUILayout.EndScrollView(); // 结束滚动区域

        GUILayout.Space(1.5f);
        // 画一条水平线
        Rect lineRect = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(lineRect, UnityEngine.Color.black);
        GUILayout.Space(3);

        // 显示一个带有图标的按钮
        if (GUILayout.Button(new GUIContent(" 项配置", itemConfigImage), GUILayout.Width(75), GUILayout.Height(22)))
        {
            //按钮被点击时的操作
            SF_DistributionAreaManagementWindow.Open();
        }

        // 显示一个带有图标的按钮
        if (GUILayout.Button(new GUIContent(" 切换到 "+ options[selectedOption], switchImage), GUILayout.Width(150), GUILayout.Height(22)))
        {
            
        }

        GUILayout.Space(170);

        EditorGUILayout.EndVertical();
    }
}

public class SF_DistributionAreaManagementWindow : EditorWindow
{
    private Vector2 scrollPosition;

    public static SF_DistributionAreaManagementWindow window;


    public static void Open()
    {
        window = (SF_DistributionAreaManagementWindow)GetWindow(typeof(SF_DistributionAreaManagementWindow), false);
        window.titleContent = UpdateWindowTitle.UpdateAs("SF_全球化发行 项配置", "d_ScriptableObject Icon", "SF_DistributionArea");
    }

    private Texture2D icon;

    void OnGUI()
    {

        /// 开始绘制滚动区域的内容
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("1.所有的对话;");
        EditorGUILayout.LabelField("2.所有的DisplayName;");
        EditorGUILayout.LabelField("3.SF_Editor场景当中的所有内容;");

        EditorGUILayout.EndScrollView(); // 结束滚动区域
    }
}

