
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(InputFieldSimple))]
    public class InputFieldSimpleEditor : Editor
    {
        SerializedProperty inputField;
        SerializedProperty background;
        SerializedProperty foreground;

        protected virtual void OnEnable()
        {
            inputField = serializedObject.FindProperty("inputField");    
            background = serializedObject.FindProperty("background");  
            foreground = serializedObject.FindProperty("foreground");              
        }

        public override void OnInspectorGUI()
        {            
            serializedObject.Update();
            EditorGUILayout.PropertyField(inputField);
            EditorGUILayout.PropertyField(background); 
            EditorGUILayout.PropertyField(foreground); 
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
