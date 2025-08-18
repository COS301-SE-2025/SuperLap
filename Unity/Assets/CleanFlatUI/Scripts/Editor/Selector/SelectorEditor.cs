
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(Selector))]
    public class SelectorEditor : Editor
    {           
        Selector selectorTarget;
        SerializedProperty buttonPrevious;
        SerializedProperty buttonNext;
        SerializedProperty imageNew;
        SerializedProperty imageCurrent;
        SerializedProperty textNew;
        SerializedProperty textCurrent;                
        SerializedProperty loop;
        SerializedProperty hasIndicator;
        SerializedProperty indicator;
        SerializedProperty indicatorRect;
        SerializedProperty animator;
        SerializedProperty startIndex;
        SerializedProperty options;
        SerializedProperty onValueChanged;
        protected virtual void OnEnable()
        {     
            selectorTarget = (Selector)target;
            buttonPrevious = serializedObject.FindProperty("buttonPrevious"); 
            buttonNext = serializedObject.FindProperty("buttonNext"); 
            imageNew = serializedObject.FindProperty("imageNew"); 
            imageCurrent = serializedObject.FindProperty("imageCurrent"); 
            textNew = serializedObject.FindProperty("textNew"); 
            textCurrent = serializedObject.FindProperty("textCurrent"); 
            loop = serializedObject.FindProperty("loop");  
            hasIndicator = serializedObject.FindProperty("hasIndicator");
            indicator = serializedObject.FindProperty("indicator");  
            indicatorRect = serializedObject.FindProperty("indicatorRect"); 
            animator = serializedObject.FindProperty("animator");  
            startIndex = serializedObject.FindProperty("startIndex");  
            options = serializedObject.FindProperty("options");
            onValueChanged = serializedObject.FindProperty("onValueChanged");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();  
            EditorGUILayout.PropertyField(buttonPrevious); 
            EditorGUILayout.PropertyField(buttonNext); 
            EditorGUILayout.PropertyField(imageNew); 
            EditorGUILayout.PropertyField(imageCurrent); 
            EditorGUILayout.PropertyField(textNew); 
            EditorGUILayout.PropertyField(textCurrent); 
            EditorGUILayout.PropertyField(loop); 
            EditorGUILayout.PropertyField(hasIndicator); 
            if(hasIndicator.boolValue == true)
            {
                EditorGUILayout.PropertyField(indicator);
                EditorGUILayout.PropertyField(indicatorRect);
            }        
            EditorGUILayout.PropertyField(animator); 
            EditorGUI.BeginChangeCheck();
            int newStartIndex = EditorGUILayout.IntField("Start Index", startIndex.intValue);
            if (EditorGUI.EndChangeCheck() && (newStartIndex < selectorTarget.options.Count) && (newStartIndex >= 0))
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
