using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.UIElements;

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

        ReorderableList reorderable;

        private void OnEnable()
        {
            var list = serializedObject.FindProperty("loadGroups");
            reorderable = CreateReorderableList(list);
        }

        private ReorderableList CreateReorderableList(SerializedProperty property)
        {
            return new ReorderableList(property.serializedObject, property, true, true, false, false)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, $"{property.displayName}: {property.arraySize}", EditorStyles.boldLabel);
                    var position =
                        new Rect(
                            rect.width - (EditorGUI.indentLevel - property.depth) * 15f - 10.5F,
                            rect.y,
                            20f,13f
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

                    rect.width -= 15F;
                    EditorGUI.PropertyField(rect, property.GetArrayElementAtIndex(index));

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

        public override void OnInspectorGUI()
        {
            reorderable.DoLayoutList();
        }
    }


    [CustomPropertyDrawer(typeof(LoadData))]
    public class LoadDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;

            FoldoutField(ref position, property, "Name", "dataName");

            EditorGUI.indentLevel++;

            for (int i = 0; i < property.FindPropertyRelative("sceneList").arraySize; i++)
            {
                var value = property.FindPropertyRelative("sceneList").GetArrayElementAtIndex(i);
                Field(ref position, property, $"{System.IO.Path.GetFileNameWithoutExtension(value.stringValue)}", value);
            }

            EditorGUI.indentLevel--;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var dataName = property.FindPropertyRelative("dataName");
            var height = GetPropertyHeight(dataName);

            for (int i = 0; i < property.FindPropertyRelative("sceneList").arraySize; i++)
            {
                var value = property.FindPropertyRelative("sceneList").GetArrayElementAtIndex(i);
                height += GetPropertyHeight(value);
            }

            return height;
        }

        private static float GetPropertyHeight(SerializedProperty property = null)
        {
            var height = property == null
                ? EditorGUIUtility.singleLineHeight
                : EditorGUI.GetPropertyHeight(property, true);

            return height + EditorGUIUtility.standardVerticalSpacing;
        }

        private static bool FoldoutField(ref Rect rect, SerializedProperty property, string label, string propertyName)
        {
            var prop = property.FindPropertyRelative(propertyName);
            prop.isExpanded = EditorGUI.Foldout(rect, prop.isExpanded, GUIContent.none);
            EditorGUI.PropertyField(rect, prop, new GUIContent(label));
            rect.y += GetPropertyHeight(prop);

            return prop.isExpanded;
        }

        private static void Field(ref Rect rect, SerializedProperty property, string label, string propertyName)
        {
            var prop = property.FindPropertyRelative(propertyName);
            EditorGUI.PropertyField(rect, prop, new GUIContent(label), true);
            rect.y += GetPropertyHeight(prop);
        }

        private static void Field(ref Rect rect, SerializedProperty property, string label, SerializedProperty prop)
        {
            EditorGUI.PropertyField(rect, prop, new GUIContent(label), true);
            rect.y += GetPropertyHeight(prop);
        }
    }
}