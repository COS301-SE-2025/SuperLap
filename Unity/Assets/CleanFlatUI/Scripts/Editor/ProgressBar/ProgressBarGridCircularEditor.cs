
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ProgressBarGridCircular))]
    public class ProgressBarGridCircularEditor : Editor
    {
        SerializedProperty currentValue;
        SerializedProperty maxValue;
        SerializedProperty background;
        SerializedProperty foreground;
        SerializedProperty bgTemplate;
        SerializedProperty fgTemplate;
        SerializedProperty hasText;
        SerializedProperty text;

        protected virtual void OnEnable()
        {
            currentValue = serializedObject.FindProperty("currentValue");
            maxValue = serializedObject.FindProperty("maxValue");
            background = serializedObject.FindProperty("background");
            foreground = serializedObject.FindProperty("foreground");
            bgTemplate = serializedObject.FindProperty("bgTemplate");
            fgTemplate = serializedObject.FindProperty("fgTemplate");
            hasText = serializedObject.FindProperty("hasText");
            text = serializedObject.FindProperty("text");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(currentValue); 
            EditorGUILayout.PropertyField(maxValue);            
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(background);
            EditorGUILayout.PropertyField(foreground);
            EditorGUILayout.PropertyField(bgTemplate);
            EditorGUILayout.PropertyField(fgTemplate);
            EditorGUILayout.PropertyField(hasText); 
            if(hasText.boolValue == true)
            {
                EditorGUILayout.PropertyField(text);
            }            
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
