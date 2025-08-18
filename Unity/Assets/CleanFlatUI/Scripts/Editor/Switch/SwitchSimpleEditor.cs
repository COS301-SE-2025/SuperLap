
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(SwitchSimple))]
    public class SwitchSimpleEditor : Editor
    {
        SerializedProperty isOn;           
        SerializedProperty onValueChanged;     
        SerializedProperty backgroundOn;
        SerializedProperty backgroundOff;
        SerializedProperty handleSlideArea; 
        SerializedProperty handleOn;
        SerializedProperty handleOff;             

        protected virtual void OnEnable()
        {
            isOn = serializedObject.FindProperty("isOn");
            backgroundOn = serializedObject.FindProperty("backgroundOn");              
            backgroundOff = serializedObject.FindProperty("backgroundOff");              
            handleSlideArea = serializedObject.FindProperty("handleSlideArea");             
            handleOn = serializedObject.FindProperty("handleOn");              
            handleOff = serializedObject.FindProperty("handleOff");              
            onValueChanged = serializedObject.FindProperty("onValueChanged");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(isOn); 
            EditorGUILayout.PropertyField(backgroundOn); 
            EditorGUILayout.PropertyField(backgroundOff); 
            EditorGUILayout.PropertyField(handleSlideArea); 
            EditorGUILayout.PropertyField(handleOn); 
            EditorGUILayout.PropertyField(handleOff);             
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(onValueChanged);  
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
