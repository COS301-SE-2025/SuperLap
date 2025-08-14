
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ToggleTextSimple))]
    public class ToggleTextSimpleEditor : Editor
    {
        SerializedProperty toggle;
        SerializedProperty on;
        SerializedProperty off;

        protected virtual void OnEnable()
        {
            toggle = serializedObject.FindProperty("toggle");
            on = serializedObject.FindProperty("on");  
            off = serializedObject.FindProperty("off");  
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(toggle); 
            EditorGUILayout.PropertyField(on); 
            EditorGUILayout.PropertyField(off); 
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
