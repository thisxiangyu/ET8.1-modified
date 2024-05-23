using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class SF_ItemCommonalityModifyWindow : EditorWindow
{
    static bool toBe;

    public static SF_ItemCommonalityModifyWindow ShowWindow(bool beCommon)
    {
        // 创建一个新的 EditorWindow
        toBe = beCommon;
        SF_ItemCommonalityModifyWindow window = (SF_ItemCommonalityModifyWindow)GetWindow(typeof(SF_ItemCommonalityModifyWindow), true, "确认", true);
        didModify = DidModify.waiting;
        SF_Vast.shallCommonItemModify = true;
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
        if (toBe)
        {
            EditorGUILayout.LabelField("正在将当前项设计为[共性项]，");
            EditorGUILayout.LabelField("这将覆盖分化体中这个项已有的数据。");
        }
        else
        {
            EditorGUILayout.LabelField("要将所选[共性项]取消设置？");
            EditorGUILayout.LabelField("    分化体的这个项将会还原为空。");
        }
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
