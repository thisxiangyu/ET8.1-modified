using ET;
using ET.Client;
using SF_Dialog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using static UnityEditor.Progress;

public partial class SF_Vast : EditorWindow
{
    Texture2D lockIcon;

    bool isPageLocked;

    private Vector2 scrollPosition;

    public static UnityEngine.Object selectedObject;

    public static SF_Vast window;

    GUIStyle MemoTextAreaStyle;
    GUIStyle disabledTextFieldStyle;

    public static readonly Color darkblue = new Color(0.57f, 0.59f, 0.65f);
    public static readonly Color lightGreen = new Color(0.6f, 0.96f, 0.6f); // 自定义浅绿色
    public static readonly Color lightlightGreen = new Color(0.94f, 1f, 1f);
    public static readonly Color weakGreen = new Color(0.92f, 1f, 0.92f);
    public static readonly Color weaklightGreen = new Color(0.87f, 0.97f, 0.87f);
    public static readonly Color lightYellow = new Color(1f, 1f, 0.61f); // 自定义浅黄色

    public static string[] 存档位;
    private int 存档选项索引 = 0;

    [MenuItem("Window/SF窗口/SF_Vast",false,int.MaxValue-3)]
    public static void Init()
    {
        window = (SF_Vast)GetWindow(typeof(SF_Vast));
        window.titleContent = UpdateWindowTitle.UpdateAs("SF_Vast", "Wave3", "SF_Vast");
    }

    // Update is called once per frame
    void Update()
    {        
        if (!SF_MenuItems.isEditorOpened)
            this.Close();
    }


    private void Awake()
    {
        isPageLocked = false;
        lockIcon = EditorGUIUtility.IconContent("解锁").image as Texture2D;
    }

    void OnEnable()
    {
        if (!EditorApplication.isPlaying)
        {
            //随机滚动名言警句
            ScrollingMaximMaker.lastUpdateTime = (float)EditorApplication.timeSinceStartup;
            EditorApplication.update += ScrollingMaximMaker.DoScrolling;
            
            // 使得对列表操作支持 Undo
            Undo.undoRedoPerformed += Repaint; // 当 Undo 操作发生时刷新编辑器界面
            
            鸽子image = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor Default Resources/Icons/鸽子.png");

            ///初始化各种资源
            string[] Guids = AssetDatabase.FindAssets("t:ObjectConfigOfNarration"); // 获取所有的CharacterConfigOfNarration资源路径
            foreach (string guid in Guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                ObjectConfigOfNarration config = AssetDatabase.LoadAssetAtPath<ObjectConfigOfNarration>(assetPath);
                config?.EditorInitDisplayName(); //确保数据不为空并且初始化显示名
            }
            string[] StringGuids = AssetDatabase.FindAssets("t:StringOfNarration");
            foreach (string guid in StringGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                StringOfNarration String = AssetDatabase.LoadAssetAtPath<StringOfNarration>(assetPath);
                if (String != null)
                {
                    string[] lines = String.Memo.Split('\n');
                    for (int i = 0; i < 70 - lines.Length; i++) //设置Memo内容始终保持在70行
                    { String.Memo = String.Memo + "\n"; }
                }
            }
            Selection.selectionChanged += () => RepaintSelected(SF_EditorMouseClickSelection.GetSF_EditorAssetsSelectedMark());   
            SceneView.duringSceneGui += OnSceneGUI;
        }
    }

    void OnDisable()
    {
        // 移除监听
        SceneView.duringSceneGui -= OnSceneGUI;
        Selection.selectionChanged -= () => RepaintSelected(SF_EditorMouseClickSelection.GetSF_EditorAssetsSelectedMark());
        Undo.undoRedoPerformed -= Repaint;
        EditorApplication.update -= ScrollingMaximMaker.DoScrolling;
    }



    bool canDraw = false;
    private static SerializedObject serializedObj;
    private static ReorderableList conditionReorderableList;

    string[] configTargetOptions = new string[] { " の", "幸运值形","幸运附属形" };

    string[] targetOptions = new string[] { "已预备", "已退出", "已激活", "已闲置", " の" , "分化于", "存在任意值形", "存在任意附属形" };
    string[] theLastTargetOptions = new string[] { "已预备", "已退出", "已激活", "已闲置", "分化于" };
    string[] numberJudgmentOptions = new string[] { "等于", "大于", "小于", "大于或等于", "小于或等于", "不等于" };
    string[] strJudgmentOptions = new string[] { "吻合", "无法吻合", "包含"};
    string[] numberAndStrJudgmentOptions = new string[] { "吻合", "无法吻合", "包含", "大于", "小于", "大于或等于", "小于或等于" };

    const int 支持获取的附属深度 = 5;  //这个值设定为5,那么就支持获取5次附属

    //两个维度，一个维度是每项条件的多个选项，另一个维度是有多少个
    int[][] targetJudgementOptionsIndex = new int[支持获取的附属深度+1][]; 
    int[][] itemValueOptionsIndex = new int[支持获取的附属深度][];

    int[] numberJudgmentOptionsIndex;
    int[] strJudgmentOptionsIndex;

    private static ReorderableList freeSettingsReorderableList;

    SF_DataTypeModifyWindow dtmw;
    SF_ItemCommonalityModifyWindow icmw;

    bool dtmwPositionShallSet = false;
    bool icmwPositionShallSet = false;

    public static bool shallDataTypeModify;
    public static bool shallCommonItemModify;
    public static int modifyingDataTypeIndex;
    public static int modifyingCommonalityIndex;

    private Texture2D 鸽子image;

    bool reoerderableListIsDragging;

    void RepaintSelected(UnityEngine.Object obj)
    {
        if (!isPageLocked)
        {
            MemoTextAreaStyle = new GUIStyle(EditorStyles.textField);
            MemoTextAreaStyle.fontSize = 13; // 设置文字大小
            MemoTextAreaStyle.normal.textColor = Color.white; // 设置文字颜色
            MemoTextAreaStyle.hover.textColor = Color.white;
            MemoTextAreaStyle.focused.textColor = Color.white;
            MemoTextAreaStyle.active.textColor = Color.white;

            // 创建一个样式，使文本框看起来不可编辑
            Color disabledColor = new Color(0.39f, 0.39f, 0.42f);
            disabledTextFieldStyle = new GUIStyle(EditorStyles.textField);
            disabledTextFieldStyle.normal.textColor = disabledColor;
            disabledTextFieldStyle.hover.textColor = disabledColor; // 悬停时文本颜色为灰色
            disabledTextFieldStyle.active.textColor = disabledColor; // 激活时文本颜色为灰色
            disabledTextFieldStyle.focused.textColor = disabledColor; // 获得焦点时文本颜色为灰色
            disabledTextFieldStyle.hover.background = disabledTextFieldStyle.normal.background; // 禁用悬停背景。

            if (SF_MenuItems.CheckIfSFEditorSceneIsOpen() && SF_MenuItems.isEditorOpened)
            {
                初始化存档位();
                
                canDraw = true;
               
                selectedObject = obj;

                if (selectedObject is StringOfNarration)
                {
                    if (selectedObject is EnergyOfNarration)
                    {
                        serializedObj = new SerializedObject(selectedObject);
                        SerializedProperty surgeConditionProperty = serializedObj?.FindProperty("SurgeCondition");

                        if (surgeConditionProperty == null)
                        {
                            Debug.LogError("(SF_Editor)获取surgeConditionProperty失败");
                            return;
                        }
                        else
                            Debug.Log("(SF_Editor)获取surgeConditionProperty成功");

                        // 初始化
                        conditionReorderableList = new ReorderableList(serializedObj, surgeConditionProperty, true, false, true, true);
                        for (int i = 0; i < targetJudgementOptionsIndex.Length; i++)
                            targetJudgementOptionsIndex[i] = new int[surgeConditionProperty.arraySize];

                        numberJudgmentOptionsIndex = new int[surgeConditionProperty.arraySize];
                        strJudgmentOptionsIndex = new int[surgeConditionProperty.arraySize];

                        for (int i = 0; i < itemValueOptionsIndex.Length; i++)
                            itemValueOptionsIndex[i] = new int[surgeConditionProperty.arraySize];

                        // 定义列表元素的高度
                        conditionReorderableList.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                        conditionReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                        {
                            float labelWidth = 35;

                            float vuWidth = 68;
                            float numberJudgmentWidth = 48;
                            float numberWidth = 140;

                            float Space = 5.5f;
                            float targetWidth = 135;
                            float targetOptionsWidth = 60;

                            float valueOptionsWidth = 105;

                            float andOrWidth = 50;

                            //条件判断语句的各项SerializedProperty
                            SerializedProperty element = conditionReorderableList.serializedProperty.GetArrayElementAtIndex(index);
                            SerializedProperty conditionValueProperty = element?.FindPropertyRelative("ConditionValue");
                            SerializedProperty andOrProperty = element?.FindPropertyRelative("AndOr");
                            SerializedProperty rootOriginConfigProperty = element?.FindPropertyRelative("RootOriginConfig");

                            rect.y += EditorGUIUtility.standardVerticalSpacing;

                            //显示label
                            EditorGUI.LabelField(new Rect(rect.x + Space, rect.y, labelWidth, EditorGUIUtility.singleLineHeight), "条件" + (index + 1).ToString());

                            if (index < surgeConditionProperty?.arraySize && selectedObject is EnergyOfNarration)
                            {
                                Rect targetRect = new Rect(rect.x + Space + labelWidth + Space, rect.y, targetWidth, EditorGUIUtility.singleLineHeight);
                                // 显示 Target
                                SerializedProperty target = element?.FindPropertyRelative("Target");
                                EditorGUI.PropertyField(targetRect, target, GUIContent.none, true);

                                EnergySurgeCondition surgeCondition = (selectedObject as EnergyOfNarration).SurgeCondition[index];
                                
                                //初始化surgeCondition.Judgment和TargetValueName的数量
                                //(最后一个+1的Judgment用来是用来收尾的)
                                if (surgeCondition.Judgment.Length != 支持获取的附属深度 + 1)
                                    surgeCondition.Judgment = new EnumSFConditionalJudgment[支持获取的附属深度 + 1];
                                if (surgeCondition.TargetValueName.Length != 支持获取的附属深度)
                                    surgeCondition.TargetValueName = new string[支持获取的附属深度];

                                if (surgeCondition.Target != null)
                                {  //开始绘制
                                    DrawConditionContent(surgeCondition, index, rect, targetOptionsWidth, Space, labelWidth, targetWidth, 
                                        numberWidth, valueOptionsWidth, numberJudgmentWidth, conditionValueProperty, ConditionBeginIndex, rootOriginConfigProperty);
                                }
                                serializedObj?.ApplyModifiedProperties();
                            };

                            //显示AndOr
                            if (andOrProperty.enumValueIndex == 2)//如果是SurgeBegin，需要扩大宽度
                            { andOrWidth += 44; GUI.backgroundColor = weaklightGreen; }
                            EditorGUI.PropertyField(new Rect(rect.width + 22 - andOrWidth, rect.y, andOrWidth, EditorGUIUtility.singleLineHeight), andOrProperty, GUIContent.none);
                            GUI.backgroundColor = SF_Conductor.lightblue; // 恢复默认背景颜色
                        };

                        conditionReorderableList.onSelectCallback = (ReorderableList list) =>
                        {
                            EnergySurgeCondition surgeCondition = (selectedObject as EnergyOfNarration).SurgeCondition[list.index];
                            if (surgeCondition.Target)
                            {
                                if (surgeCondition.TargetValueName[0] != null)
                                    Debug.Log("(SF_Editor)" + "一共" + (selectedObject as EnergyOfNarration).SurgeCondition.Count + "个条件。当前 : Target:" + surgeCondition.Target.name + ";   Values :" + string.Join(", ", surgeCondition.TargetValueName) + ";   Judgments :" + string.Join(", ", surgeCondition.Judgment) + "\n     ConditionValue :" + surgeCondition.ConditionValue + ";   AndOr:" + surgeCondition.AndOr + ";    RootOrigin :" + surgeCondition.RootOriginConfig?.name);
                                else
                                    Debug.Log("(SF_Editor)" + "一共" + (selectedObject as EnergyOfNarration).SurgeCondition.Count + "个条件。当前 : Target:" + surgeCondition.Target.name + ";   Values :" + string.Join(", ", surgeCondition.TargetValueName) + ";   Judgments :" + string.Join(", ", surgeCondition.Judgment) + "\n     ConditionValue :" + surgeCondition.ConditionValue + ";   AndOr:" + surgeCondition.AndOr + ";    RootOrigin :" + surgeCondition.RootOriginConfig?.name);
                            }
                            else
                            {
                                Debug.Log("(SF_Editor)No Target.");
                            }
                        };

                        conditionReorderableList.onAddCallback = (ReorderableList list) =>
                        {
                            surgeConditionProperty.InsertArrayElementAtIndex(list.count);

                            serializedObj?.ApplyModifiedProperties();
                        };

                        conditionReorderableList.onChangedCallback = (ReorderableList list) =>
                        {
                            Undo.RecordObject(this, "条件数量发生改变");
                            for (int i = 0; i < targetJudgementOptionsIndex.Length; i++)
                                targetJudgementOptionsIndex[i] = new int[surgeConditionProperty.arraySize];

                            numberJudgmentOptionsIndex = new int[surgeConditionProperty.arraySize];
                            strJudgmentOptionsIndex = new int[surgeConditionProperty.arraySize];

                            for (int i = 0; i < itemValueOptionsIndex.Length; i++)
                                itemValueOptionsIndex[i] = new int[surgeConditionProperty.arraySize];

                            serializedObj?.ApplyModifiedProperties();
                        };
                    }
                    else if (selectedObject is ConfigOfNarration)
                    {
                        serializedObj = new SerializedObject(selectedObject);
                        SerializedProperty itemsProperty = serializedObj?.FindProperty("items");

                        freeSettingsReorderableList = new ReorderableList(serializedObj, itemsProperty, true, false, true, true);


                        float dataAuthorityTypeWidth = 85;
                        float labelWidth = 6;
                        float dataTypeWidth = 65;
                        float Space = 5.5f;

                        //元素删除、增加、调换排序的时候会调用(修改元素值不会调用)
                        freeSettingsReorderableList.onChangedCallback = (ReorderableList list) =>
                        {

                        };

                        freeSettingsReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                        {
                            if (selectedObject is ObjectConfigOfNarration)
                            {
                                ((ObjectConfigOfNarration)selectedObject).EditorInitDisplayName();
                                EditorUtility.SetDirty(selectedObject);
                            }

                            if (index < itemsProperty.arraySize && itemsProperty.arraySize != 0)
                            {
                                SerializedProperty currentItemProperty = itemsProperty.GetArrayElementAtIndex(index);

                                SerializedProperty itemNameProperty = currentItemProperty.FindPropertyRelative("itemName");
                                SerializedProperty dataAuthorityTypeProperty = currentItemProperty.FindPropertyRelative("dataAuthorityType");
                                SerializedProperty depictionProperty = currentItemProperty.FindPropertyRelative("depiction");
                                SerializedProperty subsidiaryProperty = currentItemProperty.FindPropertyRelative("subsidiary");
                                SerializedProperty dataTypeProperty = currentItemProperty.FindPropertyRelative("dataType");
                                SerializedProperty isCommonProperty = currentItemProperty.FindPropertyRelative("isCommon");

                                Vector2 screenMousePosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

                                if (dataAuthorityTypeProperty.enumValueIndex == 0)
                                    GUI.backgroundColor = lightGreen;
                                else if (dataAuthorityTypeProperty.enumValueIndex == 1)
                                    GUI.backgroundColor = lightYellow;
                                else if (dataAuthorityTypeProperty.enumValueIndex == 2)
                                    GUI.backgroundColor = weakGreen;

                                //数据权限设置
                                EditorGUI.BeginChangeCheck();
                                EditorGUI.PropertyField(new Rect(Space + rect.x, rect.y + 2.1f, dataAuthorityTypeWidth, EditorGUIUtility.singleLineHeight + 2), dataAuthorityTypeProperty, GUIContent.none);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    ConfigOfNarration selectedConfig = (ConfigOfNarration)selectedObject;
                                    string mastersName = "";
                                    foreach (ConfigOfNarration master in selectedConfig.masters)
                                    {
                                        mastersName += master.name + "、";
                                    }
                                    //统一设置所有分化体的项数据权限类型
                                    selectedConfig.SetDataAuthorityTypeOfAllDivergiformsFromThisOriginAs(index, (EnumOfSFConfigDataAuthority)Enum.Parse(typeof(EnumOfSFConfigDataAuthority), dataAuthorityTypeProperty.enumValueIndex.ToString()));
                                    Debug.Log("(SF_Editor)已把 Master " + mastersName + "当中的对应项权限设置成" + (EnumOfSFConfigDataAuthority)Enum.Parse(typeof(EnumOfSFConfigDataAuthority), dataAuthorityTypeProperty.enumValueIndex.ToString()));
                                }

                                GUI.backgroundColor = lightlightGreen;
                                //项名设置
                                EditorGUI.BeginChangeCheck();
                                EditorGUI.DelayedTextField(new Rect(Space + rect.x + dataAuthorityTypeWidth + Space, rect.y + 1.5f, rect.width * 0.18f, EditorGUIUtility.singleLineHeight + 2), itemNameProperty, GUIContent.none);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    ConfigOfNarration selectedConfig = (ConfigOfNarration)selectedObject;
                                    //判断命名是否重复，如果重复则增加后缀"(重复)"
                                    CheckItemNameDuplicated(selectedConfig, itemNameProperty);
                                    //设置运行时数据的项名
                                    selectedConfig.items[index].RuntimeSubsidiaryRef.name = itemNameProperty.stringValue;
                                    Debug.Log("itemName :" + itemNameProperty.stringValue);
                                    //统一设置所有分化体的项名
                                    selectedConfig.SetItemNameOfAllDivergiformsFromThisOriginAs(index, itemNameProperty.stringValue);
                                    Debug.Log("RuntimeSubsidiaryRef :" + selectedConfig?.items[index].RuntimeSubsidiaryRef.name);
                                }
                                GUI.backgroundColor = SF_Conductor.lightblue; // 恢复默认背景颜色
                                EditorGUI.LabelField(new Rect(Space + rect.x + dataAuthorityTypeWidth + Space + rect.width * 0.18f + Space, rect.y + 1.5f, labelWidth, EditorGUIUtility.singleLineHeight + 2), "— ");
                                float valueX = Space + rect.x + dataAuthorityTypeWidth + Space + rect.width * 0.18f + Space + labelWidth + Space;

                                GUI.backgroundColor = lightGreen;
                                EditorGUI.BeginChangeCheck();
                                EditorGUI.PropertyField(new Rect(rect.width - dataTypeWidth, rect.y + 2.1f, dataTypeWidth, EditorGUIUtility.singleLineHeight + 2), dataTypeProperty, GUIContent.none);
                                // 在形式改变后显示确认对话框
                                if (EditorGUI.EndChangeCheck())
                                {
                                    modifyingDataTypeIndex = index;
                                    shallDataTypeModify = true;
                                    dtmw = SF_DataTypeModifyWindow.ShowWindow();
                                    EditorApplication.delayCall += () =>
                                    {
                                        ///在下一帧执行的逻辑(这里Unity怪怪的无法在EndChangeCheck的时候获取鼠标位置只能下一帧获取)
                                        dtmwPositionShallSet = true;
                                    };
                                }

                                if (dtmw != null && dtmwPositionShallSet)
                                {
#if UNITY_STANDALONE_WIN
                                    Rect windowRect = new Rect(screenMousePosition.x + 55, screenMousePosition.y - 125, 249, 8);
#elif UNITY_STANDALONE_OSX
                                Rect windowRect = new Rect(screenMousePosition.x-260, screenMousePosition.y-145, 249, 100);
#endif
                                    dtmw.position = windowRect;
                                    dtmwPositionShallSet = false; //只设置一次dtmw窗口位置
                                    EditorApplication.delayCall = null;
                                }

                                GUI.backgroundColor = SF_Conductor.lightblue; // 恢复默认背景颜色

                                //处理待修改类型的项
                                if (index == modifyingDataTypeIndex)
                                {
                                    if (dataTypeProperty.enumValueIndex == 0)
                                    {
                                        if (shallDataTypeModify) //等待确认
                                        {
                                            if (SF_DataTypeModifyWindow.didModify == DidModify.waiting)
                                                EditorGUI.LabelField(new Rect(valueX, rect.y + 1.5f, rect.width - valueX - dataTypeWidth - Space, EditorGUIUtility.singleLineHeight + 2), "项的数据形式变动中...");
                                            else if (SF_DataTypeModifyWindow.didModify == DidModify.yes)
                                            {
                                                ConfigOfNarration selected = ((ConfigOfNarration)selectedObject);
                                                //将附属分化体全部删除
                                                PreDivergeform(selected.items[index], selected);
                                                selected.items[index].RuntimeSubsidiaryRef = null;

                                                //记录修改
                                                ConfigOfNarration[] allForModify = selected.GetSelfAndAllDivergiforms();
                                                Undo.RecordObjects(allForModify, "源体" + this.name + "的所有分化体变动数据形式");
                                                //将所有分化体也修改成值形
                                                selected.SetConfigOfNarrationsItemsAs值形(allForSet: allForModify, itemIndex: index);

                                                shallDataTypeModify = false;
                                            }
                                            else if (SF_DataTypeModifyWindow.didModify == DidModify.no)
                                            {
                                                dataTypeProperty.enumValueIndex = 1; //取消修改,回到附属形
                                                shallDataTypeModify = false;
                                            }
                                        }
                                        else
                                        {
                                            modifyingDataTypeIndex = int.MaxValue;//退出类型修改
                                        }
                                    }
                                    else if (dataTypeProperty.enumValueIndex == 1)
                                    {
                                        if (shallDataTypeModify)//等待确认
                                        {
                                            if (SF_DataTypeModifyWindow.didModify == DidModify.waiting)
                                                EditorGUI.LabelField(new Rect(valueX, rect.y + 1.5f, rect.width - valueX - dataTypeWidth - Space, EditorGUIUtility.singleLineHeight + 2), "项的数据形式变动中...");
                                            else if (SF_DataTypeModifyWindow.didModify == DidModify.yes)
                                            {
                                                //记录修改
                                                ConfigOfNarration selected = ((ConfigOfNarration)selectedObject);
                                                ConfigOfNarration[] allForModify = selected.GetSelfAndAllDivergiforms();
                                                Undo.RecordObjects(allForModify, "源体" + this.name + "的所有分化体变动数据形式");
                                                //将所有分化体也修改成附属形
                                                selected.SetConfigOfNarrationsItemsAs附属形(allForSet: allForModify, itemIndex: index);

                                                shallDataTypeModify = false;
                                            }
                                            else if (SF_DataTypeModifyWindow.didModify == DidModify.no)
                                            {
                                                dataTypeProperty.enumValueIndex = 0;//取消修改,回到值形
                                                shallDataTypeModify = false;
                                            }
                                        }
                                        else
                                        {
                                            modifyingDataTypeIndex = int.MaxValue; //退出类型修改
                                        }
                                    }
                                }

                                else if (selectedObject is PureDataDesignItems && !((PureDataDesignItems)selectedObject).items[index].isCommon)
                                {
                                    EditorGUI.BeginDisabledGroup(true);  // 禁用字段编辑
                                    EditorGUI.LabelField(new Rect(valueX, rect.y + 1.5f, rect.width - valueX - dataTypeWidth - Space, EditorGUIUtility.singleLineHeight + 2), "(仅在分化中设置)", disabledTextFieldStyle);
                                    EditorGUI.EndDisabledGroup();  // 结束禁用字段编辑
                                }

                                //按照不同权限处理值形赋值
                                else if (dataTypeProperty.enumValueIndex == 0)
                                {
                                    if (dataAuthorityTypeProperty.enumValueIndex == 0)//权限为玩家存档时
                                    {
                                        EditorGUI.LabelField(new Rect(valueX, rect.y + 1.5f, 45, EditorGUIUtility.singleLineHeight + 2), "初值为");
                                        EditorGUI.PropertyField(new Rect(valueX + 45 + 3, rect.y + 1.5f, (rect.width - valueX - dataTypeWidth - Space) / 2 - 45, EditorGUIUtility.singleLineHeight + 2), depictionProperty, GUIContent.none);
                                        EditorGUI.LabelField(new Rect(valueX + (rect.width - valueX - dataTypeWidth - Space) / 2 + 7, rect.y + 1.5f, 95, EditorGUIUtility.singleLineHeight + 2), ";在当前存档中为");
                                        EditorGUI.TextField(new Rect(valueX + (rect.width - valueX - dataTypeWidth - Space) / 2 + 105, rect.y + 1.5f, (rect.width - valueX - dataTypeWidth - Space) / 2 - 103, EditorGUIUtility.singleLineHeight + 2), "  " + "(空)", disabledTextFieldStyle);
                                    }
                                    else if (dataAuthorityTypeProperty.enumValueIndex == 1)//权限为官方配置时
                                    {
                                        EditorGUI.PropertyField(new Rect(valueX, rect.y + 1.5f, rect.width - valueX - dataTypeWidth - Space, EditorGUIUtility.singleLineHeight + 2), depictionProperty, GUIContent.none);
                                    }
                                    else if (dataAuthorityTypeProperty.enumValueIndex == 2)//权限为暂存数据时
                                    {
                                        EditorGUI.LabelField(new Rect(valueX, rect.y + 1.5f, 45, EditorGUIUtility.singleLineHeight + 2), "刷新为");
                                        EditorGUI.PropertyField(new Rect(valueX + 45 + 3, rect.y + 1.5f, rect.width - valueX - dataTypeWidth - Space - 45, EditorGUIUtility.singleLineHeight + 2), depictionProperty, GUIContent.none);
                                    }
                                    subsidiaryProperty.objectReferenceValue = null;
                                }
                                //按照不同权限处理附属形赋值
                                else if (dataTypeProperty.enumValueIndex == 1)
                                {
                                    if (dataAuthorityTypeProperty.enumValueIndex == 0)//权限为玩家存档时
                                    {
                                        EditorGUI.LabelField(new Rect(valueX, rect.y + 1.5f, 45, EditorGUIUtility.singleLineHeight + 2), "初值为");
                                        EditorGUI.PropertyField(new Rect(valueX + 45 + 3, rect.y + 1.5f, (rect.width - valueX - dataTypeWidth - Space) / 2 - 45, EditorGUIUtility.singleLineHeight + 2), subsidiaryProperty, GUIContent.none);
                                        EditorGUI.LabelField(new Rect(valueX + (rect.width - valueX - dataTypeWidth - Space) / 2 + 7, rect.y + 1.5f, 95, EditorGUIUtility.singleLineHeight + 2), ";在当前存档中为");
                                        EditorGUI.TextField(new Rect(valueX + (rect.width - valueX - dataTypeWidth - Space) / 2 + 105, rect.y + 1.5f, (rect.width - valueX - dataTypeWidth - Space) / 2 - 103, EditorGUIUtility.singleLineHeight + 2), "  " + "(空)", disabledTextFieldStyle);
                                    }
                                    else if (dataAuthorityTypeProperty.enumValueIndex == 1)//权限为官方配置时
                                    {
                                        EditorGUI.PropertyField(new Rect(valueX, rect.y + 1.5f, rect.width - valueX - dataTypeWidth - Space, EditorGUIUtility.singleLineHeight + 2), subsidiaryProperty, GUIContent.none);
                                    }
                                    else if (dataAuthorityTypeProperty.enumValueIndex == 2)//权限为暂存数据时
                                    {
                                        EditorGUI.LabelField(new Rect(valueX, rect.y + 1.5f, 45, EditorGUIUtility.singleLineHeight + 2), "刷新为");
                                        EditorGUI.PropertyField(new Rect(valueX + 45 + 3, rect.y + 1.5f, rect.width - valueX - dataTypeWidth - Space - 45, EditorGUIUtility.singleLineHeight + 2), subsidiaryProperty, GUIContent.none);
                                    }
                                    depictionProperty.stringValue = default; //清空depiction

                                    // 画一条水平线作为分割
                                    Rect lineRect = EditorGUILayout.GetControlRect(false, 1f);
                                    EditorGUI.DrawRect(lineRect, Color.gray);
                                    EditorGUILayout.Space(8);

                                    ConfigOfNarration selected = (ConfigOfNarration)selectedObject;
                                    FreeDesignItem currentItem = selected?.items[index];
                                    if (currentItem.subsidiary != null)
                                    {
                                        ///分化
                                        ConfigOfNarration currentSubsidiary = currentItem.subsidiary;
                                        //检测是否为初次分化或者因currentItem.subsidiary改动而再次分化
                                        if (currentItem.RuntimeSubsidiaryRef == null || currentItem.RuntimeSubsidiaryRef.divergiform == null
                                        || currentItem.RuntimeSubsidiaryRef.divergiform.originConfig != currentItem.subsidiary)
                                        {
                                            //分化前删掉上一个分化体的资源
                                            PreDivergeform(currentItem, selected);
                                            //开始分化
                                            Subsidiary newSubsidiary = currentSubsidiary.Divergeform(Master: (ConfigOfNarration)selectedObject, Name: currentItem.itemName);
                                            if (newSubsidiary != null)
                                                currentItem.RuntimeSubsidiaryRef = newSubsidiary;

                                            if (currentItem.isCommon)//如果是共性项那么需要对所有分化体的对应项也进行处理
                                            {
                                                ConfigOfNarration[] allForSet = selected.GetSelfAndAllDivergiforms();
                                                selected.ChangeConfigOfNarrationsCommonItemsTo(allForSet, index, currentItem.subsidiary, allForSet.Length - 1);
                                            }

                                            currentSubsidiary.ClearUselessDivergiforms(); //清除未连接到Master的无用分化体
                                        }

                                        currentItem.RuntimeSubsidiaryRef.name = currentItem.itemName; //始终确保附属的项名符合设定
                                        EditorUtility.SetDirty(selectedObject);

                                        ConfigOfNarration divergiform = currentItem.RuntimeSubsidiaryRef.divergiform;
                                        //显示折叠标头
                                        currentItem.foldInVast = EditorGUILayout.BeginFoldoutHeaderGroup(currentItem.foldInVast, "     の " + itemNameProperty.stringValue);
                                        if (currentItem.foldInVast)
                                        {
                                            DrawDivergiforms(divergiform); ///绘制分化体当中的设定项
                                        }
                                        EditorGUILayout.Space(15);
                                        EditorGUILayout.EndFoldoutHeaderGroup();
                                    }
                                    else
                                    {
                                        //切换为空的时候删掉上一个分化体的资源
                                        if (currentItem.RuntimeSubsidiaryRef != null)
                                        {
                                            string p = AssetDatabase.GetAssetPath(currentItem.RuntimeSubsidiaryRef.divergiform);
                                            if (AssetDatabase.LoadAssetAtPath<ConfigOfNarration>(p))
                                                AssetDatabase.DeleteAsset(p);
                                            currentItem.RuntimeSubsidiaryRef = null;
                                        }
                                        EditorUtility.SetDirty(selectedObject);
                                        EditorGUILayout.LabelField("      の " + itemNameProperty.stringValue + " - (无)");
                                        EditorGUILayout.Space(8);
                                    }
                                }

                                //设置是否为共性项
                                EditorGUI.BeginChangeCheck();
                                EditorGUI.PropertyField(new Rect(rect.width + 3, rect.y + 2.1f, dataTypeWidth, EditorGUIUtility.singleLineHeight + 2), isCommonProperty, GUIContent.none);
                                // 在形式改变后显示确认对话框
                                if (EditorGUI.EndChangeCheck())
                                {
                                    modifyingCommonalityIndex = index;
                                    shallCommonItemModify = true;
                                    icmw = SF_ItemCommonalityModifyWindow.ShowWindow(isCommonProperty.boolValue);
                                    EditorApplication.delayCall += () =>
                                    {
                                        ///在下一帧执行的逻辑(这里Unity怪怪的无法在EndChangeCheck的时候获取鼠标位置只能下一帧获取)
                                        icmwPositionShallSet = true;
                                    };
                                }
                                if (icmw != null && icmwPositionShallSet)
                                {
#if UNITY_STANDALONE_WIN
                                    Rect windowRect = new Rect(screenMousePosition.x + 55, screenMousePosition.y - 125, 249, 8);
#elif UNITY_STANDALONE_OSX
                                Rect windowRect = new Rect(screenMousePosition.x - 256, screenMousePosition.y - 155, 249, 105);
#endif
                                    icmw.position = windowRect;
                                    icmwPositionShallSet = false; //只设置一次icmw窗口位置
                                    EditorApplication.delayCall = null;
                                }
                                //处理待设置共性的项
                                if (index == modifyingCommonalityIndex)
                                {
                                    if (shallCommonItemModify) //等待确认
                                    {
                                        if (SF_ItemCommonalityModifyWindow.didModify == DidModify.yes)//确认修改
                                        {
                                            ConfigOfNarration selected = ((ConfigOfNarration)selectedObject);
                                            //记录修改
                                            ConfigOfNarration[] allForModify = selected.GetSelfAndAllDivergiforms();
                                            Undo.RecordObjects(allForModify, "设置为共性项");

                                            //对于纯设定需要特殊的清空处理
                                            if (selected is PureDataDesignItems && isCommonProperty.boolValue == false)
                                            {
                                                selected.items[index].depiction = default;
                                                if (selected.items[index].RuntimeSubsidiaryRef != null)
                                                {
                                                    selected.items[index].subsidiary = null;
                                                    string p = AssetDatabase.GetAssetPath(selected.items[index].RuntimeSubsidiaryRef.divergiform);
                                                    if (AssetDatabase.LoadAssetAtPath<ConfigOfNarration>(p))
                                                        AssetDatabase.DeleteAsset(p);
                                                    for (int i = selected.mySubsidiarysS.Count - 1; i >= 0; i--)
                                                    {
                                                        if (selected.mySubsidiarysS[i].divergiform == null)
                                                            selected.mySubsidiarysS.Remove(selected.mySubsidiarysS[i]);
                                                    }
                                                    for (int i = selected.mySubsidiarysL.Count - 1; i >= 0; i--)
                                                    {
                                                        if (selected.mySubsidiarysL[i].divergiform == null)
                                                            selected.mySubsidiarysL.Remove(selected.mySubsidiarysL[i]);
                                                    }
                                                    selected.items[index].RuntimeSubsidiaryRef = null;
                                                }
                                            }

                                            //将所有分化体也设置共性项
                                            selected.SetConfigOfNarrationsItemsCommonality(allForModify, index, isCommonProperty.boolValue, allForModify.Length - 1);

                                            shallCommonItemModify = false;
                                        }
                                        else if (SF_ItemCommonalityModifyWindow.didModify == DidModify.no)//取消修改,回到此前状态
                                        {
                                            isCommonProperty.boolValue = !isCommonProperty.boolValue;
                                            shallCommonItemModify = false;
                                        }
                                    }
                                    else
                                    {
                                        modifyingCommonalityIndex = int.MaxValue;//退出
                                    }
                                }

                                //// 标记资源为已修改
                                //EditorUtility.SetDirty(selectedObject);

                                //// 保存修改
                                //AssetDatabase.SaveAssets();
                            }
                        };

                        freeSettingsReorderableList.onAddCallback = (ReorderableList list) => {
                            ConfigOfNarration selected = ((ConfigOfNarration)selectedObject);
                            ConfigOfNarration[] allForAdding = selected.GetSelfAndAllDivergiforms();
                            Undo.RecordObjects(allForAdding, "源体" + this.name + "和所有分化体同时添加");
                            string newName = "New Item";
                            int extension = 0;
                        add_depiction:
                            if (!selected.AddDepiction(newName, "Item Value"))
                            {
                                extension += 1;
                                newName = "New Item" + extension;
                                goto add_depiction;
                            }//添加默认内容,如果重复那么添加后缀
                            else //为分化体添加新项
                                selected.AddNewItemsForConfigOfNarrations(exceptIndex: allForAdding.Length - 1, name: newName, value: "Item Value", allForAdding: allForAdding);
                            EditorUtility.SetDirty(selectedObject);
                        };

                        freeSettingsReorderableList.onRemoveCallback = (ReorderableList list) => {
                            Undo.RecordObject(selectedObject, "Remove Element");
                            if (freeSettingsReorderableList.index >= 0)
                            {
                                //List删除元素的之前需要检测并删掉多余的分化体资源
                                FreeDesignItem currentItem = ((ConfigOfNarration)selectedObject)?.items[list.index];
                                string p = AssetDatabase.GetAssetPath(currentItem.RuntimeSubsidiaryRef.divergiform);
                                if (AssetDatabase.LoadAssetAtPath<ConfigOfNarration>(p))
                                    AssetDatabase.DeleteAsset(p);
                                //把为空的数据清除，保持mySubsidiarysS干净
                                ((ConfigOfNarration)selectedObject).mySubsidiarysS.Remove(currentItem.RuntimeSubsidiaryRef);
                                //删除指定元素
                                ((ConfigOfNarration)selectedObject).DeleteItemsOfAllDivergiformsAndThisOrigin(list.index);
                            }
                            EditorUtility.SetDirty(selectedObject);
                        };

                        int lastIndex = default;
                        freeSettingsReorderableList.onSelectCallback = (ReorderableList list) =>
                        {
                            string itemName = "";
                            foreach (var item in ((ConfigOfNarration)selectedObject).items)
                            {
                                itemName += item.itemName + "、";
                            }
                            Debug.Log("一共 :" + ((ConfigOfNarration)selectedObject).items.Count + "个Item, 当前 :" + ((ConfigOfNarration)selectedObject).items[list.index].itemName + "; 值形为 :" + ((ConfigOfNarration)selectedObject).items[list.index].depiction + "; 附属形为 :" + ((ConfigOfNarration)selectedObject).items[list.index].subsidiary?.name);
                            lastIndex = list.index;
                        };

                        freeSettingsReorderableList.onMouseDragCallback = (ReorderableList list) =>
                        {
                            reoerderableListIsDragging = true;
                        };

                        freeSettingsReorderableList.onReorderCallback = (ReorderableList list) =>
                        {
                            reoerderableListIsDragging = false;
                            ConfigOfNarration selected = ((ConfigOfNarration)selectedObject);
                            ConfigOfNarration[] allForReordering = selected.GetSelfAndAllDivergiforms();
                            Undo.RecordObjects(allForReordering, "将源体" + this.name + "和所有分化体的第" + lastIndex + "项移动到" + list.index);
                            selected.ReorderItemsForConfigOfNarrations(exceptIndex: allForReordering.Length - 1, allForReordering: allForReordering, lastIndex: lastIndex, nowIndex: list.index);
                        };

                        freeSettingsReorderableList.onMouseUpCallback = (ReorderableList list) =>
                        {
                            reoerderableListIsDragging = false;
                        };

                        ((ConfigOfNarration)selectedObject).UpdateMastersOfOrigin();
                    }
                }

                Repaint();
                Debug.Log("(SF_Editor)已更新SF_Conductor窗口到选定物体。");
            }
            else
                canDraw = false;
        }
    }

    //这个方法会递归绘制所有的分化体和更深层的分化体
    public void DrawDivergiforms(ConfigOfNarration divergiform , int level = 1)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        if (!reoerderableListIsDragging)
        {
            for (int i = 0; i < divergiform?.items?.Count; i++)
            {
                ///一行双项的排版方式
                if ((i + 1) % 2 != 0)
                {
                    GUILayout.Space(31); //在每行第一项前面增加一些空出来的空间
                }
                float singleItemWidth = (position.width - MemoTextWidth - 80) * 0.5f;
                if(!divergiform.items[i].isCommon)
                    EditorGUILayout.LabelField(ET.StringTruncater.Truncate(divergiform.items[i].itemName, 14, true) + " - ", GUILayout.Width(105));
                else
                    EditorGUILayout.LabelField(ET.StringTruncater.Truncate(divergiform.items[i].itemName, 14, true) + " = ", GUILayout.Width(105));

                // 标记已修改的项
                EditorGUI.BeginChangeCheck();
                // 在这里进行撤销操作的记录
                Undo.RecordObject(divergiform, "修改了分化体" + divergiform.name);
                //按照不同类型和权限处理
                if (divergiform.items[i].dataType == EnumOfSFType.值形 && !divergiform.items[i].isCommon)
                {
                    if (divergiform.items[i].dataAuthorityType == EnumOfSFConfigDataAuthority.玩家存档项)
                    {
                        divergiform.items[i].depiction = EditorGUILayout.TextField(divergiform.items[i].depiction, GUILayout.Width(singleItemWidth - 100 - 105));
                        EditorGUILayout.TextField("(空)", disabledTextFieldStyle, GUILayout.Width(100));
                    }
                    else if (divergiform.items[i].dataAuthorityType == EnumOfSFConfigDataAuthority.暂存数据)
                    {
                        EditorGUILayout.LabelField("(刷新为)", disabledTextFieldStyle, GUILayout.Width(58));
                        divergiform.items[i].depiction = EditorGUILayout.TextField(divergiform.items[i].depiction, GUILayout.Width(singleItemWidth - 105 - 58));
                    }
                    else
                        divergiform.items[i].depiction = EditorGUILayout.TextField(divergiform.items[i].depiction, GUILayout.Width(singleItemWidth - 105 + 1.9f));
                }
                else if (divergiform.items[i].dataType == EnumOfSFType.附属形 && !divergiform.items[i].isCommon)
                {
                    if (divergiform.items[i].dataAuthorityType == EnumOfSFConfigDataAuthority.玩家存档项)
                    {
                        EditorGUI.BeginChangeCheck();
                        divergiform.items[i].subsidiary = (ConfigOfNarration)EditorGUILayout.ObjectField(divergiform.items[i].subsidiary, typeof(ConfigOfNarration), false, GUILayout.Width(singleItemWidth - 100 - 105));
                        if (EditorGUI.EndChangeCheck())
                        {
                            PreDivergeform(divergiform.items[i], divergiform);
                            divergiform.items[i].RuntimeSubsidiaryRef = divergiform.items[i].subsidiary?.Divergeform(Master: divergiform, Name: divergiform.items[i].itemName);
                        }

                        EditorGUI.BeginDisabledGroup(true);  // 呈现存档数据 ,禁用字段编辑
                        divergiform.items[i].subsidiary = (ConfigOfNarration)EditorGUILayout.ObjectField(
                            divergiform.items[i].subsidiary,
                            typeof(ConfigOfNarration),
                            false,
                            GUILayout.Width(100)  // 设置宽度
                        );
                        EditorGUI.EndDisabledGroup();  // 结束禁用字段编辑
                    }
                    else if (divergiform.items[i].dataAuthorityType == EnumOfSFConfigDataAuthority.暂存数据)
                    {
                        EditorGUILayout.LabelField("(刷新为)", disabledTextFieldStyle, GUILayout.Width(58));
                        EditorGUI.BeginChangeCheck();
                        divergiform.items[i].subsidiary = (ConfigOfNarration)EditorGUILayout.ObjectField(divergiform.items[i].subsidiary, typeof(ConfigOfNarration), false, GUILayout.Width(singleItemWidth - 105 - 58));
                        if (EditorGUI.EndChangeCheck())
                        {
                            PreDivergeform(divergiform.items[i], divergiform);
                            divergiform.items[i].RuntimeSubsidiaryRef = divergiform.items[i].subsidiary.Divergeform(Master: divergiform, Name: divergiform.items[i].itemName);
                        }
                    }
                    else
                    {   EditorGUI.BeginChangeCheck(); divergiform.items[i].subsidiary = (ConfigOfNarration)EditorGUILayout.ObjectField(divergiform.items[i].subsidiary, typeof(ConfigOfNarration), false, GUILayout.Width(singleItemWidth - 105 + 1.9f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            PreDivergeform(divergiform.items[i], divergiform);
                            divergiform.items[i].RuntimeSubsidiaryRef = divergiform.items[i].subsidiary.Divergeform(Master: divergiform, Name: divergiform.items[i].itemName);
                        }
                    }
                }
                else if (divergiform.items[i].dataType == EnumOfSFType.值形 && divergiform.items[i].isCommon)
                {
                    if (divergiform.items[i].dataAuthorityType == EnumOfSFConfigDataAuthority.玩家存档项)
                    {
                        divergiform.items[i].depiction=EditorGUILayout.TextField(divergiform.FindRootOrigin().items[i].depiction, disabledTextFieldStyle, GUILayout.Width(singleItemWidth - 100 - 105));
                        EditorGUILayout.TextField("(空)", disabledTextFieldStyle, GUILayout.Width(100));
                    }
                    else if (divergiform.items[i].dataAuthorityType == EnumOfSFConfigDataAuthority.暂存数据)
                    {
                        EditorGUILayout.LabelField("(刷新为)", disabledTextFieldStyle, GUILayout.Width(58));
                        divergiform.items[i].depiction = EditorGUILayout.TextField(divergiform.FindRootOrigin().items[i].depiction, disabledTextFieldStyle, GUILayout.Width(singleItemWidth - 105 - 58));
                    }
                    else
                        divergiform.items[i].depiction = EditorGUILayout.TextField(divergiform.FindRootOrigin().items[i].depiction, disabledTextFieldStyle, GUILayout.Width(singleItemWidth - 105 + 1.9f));
                }
                else if (divergiform.items[i].dataType == EnumOfSFType.附属形 && divergiform.items[i].isCommon)
                {
                    if (divergiform.items[i].dataAuthorityType == EnumOfSFConfigDataAuthority.玩家存档项)
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        divergiform.items[i].subsidiary = (ConfigOfNarration)EditorGUILayout.ObjectField(
                            divergiform.FindRootOrigin().items[i].subsidiary,
                            typeof(ConfigOfNarration),
                            false,
                            GUILayout.Width(singleItemWidth - 100 - 105)
                        );
                        divergiform.items[i].subsidiary = (ConfigOfNarration)EditorGUILayout.ObjectField(
                            divergiform.FindRootOrigin().items[i].subsidiary,
                            typeof(ConfigOfNarration),
                            false,
                            GUILayout.Width(100) 
                        );
                        EditorGUI.EndDisabledGroup();
                    }
                    else if (divergiform.items[i].dataAuthorityType == EnumOfSFConfigDataAuthority.暂存数据)
                    {
                        EditorGUILayout.LabelField("(刷新为)", disabledTextFieldStyle, GUILayout.Width(58));
                        EditorGUI.BeginDisabledGroup(true);
                        divergiform.items[i].subsidiary = (ConfigOfNarration)EditorGUILayout.ObjectField(
                            divergiform.FindRootOrigin().items[i].subsidiary,
                            typeof(ConfigOfNarration),
                            false,
                            GUILayout.Width(singleItemWidth - 105 - 58)
                        );
                        EditorGUI.EndDisabledGroup(); 
                    }
                    else
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        divergiform.items[i].subsidiary = (ConfigOfNarration)EditorGUILayout.ObjectField(
                            divergiform.FindRootOrigin().items[i].subsidiary,
                            typeof(ConfigOfNarration),
                            false,
                            GUILayout.Width(singleItemWidth - 105 + 1.9f)
                        );
                        EditorGUI.EndDisabledGroup();  
                    }
                }
                // 标记为已修改，这会在任意保存的时候触发资源保存
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(divergiform);
                }

                // 每显示两项换一行
                if ((i + 1) % 2 == 0 && i != divergiform.items.Count - 1)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(18);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
                else
                {
                    GUILayout.Space(12);
                }

                ///每项一行的排版方式
                //GUILayout.Space(23);
                //EditorGUILayout.LabelField(divergiform.items[i].dataAuthorityType.ToString(), GUILayout.Width(65));
                //GUILayout.Space(6);
                //EditorGUILayout.LabelField(divergiform.items[i].itemName + " - ", GUILayout.Width((position.width - MemoTextWidth - 80) * 0.5f));
                //EditorGUILayout.Space(22);
                //EditorGUILayout.EndHorizontal();
                //EditorGUILayout.BeginHorizontal();            
            }
        }
        EditorGUILayout.EndHorizontal();
        if (!reoerderableListIsDragging)
        {
            //开始递归绘制子级别的分化体
            for (int i = 0; i < divergiform?.items?.Count; i++)
            {
                FreeDesignItem currentItem = divergiform.items[i];
                if (currentItem.subsidiary != null)
                {
                    ConfigOfNarration subDivergiform = currentItem.RuntimeSubsidiaryRef.divergiform;
                    EditorGUILayout.Space(15);
                    //结束上一个折叠标头
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    //绘制下一个折叠标头
                    string prefix = " の "; //标头前缀
                    //(为子层的项的标头空出更多的前缀空间，更好地表示这是子层的项)
                    for (int L = 0; L < level; L++)
                        prefix = "———" + prefix;
                    currentItem.foldInVast = EditorGUILayout.BeginFoldoutHeaderGroup(currentItem.foldInVast, "     "+ prefix + divergiform.items[i].itemName);
                    if (currentItem.foldInVast)
                        DrawDivergiforms(subDivergiform, level +1 );
                }
            }
        }      
    }

    private const int ConditionBeginIndex = 0;

    //这个方法会递归绘制整个条件判断语句
    void DrawConditionContent(EnergySurgeCondition surgeCondition,int index, Rect rect,float targetOptionsWidth,
       float Space,float labelWidth,float targetWidth,float numberWidth,float valueOptionsWidth,
       float numberJudgmentWidth, SerializedProperty conditionValueProperty , int conditionBeginIndex , SerializedProperty rootOriginConfigProperty)
    {
        GUI.backgroundColor = darkblue;

        if (surgeCondition.Judgment[conditionBeginIndex] == EnumSFConditionalJudgment.已预备)
        { targetJudgementOptionsIndex[conditionBeginIndex][index] = 0; targetOptionsWidth = 60; }
        else if (surgeCondition.Judgment[conditionBeginIndex] == EnumSFConditionalJudgment.已退出)
        { targetJudgementOptionsIndex[conditionBeginIndex][index] = 1; targetOptionsWidth = 60; }
        else if (surgeCondition.Judgment[conditionBeginIndex] == EnumSFConditionalJudgment.已激活)
        { targetJudgementOptionsIndex[conditionBeginIndex][index] = 2; targetOptionsWidth = 60; }
        else if (surgeCondition.Judgment[conditionBeginIndex] == EnumSFConditionalJudgment.已闲置)
        { targetJudgementOptionsIndex[conditionBeginIndex][index] = 3; targetOptionsWidth = 60; }
        else if (surgeCondition.Judgment[conditionBeginIndex] == EnumSFConditionalJudgment.存在设定项)
        { targetJudgementOptionsIndex[conditionBeginIndex][index] = 4; targetOptionsWidth = 40; }
        else if (surgeCondition.Judgment[conditionBeginIndex] == EnumSFConditionalJudgment.分化于)
        { targetJudgementOptionsIndex[conditionBeginIndex][index] = 5; targetOptionsWidth = 60; }
        else if (surgeCondition.Judgment[conditionBeginIndex] == EnumSFConditionalJudgment.存在任意值形)
        { targetJudgementOptionsIndex[conditionBeginIndex][index] = 6; targetOptionsWidth = 104; GUI.backgroundColor = lightGreen; }
        else if (surgeCondition.Judgment[conditionBeginIndex] == EnumSFConditionalJudgment.存在任意附属形)
        { targetJudgementOptionsIndex[conditionBeginIndex][index] = 7; targetOptionsWidth = 104; GUI.backgroundColor = lightGreen; }


        Rect targetOptionsRect = new (rect.x + Space + labelWidth + Space + targetWidth + Space + conditionBeginIndex*(40 + Space + 105 +Space), rect.y, targetOptionsWidth, EditorGUIUtility.singleLineHeight);

        targetJudgementOptionsIndex[conditionBeginIndex][index] = EditorGUI.Popup(targetOptionsRect, targetJudgementOptionsIndex[conditionBeginIndex][index], targetOptions);
        GUI.backgroundColor = SF_Conductor.lightblue; // 恢复默认背景颜色

        if (targetJudgementOptionsIndex[conditionBeginIndex][index] == 0)
        {   surgeCondition.Judgment[conditionBeginIndex] = EnumSFConditionalJudgment.已预备;
            for (int i = conditionBeginIndex; i < 支持获取的附属深度; i++)
                surgeCondition.TargetValueName[i] = "emp";
        }
        else if (targetJudgementOptionsIndex[conditionBeginIndex][index] == 1)
        { surgeCondition.Judgment[conditionBeginIndex] = EnumSFConditionalJudgment.已退出;
            for (int i = conditionBeginIndex; i < 支持获取的附属深度; i++)
                surgeCondition.TargetValueName[i] = "emp";
        }
        else if (targetJudgementOptionsIndex[conditionBeginIndex][index] == 2)
        { surgeCondition.Judgment[conditionBeginIndex] = EnumSFConditionalJudgment.已激活;
            for (int i = conditionBeginIndex; i < 支持获取的附属深度; i++)
                surgeCondition.TargetValueName[i] = "emp";
        }
        else if (targetJudgementOptionsIndex[conditionBeginIndex][index] == 3)
        { surgeCondition.Judgment[conditionBeginIndex] = EnumSFConditionalJudgment.已闲置;
            for (int i = conditionBeginIndex; i < 支持获取的附属深度; i++)
                surgeCondition.TargetValueName[i] = "emp";
        }
        else if (targetJudgementOptionsIndex[conditionBeginIndex][index] == 4)
        {
            surgeCondition.Judgment[conditionBeginIndex] = EnumSFConditionalJudgment.存在设定项;
            //显示TargetValueName下拉赋值框
            GUI.backgroundColor = lightGreen;

            //获取所有的值形和附属形的项名
            string[] allOptions = null;
            ConfigOfNarration currentTarget = null;

            //从分化体中获取当前真正的目标
            currentTarget = surgeCondition.Target;
            for (int i = 0; i < conditionBeginIndex; i++)
                currentTarget = currentTarget?.GetDivergiformByName(surgeCondition.TargetValueName[i]);

            //绘制所选目标的内部设定项
            if (currentTarget != null)
            {

                allOptions = currentTarget?.GetAllItemsNames().ToArray();
                string[] allOptionsWithExtension = new string[allOptions.Length]; //要让附属形带后缀
                for (int i = 0; i < allOptions.Length; i++)
                {
                    if (currentTarget.GetItemTypeByName(allOptions[i]) == EnumOfSFType.附属形)
                        allOptionsWithExtension[i] = allOptions[i] + " *";
                    else
                        allOptionsWithExtension[i] = allOptions[i];
                }

                if (surgeCondition.TargetValueName[conditionBeginIndex] == null)
                {
                    surgeCondition.TargetValueName[conditionBeginIndex] = allOptions[0];
                }
                //初始化显示TargetValueName
                else
                {
                    for (int i = 0; i < allOptions.Length; i++)
                    {
                        if (surgeCondition.TargetValueName[conditionBeginIndex] == allOptions[i])
                            itemValueOptionsIndex[conditionBeginIndex][index] = i;
                    }
                }

                Rect valueOptionsRect = new Rect(rect.x + Space + labelWidth + Space + targetWidth + Space + targetOptionsWidth + Space + conditionBeginIndex * (targetOptionsWidth + Space + valueOptionsWidth + Space), rect.y, valueOptionsWidth, EditorGUIUtility.singleLineHeight);

                EditorGUI.BeginChangeCheck();
                itemValueOptionsIndex[conditionBeginIndex][index] = EditorGUI.Popup(valueOptionsRect, itemValueOptionsIndex[conditionBeginIndex][index], allOptionsWithExtension);
                if (EditorGUI.EndChangeCheck())
                {
                    surgeCondition.TargetValueName[conditionBeginIndex] = allOptions[itemValueOptionsIndex[conditionBeginIndex][index]];
                    //当valueOptions变动的时候将后续的数据清空(重新设定)
                    for (int i = conditionBeginIndex + 1; i < 支持获取的附属深度; i++)
                        surgeCondition.TargetValueName[i] = "emp";
                    // 标记已修改并保存修改到 Asset 文件
                    EditorUtility.SetDirty(selectedObject);
                }

                GUI.backgroundColor = SF_Conductor.lightblue; // 恢复默认背景颜色                                       

                float? SFNumber = SF.Math.StringBecomeSFNumberHelper.Parse(currentTarget.GetDepictionByName(allOptionsWithExtension[itemValueOptionsIndex[conditionBeginIndex][index]]));

                #region 判断所选的下拉选项的类型，根据不同类型做下一步显示
                
                //判断所选是否是一个附属形,如果是,则显示子层选项
                if (surgeCondition.TargetValueName[conditionBeginIndex] != null && allOptionsWithExtension[itemValueOptionsIndex[conditionBeginIndex][index]].Contains(" *"))
                {
                    if (conditionBeginIndex + 1 < 支持获取的附属深度) //开始递归
                        DrawConditionContent(surgeCondition, index, rect, targetOptionsWidth, Space, labelWidth, targetWidth,  numberWidth,
                            valueOptionsWidth, numberJudgmentWidth, conditionValueProperty, conditionBeginIndex + 1, rootOriginConfigProperty);
                    else //如果达到了支持获取的附属深度，即最后一次条件判断，那么就不需要再判断更深一层的附属了
                    {
                        GUI.backgroundColor = darkblue;

                        targetOptionsWidth = 60;

                        if (surgeCondition.Judgment[支持获取的附属深度] == EnumSFConditionalJudgment.已预备)
                            targetJudgementOptionsIndex[支持获取的附属深度][index] = 0;
                        else if (surgeCondition.Judgment[支持获取的附属深度] == EnumSFConditionalJudgment.已退出)
                            targetJudgementOptionsIndex[支持获取的附属深度][index] = 1;
                        else if (surgeCondition.Judgment[支持获取的附属深度] == EnumSFConditionalJudgment.已激活)
                            targetJudgementOptionsIndex[支持获取的附属深度][index] = 2;
                        else if (surgeCondition.Judgment[支持获取的附属深度] == EnumSFConditionalJudgment.已闲置)
                            targetJudgementOptionsIndex[支持获取的附属深度][index] = 3;
                        else if (surgeCondition.Judgment[支持获取的附属深度] == EnumSFConditionalJudgment.分化于)
                            targetJudgementOptionsIndex[支持获取的附属深度][index] = 4;

                        Rect theLastTargetOptionsRect = new Rect(rect.x + Space + labelWidth + Space + targetWidth + Space + 支持获取的附属深度 * (40 + Space + 105 + Space), rect.y, targetOptionsWidth, EditorGUIUtility.singleLineHeight);

                        targetJudgementOptionsIndex[支持获取的附属深度][index] = EditorGUI.Popup(theLastTargetOptionsRect, targetJudgementOptionsIndex[支持获取的附属深度][index], theLastTargetOptions);
                        GUI.backgroundColor = SF_Conductor.lightblue; // 恢复默认背景颜色

                        if (targetJudgementOptionsIndex[支持获取的附属深度][index] == 0)
                            surgeCondition.Judgment[支持获取的附属深度] = EnumSFConditionalJudgment.已预备;
                        else if (targetJudgementOptionsIndex[支持获取的附属深度][index] == 1)
                            surgeCondition.Judgment[支持获取的附属深度] = EnumSFConditionalJudgment.已退出;
                        else if (targetJudgementOptionsIndex[支持获取的附属深度][index] == 2)
                            surgeCondition.Judgment[支持获取的附属深度] = EnumSFConditionalJudgment.已激活;
                        else if (targetJudgementOptionsIndex[支持获取的附属深度][index] == 3)
                            surgeCondition.Judgment[支持获取的附属深度] = EnumSFConditionalJudgment.已闲置;
                        else if (targetJudgementOptionsIndex[支持获取的附属深度][index] == 4)
                        {   surgeCondition.Judgment[支持获取的附属深度] = EnumSFConditionalJudgment.分化于;
                            Rect 分化于Rect = new Rect(theLastTargetOptionsRect.x + theLastTargetOptionsRect .width+Space, rect.y, numberWidth, EditorGUIUtility.singleLineHeight);
                            EditorGUI.PropertyField(分化于Rect, rootOriginConfigProperty, GUIContent.none);
                        }
                    }
                }
                //如果非附属,则根据数值和字符的不同来显示运算
                //如果是数字
                else if (surgeCondition.TargetValueName[0] != null && SFNumber != null)
                {
                    if (surgeCondition.Judgment[conditionBeginIndex + 1] == EnumSFConditionalJudgment.等于)
                        numberJudgmentOptionsIndex[index] = 0;
                    else if (surgeCondition.Judgment[conditionBeginIndex + 1] == EnumSFConditionalJudgment.大于)
                        numberJudgmentOptionsIndex[index] = 1;
                    else if (surgeCondition.Judgment[conditionBeginIndex + 1] == EnumSFConditionalJudgment.小于)
                        numberJudgmentOptionsIndex[index] = 2;
                    else if (surgeCondition.Judgment[conditionBeginIndex + 1] == EnumSFConditionalJudgment.大于或等于)
                    { numberJudgmentOptionsIndex[index] = 3; numberJudgmentWidth += 38; }
                    else if (surgeCondition.Judgment[conditionBeginIndex + 1] == EnumSFConditionalJudgment.小于或等于)
                    { numberJudgmentOptionsIndex[index] = 4; numberJudgmentWidth += 38; }
                    else if (surgeCondition.Judgment[conditionBeginIndex + 1] == EnumSFConditionalJudgment.不等于)
                    { numberJudgmentOptionsIndex[index] = 5; numberJudgmentWidth += 10; }

                    GUI.backgroundColor = darkblue;
                    Rect numberJudgmentRect = new Rect(rect.x + Space + labelWidth + Space + targetWidth + Space + (conditionBeginIndex + 1) * (40 + Space + 105 + Space), rect.y, numberJudgmentWidth, EditorGUIUtility.singleLineHeight);
                    numberJudgmentOptionsIndex[index] = EditorGUI.Popup(numberJudgmentRect, numberJudgmentOptionsIndex[index], numberJudgmentOptions);
                    GUI.backgroundColor = SF_Conductor.lightblue; // 恢复默认背景颜色      

                    if (numberJudgmentOptionsIndex[index] == 0)
                    { surgeCondition.Judgment[conditionBeginIndex + 1] = EnumSFConditionalJudgment.等于; }
                    else if (numberJudgmentOptionsIndex[index] == 1)
                    { surgeCondition.Judgment[conditionBeginIndex + 1] = EnumSFConditionalJudgment.大于; }
                    else if (numberJudgmentOptionsIndex[index] == 2)
                    { surgeCondition.Judgment[conditionBeginIndex + 1] = EnumSFConditionalJudgment.小于; }
                    else if (numberJudgmentOptionsIndex[index] == 3)
                    { surgeCondition.Judgment[conditionBeginIndex + 1] = EnumSFConditionalJudgment.大于或等于; }
                    else if (numberJudgmentOptionsIndex[index] == 4)
                    { surgeCondition.Judgment[conditionBeginIndex + 1] = EnumSFConditionalJudgment.小于或等于; }
                    else if (numberJudgmentOptionsIndex[index] == 5)
                    { surgeCondition.Judgment[conditionBeginIndex + 1] = EnumSFConditionalJudgment.不等于; }

                    Rect numberRect = new Rect(rect.x + Space + labelWidth + Space + targetWidth + Space + (conditionBeginIndex + 1) * (40 + Space + 105 + Space) + numberJudgmentWidth + Space, rect.y, numberWidth, EditorGUIUtility.singleLineHeight);
                    float ConditionValueFloat = 0;
                    float? f = SF.Math.StringBecomeSFNumberHelper.Parse(surgeCondition.ConditionValue);
                    if (f != null)
                        ConditionValueFloat = f.Value;
                    ConditionValueFloat = EditorGUI.FloatField(numberRect, ConditionValueFloat);
                    surgeCondition.ConditionValue = ConditionValueFloat.ToString();
                }
                //非数字(即字符)
                else
                {
                    if (surgeCondition.Judgment[conditionBeginIndex + 1] == EnumSFConditionalJudgment.吻合)
                        strJudgmentOptionsIndex[index] = 0;
                    else if (surgeCondition.Judgment[conditionBeginIndex + 1] == EnumSFConditionalJudgment.无法吻合)
                    { strJudgmentOptionsIndex[index] = 1; numberJudgmentWidth += 25; }
                    else if (surgeCondition.Judgment[conditionBeginIndex + 1] == EnumSFConditionalJudgment.包含)
                        strJudgmentOptionsIndex[index] = 2;

                    GUI.backgroundColor = darkblue;
                    Rect numberJudgmentRect = new Rect(rect.x + Space + labelWidth + Space + targetWidth + Space + (conditionBeginIndex + 1) * (40 + Space + 105 + Space), rect.y, numberJudgmentWidth, EditorGUIUtility.singleLineHeight);
                    strJudgmentOptionsIndex[index] = EditorGUI.Popup(numberJudgmentRect, strJudgmentOptionsIndex[index], strJudgmentOptions);
                    GUI.backgroundColor = SF_Conductor.lightblue; // 恢复默认背景颜色      

                    if (strJudgmentOptionsIndex[index] == 0)
                    { surgeCondition.Judgment[conditionBeginIndex + 1] = EnumSFConditionalJudgment.吻合; }
                    else if (strJudgmentOptionsIndex[index] == 1)
                    { surgeCondition.Judgment[conditionBeginIndex + 1] = EnumSFConditionalJudgment.无法吻合; }
                    else if (strJudgmentOptionsIndex[index] == 2)
                    { surgeCondition.Judgment[conditionBeginIndex + 1] = EnumSFConditionalJudgment.包含; }

                    Rect intRect = new Rect(rect.x + Space + labelWidth + Space + targetWidth + Space + (conditionBeginIndex + 1) * (40 + Space + 105 + Space) + numberJudgmentWidth + Space, rect.y, numberWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(intRect, conditionValueProperty, GUIContent.none);
                }
                
                #endregion
            }
            else
            {
                Rect valueOptionsRect = new (rect.x + Space + labelWidth + Space + targetWidth + Space + targetOptionsWidth + Space + conditionBeginIndex * (targetOptionsWidth + Space + valueOptionsWidth + Space), rect.y, valueOptionsWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(valueOptionsRect, "(空项)");
                GUI.backgroundColor = SF_Conductor.lightblue; // 恢复默认背景颜色
            }

        }
        else if (targetJudgementOptionsIndex[conditionBeginIndex][index] == 5)
        { surgeCondition.Judgment[conditionBeginIndex] = EnumSFConditionalJudgment.分化于;
            for (int i = conditionBeginIndex; i < 支持获取的附属深度; i++)
                surgeCondition.TargetValueName[i] = "emp";
            Rect 分化于Rect = new Rect(targetOptionsRect.x + targetOptionsRect.width + Space, rect.y, numberWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(分化于Rect, rootOriginConfigProperty, GUIContent.none);
        }
        else if (targetJudgementOptionsIndex[conditionBeginIndex][index] == 6)
        {   surgeCondition.Judgment[conditionBeginIndex] = EnumSFConditionalJudgment.存在任意值形;
            for (int i = conditionBeginIndex; i < 支持获取的附属深度; i++)
                surgeCondition.TargetValueName[i] = "emp";

            GUI.backgroundColor = darkblue;
            if (surgeCondition.Judgment[conditionBeginIndex+1] == EnumSFConditionalJudgment.吻合)
            { targetJudgementOptionsIndex[conditionBeginIndex+1][index] = 0; targetOptionsWidth = 50; }
            else if (surgeCondition.Judgment[conditionBeginIndex+1] == EnumSFConditionalJudgment.无法吻合)
            { targetJudgementOptionsIndex[conditionBeginIndex+1][index] = 1; targetOptionsWidth = 68; }
            else if (surgeCondition.Judgment[conditionBeginIndex+1] == EnumSFConditionalJudgment.包含)
            { targetJudgementOptionsIndex[conditionBeginIndex+1][index] = 2; targetOptionsWidth = 50; }
            else if (surgeCondition.Judgment[conditionBeginIndex+1] == EnumSFConditionalJudgment.大于)
            { targetJudgementOptionsIndex[conditionBeginIndex+1][index] = 3; targetOptionsWidth = 50; }
            else if (surgeCondition.Judgment[conditionBeginIndex+1] == EnumSFConditionalJudgment.小于)
            { targetJudgementOptionsIndex[conditionBeginIndex+1][index] = 4; targetOptionsWidth = 50; }
            else if (surgeCondition.Judgment[conditionBeginIndex + 1] == EnumSFConditionalJudgment.大于或等于)
            { targetJudgementOptionsIndex[conditionBeginIndex + 1][index] = 5; targetOptionsWidth = 80; }
            else if (surgeCondition.Judgment[conditionBeginIndex + 1] == EnumSFConditionalJudgment.小于或等于)
            { targetJudgementOptionsIndex[conditionBeginIndex + 1][index] = 6; targetOptionsWidth = 80; }
            else
            { targetJudgementOptionsIndex[conditionBeginIndex + 1][index] = 0; targetOptionsWidth = 60; }

            Rect 判断存在任意形Rect = new Rect(rect.x + Space + labelWidth + Space + targetWidth + Space + conditionBeginIndex * (40 + Space + 105 + Space)+ 104 +Space, rect.y, targetOptionsWidth, EditorGUIUtility.singleLineHeight);

            targetJudgementOptionsIndex[conditionBeginIndex + 1][index] = EditorGUI.Popup(判断存在任意形Rect, targetJudgementOptionsIndex[conditionBeginIndex+1][index], numberAndStrJudgmentOptions);
            GUI.backgroundColor = SF_Conductor.lightblue; // 恢复默认背景颜色

            Rect numberRect = new Rect(rect.x + Space + labelWidth + Space + targetWidth + Space + conditionBeginIndex * (40 + Space + 105 + Space) + 104 + Space + targetOptionsWidth + Space, rect.y, numberWidth, EditorGUIUtility.singleLineHeight);

            if (targetJudgementOptionsIndex[conditionBeginIndex+1][index] == 0)
            {
                surgeCondition.Judgment[conditionBeginIndex+1] = EnumSFConditionalJudgment.吻合;
                for (int i = conditionBeginIndex+1; i < 支持获取的附属深度; i++)
                    surgeCondition.TargetValueName[i] = "emp";

                EditorGUI.PropertyField(numberRect, conditionValueProperty, GUIContent.none);
            }
            else if (targetJudgementOptionsIndex[conditionBeginIndex+1][index] == 1)
            {
                surgeCondition.Judgment[conditionBeginIndex+1] = EnumSFConditionalJudgment.无法吻合;
                for (int i = conditionBeginIndex+1; i < 支持获取的附属深度; i++)
                    surgeCondition.TargetValueName[i] = "emp";

                EditorGUI.PropertyField(numberRect, conditionValueProperty,GUIContent.none);
            }
            else if (targetJudgementOptionsIndex[conditionBeginIndex+1][index] == 2)
            {
                surgeCondition.Judgment[conditionBeginIndex+1] = EnumSFConditionalJudgment.包含;
                for (int i = conditionBeginIndex+1; i < 支持获取的附属深度; i++)
                    surgeCondition.TargetValueName[i] = "emp";

                EditorGUI.PropertyField(numberRect, conditionValueProperty, GUIContent.none);
            }
            else if (targetJudgementOptionsIndex[conditionBeginIndex+1][index] == 3)
            {
                surgeCondition.Judgment[conditionBeginIndex+1] = EnumSFConditionalJudgment.大于;
                for (int i = conditionBeginIndex+1; i < 支持获取的附属深度; i++)
                    surgeCondition.TargetValueName[i] = "emp";

                float ConditionValueFloat = 0;
                float? f = SF.Math.StringBecomeSFNumberHelper.Parse(surgeCondition.ConditionValue);
                if (f != null)
                    ConditionValueFloat = f.Value;
                ConditionValueFloat = EditorGUI.FloatField(numberRect, ConditionValueFloat);
                surgeCondition.ConditionValue = ConditionValueFloat.ToString();
            }
            else if (targetJudgementOptionsIndex[conditionBeginIndex + 1][index] == 4)
            {
                surgeCondition.Judgment[conditionBeginIndex + 1] = EnumSFConditionalJudgment.小于;
                for (int i = conditionBeginIndex + 1; i < 支持获取的附属深度; i++)
                    surgeCondition.TargetValueName[i] = "emp";                                
                
                float ConditionValueFloat = 0;
                float? f = SF.Math.StringBecomeSFNumberHelper.Parse(surgeCondition.ConditionValue);
                if (f != null)
                    ConditionValueFloat = f.Value;
                ConditionValueFloat = EditorGUI.FloatField(numberRect, ConditionValueFloat);
                surgeCondition.ConditionValue = ConditionValueFloat.ToString();
            }
            else if (targetJudgementOptionsIndex[conditionBeginIndex + 1][index] == 5)
            {
                surgeCondition.Judgment[conditionBeginIndex + 1] = EnumSFConditionalJudgment.大于或等于;
                for (int i = conditionBeginIndex + 1; i < 支持获取的附属深度; i++)
                    surgeCondition.TargetValueName[i] = "emp";

                float ConditionValueFloat = 0;
                float? f = SF.Math.StringBecomeSFNumberHelper.Parse(surgeCondition.ConditionValue);
                if (f != null)
                    ConditionValueFloat = f.Value;
                ConditionValueFloat = EditorGUI.FloatField(numberRect, ConditionValueFloat);
                surgeCondition.ConditionValue = ConditionValueFloat.ToString();
            }
            else if (targetJudgementOptionsIndex[conditionBeginIndex + 1][index] == 6)
            {
                surgeCondition.Judgment[conditionBeginIndex + 1] = EnumSFConditionalJudgment.小于或等于;
                for (int i = conditionBeginIndex + 1; i < 支持获取的附属深度; i++)
                    surgeCondition.TargetValueName[i] = "emp";

                float ConditionValueFloat = 0;
                float? f = SF.Math.StringBecomeSFNumberHelper.Parse(surgeCondition.ConditionValue);
                if (f != null)
                    ConditionValueFloat = f.Value;
                ConditionValueFloat = EditorGUI.FloatField(numberRect, ConditionValueFloat);
                surgeCondition.ConditionValue = ConditionValueFloat.ToString();
            }
        }
        else if (targetJudgementOptionsIndex[conditionBeginIndex][index] == 7)
        { surgeCondition.Judgment[conditionBeginIndex] = EnumSFConditionalJudgment.存在任意附属形;
            for (int i = conditionBeginIndex; i < 支持获取的附属深度; i++)
                surgeCondition.TargetValueName[i] = "emp";

            GUI.backgroundColor = darkblue;
            if (surgeCondition.Judgment[conditionBeginIndex + 1] == EnumSFConditionalJudgment.已预备)
            { targetJudgementOptionsIndex[conditionBeginIndex + 1][index] = 0; targetOptionsWidth = 60; }
            else if (surgeCondition.Judgment[conditionBeginIndex + 1] == EnumSFConditionalJudgment.已退出)
            { targetJudgementOptionsIndex[conditionBeginIndex + 1][index] = 1; targetOptionsWidth = 60; }
            else if (surgeCondition.Judgment[conditionBeginIndex + 1] == EnumSFConditionalJudgment.已激活)
            { targetJudgementOptionsIndex[conditionBeginIndex + 1][index] = 2; targetOptionsWidth = 60; }
            else if (surgeCondition.Judgment[conditionBeginIndex + 1] == EnumSFConditionalJudgment.已闲置)
            { targetJudgementOptionsIndex[conditionBeginIndex + 1][index] = 3; targetOptionsWidth = 60; }
            else if (surgeCondition.Judgment[conditionBeginIndex + 1] == EnumSFConditionalJudgment.分化于)
            { targetJudgementOptionsIndex[conditionBeginIndex + 1][index] = 4; targetOptionsWidth = 60; }
            else
            { targetJudgementOptionsIndex[conditionBeginIndex + 1][index] = 0; targetOptionsWidth = 60; }

            Rect 判断存在任意形Rect = new Rect(rect.x + Space + labelWidth + Space + targetWidth + Space + conditionBeginIndex * (40 + Space + 105 + Space) + 104 + Space, rect.y, targetOptionsWidth, EditorGUIUtility.singleLineHeight);

            targetJudgementOptionsIndex[conditionBeginIndex + 1][index] = EditorGUI.Popup(判断存在任意形Rect, targetJudgementOptionsIndex[conditionBeginIndex + 1][index], theLastTargetOptions);
            GUI.backgroundColor = SF_Conductor.lightblue; // 恢复默认背景颜色

            if (targetJudgementOptionsIndex[conditionBeginIndex + 1][index] == 0)
            {
                surgeCondition.Judgment[conditionBeginIndex + 1] = EnumSFConditionalJudgment.已预备;
                for (int i = conditionBeginIndex + 1; i < 支持获取的附属深度; i++)
                    surgeCondition.TargetValueName[i] = "emp";
            }
            else if (targetJudgementOptionsIndex[conditionBeginIndex + 1][index] == 1)
            {
                surgeCondition.Judgment[conditionBeginIndex + 1] = EnumSFConditionalJudgment.已退出;
                for (int i = conditionBeginIndex + 1; i < 支持获取的附属深度; i++)
                    surgeCondition.TargetValueName[i] = "emp";
            }
            else if (targetJudgementOptionsIndex[conditionBeginIndex + 1][index] == 2)
            {
                surgeCondition.Judgment[conditionBeginIndex + 1] = EnumSFConditionalJudgment.已激活;
                for (int i = conditionBeginIndex + 1; i < 支持获取的附属深度; i++)
                    surgeCondition.TargetValueName[i] = "emp";
            }
            else if (targetJudgementOptionsIndex[conditionBeginIndex + 1][index] == 3)
            {
                surgeCondition.Judgment[conditionBeginIndex + 1] = EnumSFConditionalJudgment.已闲置;
                for (int i = conditionBeginIndex + 1; i < 支持获取的附属深度; i++)
                    surgeCondition.TargetValueName[i] = "emp";
            }
            else if (targetJudgementOptionsIndex[conditionBeginIndex + 1][index] == 4)
            {
                surgeCondition.Judgment[conditionBeginIndex + 1] = EnumSFConditionalJudgment.分化于;
                for (int i = conditionBeginIndex + 1; i < 支持获取的附属深度; i++)
                    surgeCondition.TargetValueName[i] = "emp";

                Rect 分化于Rect = new Rect(rect.x + Space + labelWidth + Space + targetWidth + Space + conditionBeginIndex * (40 + Space + 105 + Space) + 104 + Space + 60 + Space, rect.y, numberWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(分化于Rect, rootOriginConfigProperty,GUIContent.none);
            }
        }
        EditorUtility.SetDirty(selectedObject);
        serializedObj?.ApplyModifiedProperties();
    }

    void OnSceneGUI(SceneView sceneView)
    {

    }

    bool showCondition = true;
    private UnityEngine.Object myObjectDragged;
    float MemoTextWidth = 210;
    public static Color  maximColor =  new Color(0.6f,0.6f,0.6f);
    private Editor 普通类型资源的编辑器;
    void OnGUI()
    {
        GUI.backgroundColor = SF_Conductor.lightblue;

        Event ev = Event.current;

        GUILayout.Space(10);
        if (selectedObject)
            EditorGUILayout.InspectorTitlebar(false, selectedObject, false); //绘制对象标头

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar); // 开始水平布局


        GUILayout.FlexibleSpace(); // 将按钮推到右侧
        if (!isPageLocked&& SF_EditorMouseClickSelection.SF_EditorAssetsSelectedMark[0]!=null)
        {
            if (GUILayout.Button(new GUIContent(ET.StringTruncater.Truncate("<< 倒回 : " + SF_EditorMouseClickSelection.SF_EditorAssetsSelectedMark[0].name, 50, true)),
                EditorStyles.toolbarButton, GUILayout.Width(300), GUILayout.Height(25)))
            {
                if (SF_EditorMouseClickSelection.SF_EditorAssetsSelectedMark[0] != null)
                {
                    selectedObject = SF_EditorMouseClickSelection.SF_EditorAssetsSelectedMark[0];
                    SF_EditorMouseClickSelection.SF_EditorAssetsSelectedMark[0] = SF_EditorMouseClickSelection.SF_EditorAssetsSelectedMark[1];
                    SF_EditorMouseClickSelection.SF_EditorAssetsSelectedMark[1] = selectedObject;
                    RepaintSelected(selectedObject);
                    Debug.Log("(SF_Editor)已倒回。");
                }
            }
        }

        if (GUILayout.Button(new GUIContent("", lockIcon), EditorStyles.toolbarButton, GUILayout.Width(100), GUILayout.Height(25)))
        {
            if (isPageLocked)
            {
                lockIcon = EditorGUIUtility.IconContent("解锁").image as Texture2D;
                isPageLocked = false;
                RepaintSelected(SF_EditorMouseClickSelection.GetSF_EditorAssetsSelectedMark());
            }
            else
            {
                lockIcon = EditorGUIUtility.IconContent("锁定").image as Texture2D;
                isPageLocked = true;
            }
        }
        EditorGUILayout.EndHorizontal(); // 结束水平布局

        GUILayout.Space(10);


        if (canDraw) {         
            Event currentEvent = Event.current;
            // 检查拖放事件
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
                }

                currentEvent.Use(); // 标记事件已处理
            }

            if (selectedObject)
            {

#region 开始绘制滚动区域的内容
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, alwaysShowHorizontal: false, alwaysShowVertical: true);

                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical(GUILayout.Width(position.width-MemoTextWidth-2));

                //绘制EnergyOfNarration
                if (selectedObject is EnergyOfNarration)
                {
                    EditorGUILayout.LabelField("[前提条件]    "+((EnergyOfNarration)selectedObject).SurgeCondition?.Count, EditorStyles.boldLabel);
                    showCondition = EditorGUILayout.BeginFoldoutHeaderGroup(showCondition, "SurgeCondition");
                    if (showCondition)
                    {
                        serializedObj?.Update();
                        
                        conditionReorderableList?.DoLayoutList();

                        serializedObj?.ApplyModifiedProperties();
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();

                    // 画一条水平线作为条件分割
                    GUILayout.Space(7f);
                    Rect lineRect = EditorGUILayout.GetControlRect(false, 1f);
                    EditorGUI.DrawRect(lineRect, Color.black);

                    ///绘制Energy原本的各种属性
                    EditorGUI.BeginChangeCheck();
                    SerializedProperty iterator = serializedObj?.GetIterator();
                    if (iterator != null)
                    {
                        if(selectedObject)
                            Undo.RecordObject(selectedObject, "Energy内容发生改变。");
                        
                        bool enterChildren = true;
                        iterator.NextVisible(enterChildren);//直接跳过第一个绘制,因为第一个是脚本引用,不需要在SF_Vast里公开显示

                        while (iterator.NextVisible(enterChildren))
                        {
                            // 检查属性是否带有 HideInStirngsFlowEditorButShowInInspectorAttribute 标签
                            bool shallHide = AttributeHelper.PropertyHasAttribute<HideInStirngsFlowEditorButShowInInspector>(iterator);
                            if (!shallHide)
                            {
                                if (selectedObject is EnergyOfSpeak)
                                {
                                    EditorGUILayout.PropertyField(iterator, true);
                                    //检测右键菜单事件
                                    if (Event.current.type == EventType.ContextClick)
                                    {
                                        EnergyOfSpeak selected = selectedObject as EnergyOfSpeak;
                                        GenericMenu menu = new ();
                                        menu.AddDisabledItem(new GUIContent("刷新下列句中的声明 :"));
                                        for (int sentenceIndex=0; sentenceIndex< selected.Sentences.Count; sentenceIndex++)
                                        {
                                            int index = sentenceIndex;
                                            menu.AddItem(new GUIContent("“" + selected.Sentences[sentenceIndex].Words + "”"), false, () =>
                                            {
                                                SynchronizeStatementsToWords(selectedObject as EnergyOfSpeak, index);
                                            });
                                        }
                                        menu.AddSeparator("");
                                        menu.AddItem(new GUIContent("刷新每一句的所有声明..."), false, () =>
                                        {
                                            SynchronizeStatementsToWords(selectedObject as EnergyOfSpeak);
                                        });
                                        menu.ShowAsContext();
                                    }
                                }
                                else if(selectedObject is EnergyOfMusicGameStartUp)
                                {
                                    EnergyOfMusicGameStartUp selected = selectedObject as EnergyOfMusicGameStartUp;
                                    if (iterator.name == "MapFileGUID") {
                                        GUILayout.Space(22);
                                        GUI.backgroundColor = lightGreen;
                                        Texture2D icon = EditorGUIUtility.IconContent("游戏").image as Texture2D;
                                        if (GUILayout.Button(new GUIContent("     "+ Path.GetFileName(AssetDatabase.GUIDToAssetPath(selected.MapFileGUID)), icon), GUILayout.ExpandWidth(true), GUILayout.Height(42)))
                                        {                                           
                                            EditorApplication.delayCall += SF_GameMapSelectionWindow.Open;
                                        }

                                        GUILayout.Space(22);
                                        GUI.backgroundColor = Color.white;
                                    }
                                    else
                                        EditorGUILayout.PropertyField(iterator, true);
                                }
                                else
                                    EditorGUILayout.PropertyField(iterator, true);
                            }
                            enterChildren = false;
                        }
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObj.ApplyModifiedProperties();
                    }
                }
                //绘制ConfigOfNarration
                else if (selectedObject is ConfigOfNarration) {
                    
                    ///绘制原本Config的属性
                    EditorGUI.BeginChangeCheck();
                    SerializedProperty iterator = serializedObj?.GetIterator();

                    if(iterator!=null)
                    {
                        bool enterChildren = true;
                        iterator.NextVisible(enterChildren);//直接跳过第一个绘制,因为第一个是脚本引用,不需要在SF_Vast里公开显示

                        if(selectedObject)
                            Undo.RecordObject(selectedObject, "Config内容发生改变。");
                        while (iterator.NextVisible(enterChildren))
                        {
                            // 检查属性是否带有 HideInStirngsFlowEditorButShowInInspectorAttribute 标签
                            bool shallHide = AttributeHelper.PropertyHasAttribute<HideInStirngsFlowEditorButShowInInspector>(iterator);
                            if (!shallHide)
                                EditorGUILayout.PropertyField(iterator, true);
                            enterChildren = false;
                            if (iterator.name == "CharacterDescription")
                            {
                                //检测鼠标点击事件
                                if (ev.type == EventType.MouseDown && ev.button == 0)
                                {
                                    // 结束输入，如果点击了窗口以外的位置
                                    if (!GUILayoutUtility.GetLastRect().Contains(ev.mousePosition)
                                        && !new Rect(position.width - MemoTextWidth, 0, MemoTextWidth, position.height).Contains(Event.current.mousePosition))
                                    {
                                        GUI.FocusControl(null);
                                        Debug.Log("点击了LastRect以外的位置");
                                        Repaint();
                                    }
                                }
                            }
                        }
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObj.ApplyModifiedProperties();
                    }

                    GUILayout.Space(20);

                    if (selectedObject is PureDataDesignItems)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(new GUIContent("    [ 纯设定 ]", "纯设定的本质是一个只含设定项的Config，作为附属形使用的时候可以控制所有分化的设定结构。"), EditorStyles.helpBox, GUILayout.Width(80), GUILayout.Height(19)); 
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("[自由设定]", EditorStyles.boldLabel, GUILayout.Width(80));                                           
                    }

                    //绘制共性项提示小鸽子
                    Rect ui = GUILayoutUtility.GetRect(25, 19);
                    Rect imageRect = new Rect(position.width - 248, ui.y - 3, 25, 18);                    
                    GUI.DrawTexture(imageRect, 鸽子image, ScaleMode.ScaleToFit, true, 0);
                    // 检测鼠标悬停位置
                    if (imageRect.Contains(Event.current.mousePosition))
                    {
                        EditorGUI.LabelField(new Rect(imageRect.x - 168, imageRect.y - 25, 170, 43), "Common标记：点击下方的勾选框可将指定项设置为'共性项'，这将使其所有的分化体都分享同一项数据。", EditorStyles.helpBox);
                    }
                    EditorGUILayout.EndHorizontal();

                    serializedObj?.Update();

                    freeSettingsReorderableList?.DoLayoutList();

                    serializedObj?.ApplyModifiedProperties();
                }
                //绘制其它类型的资源
                else
                {
                    DestroyImmediate(普通类型资源的编辑器);
                    普通类型资源的编辑器 = Editor.CreateEditor(selectedObject);
                    普通类型资源的编辑器.OnInspectorGUI();
                }

                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(MemoTextWidth - 20));

                // 竖长的字符串输入框
                if (selectedObject is StringOfNarration)
                {
                    GUILayout.Label("Memo", EditorStyles.boldLabel);
                    Undo.RecordObject(selectedObject , "Memo输入");
                    (selectedObject as StringOfNarration).Memo = EditorGUILayout.TextArea((selectedObject as StringOfNarration).Memo, MemoTextAreaStyle, GUILayout.Width(MemoTextWidth - 20), GUILayout.ExpandHeight(true));
                }

                if (selectedObject is ConfigOfNarration)
                {
                    GUILayout.Space(7);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new GUIContent("转为C#脚本"), EditorStyles.miniButton, GUILayout.Width(90), GUILayout.Height(25)))
                    {

                    }
                    EditorGUILayout.EndHorizontal();
                }
                else if (selectedObject is EnergyOfSpeak)
                {
                    GUILayout.Space(7);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new GUIContent("编译Sentences"), EditorStyles.miniButton, GUILayout.Width(110), GUILayout.Height(25)))
                    {
                        CompileSentences(selectedObject as EnergyOfSpeak);
                        EditorUtility.SetDirty(selectedObject);
                        AssetDatabase.Refresh();
                    }
                    GUILayout.EndHorizontal();
                }       

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                EditorGUILayout.EndScrollView(); /// 结束滚动区域
#endregion
            }

            GUI.backgroundColor = new Color(SF_Conductor.lightblue.r-0.17f, SF_Conductor.lightblue.g - 0.14f, SF_Conductor.lightblue.b - 0.09f);
            
            // 使用toolbar样式开始水平布局
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            #region 水平布局绘制右下角各种菜单栏按钮
            
            if (GUILayout.Button(new GUIContent(ET.StringTruncater.Truncate("发行地域（功能尚未完善）", 29, true)),
                    EditorStyles.toolbarButton, GUILayout.Width(175), GUILayout.Height(25)))
            {
                // SF_DistributionAreaSelectionWindow.Open();
            }

            GUILayout.FlexibleSpace(); // 将按钮推到右侧
            GUILayout.Space(1);
            
            DrawExtensionToolsBtn();
            this.存档选项索引 = EditorGUILayout.Popup(GUIContent.none, 存档选项索引, 存档位,GUILayout.Width(125f));
            
            if (GUILayout.Button(new GUIContent(ET.StringTruncater.Truncate("在项目资源中找到", 21, true)),
                    EditorStyles.toolbarButton, GUILayout.Width(132), GUILayout.Height(25)))
            {
                EditorApplication.ExecuteMenuItem("Window/General/Project");
                EditorGUIUtility.PingObject(selectedObject);
            }
            if (GUILayout.Button(new GUIContent("删除"),
                    EditorStyles.toolbarButton, GUILayout.Width(55), GUILayout.Height(25)))
            {
                EditorApplication.ExecuteMenuItem("Assets/Delete");
            }
            if (GUILayout.Button(new GUIContent("窗口化..."),
                    EditorStyles.toolbarButton, GUILayout.Width(75), GUILayout.Height(25)))
            {
                Selection.activeObject = selectedObject;
                EditorApplication.ExecuteMenuItem("Assets/Properties...");
            }

            #endregion
            
            EditorGUILayout.EndHorizontal(); // 结束水平布局
            GUILayout.Space(1);
            GUI.backgroundColor = SF_Conductor.lightblue;
        }

        // 使用toolbar样式开始水平布局
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Space(1);
        // (计算窗口宽度并在中间显示文本)
        float titleWidth = GUI.skin.label.CalcSize(new GUIContent("SF_Vast")).x;
        float windowWidth = position.width;
        GUILayout.Space(10); // 空格，使文本居中显示
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (ScrollingMaximMaker.currentMaxim!=default) //绘制格言警句
        {
            titleWidth = GUI.skin.label.CalcSize(new GUIContent(ScrollingMaximMaker.currentMaxim)).x;
            GUIStyle maximStyle = new GUIStyle(GUI.skin.label);
            maximStyle.fontSize = 11;
            maximStyle.normal.textColor = maximColor;
            maximStyle.hover.textColor = maximColor;
            GUILayout.Label(ScrollingMaximMaker.currentMaxim,maximStyle,GUILayout.Width(titleWidth), GUILayout.Height(14)); 
        }
        else
            GUILayout.Label("SF_Vast", GUILayout.Width(titleWidth), GUILayout.Height(14));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        EditorGUILayout.EndHorizontal(); // 结束水平布局
        GUILayout.Space(1);
        GUI.backgroundColor = Color.white; // 恢复默认背景颜色
    }

    //判断项名是否重复
    private void CheckItemNameDuplicated(ConfigOfNarration selectedConfig , SerializedProperty itemNameProperty) {
        foreach (FreeDesignItem item in selectedConfig.items)
        {
            if (item != null && item.itemName == itemNameProperty.stringValue)
            { itemNameProperty.stringValue += "(重复)"; CheckItemNameDuplicated(selectedConfig , itemNameProperty); break; }
        }
    }

    //分化前的预备事项
    public void PreDivergeform(FreeDesignItem currentItem,ConfigOfNarration masterToClearRuntimeData) {
        //分化前删掉上一个分化体的资源
        string p = AssetDatabase.GetAssetPath(currentItem.RuntimeSubsidiaryRef.divergiform);
        if (AssetDatabase.LoadAssetAtPath<ConfigOfNarration>(p))
            AssetDatabase.DeleteAsset(p);
        //把为空的运行时数据清除，保持mySubsidiarysS干净
        for (int i = masterToClearRuntimeData.mySubsidiarysS.Count - 1; i >= 0; i--)
        {
            if (masterToClearRuntimeData.mySubsidiarysS[i].divergiform == null)
                masterToClearRuntimeData.mySubsidiarysS.Remove(masterToClearRuntimeData.mySubsidiarysS[i]);
        }
        for (int i = masterToClearRuntimeData.mySubsidiarysL.Count - 1; i >= 0; i--)
        {
            if (masterToClearRuntimeData.mySubsidiarysL[i].divergiform == null)
                masterToClearRuntimeData.mySubsidiarysL.Remove(masterToClearRuntimeData.mySubsidiarysL[i]);
        }
    }
    
    public static void 初始化存档位()
    {
        // 创建一个空的字符串数组，用于存储转换后的值
        存档位 = new string[SF_SettingsWindow.存档位Property.arraySize+1];
        // 将 SerializedProperty 中的值转换为普通的字符串数组
        for (int i = 0; i < SF_SettingsWindow.存档位Property.arraySize+1; i++)
        {
            if (i == SF_SettingsWindow.存档位Property.arraySize)//将最后一个存档位设置为空存档
                存档位[i] = "空存档";
            else
                存档位[i] = SF_SettingsWindow.存档位Property.GetArrayElementAtIndex(i).stringValue;
        }   
    }
}

public static class AttributeHelper
{
    // 通过反射判断 SerializedProperty 是否带有指定的属性
    public static bool PropertyHasAttribute<T>(SerializedProperty property) where T : UnityEngine.PropertyAttribute
    {
        var attributes = property?.serializedObject?.targetObject?.GetType()
            .GetField(property.propertyPath)?
            .GetCustomAttributes(typeof(T), true);

        return attributes != null && attributes.Length > 0;
    }
}
