using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ModalWindowProgressBarLoop))]
    public class ModalWindowProgressBarLoopEditor : Editor
    {
        SerializedProperty iconTitle;
        SerializedProperty title;        
        SerializedProperty animator;  
        SerializedProperty description; 
        SerializedProperty progressBar;  
        SerializedProperty onFinish;             

        protected virtual void OnEnable()
        {
            iconTitle = serializedObject.FindProperty("iconTitle");
            title = serializedObject.FindProperty("title");  
            animator = serializedObject.FindProperty("animator");
            description = serializedObject.FindProperty("description");
            progressBar = serializedObject.FindProperty("progressBar");
            onFinish = serializedObject.FindProperty("onFinish");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(iconTitle); 
            EditorGUILayout.PropertyField(title);            
            EditorGUILayout.PropertyField(animator); 
            EditorGUILayout.PropertyField(description); 
            EditorGUILayout.PropertyField(progressBar); 
            EditorGUILayout.Space();      
            EditorGUILayout.PropertyField(onFinish);                
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
