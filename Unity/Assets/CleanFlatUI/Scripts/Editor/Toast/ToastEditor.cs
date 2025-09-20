using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(Toast))]
    public class ToastEditor : Editor
    {
        SerializedProperty icon;
        SerializedProperty title;
        SerializedProperty description;
        SerializedProperty animator;
        SerializedProperty showTime;
        SerializedProperty origin;
        SerializedProperty offsetX;
        SerializedProperty offsetY;

        protected virtual void OnEnable()
        {
            icon = serializedObject.FindProperty("icon");
            title = serializedObject.FindProperty("title");  
            description = serializedObject.FindProperty("description");         
            animator = serializedObject.FindProperty("animator");
            showTime = serializedObject.FindProperty("showTime");
            origin = serializedObject.FindProperty("origin");
            offsetX = serializedObject.FindProperty("offsetX");
            offsetY = serializedObject.FindProperty("offsetY");            
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(icon); 
            EditorGUILayout.PropertyField(title); 
            EditorGUILayout.PropertyField(description);
            EditorGUILayout.PropertyField(animator);     
            EditorGUILayout.PropertyField(showTime);     
            EditorGUILayout.PropertyField(origin);
            EditorGUILayout.PropertyField(offsetX);      
            EditorGUILayout.PropertyField(offsetY); 
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
