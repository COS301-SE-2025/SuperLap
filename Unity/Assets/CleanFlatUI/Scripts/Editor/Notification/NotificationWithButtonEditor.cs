using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(NotificationWithButton))]
    public class NotificationWithButtonEditor : Editor
    {
        SerializedProperty icon;
        SerializedProperty title;
        SerializedProperty description;
        SerializedProperty animator;
        SerializedProperty showTime;
        SerializedProperty origin;
        SerializedProperty offsetX;
        SerializedProperty offsetY;   
        SerializedProperty buttonClose;
        SerializedProperty buttonFirst; 
        SerializedProperty buttonSecond; 
        SerializedProperty buttonThird;  
        SerializedProperty onCancel;
        SerializedProperty onFirst;
        SerializedProperty onSecond;  
        SerializedProperty onThird;           

        protected virtual void OnEnable()
        {
            icon = serializedObject.FindProperty("icon");
            title = serializedObject.FindProperty("title");  
            buttonClose = serializedObject.FindProperty("buttonClose");
            buttonFirst = serializedObject.FindProperty("buttonFirst");  
            buttonSecond = serializedObject.FindProperty("buttonSecond");  
            buttonThird = serializedObject.FindProperty("buttonThird"); 
            description = serializedObject.FindProperty("description");         
            animator = serializedObject.FindProperty("animator");
            showTime = serializedObject.FindProperty("showTime");    
            origin = serializedObject.FindProperty("origin");
            offsetX = serializedObject.FindProperty("offsetX");
            offsetY = serializedObject.FindProperty("offsetY");                 
            onCancel = serializedObject.FindProperty("onCancel");
            onFirst = serializedObject.FindProperty("onFirst");            
            onSecond = serializedObject.FindProperty("onSecond");
            onThird = serializedObject.FindProperty("onThird");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(icon); 
            EditorGUILayout.PropertyField(title); 
            EditorGUILayout.PropertyField(buttonClose); 
            EditorGUILayout.PropertyField(buttonFirst); 
            EditorGUILayout.PropertyField(buttonSecond); 
            EditorGUILayout.PropertyField(buttonThird);  
            EditorGUILayout.PropertyField(description);
            EditorGUILayout.PropertyField(animator); 
            EditorGUILayout.PropertyField(showTime);   
            EditorGUILayout.PropertyField(origin);
            EditorGUILayout.PropertyField(offsetX);      
            EditorGUILayout.PropertyField(offsetY);               
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
