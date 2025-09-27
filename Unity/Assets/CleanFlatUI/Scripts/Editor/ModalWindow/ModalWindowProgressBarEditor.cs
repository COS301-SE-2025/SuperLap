using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ModalWindowProgressBar))]
    public class ModalWindowProgressBarEditor : Editor
    {
        SerializedProperty iconTitle;
        SerializedProperty title;    
        SerializedProperty buttonClose;       
        SerializedProperty animator;  
        SerializedProperty description; 
        SerializedProperty progressBar;  
        SerializedProperty onCancel;     
        SerializedProperty onFinish;             

        protected virtual void OnEnable()
        {
            iconTitle = serializedObject.FindProperty("iconTitle");
            title = serializedObject.FindProperty("title");  
            buttonClose = serializedObject.FindProperty("buttonClose");            
            animator = serializedObject.FindProperty("animator");
            description = serializedObject.FindProperty("description");
            progressBar = serializedObject.FindProperty("progressBar");
            onCancel = serializedObject.FindProperty("onCancel");
            onFinish = serializedObject.FindProperty("onFinish");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(iconTitle); 
            EditorGUILayout.PropertyField(title);    
            EditorGUILayout.PropertyField(buttonClose);         
            EditorGUILayout.PropertyField(animator); 
            EditorGUILayout.PropertyField(description); 
            EditorGUILayout.PropertyField(progressBar); 
            EditorGUILayout.Space();   
            if(buttonClose.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(onCancel);
            }                  
            EditorGUILayout.PropertyField(onFinish);                
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
