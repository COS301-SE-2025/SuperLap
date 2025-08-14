
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(EventForward))]
    public class EventForwardEditor : Editor
    {    
        SerializedProperty targetGameObject;        

        protected virtual void OnEnable()
        {
            targetGameObject = serializedObject.FindProperty("targetGameObject");          
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(targetGameObject);   
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
