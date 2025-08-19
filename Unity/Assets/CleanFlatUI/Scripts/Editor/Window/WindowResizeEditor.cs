
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(WindowResize))]
    public class WindowResizeEditor : Editor
    {
        SerializedProperty resizableArea;
        SerializedProperty cursorHorizonal;
        SerializedProperty cursorVertical;
        SerializedProperty cursorDiagonalLeft;
        SerializedProperty cursorDiagonalRight;

        protected virtual void OnEnable()
        {
            resizableArea = serializedObject.FindProperty("resizableArea");        
            cursorHorizonal = serializedObject.FindProperty("cursorHorizonal");
            cursorVertical = serializedObject.FindProperty("cursorVertical");
            cursorDiagonalLeft = serializedObject.FindProperty("cursorDiagonalLeft");
            cursorDiagonalRight = serializedObject.FindProperty("cursorDiagonalRight");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(resizableArea);
            EditorGUILayout.PropertyField(cursorHorizonal);
            EditorGUILayout.PropertyField(cursorVertical);
            EditorGUILayout.PropertyField(cursorDiagonalLeft);
            EditorGUILayout.PropertyField(cursorDiagonalRight);
            serializedObject.ApplyModifiedProperties();
        }
    }


}
