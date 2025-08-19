
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(SliderCircular))]
    public class SliderCircularEditor : Editor
    {           
        SerializedProperty fillImage;
        SerializedProperty handleRect;
        SerializedProperty handleRootRect;
        SerializedProperty fillOrigin;
        SerializedProperty clockwise;               
        SerializedProperty minValue;
        SerializedProperty maxValue;
        SerializedProperty wholeNumbers;
        SerializedProperty value;
        SerializedProperty hasText;
        SerializedProperty text;
        SerializedProperty onValueChanged;

        protected virtual void OnEnable()
        {
            fillImage = serializedObject.FindProperty("fillImage");
            handleRect = serializedObject.FindProperty("handleRect");
            handleRootRect = serializedObject.FindProperty("handleRootRect");
            fillOrigin = serializedObject.FindProperty("fillOrigin");  
            clockwise = serializedObject.FindProperty("clockwise");  
            minValue = serializedObject.FindProperty("minValue");  
            maxValue = serializedObject.FindProperty("maxValue");  
            wholeNumbers = serializedObject.FindProperty("wholeNumbers");  
            value = serializedObject.FindProperty("value");             
            hasText = serializedObject.FindProperty("hasText");
            text = serializedObject.FindProperty("text");  
            onValueChanged = serializedObject.FindProperty("onValueChanged");                 
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();         
            EditorGUILayout.PropertyField(fillImage); 
            EditorGUILayout.PropertyField(handleRect); 
            EditorGUILayout.PropertyField(handleRootRect); 
            EditorGUILayout.PropertyField(fillOrigin); 
            EditorGUILayout.PropertyField(clockwise); 
            EditorGUI.BeginChangeCheck();
            float newMin = EditorGUILayout.FloatField("Min Value", minValue.floatValue);
            if (EditorGUI.EndChangeCheck() && newMin <= maxValue.floatValue)
            {
                minValue.floatValue = newMin;
            }
            EditorGUI.BeginChangeCheck();
            float newMax = EditorGUILayout.FloatField("Max Value", maxValue.floatValue);
            if (EditorGUI.EndChangeCheck() && newMax >= minValue.floatValue)
            {
                maxValue.floatValue = newMax;
            }
            EditorGUILayout.PropertyField(wholeNumbers); 
            EditorGUILayout.Slider(value, minValue.floatValue, maxValue.floatValue);
            EditorGUILayout.PropertyField(hasText); 
            if(hasText.boolValue == true)
            {
                EditorGUILayout.PropertyField(text);
            }        
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(onValueChanged); 
            serializedObject.ApplyModifiedProperties();           
        }      
    }
}
