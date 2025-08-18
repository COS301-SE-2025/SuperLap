
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(TooltipSpecial))]
    public class TooltipSpecialEditor : Editor
    {
        SerializedProperty description;
        SerializedProperty animator;

        protected virtual void OnEnable()
        {
            description = serializedObject.FindProperty("description");
            animator = serializedObject.FindProperty("animator");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(description);
            EditorGUILayout.PropertyField(animator);      
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
