
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(Switch))]
    public class SwitchEditor : Editor
    {
        SerializedProperty isOn;        
        SerializedProperty animator;        
        SerializedProperty onValueChanged;

        protected virtual void OnEnable()
        {
            isOn = serializedObject.FindProperty("isOn");
            animator = serializedObject.FindProperty("animator");              
            onValueChanged = serializedObject.FindProperty("onValueChanged");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(isOn); 
            EditorGUILayout.PropertyField(animator); 
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(onValueChanged);  
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
