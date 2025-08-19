
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace RainbowArt.CleanFlatUI
{
    [CustomEditor(typeof(GradientModifier))]
    public class GradientModifierEditor : Editor
    {
        SerializedProperty gradientStyle;        
        SerializedProperty blend; 
        SerializedProperty moreVertices; 
        SerializedProperty offset; 
        SerializedProperty scale; 
        SerializedProperty gradient;    

        protected virtual void OnEnable()
        {
            gradientStyle = serializedObject.FindProperty("gradientStyle");
            blend = serializedObject.FindProperty("blend");    
            moreVertices = serializedObject.FindProperty("moreVertices");    
            offset = serializedObject.FindProperty("offset");    
            scale = serializedObject.FindProperty("scale");    
            gradient = serializedObject.FindProperty("gradient");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(gradientStyle); 
            EditorGUILayout.PropertyField(blend); 
            EditorGUILayout.PropertyField(moreVertices); 
            EditorGUILayout.PropertyField(offset); 
            EditorGUILayout.PropertyField(scale); 
            EditorGUILayout.PropertyField(gradient); 
            serializedObject.ApplyModifiedProperties();           
        }
    }
}
