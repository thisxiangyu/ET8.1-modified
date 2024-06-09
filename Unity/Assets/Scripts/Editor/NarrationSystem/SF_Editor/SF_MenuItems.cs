using ET.Client;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine;
using System;
using SF基本设置;
using TMPro;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class SFUnityBoot
{
    static SFUnityBoot()
    {
        SF_MenuItems.QuitSFEditor();
    }
}

public class SF_MenuItems : EditorWindow
{
    public static bool isEditorOpened;
    
    public static string editorPath = "Assets/Bundles/Narration/NarrationEditor/SF_Editor 1.0.unity";
    public static string stringsFlowingPath = "Assets/Bundles/Scenes/Road/ChaptersAndSections/StringsFlowing.unity";
    public static Transform LayoutRef;
    private static SF_Layouter LayouterComponent;
    public static Scene SF_EditorRef;
    public static Transform VibrationBasinRef;
    public static VibrationBasin VibrationBasinComponent;

    static void OnAwakeCustomHierarchyMenuGUI(int instanceID, Rect selectionRect)
    {
        Vector2 mousePosition = Event.current.mousePosition;
        GameObject selectedGameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        // CheckIfSFEditorSceneIsOpen();

        ///绘制Hierarchy当中Vibrator对象的图标。
        VibrationUnit vibratorComponent = selectedGameObject?.GetComponent<VibrationUnit>();
        if (selectedGameObject != null && vibratorComponent != null && vibratorComponent.isEnergize)
        {
            Rect labelRect = new Rect(selectionRect.x + selectionRect.width - 60, selectionRect.y, 40, 17);
            Rect waitIconRect = new Rect(selectionRect.x + selectionRect.width - 21, selectionRect.y, 17, 17);
            Rect finishIconRect = new Rect(selectionRect.x + selectionRect.width - 30, selectionRect.y, 17, 17);
            bool buttonCliked;
            if (vibratorComponent.gameObject.activeInHierarchy)
                buttonCliked=GUI.Button(finishIconRect, EditorGUIUtility.IconContent("Wave通电"), GUIStyle.none);
            else
                buttonCliked=GUI.Button(finishIconRect, EditorGUIUtility.IconContent("Wave"), GUIStyle.none);
            if (buttonCliked)
                Check();
            VibrationBasinComponent = VibrationBasinRef.GetComponent<VibrationBasin>();
            VibrationBasinComponent.CurrentUnit = vibratorComponent;
            VibrationBasinComponent.EditorEntrance = vibratorComponent;
        }

        ///绘制VibrationBasin和Layout对象的图标
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj != null && obj.GetComponent<VibrationBasin>() != null)
        {
            Rect VibrationBasin = new Rect(selectionRect.x + selectionRect.width - 30, selectionRect.y, 18, 18);
            if(GUI.Button(VibrationBasin, EditorGUIUtility.IconContent("Tree"), GUIStyle.none))
            {
                Selection.activeGameObject = obj;
                Check();
            }
        }
        if (obj != null && obj.GetComponent<SF_Layouter>() != null)
        {
            Rect SF_LayouterIconRect = new Rect(selectionRect.x + selectionRect.width - 30, selectionRect.y, 15, 15);
            if(GUI.Button(SF_LayouterIconRect, EditorGUIUtility.IconContent("pileUp"), GUIStyle.none))
            {
                Selection.activeGameObject = obj;
                Check();
            }
        }

        ///自定义鼠标右键换出菜单
        if (Event.current != null && selectionRect.Contains(Event.current.mousePosition) && Event.current.button == 1 && Event.current.type <= EventType.MouseUp)
        {
            //if (Selection.objects.Length == 0|| Selection.objects.Length == 1)
            //    Selection.activeObject = selectedGameObject; //这一句是配合下面Event.current.Use()的。似乎开了之后有问题,会导致无法选中单个,所以先判断Selection.objects.Length==1的时候再这么干

            //这里可以判断唤出条件
            if (selectedGameObject.GetComponent<VibrationUnit>() || selectedGameObject.GetComponent<VibrationBasin>())
            {
#if UNITY_STANDALONE_WIN  
                OpenWinVibrationBasinRightClickMenu(); 
#elif UNITY_STANDALONE_OSX
                Event.current.Use();//操作已完成
#endif
            }
            else if (CheckTypeOnParentTrunk(typeof(SF_Layouter), selectedGameObject) != null)
            {   ///打开自定义的Layouter菜单
#if UNITY_STANDALONE_WIN 
                OpenWinLayoutRightClickMenu();
#elif UNITY_STANDALONE_OSX
                Event.current.Use();//操作已完成
#endif
            }
        }
        else if (Event.current.button == 1 && Event.current.type <= EventType.MouseUp)
        {
            if (selectedGameObject == null)
            {
#if UNITY_STANDALONE_WIN          
                EditorUtility.DisplayPopupMenu(new Rect(mousePosition.x+5, mousePosition.y, 0, 0), "GameObject/StringsFlow", null);
#elif UNITY_STANDALONE_OSX
               OpenMacRightClickMenu(Selection.activeObject,mousePosition);
               Event.current.Use();
#endif
            }
        }
    }

    private static void OpenWinLayoutRightClickMenu()
    {
        GenericMenu menu = new ();
        menu.AddItem(new GUIContent("聚焦"), false, FocusInScene);
        // 添加分隔线
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("保存"), false, Save);
        // 添加分隔线
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("窗口化..."), false, Check);
        // 显示菜单
        menu.ShowAsContext();
    }
    private static void OpenWinVibrationBasinRightClickMenu()
    {
        GenericMenu menu = new ();
        menu.AddItem(new GUIContent("激活 \\ 闲置"), false, SetActivity);
        // 添加分隔线
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("重命名"), false, Rename);
        menu.AddItem(new GUIContent("删除"), false, Delete);
        // 添加分隔线    
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("在主流插入 振动器"), false, CreateVibratorObjectOnCurrentMainStream);
        menu.AddItem(new GUIContent("在支流添加 振动器"), false, CreateVibratorObjectOnSF_Editor);
        // 添加分隔线
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("窗口化..."), false, Check);
        // 显示菜单
        menu.ShowAsContext();
    }
    private static void OpenMacRightClickMenu(UnityEngine.Object selectedObject,Vector2 mousePosition)
    {
        GameObject selected;
        if (selectedObject is GameObject)
            selected = (GameObject)selectedObject;
        else
        {
            GenericMenu menu = new ();
            menu.AddItem(new GUIContent("(未选择)"), false, null);
            menu.ShowAsContext();
            return;
        }
        if ( selected.GetComponent<VibrationUnit>() || selected.GetComponent<VibrationBasin>())
            OpenWinVibrationBasinRightClickMenu();
        else if (CheckTypeOnParentTrunk(typeof (SF_Layouter), selected) != null)
            OpenWinLayoutRightClickMenu();
        else EditorUtility.DisplayPopupMenu(new Rect(mousePosition.x+5, mousePosition.y, 0, 0), "GameObject/StringsFlow", null);
    }

    [MenuItem("SF/启用 Editor", false, int.MinValue)]
    public static void StartSFEditor()
    {
        QuitSFEditor();
        
        isEditorOpened = true;
    
        SF_EditorBasicConfig basicConfig = SF_SettingsWindow.LoadBasicConfig();
        SF_SettingsWindow.设置Keywords设计数据读写宏(basicConfig.Keywords设计数据读写.ToString());
    
        SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(editorPath);
        if (scene != null)
            SF_EditorRef = EditorSceneManager.OpenScene(editorPath, OpenSceneMode.Additive);

        SF_EditorRef.GetRootGameObjects()[0].transform.Find("VibrationBasin").GetComponent<VibrationBasin>().CurrentUnit?.SetEnergize(false);
    
        SF_EditorMouseClickSelection.SF_EditorGameObjectSelectedStack[1] = SF_EditorRef.GetRootGameObjects()[0];

        Selection.selectionChanged += SF_EditorMouseClickSelection.OnSFSelectionChanged;
        EditorApplication.hierarchyWindowItemOnGUI += OnAwakeCustomHierarchyMenuGUI;
        EditorApplication.quitting += QuitSFEditor;
        EditorSceneManager.sceneSaving += SaveCurrentVibrationUnit;

        EditorGUIUtility.PingObject(VibrationBasinRef);

        SF_Monitor.Init();
        SF_Conductor.Init();

        EditorApplication.ExecuteMenuItem("SF/窗口布局/默认");

        Debug.Log("(SF_Editor)弦涌编辑器启动完毕！");
    }


    [MenuItem("Window/SF窗口/null/SF_空项", false, int.MaxValue)]
    private static void NullItem()
    {
    }


    [MenuItem("SF/窗口布局/默认", false, 5)]
    private static void GoDefaultMode()
    {
        if (CheckIfSFEditorSceneIsOpen())
        {
            EditorApplication.ExecuteMenuItem("Window/Layouts/SF_Default");
        }
        else
        {
            Debug.LogWarning("(SF_Editor)弦涌编辑器是未开启状态。");
        }
    }

    [MenuItem("SF/窗口布局/编辑模式", false , 6)]
    private static void GoEditMode()
    {
        if (CheckIfSFEditorSceneIsOpen())
        {
            EditorApplication.ExecuteMenuItem("Window/Layouts/StoryEditMode");
            Debug.Log("(SF_Editor)编辑模式布局。");
        }
        else
            Debug.LogWarning("(SF_Editor)弦涌编辑器是未开启状态。");
    }

    [MenuItem("SF/窗口布局/Layout模式", false , 7 )]
    private static void GoLayoutMode()
    {
        if (CheckIfSFEditorSceneIsOpen())
        {
            EditorApplication.ExecuteMenuItem("Window/Layouts/StoryLayoutMode");
            Debug.Log("(SF_Editor)Layout模式布局。");
        }
        else 
        {
            Debug.LogWarning("(SF_Editor)弦涌编辑器是未开启状态。");
        }
    }

    [MenuItem("SF/将整层物体图标统一设为...", false, int.MaxValue/3)]
    public static void SetGameObjectIcons()
    {
        GameObject theActive = Selection.activeGameObject;
        SF_MakeSureIconsModifyWindow makeSure = SF_MakeSureIconsModifyWindow.ShowWindow(theActive);
        if (makeSure != null)
            makeSure.position = new Rect(140, 180, 320, 120);
    }

    [MenuItem("SF/激活编辑器变量批量转换工具...", false, int.MaxValue / 3+1)]
    public static void 激活编辑器变量批量转换工具()
    {
        SF_编辑器变量批量转换工具.开始转换();
    }


    [MenuItem("SF/退出 Editor", false, int.MaxValue)]
    public  static void QuitSFEditor()
    {
        EditorApplication.hierarchyWindowItemOnGUI -= OnAwakeCustomHierarchyMenuGUI;
        EditorApplication.quitting -= QuitSFEditor;
        EditorSceneManager.sceneSaving -= SaveCurrentVibrationUnit;
        Selection.selectionChanged -= SF_EditorMouseClickSelection.OnSFSelectionChanged;

        if (CheckIfSFEditorSceneIsOpen())
        {
            SF_EditorMouseClickSelection.UnloadUnit();
            
            if (Selection.activeGameObject == null || Selection.activeGameObject.scene!=SF_EditorRef)
                Selection.activeGameObject = LayoutRef.gameObject;//这样处理一下是为了避免因选中非SF的对象时关闭编辑器而产生的各种空引用报错

            if (VibrationBasinComponent != null)
            {
                VibrationBasinComponent.CurrentUnit.SetEnergize(false);
                VibrationBasinComponent.CurrentUnit = null; }
                LayoutRef.Find("2DStage").Find("Monitor").GetComponent<TextMeshProUGUI>().text = "Strings Flow";
            
            if(SF_EditorMouseClickSelection.SF_EditorGameObjectSelectedStack[1]!=null)
                SF_EditorMouseClickSelection.SF_EditorGameObjectSelectedStack[1].GetComponent<VibrationUnit>()?.SetEnergize(false);

            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
            
            if(isEditorOpened)
            {
                EditorSceneManager.SaveScene(SF_EditorRef);
                EditorSceneManager.CloseScene(SF_EditorRef, true);
                Debug.Log("弦涌编辑器已退出。(已自动保存SF_Editor内容)");
            }

            isEditorOpened = false;
        }
    }


    //激活或者闲置
    private static void SetActivity()
    {
        if(CheckIfSFEditorSceneIsOpen())
            EditorApplication.ExecuteMenuItem("GameObject/Toggle Active State"); // 要点击的菜单项路径
        else
            Debug.LogWarning("(SF_Editor)弦涌编辑器未开启，这个菜单项无效。");
    }

    //保存
    static void Save()
    {
        if (CheckIfSFEditorSceneIsOpen())
        {
                EditorApplication.ExecuteMenuItem("File/Save");
        }
        else
            Debug.LogWarning("(SF_Editor)弦涌编辑器未开启，这个菜单项无效。");
    }
    
    //聚焦
    static void FocusInScene()
    {
        if (CheckIfSFEditorSceneIsOpen())
        {
            EditorApplication.ExecuteMenuItem("Edit/Lock View to Selected");
        }
        else
            Debug.LogWarning("(SF_Editor)弦涌编辑器未开启，这个菜单项无效。");
    }

    //重命名
    static void Rename()
    {
        if (CheckIfSFEditorSceneIsOpen())
        {
            if (Selection.objects.Length == 1)
                EditorApplication.ExecuteMenuItem("Edit/Rename");
            else
            {
                SF_VibratorRenameWindow window = SF_VibratorRenameWindow.Open(Selection.objects);
#if UNITY_STANDALONE_WIN          
                window.position = new Rect(Screen.width / 2, Screen.height / 2, 185, 10);
#elif UNITY_STANDALONE_OSX
                window.position = new Rect(Screen.width / 2, Screen.height / 2, 210, 75);
#endif
                window.ShowModalUtility();
            }
        }
        else
            Debug.LogWarning("(SF_Editor)弦涌编辑器未开启，这个菜单项无效。");
    }

    //删除
    private static void Delete()
    {
        if (CheckIfSFEditorSceneIsOpen())
        {
            EditorApplication.ExecuteMenuItem("Edit/Delete");
        Debug.Log("(SF_Editor)已删除。");
        }
        else
            Debug.LogWarning("(SF_Editor)弦涌编辑器未开启，这个菜单项无效。");
    }

    //窗口化
    private static void Check()
    {
        if (CheckIfSFEditorSceneIsOpen())
        {
            EditorApplication.ExecuteMenuItem("Assets/Properties...");
            Debug.Log("(SF_Editor)打开振动器独立窗口。");
        }
        else
            Debug.LogWarning("(SF_Editor)弦涌编辑器未开启，这个菜单项无效。");
    }


    //在主流插入振动器
    private static void CreateVibratorObjectOnCurrentMainStream()
    {
        // 创建一个空的Vibrator
        GameObject newVibrator = new GameObject("new Vibrator");

        // 添加所需的脚本
        newVibrator.AddComponent<VibrationUnit>();
        newVibrator.AddComponent<RectTransform>();

        //判断是否打开编辑器
        if (!CheckIfSFEditorSceneIsOpen())
        {
            Debug.LogWarning("SF_Editor没有打开，无法创建振动器。");
            DestroyImmediate(newVibrator);
            return;
        }

        //添加到选中对象同级
        GameObject[] currentSelected = Selection.gameObjects;
        newVibrator.transform.SetParent(currentSelected[currentSelected.Length - 1].transform.parent);
        newVibrator.transform.SetSiblingIndex(currentSelected[currentSelected.Length - 1].transform.GetSiblingIndex()+ 1); // 设置物体newVibrator的位置在当前物体之后
        Debug.Log("(SF_Editor)已自动将Vibrator创建到选中的主流当中。");

        SceneManager.SetActiveScene(SF_EditorRef);

        // 设置锚点和偏移
        RectTransform r = newVibrator.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0f, 0f); // 锚点的最小值
        r.anchorMax = new Vector2(1f, 1f); // 锚点的最大值
        r.offsetMin = new Vector2(0f, 0f); // 下、左偏移
        r.offsetMax = Vector2.zero; // 上、右偏移为0
        r.anchoredPosition = new Vector2(5f, 0f);
        r.hideFlags = HideFlags.HideInInspector;

        //设置Icon
        var iconContent = EditorGUIUtility.IconContent("Wave通电");
        EditorGUIUtility.SetIconForObject(newVibrator, (Texture2D)iconContent.image);

        // 高亮显示新的Vibrator
        EditorGUIUtility.PingObject(newVibrator);
        Selection.activeGameObject = newVibrator;

        // 将newVibrator设置为新创建的物体
        Undo.RegisterCreatedObjectUndo(newVibrator, "Create " + newVibrator.name);
    }


    //在支流创建振动器
    [MenuItem("GameObject/StringsFlow/新振动器", false, 3)]
    private static void CreateVibratorObjectOnSF_Editor(MenuCommand menuCommand)
    {
        // 创建一个空的Vibrator
        GameObject newVibrator = new GameObject("new Vibrator");

        // 添加所需的脚本
        newVibrator.AddComponent<VibrationUnit>();
        newVibrator.AddComponent<RectTransform>();

        //判断是否打开编辑器
        if (!CheckIfSFEditorSceneIsOpen())
        {
            Debug.LogWarning("SF_Editor没有打开，无法创建振动器。");
            DestroyImmediate(newVibrator);
            return;
        }

        //添加到选中对象子级或者添加到VibrationBasin主分支的最下层
        SceneManager.SetActiveScene(SF_EditorRef);
        GameObject[] currentSelected = Selection.gameObjects;
        if (currentSelected == null || currentSelected.Length == 0 ||
            currentSelected[0].scene != SF_EditorRef || currentSelected[currentSelected.Length - 1].scene != SF_EditorRef
            || CheckTypeOnParentTrunk(typeof(VibrationBasin), currentSelected[0]) == null
            || CheckTypeOnParentTrunk(typeof(VibrationBasin), currentSelected[currentSelected.Length - 1]) == null)
        {
            newVibrator.transform.SetParent(VibrationBasinRef);
            Debug.Log("(SF_Editor)已自动将Vibrator创建在VibrationBasin当中。");
        }
        else
        {   newVibrator.transform.SetParent(currentSelected[currentSelected.Length - 1].transform);
            Selection.activeGameObject = newVibrator;
        }

        // 设置锚点和偏移
        RectTransform r = newVibrator.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0f, 0f); // 锚点的最小值
        r.anchorMax = new Vector2(1f, 1f); // 锚点的最大值
        r.offsetMin = new Vector2(0f, 0f); // 下、左偏移
        r.offsetMax = Vector2.zero; // 上、右偏移为0
        r.anchoredPosition = new Vector2(5f, 0f);
        r.hideFlags = HideFlags.HideInInspector;

        //设置Icon
        var iconContent = EditorGUIUtility.IconContent("Wave通电");
        EditorGUIUtility.SetIconForObject(newVibrator, (Texture2D)iconContent.image);

        // 高亮显示新的Vibrator
        EditorGUIUtility.PingObject(newVibrator);

        // 将newVibrator设置为新创建的物体
        Undo.RegisterCreatedObjectUndo(newVibrator, "Create " + newVibrator.name);
    }
    private static void CreateVibratorObjectOnSF_Editor()
    {
        // 创建一个空的Vibrator
        GameObject newVibrator = new GameObject("new Vibrator");

        // 添加所需的脚本
        newVibrator.AddComponent<VibrationUnit>();
        newVibrator.AddComponent<RectTransform>();

        //判断是否打开编辑器
        if (!CheckIfSFEditorSceneIsOpen())
        {
            Debug.LogWarning("SF_Editor没有打开，无法创建振动器。");
            DestroyImmediate(newVibrator);
            return;
        }

        //添加到选中对象子级或者添加到VibrationBasin主分支的最下层
        SceneManager.SetActiveScene(SF_EditorRef);
        GameObject[] currentSelected = Selection.gameObjects;
        if (currentSelected == null || currentSelected.Length == 0 ||
            currentSelected[0].scene != SF_EditorRef || currentSelected[currentSelected.Length - 1].scene != SF_EditorRef
            || CheckTypeOnParentTrunk(typeof(VibrationBasin), currentSelected[0]) == null
            || CheckTypeOnParentTrunk(typeof(VibrationBasin), currentSelected[currentSelected.Length - 1]) == null)
        {
            newVibrator.transform.SetParent(VibrationBasinRef);
            Debug.Log("(SF_Editor)已自动将Vibrator创建在VibrationBasin当中。");
        }
        else
        {   newVibrator.transform.SetParent(currentSelected[currentSelected.Length - 1].transform);
            Selection.activeGameObject = newVibrator;
        }

        // 设置锚点和偏移
        RectTransform r = newVibrator.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0f, 0f); // 锚点的最小值
        r.anchorMax = new Vector2(1f, 1f); // 锚点的最大值
        r.offsetMin = new Vector2(0f, 0f); // 下、左偏移
        r.offsetMax = Vector2.zero; // 上、右偏移为0
        r.anchoredPosition = new Vector2(5f, 0f);
        r.hideFlags = HideFlags.HideInInspector;

        //设置Icon
        var iconContent = EditorGUIUtility.IconContent("Wave通电");
        EditorGUIUtility.SetIconForObject(newVibrator, (Texture2D)iconContent.image);

        // 高亮显示新的Vibrator
        EditorGUIUtility.PingObject(newVibrator);

        // 将newVibrator设置为新创建的物体
        Undo.RegisterCreatedObjectUndo(newVibrator, "Create " + newVibrator.name);
    }

    public  static bool CheckIfSFEditorSceneIsOpen()
    {
        bool isOpen = false;
        //// 获取当前打开的所有场景
        Scene[] scenes = new Scene[SceneManager.sceneCount];
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            scenes[i] = SceneManager.GetSceneAt(i);
        }
        foreach (var scene in scenes) // 遍历所有场景
        {
            if (scene.name.Contains("SF_Editor") && scene.isLoaded)
            {
                isOpen = true;
                SF_EditorRef = scene;
                //检查场景下第一个物体
                VibrationBasinRef = scene.GetRootGameObjects()[0].transform.Find("VibrationBasin")?.transform;
                LayoutRef = scene.GetRootGameObjects()[0].transform.Find("Layout")?.transform;
                if (VibrationBasinRef == null)
                    Debug.LogWarning("(SF_Editor)VibrationBasin位置有误或丢失，请检查。");
                if (LayoutRef == null)
                    Debug.LogWarning("(SF_Editor)Layout对象的位置有误或丢失，请检查。");   
                return isOpen;
            }
        }
        return isOpen;
    }

    // 递归检索所有父物体寻找是否有某种类型
    static GameObject CheckTypeOnParentTrunk(Type type,GameObject currentObject)
    {
        // 检查当前物体是否有目标脚本
        if (currentObject.GetComponent(type) != null)
        {
            return currentObject;
        }
        // 递归查找父物体
        if (currentObject.transform.parent != null)
        {
            return CheckTypeOnParentTrunk(type, currentObject.transform.parent.gameObject);
        }
        // 如果已经到达顶层仍未找到，返回空
        return null;
    }


    ///保存振动单元的布局
    static void SaveCurrentVibrationUnit(Scene scene, string path) {
        if(VibrationBasinComponent?.CurrentUnit)
        {
            string VUSPath = "Assets/Bundles/Narration/NarrationEditor/VUS/" + VibrationBasinComponent.CurrentUnit.name + ".unity";
            // 检查指定路径的场景是否存在
            bool sceneExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(VUSPath) != null;
            //if (sceneExists)
            //{
            //    Scene VUS = EditorSceneManager.OpenScene(VUSPath, OpenSceneMode.AdditiveWithoutLoading);
            //    Transform Stage2D = LayoutRef.Find("2DStage");
            //    Transform Stage3D = LayoutRef.Find("3DStage");
            //    for (int i = Stage2D.childCount - 1; i >= 0; i--)
            //    {
            //        Transform child = Stage2D.GetChild(i);

            //        // 在新场景中复制物体
            //        if (child.CompareTag("SFLayout"))
            //        {
            //            SceneManager.MoveGameObjectToScene(child.gameObject, VUS);
            //        }
            //    }
            //    for (int i = Stage3D.childCount - 1; i >= 0; i--)
            //    {
            //        Transform child = Stage3D.GetChild(i);
            //        if (child.CompareTag("SFLayout"))
            //        {
            //            SceneManager.MoveGameObjectToScene(child.gameObject, VUS);
            //        }
            //    }
            //    EditorSceneManager.SaveScene(VUS);
            //    EditorSceneManager.CloseScene(VUS, true);
            //}
            //else
            //{
            //    // 如果场景不存在，则创建一个新场景
            //    Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            //    if (newScene.IsValid())
            //    {
            //        Transform Stage2D = LayoutRef.Find("2DStage");
            //        Transform Stage3D = LayoutRef.Find("3DStage");
            //        for (int i = Stage2D.childCount - 1; i >= 0; i--)
            //        {
            //            Transform child = Stage2D.GetChild(i);
            //            // 在新场景中复制物体
            //            if (child.CompareTag("SFLayout"))
            //            {
            //                SceneManager.MoveGameObjectToScene(child.gameObject, newScene);
            //            }
            //        }
            //        for (int i = Stage3D.childCount - 1; i >= 0; i--)
            //        {
            //            Transform child = Stage3D.GetChild(i);
            //            if (child.CompareTag("SFLayout"))
            //            {
            //                SceneManager.MoveGameObjectToScene(child.gameObject, newScene);
            //            }
            //        }
            //        EditorSceneManager.SaveScene(newScene, VUSPath);
            //    }
            //}
        }
    }

    //更新运行时场景 
    public static void UpdateStringsFlowingScene() {
        // 如果目标场景已经存在，则删除原有的场景
        if (AssetDatabase.LoadAssetAtPath(stringsFlowingPath, typeof(UnityEngine.Object)) != null)
        {
            AssetDatabase.DeleteAsset(stringsFlowingPath);
            AssetDatabase.Refresh(); // 刷新资源数据库，使其在编辑器中立即生效
        }
        // 复制场景
        AssetDatabase.CopyAsset(editorPath, stringsFlowingPath);
        AssetDatabase.Refresh(); // 刷新资源数据库，使其在编辑器中立即生效

        Debug.Log("(SF_Editor)已生成StringsFlowing场景!");
    }
}
