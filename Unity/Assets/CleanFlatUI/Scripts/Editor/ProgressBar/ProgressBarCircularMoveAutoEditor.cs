
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;


namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ProgressBarCircularMoveAuto))]
    public class ProgressBarCircularMoveAutoEditor : Editor
    {
        SerializedProperty minValue;
        SerializedProperty maxValue;
        SerializedProperty loadSpeed;
        SerializedProperty forward;
        SerializedProperty loop;
        SerializedProperty foregroundArea;        
        SerializedProperty origin;
        SerializedProperty patternImage;
        SerializedProperty patternRect;
        SerializedProperty patternPlay;
        SerializedProperty patternSpeed;
        SerializedProperty patternOrigin;
        SerializedProperty hasText;
        SerializedProperty text;        

        protected virtual void OnEnable()
        {
            minValue = serializedObject.FindProperty("minValue");
            maxValue = serializedObject.FindProperty("maxValue");  
            loadSpeed = serializedObject.FindProperty("loadSpeed");
            forward = serializedObject.FindProperty("forward");
            loop = serializedObject.FindProperty("loop");
            foregroundArea = serializedObject.FindProperty("foregroundArea");
            origin = serializedObject.FindProperty("origin");
            patternImage = serializedObject.FindProperty("patternImage");
            patternRect = serializedObject.FindProperty("patternRect");
            patternPlay = serializedObject.FindProperty("patternPlay");
            patternSpeed = serializedObject.FindProperty("patternSpeed");
            patternOrigin = serializedObject.FindProperty("patternOrigin");
            hasText = serializedObject.FindProperty("hasText");
            text = serializedObject.FindProperty("text");            
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(minValue); 
            EditorGUILayout.PropertyField(maxValue); 
            EditorGUILayout.PropertyField(loadSpeed);
            EditorGUILayout.PropertyField(forward);
            EditorGUILayout.PropertyField(loop);            
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(foregroundArea); 
            EditorGUI.BeginChangeCheck();          
            EditorGUILayout.PropertyField(origin); 
            if (EditorGUI.EndChangeCheck())
            {
                patternOrigin.intValue = 0;
            } 
            EditorGUILayout.PropertyField(patternImage);
            EditorGUILayout.PropertyField(patternRect);
            EditorGUILayout.PropertyField(patternPlay);
            if(patternPlay.boolValue == true)
            {
                EditorGUILayout.PropertyField(patternSpeed);
                switch ((ProgressBarCircularMoveAuto.Origin)origin.enumValueIndex)
                {
                    case ProgressBarCircularMoveAuto.Origin.Bottom:
                        patternOrigin.intValue = (int)(ProgressBarCircularMoveAuto.PatternOriginHorizontal)EditorGUILayout.EnumPopup(
                            "Pattern Origin", (ProgressBarCircularMoveAuto.PatternOriginHorizontal)patternOrigin.intValue);
                        break;
                    case ProgressBarCircularMoveAuto.Origin.Top:
                        patternOrigin.intValue = (int)(ProgressBarCircularMoveAuto.PatternOriginHorizontal)EditorGUILayout.EnumPopup(
                            "Pattern Origin", (ProgressBarCircularMoveAuto.PatternOriginHorizontal)patternOrigin.intValue);
                        break;
                    case ProgressBarCircularMoveAuto.Origin.Left:
                        patternOrigin.intValue = (int)(ProgressBarCircularMoveAuto.PatternOriginVertical)EditorGUILayout.EnumPopup(
                            "Pattern Origin", (ProgressBarCircularMoveAuto.PatternOriginVertical)patternOrigin.intValue);
                        break;
                    case ProgressBarCircularMoveAuto.Origin.Right:
                        patternOrigin.intValue = (int)(ProgressBarCircularMoveAuto.PatternOriginVertical)EditorGUILayout.EnumPopup(
                            "Pattern Origin", (ProgressBarCircularMoveAuto.PatternOriginVertical)patternOrigin.intValue);
                        break;
                }                
            }
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(hasText); 
            if(hasText.boolValue == true)
            {
                EditorGUILayout.PropertyField(text);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
