
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ModalWindowContentFitterMultiButton))]
    public class ModalWindowContentFitterMultiButtonEditor : Editor
    {
        SerializedProperty iconTitle;
        SerializedProperty title;
        SerializedProperty buttonClose;          
        SerializedProperty buttonFirst; 
        SerializedProperty buttonSecond; 
        SerializedProperty buttonThird;         
        SerializedProperty animator; 
        SerializedProperty view;    
        SerializedProperty description;  
        SerializedProperty buttonBar;                
        SerializedProperty onFirst;
        SerializedProperty onSecond;  
        SerializedProperty onThird; 
        SerializedProperty onCancel;  

        protected virtual void OnEnable()
        {
            iconTitle = serializedObject.FindProperty("iconTitle");
            title = serializedObject.FindProperty("title");  
            buttonClose = serializedObject.FindProperty("buttonClose");
            buttonFirst = serializedObject.FindProperty("buttonFirst");  
            buttonSecond = serializedObject.FindProperty("buttonSecond");  
            buttonThird = serializedObject.FindProperty("buttonThird");  
            animator = serializedObject.FindProperty("animator");
            view = serializedObject.FindProperty("view");  
            description = serializedObject.FindProperty("description");
            buttonBar = serializedObject.FindProperty("buttonBar"); 
            onFirst = serializedObject.FindProperty("onFirst");            
            onSecond = serializedObject.FindProperty("onSecond");
            onThird = serializedObject.FindProperty("onThird");
            onCancel = serializedObject.FindProperty("onCancel");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(iconTitle); 
            EditorGUILayout.PropertyField(title); 
            EditorGUILayout.PropertyField(buttonClose); 
            EditorGUILayout.PropertyField(view); 
            EditorGUILayout.PropertyField(description); 
            EditorGUILayout.PropertyField(buttonBar);  
            EditorGUILayout.PropertyField(buttonFirst); 
            EditorGUILayout.PropertyField(buttonSecond); 
            EditorGUILayout.PropertyField(buttonThird); 
            EditorGUILayout.PropertyField(animator); 
            if((buttonClose.objectReferenceValue != null) || (buttonFirst.objectReferenceValue != null) || (buttonSecond.objectReferenceValue != null) || (buttonThird.objectReferenceValue != null))
            {
                EditorGUILayout.Space();
            } 
            if( buttonClose.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(onCancel);         
            }  
            if( buttonFirst.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(onFirst);         
            }  
            if( buttonSecond.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(onSecond);         
            }  
            if( buttonThird.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(onThird);         
            }             
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
