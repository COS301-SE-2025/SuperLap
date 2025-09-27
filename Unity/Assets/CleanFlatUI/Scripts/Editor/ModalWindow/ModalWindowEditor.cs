using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ModalWindow))]
    public class ModalWindowEditor : Editor
    {
        SerializedProperty iconTitle;
        SerializedProperty title;
        SerializedProperty buttonClose;         
        SerializedProperty buttonConfirm; 
        SerializedProperty buttonCancel;           
        SerializedProperty animator;  
        SerializedProperty description;              
        SerializedProperty onConfirm;
        SerializedProperty onCancel;  

        protected virtual void OnEnable()
        {
            iconTitle = serializedObject.FindProperty("iconTitle");
            title = serializedObject.FindProperty("title");  
            buttonClose = serializedObject.FindProperty("buttonClose");            
            buttonConfirm = serializedObject.FindProperty("buttonConfirm");  
            buttonCancel = serializedObject.FindProperty("buttonCancel");  
            animator = serializedObject.FindProperty("animator");
            description = serializedObject.FindProperty("description");
            onConfirm = serializedObject.FindProperty("onConfirm");
            onCancel = serializedObject.FindProperty("onCancel");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(iconTitle); 
            EditorGUILayout.PropertyField(title); 
            EditorGUILayout.PropertyField(buttonClose);             
            EditorGUILayout.PropertyField(buttonConfirm); 
            EditorGUILayout.PropertyField(buttonCancel); 
            EditorGUILayout.PropertyField(animator); 
            EditorGUILayout.PropertyField(description);  
            if((buttonClose.objectReferenceValue != null) || (buttonCancel.objectReferenceValue != null) || (buttonConfirm.objectReferenceValue != null))
            {
                EditorGUILayout.Space();
            } 
            if( buttonConfirm.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(onConfirm);         
            }  
            if( (buttonClose.objectReferenceValue != null) ||(buttonCancel.objectReferenceValue != null))
            {
                EditorGUILayout.PropertyField(onCancel);         
            }                                      
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
