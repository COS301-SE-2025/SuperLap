using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/*
//Set properties in C# example codes.
using RainbowArt.CleanFlatUI;
public class SwitchDemo : MonoBehaviour
{
    //The Switch Component.
    public Switch m_Switch;
    void Start()
    {
        //Add OnValueChanged event listener.
        m_Switch.OnValueChanged.AddListener(SwitchValueChange);
        //Set current value.
        m_Switch.IsOn = true;
    }
    public void SwitchValueChange(bool val)
    {
        Debug.Log("SwitchValueChange, value: " + val);
    } 
}
*/

namespace RainbowArt.CleanFlatUI
{
    public class Switch : MonoBehaviour,IPointerDownHandler 
    {
        [SerializeField]
        bool isOn = false;   

        [SerializeField]        
        Animator animator;  

        [Serializable]
        public class SwitchEvent : UnityEvent<bool>{ }

        [SerializeField]
        SwitchEvent onValueChanged = new SwitchEvent();     
             
        public bool IsOn
        {
            get => isOn;
            set
            {
                if(isOn == value)
                {
                    return;
                }
                isOn = value;
                UpdateGUI(false);
            }
        }    

        public SwitchEvent OnValueChanged
        {
            get => onValueChanged;
            set
            {
                onValueChanged = value;
            }
        }  

        void Start () 
        {
           UpdateGUI(true);
        }   

        public void OnPointerDown(PointerEventData eventData)
        {
            isOn = !isOn;  
            UpdateGUI(false);      
        }  

        void UpdateGUI(bool isInit)
        {
            if(isInit)
            {
                if(isOn)
                {
                    animator.Play("On Init",0,0); 
                }
                else
                {
                    animator.Play("Off Init",0,0); 
                } 
            }
            else
            {
                if(isOn)
                {
                    animator.Play("On",0,0); 
                    onValueChanged.Invoke(true);
                }
                else
                {
                    animator.Play("Off",0,0); 
                    onValueChanged.Invoke(false);
                } 
            }            
        }  
    }
}