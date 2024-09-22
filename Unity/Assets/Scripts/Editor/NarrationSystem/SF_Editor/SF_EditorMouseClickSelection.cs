using ET;
using ET.Client;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
public class SF_EditorMouseClickSelection /*选中物体发生响应*/
{
    //注意：0是上一个选择,1是当前选择。
    public static GameObject[] SF_EditorGameObjectSelectedStack = new GameObject[2];
    public static UnityEngine.Object[] SF_EditorAssetsSelectedMark = new UnityEngine.Object[2];

    public static void OnSFSelectionChanged()
    {
        if (Selection.objects != null && Selection.objects.Length != 0)
        {
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                if (Selection.objects[i] is DefaultAsset)
                {
                    Debug.Log("(SF_Editor)当前的" + Selection.objects[i].name + "是一个文件夹或文件。");
                    //直接打开
                    EditorApplication.ExecuteMenuItem("Assets/Open");
                    return;
                }
            }
            UnityEngine.Object o = Selection.objects[Selection.objects.Length - 1];
            if (o is GameObject)
            {
                SF_EditorGameObjectSelectedStack[0] = SF_EditorGameObjectSelectedStack[1];
                SF_EditorGameObjectSelectedStack[1] = (GameObject)Selection.objects[Selection.objects.Length - 1];                
                
                if (((GameObject)o).GetComponent<VibrationUnit>())
                {
                    UnloadUnit();
                    LoadUnit();
                }
            }
            else
            {
                SF_EditorAssetsSelectedMark[0] = SF_EditorAssetsSelectedMark[1];

                SF_EditorAssetsSelectedMark[1] = Selection.objects[Selection.objects.Length - 1];

                ///下面这种做法适合在Layout模式使用
                // 打开一个SF_Vast窗口
                //SF_Vast window =EditorWindow.CreateWindow<SF_Vast>(typeof(SF_Vast));
                //window.titleContent = UpdateWindowTitle.UpdateAs(" (空)", "ScriptableObject Icon", "SF_Vast");
            }

            EditorApplication.RepaintHierarchyWindow();
            Debug.Log("(SF_Editor)选中物体: " + Selection.objects[Selection.objects.Length - 1]?.name);
        }
        else
        {
            //SF_EditorObjectSelectedMark = null;
            Debug.Log("(SF_Editor)未选中SF_Editor当中的任何物体");
        }
    }

    public static UnityEngine.Object GetSF_EditorAssetsSelectedMark() {
        if (SF_EditorAssetsSelectedMark[1] != null)
            return SF_EditorAssetsSelectedMark[1];
        return null;
    }

    public static GameObject GetSF_EditorGameObjectSelectedMark()
    {
        if (SF_EditorGameObjectSelectedStack[1] != null)
            return SF_EditorGameObjectSelectedStack[1];
        else
            return null;
    }


    public static void LoadUnit() {
        VibrationUnit unit = SF_EditorGameObjectSelectedStack[1].GetComponent<VibrationUnit>();
        if (unit != null)
        {
            unit.SetEnergize(true);
            unit.InstantiatePrefabs(SF_MenuItems.LayoutRef);
        }
    }

    public static void UnenergizeTheLastUnit()
    {
        VibrationUnit theLastUnit = SF_MenuItems.SF_EditorRef.GetRootGameObjects()[0].transform.Find("VibrationBasin").GetComponent<VibrationBasin>().CurrentUnit;
        if (theLastUnit)
        {
            theLastUnit.SetEnergize(false);
            if (theLastUnit.CharacterList != null)
            {
                foreach (var characterConfig in theLastUnit.CharacterList)
                {
                    characterConfig.isPrepared = YesOrNo.No;
                    characterConfig.hasBeenActivated = YesOrNo.No;
                }   
            }

            if (theLastUnit.SpaceList != null)
            {
                foreach (var spaceConfig in theLastUnit.SpaceList)
                {
                    spaceConfig.isPrepared = YesOrNo.No;
                    spaceConfig.hasBeenActivated = YesOrNo.No;
                }      
            }
        }
    }

    public static void UnloadUnit()
    {
        UnenergizeTheLastUnit();

        if(SF_MenuItems.LayoutRef!=null)
        {
            Transform Stage2D = SF_MenuItems.LayoutRef.Find("2DStage");
            Transform Stage3D = SF_MenuItems.LayoutRef.Find("3DStage");
            if (Stage2D != null && Stage3D != null)
            {
                for (int i = Stage2D.childCount - 1; i >= 0; i--)
                {
                    Transform child = Stage2D.GetChild(i);

                    // 如果子物体有SFLayout标签，则销毁该子物体
                    if (child.CompareTag("SFLayout"))
                    {
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                    }
                }
                for (int i = Stage3D.childCount - 1; i >= 0; i--)
                {
                    Transform child = Stage3D.GetChild(i);
                    if (child.CompareTag("SFLayout"))
                    {
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                    }
                }
            }
        }
    }
}
