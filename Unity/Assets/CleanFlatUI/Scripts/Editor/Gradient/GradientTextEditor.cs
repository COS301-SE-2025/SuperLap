using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;
using TMPro.EditorUtilities;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(GradientText))]
    public class GradientTextEditor : TMP_EditorPanelUI
    {
        SerializedProperty colorGradientLine;
        SerializedProperty gradientColors;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            colorGradientLine = serializedObject.FindProperty("colorGradientLine");
            gradientColors = serializedObject.FindProperty("gradientColors");            
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(colorGradientLine);
            if (colorGradientLine.boolValue)
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(gradientColors);
                --EditorGUI.indentLevel;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
