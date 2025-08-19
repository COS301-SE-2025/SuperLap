using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ToastUI))]
    public class ToastUIEditor : Editor
    {
        SerializedProperty button;
        SerializedProperty toast;

        protected virtual void OnEnable()
        {
            button = serializedObject.FindProperty("button");
            toast = serializedObject.FindProperty("toast");  
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(button); 
            EditorGUILayout.PropertyField(toast);       
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
