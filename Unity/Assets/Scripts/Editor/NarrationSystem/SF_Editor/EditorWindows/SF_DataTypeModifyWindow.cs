using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public enum DidModify
{
    yes,
    no,
    waiting
}

public class SF_DataTypeModifyWindow : EditorWindow
{
    public static SF_DataTypeModifyWindow ShowWindow()
    {
        // 创建一个新的 EditorWindow
        SF_DataTypeModifyWindow window = (SF_DataTypeModifyWindow)GetWindow(typeof(SF_DataTypeModifyWindow), true, "确认", true);
        didModify = DidModify.waiting;
        SF_Vast.shallDataTypeModify = true;
        return window;
    }
    private void OnDestroy()
    {
        if(didModify != DidModify.yes)
            didModify = DidModify.no;
    }

    private Event e;
    public static DidModify didModify = DidModify.waiting;

    void OnGUI()
    {
        e = Event.current;

        GUILayout.Space(18);
        EditorGUILayout.LabelField("是否修改当前项的数据类型？");
        EditorGUILayout.LabelField("    这将会清空这个设定项所有已有的数据。");
        GUILayout.Space(8);
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar); // 开始水平布局。
        if (GUILayout.Button("是"))
        {
            didModify = DidModify.yes;
            Close();
        }
        if (GUILayout.Button("取消"))
        {
            Close();
        }
        EditorGUILayout.EndHorizontal(); // 结束开始水平布局
    }
}
