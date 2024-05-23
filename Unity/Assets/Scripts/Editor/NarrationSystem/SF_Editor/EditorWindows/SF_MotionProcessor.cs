using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SF_MotionProcessor: EditorWindow
{
    private Vector2 scrollPosition;
    public SF_MotionProcessorConfig config;
    SerializedObject serializedMPConfig;

    private const string motionProcessorConfigPath = "Assets/Scripts/Editor/NarrationSystem/SF_Editor/SFMotionProcessorConfig.asset";

    private static SF_MotionProcessor window;

    private bool foldAnimationClipList;
    
    public void LoadMDConfig()
    {
        if (System.IO.File.Exists(motionProcessorConfigPath))
            config = AssetDatabase.LoadAssetAtPath<SF_MotionProcessorConfig>(motionProcessorConfigPath);
        else
        {
            config = CreateInstance<SF_MotionProcessorConfig>();
            AssetDatabase.CreateAsset(config, motionProcessorConfigPath);
        }
        config.AvoidNullSampleBody();
        serializedMPConfig = new SerializedObject(config);
    }

    void Update()
    {
        if (EditorApplication.isPlaying && !SF_Monitor.stringsFlowing)
        {
            EditorApplication.ExecuteMenuItem("Window/Layouts/SF_StringsFlowingTime");
            SF_MenuItems.UpdateStringsFlowingScene();
            SF_Monitor.stringsFlowing = true;
        }
    }
    
    void OnEnable()
    {
        LoadMDConfig();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        // 移除监听。
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnDestroy()
    {
        DestroyImmediate(config.SampleBodyGameObj);
    }

    void OnSceneGUI(SceneView sceneView)
    {

    }

    void OnGUI()
    {
        // 使用toolbar样式开始水平布局
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUI.backgroundColor = Color.green; 
        float titleWidth = GUI.skin.label.CalcSize(new GUIContent("     <- 返回 SF_Editor     ")).x;
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(new GUIContent(" <- 返回 SF_Editor"), EditorStyles.toolbarButton,GUILayout.Width(titleWidth), GUILayout.Height(25)))
        {
            SF_Vast.ReturnSFEditor();
        }
        GUI.backgroundColor = Color.white; 
        EditorGUILayout.EndHorizontal(); // 结束水平布局

        /// 开始绘制滚动区域的内容
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, alwaysShowHorizontal: false, alwaysShowVertical: false);
        ///绘制配置项的各种设置数据
        EditorGUI.BeginChangeCheck();

        SerializedProperty iterator = serializedMPConfig?.GetIterator();
        if (iterator != null)
        {
            bool enterChildren = true;
            iterator.NextVisible(enterChildren);
            GUILayout.Space(10);
            while (iterator.NextVisible(enterChildren))
            {
                if (AttributeHelper.PropertyHasAttribute<SF基本设置.ShowInSettingWindowAttribute>(iterator))
                {
                    if (iterator.name == "SampleBody")
                    {
                        EditorGUILayout.PropertyField(iterator, enterChildren); 
                        // 画一条水平分割线
                        GUILayout.Space(12);
                        Rect lineRect = EditorGUILayout.GetControlRect(false, 1f);
                        EditorGUI.DrawRect(lineRect, Color.black);
                        GUILayout.Space(5);
                    }
                    else if (iterator.name == "ToBeEdited")
                    {
                        foldAnimationClipList = EditorGUILayout.BeginFoldoutHeaderGroup(foldAnimationClipList, "List To be edited...");
                        GUILayout.Space(6);
                        if(foldAnimationClipList)
                            for (int i = config.ToBeEdited.Count-1; i >= 0; i--)
                            {
                                GUI.backgroundColor = SF_Vast.weakGreen; 
                                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                                string clipNameLabel = "    " + (config.ToBeEdited.Count - i) + ".  " + config.ToBeEdited[i].name;
                               if (GUILayout.Button(new GUIContent(clipNameLabel), EditorStyles.label, GUILayout.Height(20)))
                               {
                                   Debug.Log("(SF_Editor)MotionProcessor聚焦于动画："+clipNameLabel);
                               }
                               if (GUILayout.Button(new GUIContent("X"), EditorStyles.linkLabel,GUILayout.Width(25), GUILayout.Height(20)))
                               {
                                   config.ToBeEdited.RemoveAt(i);
                               }
                               EditorGUILayout.EndHorizontal(); 
                               GUI.backgroundColor = Color.white; 
                               GUILayout.Space(2);
                            }
                        EditorGUILayout.EndFoldoutHeaderGroup();
                        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                        GUILayout.FlexibleSpace();
                        GUI.backgroundColor = SF_Vast.lightlightGreen; 
                        if (GUILayout.Button(new GUIContent("Import clip"), EditorStyles.toolbarButton,GUILayout.Width(100), GUILayout.Height(25)))
                        {
                            
                        }
                        GUI.backgroundColor = Color.white; 
                        EditorGUILayout.EndHorizontal(); 
                    }
                    else
                        EditorGUILayout.PropertyField(iterator, enterChildren); 
                }
            }
            enterChildren = false;
        }
        if (EditorGUI.EndChangeCheck())
            serializedMPConfig.ApplyModifiedProperties();
    
        EditorGUILayout.EndScrollView(); // 结束滚动区域
        
        
        EditorGUILayout.LabelField("后续版本升级优先级：");
        EditorGUILayout.LabelField("1.更好的动画预览（要可以预览多个动画Layer混合）;");
        EditorGUILayout.LabelField("2.显示Energy、Config使用的信息，即这个动作在哪些地方被使用;");
        EditorGUILayout.LabelField("3.动画文件重定向（批量修改.anim文件当中记录的\"path\"属性）");
        EditorGUILayout.LabelField("4.骨骼重定向（骨骼检测与批量改名）;");
        EditorGUILayout.LabelField("5.编辑时IK；");
        GUILayout.Space(8);
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.FlexibleSpace();
        GUILayout.Label("2024.2.28   ");
        EditorGUILayout.EndHorizontal(); 
    }

    public static bool CheckActDirectionRoomIsOpened()
    {
        // 获取当前打开的所有场景
        Scene[] scenes = new Scene[SceneManager.sceneCount];
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            scenes[i] = SceneManager.GetSceneAt(i);
        }
        foreach (var scene in scenes) // 遍历所有场景
        {
            if (scene.name.Contains("ActDirectionRoom"))
            {
                return true;
            }
        }
        return false;
    }
}
