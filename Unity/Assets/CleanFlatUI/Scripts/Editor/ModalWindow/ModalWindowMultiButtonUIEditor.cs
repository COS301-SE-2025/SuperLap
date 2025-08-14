
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(ModalWindowMultiButtonUI))]
    public class ModalWindowMultiButtonUIEditor : Editor
    {
        SerializedProperty button;
        SerializedProperty modalWindow;

        protected virtual void OnEnable()
        {
            button = serializedObject.FindProperty("button");
            modalWindow = serializedObject.FindProperty("modalWindow");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(button); 
            EditorGUILayout.PropertyField(modalWindow);                            
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
