
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(WindowResizeHandle))]
    public class WindowResizeHandleEditor : Editor
    {
        SerializedProperty resizableArea;
        SerializedProperty topHandle;
        SerializedProperty bottomHandle;
        SerializedProperty leftHandle;
        SerializedProperty rightHandle;
        SerializedProperty leftTopHandle;
        SerializedProperty rightTopHandle;
        SerializedProperty leftBottomHandle;
        SerializedProperty rightBottomHandle;

        protected virtual void OnEnable()
        {
            resizableArea = serializedObject.FindProperty("resizableArea");
            topHandle = serializedObject.FindProperty("topHandle");
            bottomHandle = serializedObject.FindProperty("bottomHandle");
            leftHandle = serializedObject.FindProperty("leftHandle");
            rightHandle = serializedObject.FindProperty("rightHandle");
            leftTopHandle = serializedObject.FindProperty("leftTopHandle");
            rightTopHandle = serializedObject.FindProperty("rightTopHandle");
            leftBottomHandle = serializedObject.FindProperty("leftBottomHandle");
            rightBottomHandle = serializedObject.FindProperty("rightBottomHandle");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(resizableArea);
            EditorGUILayout.PropertyField(leftHandle);
            EditorGUILayout.PropertyField(rightHandle);
            EditorGUILayout.PropertyField(topHandle);
            EditorGUILayout.PropertyField(bottomHandle);
            EditorGUILayout.PropertyField(leftTopHandle);
            EditorGUILayout.PropertyField(rightTopHandle);
            EditorGUILayout.PropertyField(leftBottomHandle);
            EditorGUILayout.PropertyField(rightBottomHandle);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
