
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(TabView))]
    public class TabViewEditor : Editor
    {
        SerializedProperty startIndex;
        SerializedProperty tabViews;
        SerializedProperty onValueChanged;

        protected virtual void OnEnable()
        {
            startIndex = serializedObject.FindProperty("startIndex");
            tabViews = serializedObject.FindProperty("tabViews"); 
            onValueChanged = serializedObject.FindProperty("onValueChanged"); 
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(startIndex); 
            EditorGUILayout.PropertyField(tabViews); 
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(onValueChanged);
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
