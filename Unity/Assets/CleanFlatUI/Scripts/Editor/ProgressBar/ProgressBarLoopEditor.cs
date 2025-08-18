
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ProgressBarLoop))]
    public class ProgressBarLoopEditor : Editor
    {
        SerializedProperty hasBackground;
        SerializedProperty background;

        protected virtual void OnEnable()
        {
            hasBackground = serializedObject.FindProperty("hasBackground");
            background = serializedObject.FindProperty("background");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();          
            EditorGUILayout.PropertyField(hasBackground); 
            if(hasBackground.boolValue == true)
            {
                EditorGUILayout.PropertyField(background);
            }           
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
