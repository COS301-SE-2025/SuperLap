
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(TransitionMultiClick))]
    public class TransitionMultiClickEditor : Editor
    {    
        SerializedProperty animators;        

        protected virtual void OnEnable()
        {
            animators = serializedObject.FindProperty("animators");          
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(animators);   
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
