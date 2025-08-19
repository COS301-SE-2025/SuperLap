
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;


namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ProgressBarPatternCircularAuto))]
    public class ProgressBarPatternCircularAutoEditor : Editor
    {
        SerializedProperty minValue;
        SerializedProperty maxValue;
        SerializedProperty loadSpeed;
        SerializedProperty forward;
        SerializedProperty loop;
        SerializedProperty foreground;        
        SerializedProperty patternImage;
        SerializedProperty patternPlay;
        SerializedProperty patternSpeed;
        SerializedProperty patternForward;
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
            patternImage = serializedObject.FindProperty("patternImage");
            patternPlay = serializedObject.FindProperty("patternPlay");
            patternSpeed = serializedObject.FindProperty("patternSpeed");
            patternForward = serializedObject.FindProperty("patternForward");
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
            EditorGUILayout.PropertyField(patternImage);        
            EditorGUILayout.PropertyField(patternPlay);
            if(patternPlay.boolValue == true)
            {
                EditorGUILayout.PropertyField(patternSpeed);
                EditorGUILayout.PropertyField(patternForward);               
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
