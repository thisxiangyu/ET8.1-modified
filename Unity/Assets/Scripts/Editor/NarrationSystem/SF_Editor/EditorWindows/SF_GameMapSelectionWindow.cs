using System;
using ET.Client;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;
using SF基本设置;
using System.IO;
using System.Drawing;
using SharpCompress.Common;

//在指定文件夹路径下选择已经置备好的关卡
public class SF_GameMapSelectionWindow : EditorWindow
{    
    private Vector2 scrollPosition;

    static List<string> 关卡路径列表;
    const string 关卡存放路径 = "Assets/Bundles/GameMaps/";


    public static void Open()
    {
        if (AssetDatabase.IsValidFolder(关卡存放路径))
        {
            // 获取目标路径下的所有文件
            string[] files = Directory.GetFiles(关卡存放路径, "*.SunRain", SearchOption.AllDirectories);
            关卡路径列表 = new();

            // 将文件名添加到List<string>
            foreach (string filePath in files)
            {
                关卡路径列表.Add(filePath);
            }
        }
        else
            Debug.Log("文件夹不存在！");

        SF_GameMapSelectionWindow window = (SF_GameMapSelectionWindow)GetWindow(typeof(SF_GameMapSelectionWindow), true, " 已制备的关卡", true);
        window.ShowModalUtility();
    }


    private void 设置当前选中的EnergyOfMusicGameStartUp关卡为(string 关卡路径) {
        (SF_Vast.selectedObject as EnergyOfMusicGameStartUp).MapFileGUID = AssetDatabase.AssetPathToGUID(关卡路径);
        EditorApplication.delayCall = null;
    }


    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar); // 使用toolbar样式开始水平布局
        GUILayout.FlexibleSpace(); // 将按钮推到右侧
        GUILayout.Space(1);

        EditorGUILayout.EndHorizontal(); // 结束水平布局
        GUILayout.Space(1);

        /// 开始绘制滚动区域的内容
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, alwaysShowHorizontal: false, alwaysShowVertical: false);
        
        if (关卡路径列表.Count!=0)
        {
            if (GUILayout.Button(new GUIContent(" None "), GUILayout.ExpandWidth(true), GUILayout.Height(20)))
            {
                设置当前选中的EnergyOfMusicGameStartUp关卡为("$无关卡$");
                Close();
            }
            GUILayout.Space(4);
            foreach (string 关卡路径 in 关卡路径列表)
            {
                // 获取文件名（不包含路径）
                string 关卡名 = Path.GetFileName(关卡路径);
                Texture2D icon = null;
                if (关卡名.Contains("SunRain"))
                {
                    GUI.backgroundColor = SF_Vast.lightGreen;
                    icon = EditorGUIUtility.IconContent("谱面").image as Texture2D;
                }
                else if(关卡名.Contains("")) //其它格式的关卡文件(以后可以拓展)
                {

                }

                if (GUILayout.Button(new GUIContent("  " + 关卡名, icon), GUILayout.ExpandWidth(true), GUILayout.Height(25)))
                {
                    设置当前选中的EnergyOfMusicGameStartUp关卡为(关卡路径);
                    Close();
                }
                GUILayout.Space(4);
            }
            GUI.backgroundColor = UnityEngine.Color.white;
        }
        else
            EditorGUILayout.LabelField("( 未找到任何关卡。)");


        //EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView(); // 结束滚动区域

        // 使用toolbar样式开始水平布局
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Space(1);
        EditorGUILayout.LabelField(关卡存放路径);
        EditorGUILayout.EndHorizontal(); // 结束水平布局
        GUILayout.Space(1);
    }
}
