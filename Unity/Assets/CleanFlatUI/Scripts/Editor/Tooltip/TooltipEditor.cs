
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(Tooltip))]
    public class TooltipEditor : Editor
    {
        SerializedProperty description;
        SerializedProperty arrowRect;
        SerializedProperty distance;
        SerializedProperty animator;
        SerializedProperty origin;

        protected virtual void OnEnable()
        {
            description = serializedObject.FindProperty("description");
            arrowRect = serializedObject.FindProperty("arrowRect");  
            distance = serializedObject.FindProperty("distance");
            animator = serializedObject.FindProperty("animator");
            origin = serializedObject.FindProperty("origin");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(description); 
            EditorGUILayout.PropertyField(arrowRect); 
            EditorGUILayout.PropertyField(distance);
            EditorGUILayout.PropertyField(animator); 
            EditorGUILayout.PropertyField(origin);         
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
