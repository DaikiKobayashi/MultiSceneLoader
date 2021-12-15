using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace MultiSceneLoader
{
    [CustomEditor(typeof(SceneLoadDataSO))]
    public class SceneLoadDataSOEditor : Editor
    {
        private static class Style
        {
            public static GUIContent AddContent;
            public static GUIStyle AddStyle;
            public static GUIContent SubContent;
            public static GUIStyle SubStyle;
            static Style()
            {
                AddContent = EditorGUIUtility.TrIconContent("Toolbar Plus", "Add to list");
                AddStyle = "RL FooterButton";
                SubContent = EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove selection from list");
                SubStyle = "RL FooterButton";
            }
        }
        private ReorderableList reorderableList;

        public SceneLoadDataSOEditor(SerializedProperty property)
        {
            reorderableList = CreateInstance(property);
        }

        public void Draw()
        {
            reorderableList.DoLayoutList();
        }

        private ReorderableList CreateInstance(SerializedProperty property)
        {
            return new ReorderableList(property.serializedObject, property, true, true, false, false)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, $"{property.displayName}: {property.arraySize}", EditorStyles.boldLabel);
                    var position =
                        new Rect(
                            rect.width - (EditorGUI.indentLevel - property.depth) * 15f,
                            rect.y,
                            20f,
                            13f
                        );
                    if (GUI.Button(position, Style.AddContent, Style.AddStyle))
                    {
                        property.serializedObject.UpdateIfRequiredOrScript();
                        property.InsertArrayElementAtIndex(property.arraySize);
                        property.serializedObject.ApplyModifiedProperties();
                    }
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    if (property.arraySize <= index)
                        return;

                    DrawElement(property, rect, index);

                    rect.xMin = rect.width + 20f - (EditorGUI.indentLevel - property.depth) * 15f;
                    rect.width = 20f;
                    if (GUI.Button(rect, Style.SubContent, Style.SubStyle))
                    {
                        property.serializedObject.UpdateIfRequiredOrScript();
                        property.DeleteArrayElementAtIndex(index);
                        property.serializedObject.ApplyModifiedProperties();
                    }
                },

                drawFooterCallback = rect => { },
                footerHeight = 0f,
                elementHeightCallback = index =>
                {
                    if (property.arraySize <= index)
                        return 0;

                    return EditorGUI.GetPropertyHeight(property.GetArrayElementAtIndex(index));
                }
            };
        }
        private void DrawElement(SerializedProperty property, Rect rect, int index)
        {
            var indexName = index.ToString();

            rect.x += 5f;
            rect.width -= 25f;
            var elementProperty = property.GetArrayElementAtIndex(index);
            if (elementProperty.propertyType != SerializedPropertyType.Generic)
            {
                EditorGUI.PropertyField(rect, elementProperty, new GUIContent(indexName));
                return;
            }

            rect.x += 10f;
            rect.width -= 20f;
            rect.height = EditorGUIUtility.singleLineHeight;

            elementProperty.isExpanded = EditorGUI.Foldout(rect, elementProperty.isExpanded, new GUIContent(indexName));
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (!elementProperty.isExpanded)
                return;

            var depth = -1;
            while (elementProperty.NextVisible(true) || depth == -1)
            {
                if (depth != -1 && elementProperty.depth != depth)
                    break;
                depth = elementProperty.depth;
                rect.height = EditorGUI.GetPropertyHeight(elementProperty);
                EditorGUI.PropertyField(rect, elementProperty, true);
                rect.y += rect.height;
            }
        }
    }
}