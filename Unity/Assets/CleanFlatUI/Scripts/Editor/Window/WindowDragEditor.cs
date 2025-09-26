
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(WindowDrag))]
    public class WindowDragEditor : Editor
    {
        SerializedProperty draggableArea;

        protected virtual void OnEnable()
        {
            draggableArea = serializedObject.FindProperty("draggableArea");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(draggableArea);
            serializedObject.ApplyModifiedProperties();           
        }
    }

    
}
