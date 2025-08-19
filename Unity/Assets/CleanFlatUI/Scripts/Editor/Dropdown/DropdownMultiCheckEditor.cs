
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.UI;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(DropdownMultiCheck))]
    public class DropdownMultiCheckEditor : DropdownEditor
    {      
        SerializedProperty selectedOptions;     
        SerializedProperty onSelectValueChanged;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            selectedOptions = serializedObject.FindProperty("selectedOptions");
            onSelectValueChanged = serializedObject.FindProperty("onSelectValueChanged");           
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();       
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(selectedOptions);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(onSelectValueChanged);  
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
