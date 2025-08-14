using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ProgressBarCircularRoundAuto))]
    public class ProgressBarCircularRoundAutoEditor : Editor
    {
        SerializedProperty minValue;
        SerializedProperty maxValue;
        SerializedProperty loadSpeed;
        SerializedProperty clockwise;
        SerializedProperty loop;
        SerializedProperty foreground;
        SerializedProperty roundArea;
        SerializedProperty roundImage;
        SerializedProperty origin;
        SerializedProperty hasText;
        SerializedProperty text;
        
        protected virtual void OnEnable()
        {
            minValue = serializedObject.FindProperty("minValue");
            maxValue = serializedObject.FindProperty("maxValue");  
            loadSpeed = serializedObject.FindProperty("loadSpeed");
            loop = serializedObject.FindProperty("loop");
            foreground = serializedObject.FindProperty("foreground");   
            roundArea = serializedObject.FindProperty("roundArea");
            roundImage = serializedObject.FindProperty("roundImage");
            origin = serializedObject.FindProperty("origin");   
            clockwise = serializedObject.FindProperty("clockwise");  
            hasText = serializedObject.FindProperty("hasText");
            text = serializedObject.FindProperty("text");           
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();    
            EditorGUILayout.PropertyField(minValue); 
            EditorGUILayout.PropertyField(maxValue); 
            EditorGUILayout.PropertyField(loadSpeed);
            EditorGUILayout.PropertyField(loop);
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(foreground);
            EditorGUILayout.PropertyField(roundArea); 
            EditorGUILayout.PropertyField(roundImage); 
            EditorGUILayout.PropertyField(origin); 
            EditorGUILayout.PropertyField(clockwise); 
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
