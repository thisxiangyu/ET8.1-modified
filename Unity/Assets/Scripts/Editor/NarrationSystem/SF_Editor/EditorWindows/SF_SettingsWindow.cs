using System;
using ET.Client;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;

public class SF_SettingsWindow : EditorWindow
{
    
    static SF_SettingsWindow window;

    private static SF基本设置.SF_EditorBasicConfig config;
    public static SerializedObject serializedConfig;
    public static SerializedProperty 存档位Property;
    
    private Vector2 scrollPosition;

    private void OnLostFocus()
    {
        if (SF_Vast.存档位!=null)
            SF_Vast.初始化存档位();   
    }

    public static SF基本设置.SF_EditorBasicConfig LoadBasicConfig()
    {
        if (System.IO.File.Exists(VibrationBasin.BasicConfigFilePath+"/"+ VibrationBasin.BasicConfigAssetName))
            config = AssetDatabase.LoadAssetAtPath<SF基本设置.SF_EditorBasicConfig>(VibrationBasin.BasicConfigFilePath+"/" + VibrationBasin.BasicConfigAssetName);
        else
        {
            config = CreateInstance<SF基本设置.SF_EditorBasicConfig>();
            AssetDatabase.CreateAsset(config, VibrationBasin.BasicConfigFilePath+"/" + VibrationBasin.BasicConfigAssetName);
        }
        serializedConfig = new SerializedObject(config);
        存档位Property = serializedConfig.FindProperty("开发测试存档位");
        return config;
    }
    
    [MenuItem("SF/基本设置", false, int.MaxValue/2)]
    public static void Open()
    {
        window = (SF_SettingsWindow)GetWindow(typeof(SF_SettingsWindow));
        window.titleContent = UpdateWindowTitle.UpdateAs(" 基本设置", "Wave2", "SF_Settings");
        LoadBasicConfig();
        设置Keywords设计数据读写宏(config.Keywords设计数据读写.ToString());
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
        ///绘制配置项的各种设置数据
        EditorGUI.BeginChangeCheck();
        SerializedProperty iterator = serializedConfig?.GetIterator();
        if (iterator != null)
        {
            bool enterChildren = true;
            iterator.NextVisible(enterChildren);
            while (iterator.NextVisible(enterChildren))
            {
                if(iterator!=null && AttributeHelper.PropertyHasAttribute<SF基本设置.ShowInSettingWindowAttribute>(iterator))
                    EditorGUILayout.PropertyField(iterator, enterChildren);
                if (iterator.name == "Keywords设计数据读写")
                {        
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(iterator);
                    if (EditorGUI.EndChangeCheck())
                    {
                        设置Keywords设计数据读写宏(((SF基本设置.Keywords设计数据读写宏)iterator.enumValueIndex).ToString());
                    }
                }
            }
            enterChildren = false;
        }
        if (EditorGUI.EndChangeCheck())
            serializedConfig.ApplyModifiedProperties();
        
        EditorGUILayout.EndScrollView(); // 结束滚动区域
            
        // 使用toolbar样式开始水平布局
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Space(1);
        // 计算窗口宽度并在中间显示文本
        float titleWidth = GUI.skin.label.CalcSize(new GUIContent("Strings Flow")).x;
        GUILayout.Space(10); // 空格，使文本居中显示
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUIStyle maximStyle = new GUIStyle(GUI.skin.label);
        maximStyle.fontSize = 11;
        maximStyle.normal.textColor = SF_Vast.maximColor;
        maximStyle.hover.textColor = SF_Vast.maximColor;
        GUILayout.Label("Strings Flow", maximStyle,GUILayout.Width(titleWidth), GUILayout.Height(14));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        EditorGUILayout.EndHorizontal(); // 结束水平布局
        GUILayout.Space(1);
    }
    
    
    public static void 设置Keywords设计数据读写宏(string setAs)
    {
        //获取当前是哪个平台
        BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        //获取当前平台已有的宏定义
        var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        //移除任意已有的宏
        var ss = symbols.Split(';').ToList();
        for (int i = ss.Count-1; i >=0 ; i--)
        {
            SF基本设置.Keywords设计数据读写宏 result;
            if (Enum.TryParse(ss[i], out result))
                ss.RemoveAt(i);
        }
        //添加想要的宏定义
        if (ss.Contains(setAs))
        {
            return;
        }
        ss.Add(setAs);
        symbols = string.Join(";", ss);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, symbols);
    }
}
