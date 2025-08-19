using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(SliderRangeTransition))]
    public class SliderRangeTransitionEditor : Editor
    {
        SerializedProperty curAxis;
        SerializedProperty fillRect;
        SerializedProperty handle1Rect;
        SerializedProperty handle2Rect;
        SerializedProperty minValue;
        SerializedProperty maxValue;
        SerializedProperty wholeNumbers;
        SerializedProperty value1;
        SerializedProperty value2;
        SerializedProperty hasText;
        SerializedProperty text1;
        SerializedProperty text2;
        SerializedProperty animatorHandle1; 
        SerializedProperty animatorHandle2; 
        SerializedProperty onValue1Changed;
        SerializedProperty onValue2Changed;

        protected void OnEnable()
        {
            fillRect = serializedObject.FindProperty("fillRect");
            handle1Rect = serializedObject.FindProperty("handle1Rect");
            handle2Rect = serializedObject.FindProperty("handle2Rect");
            curAxis = serializedObject.FindProperty("axis");
            minValue = serializedObject.FindProperty("minValue");
            maxValue = serializedObject.FindProperty("maxValue");
            wholeNumbers = serializedObject.FindProperty("wholeNumbers");
            value1 = serializedObject.FindProperty("value1");
            value2 = serializedObject.FindProperty("value2");
            hasText = serializedObject.FindProperty("hasText"); 
            text1 = serializedObject.FindProperty("text1");              
            text2 = serializedObject.FindProperty("text2");  
            animatorHandle1 = serializedObject.FindProperty("animatorHandle1");  
            animatorHandle2 = serializedObject.FindProperty("animatorHandle2");   
            onValue1Changed = serializedObject.FindProperty("onValue1Changed"); 
            onValue2Changed = serializedObject.FindProperty("onValue2Changed");                                  
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SliderRangeTransition slider = serializedObject.targetObject as SliderRangeTransition;
            EditorGUILayout.PropertyField(fillRect);
            EditorGUILayout.PropertyField(handle1Rect);
            EditorGUILayout.PropertyField(handle2Rect);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(curAxis);
            if (EditorGUI.EndChangeCheck())
            {
                slider.SetDirection((SliderRangeTransition.AxisEnum)curAxis.enumValueIndex);
            }
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
            EditorGUI.BeginChangeCheck();
            String strValue1 = "Value1";
            float newValue1 = EditorGUILayout.Slider(strValue1,value1.floatValue, minValue.floatValue, maxValue.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                if (newValue1 > value2.floatValue)
                {
                    newValue1 = value2.floatValue;
                }
                value1.floatValue = newValue1;
            }
            EditorGUI.BeginChangeCheck();
            String strValue2 = "Value2";
            float newValue2 = EditorGUILayout.Slider(strValue2, value2.floatValue, minValue.floatValue, maxValue.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                if (newValue2 < value1.floatValue)
                {
                    newValue2 = value1.floatValue;
                }
                value2.floatValue = newValue2;
            }
            EditorGUILayout.PropertyField(hasText); 
            if(hasText.boolValue == true)
            {
                EditorGUILayout.PropertyField(text1);
                EditorGUILayout.PropertyField(text2);
            }        
            EditorGUILayout.PropertyField(animatorHandle1);
            EditorGUILayout.PropertyField(animatorHandle2);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(onValue1Changed); 
            EditorGUILayout.PropertyField(onValue2Changed); 
            serializedObject.ApplyModifiedProperties();      
        }
    }
}
