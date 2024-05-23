using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(InspectorReadOnlyAttribute))]
public class InspectorReadOnlyAttributeDrawer : PropertyDrawer //在Inspector当中显示为只读。
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        InspectorReadOnlyAttribute readOnlyAttribute = attribute as InspectorReadOnlyAttribute;

        if (readOnlyAttribute != null)
        {          
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = true;
        }
        else
        {
            EditorGUI.PropertyField(position, property, label);
        }
    }
}

[CustomPropertyDrawer(typeof(InspectorReadOnlyWhileNotNullAttribute))]
public class InspectorReadOnlyWhileNotNullAttributeDrawer : PropertyDrawer //当不为空的时候在Inspector当中显示为只读，否则不显示。
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        InspectorReadOnlyWhileNotNullAttribute readOnlyNotNullAttribute = attribute as InspectorReadOnlyWhileNotNullAttribute;

        if (readOnlyNotNullAttribute != null)
        {
            // 获取字段的值
            object fieldValue = GetFieldValue(property);

            // 如果字段的值为空，则不绘制该字段
            if (fieldValue == null)
            {
                // 画一条水平线
                Rect lineRect = EditorGUILayout.GetControlRect(false, 1f);
                EditorGUI.DrawRect(lineRect, Color.black);
                return; 
            }

            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = true;
        }
    }

    // 辅助方法用于获取字段的值
    private object GetFieldValue(SerializedProperty property)
    {
        if (property.isArray && property.arraySize == 0)
            return null;

        else if(property.propertyType == SerializedPropertyType.ObjectReference)
                return property.objectReferenceValue;

        else if (property.propertyType == SerializedPropertyType.String)
            return property.stringValue;

        else
            return null;
    }
}
