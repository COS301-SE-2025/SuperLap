
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;


namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ProgressBarCircularMove))]
    public class ProgressBarCircularMoveEditor : Editor
    {
        SerializedProperty currentValue;
        SerializedProperty maxValue;
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
            currentValue = serializedObject.FindProperty("currentValue");
            maxValue = serializedObject.FindProperty("maxValue");  
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
            EditorGUILayout.PropertyField(currentValue); 
            EditorGUILayout.PropertyField(maxValue); 
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
                switch ((ProgressBarCircularMove.Origin)origin.enumValueIndex)
                {
                    case ProgressBarCircularMove.Origin.Bottom:
                        patternOrigin.intValue = (int)(ProgressBarCircularMove.PatternOriginHorizontal)EditorGUILayout.EnumPopup(
                            "Pattern Origin", (ProgressBarCircularMove.PatternOriginHorizontal)patternOrigin.intValue);
                        break;
                    case ProgressBarCircularMove.Origin.Top:
                        patternOrigin.intValue = (int)(ProgressBarCircularMove.PatternOriginHorizontal)EditorGUILayout.EnumPopup(
                            "Pattern Origin", (ProgressBarCircularMove.PatternOriginHorizontal)patternOrigin.intValue);
                        break;
                    case ProgressBarCircularMove.Origin.Left:
                        patternOrigin.intValue = (int)(ProgressBarCircularMove.PatternOriginVertical)EditorGUILayout.EnumPopup(
                            "Pattern Origin", (ProgressBarCircularMove.PatternOriginVertical)patternOrigin.intValue);
                        break;
                    case ProgressBarCircularMove.Origin.Right:
                        patternOrigin.intValue = (int)(ProgressBarCircularMove.PatternOriginVertical)EditorGUILayout.EnumPopup(
                            "Pattern Origin", (ProgressBarCircularMove.PatternOriginVertical)patternOrigin.intValue);
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
