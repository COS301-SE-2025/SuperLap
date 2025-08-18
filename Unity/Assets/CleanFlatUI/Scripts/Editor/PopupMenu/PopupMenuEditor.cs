using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(PopupMenu))]
    public class PopupMenuEditor : Editor
    {
        SerializedProperty itemTemplate;
        SerializedProperty itemText;
        SerializedProperty itemImage;
        SerializedProperty itemLine;
        SerializedProperty animator;
        SerializedProperty padding;
        SerializedProperty spacing;
        SerializedProperty origin;
        SerializedProperty options;
        SerializedProperty onValueChanged;

        protected virtual void OnEnable()
        {
            itemTemplate = serializedObject.FindProperty("itemTemplate");
            itemText = serializedObject.FindProperty("itemText");
            itemImage = serializedObject.FindProperty("itemImage");  
            itemLine = serializedObject.FindProperty("itemLine");                  
            animator = serializedObject.FindProperty("animator");
            padding = serializedObject.FindProperty("padding");
            spacing = serializedObject.FindProperty("spacing");
            origin = serializedObject.FindProperty("origin");
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
            EditorGUILayout.PropertyField(origin);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(options);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(onValueChanged);      
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
