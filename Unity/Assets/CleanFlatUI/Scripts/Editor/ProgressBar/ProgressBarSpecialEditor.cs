
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;


namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ProgressBarSpecial))]
    public class ProgressBarSpecialEditor : Editor
    {
        SerializedProperty currentValue;
        SerializedProperty maxValue;
        SerializedProperty foregroundArea;
        SerializedProperty foreground;
        SerializedProperty hasText;
        SerializedProperty text;

        protected virtual void OnEnable()
        {
            currentValue = serializedObject.FindProperty("currentValue");
            maxValue = serializedObject.FindProperty("maxValue");  
            foregroundArea = serializedObject.FindProperty("foregroundArea");
            foreground = serializedObject.FindProperty("foreground");
            hasText = serializedObject.FindProperty("hasText");
            text = serializedObject.FindProperty("text");            
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(currentValue); 
            EditorGUILayout.PropertyField(maxValue); 
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(foregroundArea);
            EditorGUILayout.PropertyField(foreground);
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(hasText); 
            if(hasText.boolValue == true)
            {
                EditorGUILayout.PropertyField(text);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
