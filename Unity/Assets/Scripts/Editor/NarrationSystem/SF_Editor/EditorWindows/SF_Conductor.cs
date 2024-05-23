using ET.Client;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEngine;

public class SF_Conductor : EditorWindow
{
    public static SF_Conductor window;

    private Vector2 scrollPosition;

    private static GameObject selectedGameObject;

    private static SerializedObject serializedVibrationUnit;
    
    private static ReorderableList energyFlowReorderableList;


    const string DefaultTitle = "SF_Conductor";


    [MenuItem("Window/SF窗口/SF_Conductor", false,int.MaxValue-2)]
    public static void Init()
    {
        window = (SF_Conductor)GetWindow(typeof(SF_Conductor));
        window.titleContent = UpdateWindowTitle.UpdateAs(DefaultTitle, "乐队", DefaultTitle);

    }

    // Update is called once per frame
    void Update()
    {
        if (!SF_MenuItems.isEditorOpened)
            this.Close();
    }

    void OnEnable()
    {
        Undo.undoRedoPerformed += Repaint; // 当 Undo 操作发生时刷新编辑器界面。
        Selection.selectionChanged += ()=>RepaintSelected(SF_EditorMouseClickSelection.GetSF_EditorGameObjectSelectedMark());
    }

    void OnDisable()
    {
        Undo.undoRedoPerformed -= Repaint;
        Selection.selectionChanged -= () => RepaintSelected(SF_EditorMouseClickSelection.GetSF_EditorGameObjectSelectedMark());
    }

    private void OnFocus()
    {

    }



    bool canDraw;
    VibrationUnit vu;
    VibrationBasin basin;
    void RepaintSelected(GameObject obj)
    {
        if (SF_MenuItems.CheckIfSFEditorSceneIsOpen())
        {
            canDraw = true;

            selectedGameObject = obj;
            if (obj != null)
                titleContent = UpdateWindowTitle.UpdateAs(ET.StringTruncater.Truncate(" > " + selectedGameObject.name, 20, true),"乐队", DefaultTitle);
            else
                titleContent = UpdateWindowTitle.UpdateAs(DefaultTitle,"乐队", DefaultTitle);

            if(selectedGameObject)
            {
                VibrationUnit newVU = selectedGameObject.GetComponent<VibrationUnit>();
                if (newVU)
                    vu = newVU;
                basin = selectedGameObject.GetComponent<VibrationBasin>();

                if (vu)
                {
                    serializedVibrationUnit = new SerializedObject(vu);
                    SerializedProperty energyFlowProperty = serializedVibrationUnit?.FindProperty("EnergyFlow");
                    if (energyFlowProperty == null)
                    {
                        Debug.LogError("(SF_Editor)获取energyFlowProperty失败");
                        return;
                    }

                    // 初始化
                    energyFlowReorderableList = new ReorderableList(serializedVibrationUnit, energyFlowProperty, true, true, true, true);
                    //下面这个不能用，它适合给自定义Inspector而不是EditorWindow
                    //energyFlowReorderableList = ReorderableList.GetReorderableListFromSerializedProperty(energyFlowProperty);
                    
                    // 定义列表元素的高度
                    energyFlowReorderableList.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                   
                    float vuSpace = 15;
                    float vuWidth = 68;
                    float intSpace = 7;
                    float intWidth = 32;
                    float energySpace = 30;

                    //显示标头
                    energyFlowReorderableList.drawHeaderCallback = rect =>
                    {
                        rect.x += 12;
                        EditorGUI.LabelField(rect, "序号");
                        rect.x += 33;
                        EditorGUI.LabelField(rect, "我的内容");
                        rect.x -= 45;
                        rect.x += (rect.width - energySpace - vuWidth - vuSpace - intWidth - intSpace) + energySpace+ vuSpace - 2;
                        EditorGUI.LabelField(rect, "跳转到 ↓");
                    };

                    energyFlowReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                    {
                        if(selectedGameObject)
                        {
                            var element = energyFlowReorderableList.serializedProperty.GetArrayElementAtIndex(index);
                            rect.y += EditorGUIUtility.standardVerticalSpacing;

                            float energyWidth = rect.width - energySpace - vuWidth - vuSpace - intWidth - intSpace;

                            Rect energyRect = new Rect(rect.x + energySpace, rect.y, energyWidth, EditorGUIUtility.singleLineHeight);
                            Rect vuRect = new Rect(rect.x + energySpace + energyWidth + vuSpace, rect.y, vuWidth, EditorGUIUtility.singleLineHeight);
                            Rect intRect = new Rect(rect.x + energySpace + energyWidth + vuSpace + vuWidth + intSpace, rect.y, intWidth, EditorGUIUtility.singleLineHeight);
                            Rect vuAndIntRect = new Rect(rect.x + energySpace + energyWidth + vuSpace, rect.y, vuWidth + intWidth + intSpace, EditorGUIUtility.singleLineHeight);
                            
                            if (index < energyFlowProperty?.arraySize)
                            {
                                if (vu.EnergyFlow[index].EnergyOfNarration!=null&& vu.EnergyFlow[index].EnergyOfNarration.isContinuous)
                                    //显示索引
                                    EditorGUI.LabelField(new Rect(rect.x + 4, rect.y, rect.width - 5, EditorGUIUtility.singleLineHeight), index.ToString(), new GUIStyle() { normal = new GUIStyleState() { textColor = Color.yellow } });
                                else
                                    //显示索引
                                    EditorGUI.LabelField(new Rect(rect.x + 4, rect.y, rect.width - 5, EditorGUIUtility.singleLineHeight), index.ToString());

                                // 显示跳转
                                SerializedProperty vibrationUnit = element?.FindPropertyRelative("GoTo");
                                
                                if (vibrationUnit.objectReferenceValue != null)
                                {
                                    EditorGUI.PropertyField(vuRect, vibrationUnit, GUIContent.none, true);
                                    // 显示 int 值(跳转位置)
                                    SerializedProperty indexValue = element?.FindPropertyRelative("Index");
                                    EditorGUI.PropertyField(intRect, indexValue, GUIContent.none, true);
                                }
                                else
                                    EditorGUI.PropertyField(vuAndIntRect, vibrationUnit, GUIContent.none, true);

                                // 显示 EnergyOfNarration
                                SerializedProperty energyOfNarration = element?.FindPropertyRelative("EnergyOfNarration");
                                EditorGUI.PropertyField(energyRect, energyOfNarration, GUIContent.none, true);
                                
                            };
                        }
                    
                    };

                    energyFlowReorderableList.onSelectCallback = (ReorderableList list) =>
                    {
                        if (vu?.EnergyFlow[list.index].EnergyOfNarration!=null)
                        {
                            EditorGUIUtility.PingObject(vu.EnergyFlow[list.index].EnergyOfNarration);
                            if (vu.EnergyFlow[list.index].GoTo)
                                Debug.Log("(SF_Editor)" + "一共" + vu.EnergyFlow.Count + "个能量。当前 : " + vu.EnergyFlow[list.index].EnergyOfNarration.name + ";   跳转到 :" + vu.EnergyFlow[list.index].GoTo.name + " 的 " + vu.EnergyFlow[list.index].Index);
                            else
                                Debug.Log("(SF_Editor)" + "一共" + vu.EnergyFlow.Count + "个能量。当前 : " + vu.EnergyFlow[list.index].EnergyOfNarration.name);
                        }
                        else
                        {
                            Debug.Log("(SF_Editor)" + "当前Energy为空");
                        }
                    };

                    energyFlowReorderableList.onReorderCallback = (ReorderableList list) =>
                    {

                    };

                    energyFlowReorderableList.onChangedCallback = (ReorderableList list) =>
                    {

                    };

                    energyFlowReorderableList.onMouseDragCallback = (ReorderableList list) =>
                    {
                   
                    };

                    energyFlowReorderableList.onMouseUpCallback = (ReorderableList list) =>
                    {
      
                    };
                }
                else if (basin)
                {

                }
            }    

            Repaint();
            Debug.Log("(SF_Editor)已更新SF_Conductor窗口到选定物体。");
        }
        else
            canDraw = false;
    }

    private UnityEngine.Object myObjectDragged;
    bool showFlow = true;
    public static Color lightblue = new Color(0.96f, 0.98f, 1f);
    public static Color darkBlueColor = new Color(0.55f, 0.58f, 0.62f);
    void OnGUI()
    {
        if (canDraw)
        {
            GUI.backgroundColor = lightblue;

            // 检测鼠标点击事件
            Event ev = Event.current;
            if (ev.type == EventType.MouseDown && ev.button == 0)
            {
                // 结束输入，如果点击了窗口以外的位置
                if (!GUILayoutUtility.GetLastRect().Contains(ev.mousePosition))
                {
                    GUI.FocusControl(null);
                    Repaint();
                }
            }

            // 检测键盘事件
            if (ev.isKey && ev.keyCode == KeyCode.Return)
            {
                GUI.FocusControl(null);
                Repaint();
            }


            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar); // 开始水平布局

            GUILayout.FlexibleSpace(); // 将按钮推到右侧

            if(SF_EditorMouseClickSelection.SF_EditorGameObjectSelectedStack[0]!=null)
            {
                if (GUILayout.Button(new GUIContent(ET.StringTruncater.Truncate("<< 倒回 : " + SF_EditorMouseClickSelection.SF_EditorGameObjectSelectedStack[0].name, 21, true)),
     EditorStyles.toolbarButton, GUILayout.Width(150), GUILayout.Height(25)))
                {
                    if (SF_EditorMouseClickSelection.SF_EditorGameObjectSelectedStack[0] != null)
                    {
                        selectedGameObject = SF_EditorMouseClickSelection.SF_EditorGameObjectSelectedStack[0];
                        SF_EditorMouseClickSelection.SF_EditorGameObjectSelectedStack[0] = SF_EditorMouseClickSelection.SF_EditorGameObjectSelectedStack[1];
                        SF_EditorMouseClickSelection.SF_EditorGameObjectSelectedStack[1] = selectedGameObject;
                        SF_EditorMouseClickSelection.UnloadUnit();
                        SF_EditorMouseClickSelection.LoadUnit();
                        RepaintSelected(selectedGameObject);
                        Debug.Log("(SF_Editor)已倒回。");
                    }
                }
            }
            EditorGUILayout.EndHorizontal(); // 结束水平布局


            /// 检查拖放事件
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                    {
                        myObjectDragged = draggedObject;
                        Debug.Log("draggedObject :" + myObjectDragged.name);
                        break; // 如果有多个对象拖动进来，仅获取第一个
                    }
                    if(myObjectDragged is CharacterConfigOfNarration)
                    {
                        CharacterConfigOfNarration dragged = (CharacterConfigOfNarration)myObjectDragged;
                        selectedGameObject.GetComponent<VibrationUnit>()?.CharacterList.Add(dragged);
                    }
                }

                currentEvent.Use(); // 标记事件已处理
            }

            /// 开始绘制滚动区域的内容
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, alwaysShowHorizontal: false, alwaysShowVertical: false);

            if (selectedGameObject)
            {
                EditorGUI.BeginChangeCheck();
                SerializedProperty iterator = serializedVibrationUnit?.GetIterator();
                if (iterator != null)
                {
                    bool enterChildren = true;
                    iterator.NextVisible(enterChildren);//直接跳过第一个绘制,因为第一个是脚本引用,不需要在SF_Vast里公开显示

                    if(vu)
                        Undo.RecordObject(vu, "振动单元运行内容发生改变。");
                    while (iterator.NextVisible(enterChildren))
                    {
                        if (iterator.name == "EnergyFlow")
                        {
                            EditorGUILayout.LabelField("二、运行", EditorStyles.boldLabel);
                            GUILayout.Space(5f);
                            showFlow = EditorGUILayout.BeginFoldoutHeaderGroup(showFlow, "EnergyFlow");
                            if (showFlow && serializedVibrationUnit != null && energyFlowReorderableList != null)
                            {
                                GUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth - 21));
                                energyFlowReorderableList.DoLayoutList();
                                GUILayout.EndVertical();
                            }
                            EditorGUILayout.EndFoldoutHeaderGroup();
                        }
                        else
                        {
                            GUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth - 21));
                            // 检查属性是否带有 HideInStirngsFlowEditorButShowInInspectorAttribute 标签
                            bool shallHide = AttributeHelper.PropertyHasAttribute<HideInStirngsFlowEditorButShowInInspector>(iterator);
                            if (!shallHide)
                                EditorGUILayout.PropertyField(iterator, true);
                            if (iterator.name == "UnitSynopsis")
                            {
                                // 画一条水平线作为分割
                                GUILayout.Space(10f);
                                Rect lineRect = EditorGUILayout.GetControlRect(false, 1f);
                                EditorGUI.DrawRect(lineRect, Color.black);
                            }
                            else if (iterator.name == "Link")
                            {
                                // 使用toolbar样式开始水平布局
                                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                                GUILayout.FlexibleSpace(); // 将按钮推到右侧
                                GUILayout.Space(1);
                                if (GUILayout.Button(new GUIContent(ET.StringTruncater.Truncate("Enter", 21, true)),
                                        EditorStyles.toolbarButton, GUILayout.Width(50), GUILayout.Height(25)))
                                {
                                    if(vu)
                                    Application.OpenURL(vu.Link);
                                    else
                                    {
                                        
                                    }
                                }
                                EditorGUILayout.EndHorizontal(); // 结束水平布局
                            }
                            else if (iterator.name == "SpaceList")
                            {
                                
                            }
                            else if (iterator.name == "CharacterList")
                            {
                                
                            }

                            GUILayout.EndVertical();
                        }
                        enterChildren = false;
                    }
                }
                if (EditorGUI.EndChangeCheck())
                {
                    serializedVibrationUnit.ApplyModifiedProperties();
                }              
            }
            EditorGUILayout.EndScrollView(); // 结束滚动区域

            //GUI.backgroundColor = darkBlueColor; // 设置按钮的背景颜色为深蓝
            //if (GUILayout.Button(new GUIContent(" 导出", EditorGUIUtility.IconContent("螺丝扭").image), GUILayout.Width(EditorGUIUtility.currentViewWidth - 19), GUILayout.Height(20)))
            //{

            //}
            //GUI.backgroundColor = lightblue; // 恢复默认背景颜色
        }
    
        // 使用toolbar样式开始水平布局
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        // 计算窗口宽度并在中间显示文本
        float titleWidth = GUI.skin.label.CalcSize(new GUIContent("SF_Conductor")).x;
        float windowWidth = position.width;
        
        GUILayout.Space(10); // 空格，使文本居中显示
        GUILayout.FlexibleSpace();
        GUILayout.Label("SF_Conductor", GUILayout.Width(titleWidth), GUILayout.Height(14));
        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal(); // 结束水平布局
        GUI.backgroundColor = Color.white; // 恢复默认背景颜色
    }

    private void UpdateElementOrders()
    {
        //elementOrders.Clear();

        //SerializedProperty array = customList.serializedProperty;
        //for (int i = 0; i < array.arraySize; i++)
        //{
        //    SerializedProperty element = array.GetArrayElementAtIndex(i);
        //    int id = element.prefabOverride ? element.prefabOverrideIndex : element.propertyPath.GetHashCode();
        //    elementOrders[id] = i; // 记录元素的唯一标识和其在列表中的索引
        //}
    }

    // 创建纯色贴图
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}

public class UpdateWindowTitle
{
    public static GUIContent UpdateAs(string newTitle, string iconName, string tooltip = "")
    {
        GUIContent titleContent = new GUIContent(newTitle);
        titleContent.image = EditorGUIUtility.IconContent(iconName).image; // 设置窗口图标
        titleContent.tooltip = tooltip; // 设置窗口标题的工具提示

        return titleContent;
    }
}

//[CustomEditor(typeof(VibrationUnit))]
//public class VibrationUnitEditor : Editor
//{
//    public override void OnInspectorGUI()
//    {
//        serializedObject.Update();

//        // 隐藏当前被编辑对象的 Inspector
//        EditorGUIUtility.fieldWidth = 0;

//        serializedObject.ApplyModifiedProperties();
//    }

//    public static void DisplayEnergyFlowUnits(SerializedProperty energyFlowUnits)
//    {

//        // 获取当前视图的宽度
//        float fullWidth = EditorGUIUtility.currentViewWidth;

//        // 计算每个字段应该占据的宽度（这里示意性地均分）
//        float gameObjectWidth = 68f;
//        float space1 = 10;
//        float intWidth = 32f;
//        float space2 = 15;
//        float energyOfNarrationWidth = fullWidth - gameObjectWidth - intWidth - 2 * space1 - 2 * space2 - 10;


//        GUILayout.Space(8);
//        GUILayout.BeginArea(new Rect(energyOfNarrationWidth + 37, 20, 200, 100)); // 设置一个区域来放置标签
//        GUILayout.Label("goto");
//        GUILayout.EndArea();

//        for (int index = 0; index < energyFlowUnits.arraySize; index++)
//        {
//            SerializedProperty energyFlowUnit = energyFlowUnits.GetArrayElementAtIndex(index);

//            EditorGUILayout.BeginHorizontal();

//            // 显示序号
//            EditorGUILayout.SelectableLabel(index.ToString(), GUILayout.Width(20));

//            Rect energyRect = GUILayoutUtility.GetRect(0, float.MaxValue, EditorGUIUtility.singleLineHeight + 2, EditorGUIUtility.singleLineHeight + 2);
//            energyRect.width = energyOfNarrationWidth;
//            Rect objectRect = GUILayoutUtility.GetRect(0, float.MaxValue, EditorGUIUtility.singleLineHeight + 2, EditorGUIUtility.singleLineHeight + 2);
//            objectRect.width = gameObjectWidth;
//            objectRect.x += energyOfNarrationWidth + space1; // 偏移量，使其相邻
//            Rect intRect = GUILayoutUtility.GetRect(0, float.MaxValue, EditorGUIUtility.singleLineHeight + 2, EditorGUIUtility.singleLineHeight + 2);
//            intRect.width = intWidth;
//            intRect.x += energyOfNarrationWidth + gameObjectWidth + space2; // 偏移量，使其相邻

//            // 显示 EnergyOfNarration
//            SerializedProperty energyOfNarration = energyFlowUnit.FindPropertyRelative("EnergyOfNarration");
//            EditorGUI.PropertyField(energyRect, energyOfNarration, GUIContent.none, true);

//            // 显示 VibrationUnit
//            SerializedProperty vibrationUnit = energyFlowUnit.FindPropertyRelative("GoTo");
//            EditorGUI.PropertyField(objectRect, vibrationUnit, GUIContent.none, true);

//            // 显示 int 值
//            SerializedProperty intValue = energyFlowUnit.FindPropertyRelative("Index");
//            EditorGUI.PropertyField(intRect, intValue, GUIContent.none, true);

//            EditorGUILayout.EndHorizontal();

//            GUILayout.Space(3);
//        }

//        //EditorGUILayout.EndToggleGroup();
//    }

//    //绘制除了指定名称之外的所有属性
//    public static void DrawDefaultPropertiesExcluding(SerializedObject obj, params string[] exclusions)
//    {
//        SerializedProperty iterator = obj.GetIterator();
//        bool enterChildren = true;

//        while (iterator.NextVisible(enterChildren))
//        {
//            enterChildren = false;

//            if (exclusions != null && exclusions.Length > 0 && System.Array.Exists(exclusions, element => iterator.name == element))
//                continue;

//            EditorGUILayout.PropertyField(iterator, true);
//        }
//    }
//}