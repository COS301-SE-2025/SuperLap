
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ProgressBarBubble))]
    public class ProgressBarBubbleEditor : Editor
    {
        SerializedProperty currentValue;
        SerializedProperty maxValue;
        SerializedProperty foreground;
        SerializedProperty hasText;
        SerializedProperty text;
        SerializedProperty bubble;

        protected virtual void OnEnable()
        {
            currentValue = serializedObject.FindProperty("currentValue");
            maxValue = serializedObject.FindProperty("maxValue");  
            foreground = serializedObject.FindProperty("foreground");
            hasText = serializedObject.FindProperty("hasText");
            text = serializedObject.FindProperty("text");    
            bubble = serializedObject.FindProperty("bubble");        
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(currentValue); 
            EditorGUILayout.PropertyField(maxValue); 
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(foreground);
            EditorGUILayout.PropertyField(hasText); 
            if(hasText.boolValue == true)
            {
                EditorGUILayout.PropertyField(text);
                EditorGUILayout.PropertyField(bubble);
            }            
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
