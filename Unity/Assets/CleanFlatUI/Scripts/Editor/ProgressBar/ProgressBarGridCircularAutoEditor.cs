
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ProgressBarGridCircularAuto))]
    public class ProgressBarGridCircularAutoEditor : Editor
    {
        SerializedProperty minValue;
        SerializedProperty maxValue;
        SerializedProperty loadSpeed;
        SerializedProperty forward;
        SerializedProperty loop;       
        SerializedProperty background;
        SerializedProperty foreground;
        SerializedProperty bgTemplate;
        SerializedProperty fgTemplate;
        SerializedProperty hasText;
        SerializedProperty text;

        protected virtual void OnEnable()
        {
            minValue = serializedObject.FindProperty("minValue");
            maxValue = serializedObject.FindProperty("maxValue");  
            loadSpeed = serializedObject.FindProperty("loadSpeed");
            forward = serializedObject.FindProperty("forward");
            loop = serializedObject.FindProperty("loop");
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
            EditorGUILayout.PropertyField(minValue); 
            EditorGUILayout.PropertyField(maxValue); 
            EditorGUILayout.PropertyField(loadSpeed);
            EditorGUILayout.PropertyField(forward);
            EditorGUILayout.PropertyField(loop);            
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(background);
            EditorGUILayout.PropertyField(foreground);
            EditorGUILayout.PropertyField(bgTemplate);
            EditorGUILayout.PropertyField(fgTemplate);
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
