
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(SliderTransition))]
    public class SliderTransitionEditor : Editor
    {
        SerializedProperty slider;
        SerializedProperty animator;
        SerializedProperty hasText;
        SerializedProperty text;

        protected virtual void OnEnable()
        {
            slider = serializedObject.FindProperty("slider");    
            animator = serializedObject.FindProperty("animator");    
            hasText = serializedObject.FindProperty("hasText");
            text = serializedObject.FindProperty("text");                      
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();   
            EditorGUILayout.PropertyField(slider);        
            EditorGUILayout.PropertyField(animator);        
            EditorGUILayout.PropertyField(hasText); 
            if(hasText.boolValue == true)
            {
                EditorGUILayout.PropertyField(text);
            }                     
            serializedObject.ApplyModifiedProperties();           
        }      
    }
}
