using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(NotificationContentFitter))]
    public class NotificationContentFitterEditor : Editor
    {
        SerializedProperty icon;
        SerializedProperty title;
        SerializedProperty buttonClose;
        SerializedProperty description;
        SerializedProperty animator;
        SerializedProperty showTime;   
        SerializedProperty origin;
        SerializedProperty offsetX;
        SerializedProperty offsetY;                
        SerializedProperty onCancel;

        protected virtual void OnEnable()
        {
            icon = serializedObject.FindProperty("icon");
            title = serializedObject.FindProperty("title");  
            buttonClose = serializedObject.FindProperty("buttonClose");
            description = serializedObject.FindProperty("description");         
            animator = serializedObject.FindProperty("animator");
            showTime = serializedObject.FindProperty("showTime");    
            origin = serializedObject.FindProperty("origin");
            offsetX = serializedObject.FindProperty("offsetX");
            offsetY = serializedObject.FindProperty("offsetY");                
            onCancel = serializedObject.FindProperty("onCancel");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(icon); 
            EditorGUILayout.PropertyField(title); 
            EditorGUILayout.PropertyField(buttonClose);  
            EditorGUILayout.PropertyField(description);
            EditorGUILayout.PropertyField(animator); 
            EditorGUILayout.PropertyField(showTime);   
            EditorGUILayout.PropertyField(origin);
            EditorGUILayout.PropertyField(offsetX);      
            EditorGUILayout.PropertyField(offsetY);     
            if( buttonClose.objectReferenceValue != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(onCancel);         
            }  
            serializedObject.ApplyModifiedProperties();          
        }
    }
}
