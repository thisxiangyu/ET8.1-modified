using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//批量重命名
public class SF_VibratorRenameWindow : EditorWindow
{
    private static Object[] objects;
    private string userInput = "";
    private Event e;

    public static SF_VibratorRenameWindow Open(Object[] objectsForSetting)
    {
        objects = objectsForSetting;
        SF_VibratorRenameWindow window = (SF_VibratorRenameWindow)GetWindow(typeof(SF_VibratorRenameWindow), true, "SF_批量重命名工具", true);
        return window;
    }

    void OnGUI()
    {
        e = Event.current;

        GUILayout.Space(15);
        userInput = EditorGUILayout.TextField("", userInput);
        GUILayout.Space(5);
        if (GUILayout.Button("确认(Enter)"))
        {
            DoRename();
            Close();
        }
        GUILayout.Space(5);
        if (e.isKey && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
        {
            // 在这里执行按下回车键时的操作。
            EditorGUI.FocusTextInControl("Enter");
            DoRename();
            Close();
        }
    }

    void DoRename()
    {
        Undo.RecordObjects(objects,"SF对象批量重命名");
        for (int i = 0; i < objects.Length; i++)
            objects[i].name = userInput + (i + 1);
    }
}
