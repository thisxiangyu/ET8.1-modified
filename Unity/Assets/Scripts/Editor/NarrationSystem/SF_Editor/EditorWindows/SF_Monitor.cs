using System.Collections;
using System.Collections.Generic;
using ET.Clent;
using ET.Client;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SF_Monitor : EditorWindow
{
    private Vector2 scrollPosition;

    Object selectedObject;
    SerializedObject serializedObject;

    public static SF_Monitor window;


    [MenuItem("Window/SF窗口/SF_Monitor",false, int.MaxValue-1)]
    public static void Init()
    {
        window = (SF_Monitor)GetWindow(typeof(SF_Monitor));
        Texture icon = EditorGUIUtility.IconContent("devices").image;
        GUIContent titleContent = new GUIContent("内容监看器", icon);
        window.titleContent = titleContent;
    }

    public static bool stringsFlowing=false;
    void Update()
    {
        //启动运行时
        if(EditorApplication.isPlaying&&!stringsFlowing)
        {
            SF_Vast.CompileAllEnergyOfSpeak();
            EditorApplication.ExecuteMenuItem("Window/Layouts/SF_StringsFlowingTime");
            SF_MenuItems.UpdateStringsFlowingScene();
            stringsFlowing = true;
        }

        else if (!EditorApplication.isPlaying&&!SF_MenuItems.isEditorOpened)
            this.Close();
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += RepaintSelected;
        
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    void OnDisable()
    {
        // 移除监听。
        SceneView.duringSceneGui -= OnSceneGUI;
        Selection.selectionChanged -= RepaintSelected;
        
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }
    
    //编辑器运行模式切换
    private static void OnPlayModeStateChanged(PlayModeStateChange mode)
    {
        if (mode == PlayModeStateChange.EnteredEditMode)
        {
            stringsFlowing = false;
            
            //关掉ActDirectionRoom
            if(SceneManager.GetSceneByName("ActDirectionRoom").isLoaded)
                SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("ActDirectionRoom"));
            
            //再次启动SFEditor
            SF_MenuItems.StartSFEditor();
            SF_MenuItems.SF_EditorRef.GetRootGameObjects()[0].transform.Find("VibrationBasin").GetComponent<VibrationBasin>().CurrentUnit?.SetEnergize(false);
        }
        else if (mode == PlayModeStateChange.ExitingEditMode) {
            SF_MenuItems.QuitSFEditor();
            //运行前卸载SF_Editor场景以免触发其中的脚本Start()导致报错
        }
    }

    bool canDraw;
    void RepaintSelected()
    {
        if(SF_MenuItems.CheckIfSFEditorSceneIsOpen())
        {
            canDraw = true;

            if (Selection.activeObject is GameObject)
            {
                selectedObject = Selection.activeObject;
                Repaint();
                Debug.Log("(SF_Editor)已更新SF_Monitor窗口到选定物体:" + selectedObject?.name);
            }
        }
        else
        { canDraw = false; }
    }
    void OnSceneGUI(SceneView sceneView)
    {

    }

    void OnGUI()
    {
        if(canDraw)
        {
               EditorGUILayout.BeginHorizontal(EditorStyles.toolbar); // 开始水平布局

        GUILayout.FlexibleSpace(); // 将按钮推到右侧       
       
        EditorGUILayout.EndHorizontal(); // 结束水平布局

        /// 开始绘制滚动区域的内容
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        if (selectedObject&& selectedObject is GameObject)
        {
            serializedObject = new SerializedObject(selectedObject);
            serializedObject.Update();

            SerializedProperty iterator = serializedObject.GetIterator();

            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
        EditorGUILayout.EndScrollView(); // 结束滚动

        }
     
        //GUILayout.FlexibleSpace(); // 在按钮之前添加弹性空间，将按钮推到底部
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar); // 使用toolbar样式开始水平布局
        //GUILayout.FlexibleSpace(); // 将按钮推到右侧
        GUILayout.Space(1);
        // 计算窗口宽度并在中间显示文本
        float titleWidth = GUI.skin.label.CalcSize(new GUIContent("SF_Monitor")).x;
        float windowWidth = position.width;
        GUILayout.Space(10); // 空格，使文本居中显示
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("SF_Monitor", GUILayout.Width(titleWidth), GUILayout.Height(14));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        EditorGUILayout.EndHorizontal(); // 结束水平布局
        GUILayout.Space(1);
    }
}
