#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

[ExecuteInEditMode]
public class UIClickAreaDebugTool : MonoBehaviour
{
    private static readonly Vector3[] m_corners = new Vector3[4];
    private static readonly List<Rect> m_rects = new List<Rect>();
    private static readonly List<Graphic> m_graphics = new List<Graphic>();

    private static Texture m_texture;
    private static Texture Texture
    {
        get
        {
            if (m_texture == null)
            {
                m_texture = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Res/Editor/ClickAreaTex.png");
            }

            return m_texture;
        }
    }

    private void OnGUI()
    {
        m_rects.Clear();
        m_graphics.Clear();

        if (!EditorPrefs.HasKey(UIClickAreaDebugToolEditor.EditorPrefKeyForUIClickArea))
            return;

        if (EditorPrefs.GetInt(UIClickAreaDebugToolEditor.EditorPrefKeyForUIClickArea) == UIClickAreaDebugToolEditor.EditorPrefValueForDisplayAll)
        {
            m_graphics.AddRange(GameObject.FindObjectsOfType<Graphic>());
        }
        else if(EditorPrefs.GetInt(UIClickAreaDebugToolEditor.EditorPrefKeyForUIClickArea) == UIClickAreaDebugToolEditor.EditorPrefValueForDisplaySelection)
        {
            foreach (var trans in Selection.transforms)
            {
                if (trans.gameObject.activeSelf)
                {
                    m_graphics.AddRange(trans.GetComponentsInChildren<Graphic>());
                }
            }
        }

        foreach (var graphic in m_graphics)
        {
            if (IsBlockRaycast(graphic))
            {
                RectTransform rectTransform = graphic.transform as RectTransform;
                if (rectTransform != null)
                {
                    rectTransform.GetWorldCorners(m_corners);
                    var screenPos = RectTransformUtility.WorldToScreenPoint(graphic.canvas.worldCamera, (m_corners[0] + m_corners[2]) / 2);
                    var screenPosMin = RectTransformUtility.WorldToScreenPoint(graphic.canvas.worldCamera, m_corners[0]);
                    var screenPosMax = RectTransformUtility.WorldToScreenPoint(graphic.canvas.worldCamera, m_corners[2]);
                    var width = screenPosMax.x - screenPosMin.x;
                    var height = screenPosMax.y - screenPosMin.y;
                    m_rects.Add(new Rect(screenPos.x - width / 2, Screen.height - screenPos.y - height / 2, width, height));
                }
            }
        }

        if (m_rects.Count > 0)
        {
            foreach (var rect in m_rects)
            {
                GUI.DrawTexture(new Rect(rect), Texture);
            }
        }
    }

    public static bool IsBlockRaycast(Graphic graphic)
    {
        if (graphic == null || !graphic.enabled || graphic.canvas == null)
            return false;

        if (graphic.depth == -1 || !graphic.raycastTarget || graphic.canvasRenderer.cull)
            return false;

        var caster = graphic.canvas.GetComponent<GraphicRaycaster>();
        if (caster == null || !caster.enabled) 
            return false;

        int canvasDepth = GetHierarchyDepth(graphic.canvas);

        var groupList = graphic.GetComponentsInParent<CanvasGroup>();
        foreach (var group in groupList)
        {
            int groupDepth = GetHierarchyDepth(group);
            if (groupDepth < canvasDepth)
            {
                break;
            }

            if (!group.blocksRaycasts)
            {
                return false;
            }

            if (group.ignoreParentGroups)
            {
                break;
            }
        }

        return true;
    }

    public static int GetHierarchyDepth(Component com)
    {
        int depth = 0;
        var trans = com.transform;
        while (trans != null)
        {
            depth++;
            trans = trans.parent;
        }

        return depth;
    }
}

[InitializeOnLoad]
public class UIClickAreaDebugToolEditor
{
    private static readonly Vector3[] m_corners = new Vector3[4];

    static UIClickAreaDebugToolEditor()
    {
        PrepareDebugTool();
    }

    #region Gizmo

    [DrawGizmo(GizmoType.InSelectionHierarchy, typeof(Graphic))]
    private static void DrawGizmoForUIGraphic(
        Graphic graphic, GizmoType gizmoType)
    {
        DrawClickAreaGizmos(graphic, gizmoType);
    }

    private static void DrawClickAreaGizmos(Graphic graphic, GizmoType gizmoType)
    {
        if (graphic == null || !graphic.raycastTarget)
            return;

        if (graphic.canvas == null || graphic.canvas.worldCamera == null)
            return;

        if (!EditorPrefs.HasKey(EditorPrefKeyForUIClickArea))
            return;

        var tex = AssetDatabase.LoadAssetAtPath<Texture>("Assets/wfc/Gizmos/ClickAreaTex.png");

        var oldColor = Gizmos.color;
        Gizmos.color = Color.yellow;
        RectTransform rectTransform = graphic.transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.GetWorldCorners(m_corners);
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(m_corners[i], m_corners[(i + 1) % 4]);
            }
        }

        Gizmos.color = oldColor;
    }

    #endregion

    #region MenuItem

    private const string MenuItemHideClickArea = "UI/点击区域/不显示";
    private const string MenuItemDisplaySelectionClickArea = "UI/点击区域/显示选中";
    private const string MenuItemDisplayAllClickArea = "UI/点击区域/显示全部";
    public const string EditorPrefKeyForUIClickArea = "DisplayUIClickAreaDebugInfo";
    public const int EditorPrefValueForDisplaySelection = 1;
    public const int EditorPrefValueForDisplayAll = 2;

    [MenuItem(MenuItemHideClickArea, true)]
    public static bool ClickAreaHideToggle()
    {
        return EditorPrefs.HasKey(EditorPrefKeyForUIClickArea) && HasDebugTool();
    }

    [MenuItem(MenuItemHideClickArea, false)]
    public static void ClickAreaHide()
    {
        EditorPrefs.DeleteKey(EditorPrefKeyForUIClickArea);
        PrepareDebugTool();
    }

    [MenuItem(MenuItemDisplaySelectionClickArea, true)]
    public static bool ClickAreaDisplaySelectionToggle()
    {
        return !EditorPrefs.HasKey(EditorPrefKeyForUIClickArea) && !HasDebugTool() ||
               EditorPrefs.GetInt(EditorPrefKeyForUIClickArea) != EditorPrefValueForDisplaySelection && HasDebugTool();
    }

    [MenuItem(MenuItemDisplaySelectionClickArea, false)]
    public static void ClickAreaDisplaySelection()
    {
        EditorPrefs.SetInt(EditorPrefKeyForUIClickArea, EditorPrefValueForDisplaySelection);
        PrepareDebugTool();
    }

    [MenuItem(MenuItemDisplayAllClickArea, true)]
    public static bool ClickAreaDisplayAllToggle()
    {
        return !EditorPrefs.HasKey(EditorPrefKeyForUIClickArea) && !HasDebugTool() ||
               EditorPrefs.GetInt(EditorPrefKeyForUIClickArea) != EditorPrefValueForDisplayAll && HasDebugTool();
    }

    [MenuItem(MenuItemDisplayAllClickArea, false)]
    public static void ClickAreaDisplayAll()
    {
        EditorPrefs.SetInt(EditorPrefKeyForUIClickArea, EditorPrefValueForDisplayAll);
        PrepareDebugTool();
    }

    private static bool HasDebugTool()
    {
        var tool = GameObject.FindObjectOfType<UIClickAreaDebugTool>();
        var toolObj = GameObject.Find("UIClickAreaDebugTool");
        return tool != null || toolObj != null;
    }

    public static void PrepareDebugTool()
    {
        bool needDebugTool = EditorPrefs.HasKey(EditorPrefKeyForUIClickArea);
        if (HasDebugTool() ^ needDebugTool)
        {
            if (needDebugTool)
            {
                var go = new GameObject("UIClickAreaDebugTool", typeof(UIClickAreaDebugTool));
                go.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                var tools = GameObject.FindObjectsOfType<UIClickAreaDebugTool>();
                foreach (var tool in tools)
                {
                    Object.DestroyImmediate(tool);
                }

                while (true)
                {
                    var toolObj = GameObject.Find("UIClickAreaDebugTool");
                    if (toolObj == null) break;
                    Object.DestroyImmediate(toolObj);
                }
            }
        }
    }

    #endregion
}
#endif
