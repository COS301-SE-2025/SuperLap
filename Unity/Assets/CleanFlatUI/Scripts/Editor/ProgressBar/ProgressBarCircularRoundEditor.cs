using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ProgressBarCircularRound))]
    public class ProgressBarCircularRoundEditor : Editor
    {
        SerializedProperty currentValue;
        SerializedProperty maxValue;
        SerializedProperty foreground;
        SerializedProperty hasText;
        SerializedProperty text;
        SerializedProperty roundArea;
        SerializedProperty roundImage;
        SerializedProperty origin;
        SerializedProperty clockwise;

        protected virtual void OnEnable()
        {
            currentValue = serializedObject.FindProperty("currentValue");
            maxValue = serializedObject.FindProperty("maxValue");  
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
            EditorGUILayout.PropertyField(currentValue); 
            EditorGUILayout.PropertyField(maxValue);          
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
            }serializedObject.ApplyModifiedProperties();           
        }
    }
}
