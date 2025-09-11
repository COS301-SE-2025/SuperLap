
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ProgressBarAuto))]
    public class ProgressBarAutoEditor : Editor
    {
        SerializedProperty minValue;
        SerializedProperty maxValue;
        SerializedProperty loadSpeed;
        SerializedProperty forward;
        SerializedProperty loop;
        SerializedProperty foreground;
        SerializedProperty hasText;
        SerializedProperty text;

        protected virtual void OnEnable()
        {
            minValue = serializedObject.FindProperty("minValue");
            maxValue = serializedObject.FindProperty("maxValue");  
            loadSpeed = serializedObject.FindProperty("loadSpeed");
            forward = serializedObject.FindProperty("forward");
            loop = serializedObject.FindProperty("loop");
            foreground = serializedObject.FindProperty("foreground");
            hasText = serializedObject.FindProperty("hasText");
            text = serializedObject.FindProperty("text");            
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(minValue); 
            EditorGUILayout.PropertyField(maxValue); 
            EditorGUILayout.PropertyField(loadSpeed);
            EditorGUILayout.PropertyField(forward);
            EditorGUILayout.PropertyField(loop);            
            EditorGUILayout.Separator();
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
