
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(TabViewGroup))]
    public class TabViewGroupEditor : Editor
    {        
        TabViewGroup tabViewGroupTarget;   
        SerializedProperty buttonPrevious;
        SerializedProperty buttonNext;       
        SerializedProperty startIndex;
        SerializedProperty tabViewGroups; 
        protected virtual void OnEnable()
        {     
            tabViewGroupTarget = (TabViewGroup)target;
            buttonPrevious = serializedObject.FindProperty("buttonPrevious"); 
            buttonNext = serializedObject.FindProperty("buttonNext");            
            startIndex = serializedObject.FindProperty("startIndex");  
            tabViewGroups = serializedObject.FindProperty("tabViewGroups");    
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();  
            EditorGUILayout.PropertyField(buttonPrevious); 
            EditorGUILayout.PropertyField(buttonNext); 
            EditorGUI.BeginChangeCheck();
            int newStartIndex = EditorGUILayout.IntField("Start Index", startIndex.intValue);
            if (EditorGUI.EndChangeCheck() && (newStartIndex < tabViewGroupTarget.tabViewGroups.Length) && (newStartIndex >= 0))
            {
                startIndex.intValue = newStartIndex;
            }
            EditorGUILayout.PropertyField(tabViewGroups); 
            serializedObject.ApplyModifiedProperties();           
        }      
    }
}
