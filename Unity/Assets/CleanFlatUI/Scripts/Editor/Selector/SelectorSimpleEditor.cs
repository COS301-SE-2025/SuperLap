
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(SelectorSimple))]
    public class SelectorSimpleEditor : Editor
    {           
        SelectorSimple SelectorSimpleTarget;
        SerializedProperty buttonPrevious;
        SerializedProperty buttonNext;
        SerializedProperty imageCurrent;
        SerializedProperty textCurrent;                
        SerializedProperty loop;
        SerializedProperty hasIndicator;
        SerializedProperty indicator;
        SerializedProperty indicatorRect;
        SerializedProperty startIndex;
        SerializedProperty options;
        SerializedProperty onValueChanged;
        protected virtual void OnEnable()
        {     
            SelectorSimpleTarget = (SelectorSimple)target;
            buttonPrevious = serializedObject.FindProperty("buttonPrevious"); 
            buttonNext = serializedObject.FindProperty("buttonNext"); 
            imageCurrent = serializedObject.FindProperty("imageCurrent");  
            textCurrent = serializedObject.FindProperty("textCurrent"); 
            loop = serializedObject.FindProperty("loop");  
            hasIndicator = serializedObject.FindProperty("hasIndicator");
            indicator = serializedObject.FindProperty("indicator");  
            indicatorRect = serializedObject.FindProperty("indicatorRect"); 
            startIndex = serializedObject.FindProperty("startIndex");  
            options = serializedObject.FindProperty("options");
            onValueChanged = serializedObject.FindProperty("onValueChanged");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();  
            EditorGUILayout.PropertyField(buttonPrevious); 
            EditorGUILayout.PropertyField(buttonNext); 
            EditorGUILayout.PropertyField(imageCurrent); 
            EditorGUILayout.PropertyField(textCurrent); 
            EditorGUILayout.PropertyField(loop); 
            EditorGUILayout.PropertyField(hasIndicator); 
            if(hasIndicator.boolValue == true)
            {
                EditorGUILayout.PropertyField(indicator);
                EditorGUILayout.PropertyField(indicatorRect);
            }        
            EditorGUI.BeginChangeCheck();
            int newStartIndex = EditorGUILayout.IntField("Start Index", startIndex.intValue);
            if (EditorGUI.EndChangeCheck() && (newStartIndex < SelectorSimpleTarget.options.Count) && (newStartIndex >= 0))
            {
                startIndex.intValue = newStartIndex;
            }
            EditorGUILayout.PropertyField(options);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(onValueChanged);
            serializedObject.ApplyModifiedProperties();           
        }      
    }
}
