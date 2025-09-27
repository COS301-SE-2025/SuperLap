using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ContextMenu))]
    public class ContextMenuEditor : Editor
    {
        SerializedProperty itemTemplate;
        SerializedProperty itemText;
        SerializedProperty itemImage;
        SerializedProperty itemLine;
        SerializedProperty animator;
        SerializedProperty options;
        SerializedProperty onValueChanged;
        SerializedProperty padding;
        SerializedProperty spacing;

        protected virtual void OnEnable()
        {
            itemTemplate = serializedObject.FindProperty("itemTemplate");
            itemText = serializedObject.FindProperty("itemText");
            itemImage = serializedObject.FindProperty("itemImage");
            itemLine = serializedObject.FindProperty("itemLine");            
            animator = serializedObject.FindProperty("animator");
            padding = serializedObject.FindProperty("padding");
            spacing = serializedObject.FindProperty("spacing");
            options = serializedObject.FindProperty("options");
            onValueChanged = serializedObject.FindProperty("onValueChanged");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(itemTemplate);
            EditorGUILayout.PropertyField(itemText);
            EditorGUILayout.PropertyField(itemImage);
            EditorGUILayout.PropertyField(itemLine);
            EditorGUILayout.PropertyField(animator);
            EditorGUILayout.PropertyField(padding);
            EditorGUILayout.PropertyField(spacing);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(options);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(onValueChanged);      
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
