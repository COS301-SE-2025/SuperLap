
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(TabSimple))]
    public class TabSimpleEditor : Editor
    {
        SerializedProperty toggle;
        SerializedProperty checkmark;  
        SerializedProperty on;
        SerializedProperty off;            

        protected virtual void OnEnable()
        {
            toggle = serializedObject.FindProperty("toggle"); 
            checkmark = serializedObject.FindProperty("checkmark");    
            on = serializedObject.FindProperty("on");    
            off = serializedObject.FindProperty("off");                         
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(toggle); 
            EditorGUILayout.PropertyField(checkmark); 
            EditorGUILayout.PropertyField(on); 
            EditorGUILayout.PropertyField(off);             
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
