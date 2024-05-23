using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class SF_MakeSureIconsModifyWindow : EditorWindow
{
    private GameObject toModify;
    private Transform[] allChilds;

    private enum 图标选项
    {
        none,
        绿色小圆点,
        黄色小圆点,
        灰色小圆点,
        蓝色小圆点,
        黑色小圆点,
        绿色同心圆,
        黄色同心圆,
        白色同心圆,
        黑色同心圆,
        白十字,
        绿色十字,
        黄色十字,
        品红十字,
        十字框,
        绿色准星,
        黄色准星,
        白色跟踪标记,
        蓝色跟踪标记,
        绿色跟踪标记,
        黄色跟踪标记,
        Wave通电
    }

    private 图标选项 所选图标;
    
    public static SF_MakeSureIconsModifyWindow ShowWindow(GameObject toModifyInput)
    {
        SF_MakeSureIconsModifyWindow window = (SF_MakeSureIconsModifyWindow)GetWindow(typeof(SF_MakeSureIconsModifyWindow), true, "统一图标", true);
        window.toModify = toModifyInput;
        window.allChilds = window.toModify?.transform.GetComponentsInChildren<Transform>();
        
        return window;
    }
    
    void OnGUI()
    {
        GUILayout.Space(8);
        if (toModify == null)
        {GUILayout.Space(8);
            EditorGUILayout.LabelField("  未选中任何游戏物体。当前操作无效。");
        }
        else
        {
            EditorGUILayout.LabelField("当前物体为 '"+toModify.name+"'"+",子物体个数："+(allChilds.Length-1)+"个；");
            EditorGUILayout.LabelField("是否修改当前物体及其所有子物体的图标？");
            GUILayout.Space(8);
            所选图标 = (图标选项)EditorGUILayout.EnumPopup("图标选项：",所选图标);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar); // 开始水平布局。
            if (GUILayout.Button("是"))
            {
                //设置Icon
                GUIContent iconContent = null;
                if (所选图标 != 图标选项.none)
                {
                    iconContent = EditorGUIUtility.IconContent(所选图标.ToString());
                    foreach (Transform child in allChilds)
                    {
                        EditorGUIUtility.SetIconForObject(child.gameObject, (Texture2D)iconContent.image);
                    }   
                }
                else
                {
                    foreach (Transform child in allChilds)
                    {
                        EditorGUIUtility.SetIconForObject(child.gameObject, null);
                    }  
                }
                Close();
            }
            if (GUILayout.Button("取消"))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal(); // 结束开始水平布局   
        }
    }
}
