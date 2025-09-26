 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;


namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ProgressBarPattern))]
    public class ProgressBarPatternEditor : Editor
    {
        SerializedProperty currentValue;
        SerializedProperty maxValue;
        SerializedProperty foreground;
        SerializedProperty patternImage;
        SerializedProperty patternRect;
        SerializedProperty patternPlay;
        SerializedProperty patternSpeed;
        SerializedProperty patternForward;
        SerializedProperty patternScale;
        SerializedProperty hasText;
        SerializedProperty text;

        protected virtual void OnEnable()
        {
            currentValue = serializedObject.FindProperty("currentValue");
            maxValue = serializedObject.FindProperty("maxValue");  
            foreground = serializedObject.FindProperty("foreground");  
            patternImage = serializedObject.FindProperty("patternImage");
            patternRect = serializedObject.FindProperty("patternRect");
            patternPlay = serializedObject.FindProperty("patternPlay");
            patternSpeed = serializedObject.FindProperty("patternSpeed");
            patternForward = serializedObject.FindProperty("patternForward");
            patternScale = serializedObject.FindProperty("patternScale");
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
            EditorGUILayout.PropertyField(patternImage);
            EditorGUILayout.PropertyField(patternRect);
            EditorGUILayout.PropertyField(patternPlay);
            if(patternPlay.boolValue == true)
            {
                EditorGUILayout.PropertyField(patternSpeed);
                EditorGUILayout.PropertyField(patternForward);
                EditorGUILayout.PropertyField(patternScale);
            }
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
