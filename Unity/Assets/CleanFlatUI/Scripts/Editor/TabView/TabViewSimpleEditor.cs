
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(TabViewSimple))]
    public class TabViewSimpleEditor : Editor
    {
        SerializedProperty startIndex;
        SerializedProperty TabViewSimples;
        SerializedProperty onValueChanged;

        protected virtual void OnEnable()
        {
            startIndex = serializedObject.FindProperty("startIndex");
            TabViewSimples = serializedObject.FindProperty("TabViewSimples"); 
            onValueChanged = serializedObject.FindProperty("onValueChanged"); 
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(startIndex); 
            EditorGUILayout.PropertyField(TabViewSimples); 
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(onValueChanged);
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
