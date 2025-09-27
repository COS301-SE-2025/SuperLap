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
public class SwitchSimpleDemo : MonoBehaviour
{
    //The SwitchSimple Component.
    public SwitchSimple m_Switch;
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
    public class SwitchSimple : MonoBehaviour,IPointerDownHandler 
    {
        [SerializeField]
        bool isOn = false;  

        [SerializeField]
        RectTransform backgroundOn;

        [SerializeField]
        RectTransform backgroundOff;

        [SerializeField]
        RectTransform handleOn;

        [SerializeField]
        RectTransform handleOff;

        [SerializeField]
        RectTransform handleSlideArea;     

        [Serializable]
        public class SwitchSimpleEvent : UnityEvent<bool>{ }

        [SerializeField]
        SwitchSimpleEvent onValueChanged = new SwitchSimpleEvent();      
        
        CanvasGroup canvasGroupBGOn;
        CanvasGroup canvasGroupBGOff;
        CanvasGroup canvasGroupOn;
        CanvasGroup canvasGroupOff;
             
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
                UpdateGUI();
            }
        }       

        public SwitchSimpleEvent OnValueChanged
        {
            get => onValueChanged;
            set
            {
                onValueChanged = value;
            }
        }        

        IEnumerator Start()
        {
            InitGUI();
            yield return null;
            UpdateGUI();                 
        }

        void InitGUI()
        {
            canvasGroupBGOn = backgroundOn.gameObject.GetComponent<CanvasGroup>();
            canvasGroupBGOff = backgroundOff.gameObject.GetComponent<CanvasGroup>();
            canvasGroupOn = handleOn.gameObject.GetComponent<CanvasGroup>();
            canvasGroupOff = handleOff.gameObject.GetComponent<CanvasGroup>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isOn = !isOn;  
            UpdateGUI();      
        }  

        void UpdateGUI()
        {  
            float maxWidth = handleSlideArea.rect.width; 
            handleOn.anchoredPosition3D = new Vector3(maxWidth, 0, 0);
            handleOff.anchoredPosition3D = new Vector3(0, 0, 0);
            
            if(isOn)
            {
                SetCanvasGroupAlpha(canvasGroupBGOn, 1.0f); 
                SetCanvasGroupAlpha(canvasGroupBGOff, 0f); 
                SetCanvasGroupAlpha(canvasGroupOn, 1.0f);                 
                SetCanvasGroupAlpha(canvasGroupOff, 0f);                    
                onValueChanged.Invoke(true);
            }
            else
            {
                SetCanvasGroupAlpha(canvasGroupBGOn, 0f); 
                SetCanvasGroupAlpha(canvasGroupBGOff, 1.0f); 
                SetCanvasGroupAlpha(canvasGroupOn, 0f);                 
                SetCanvasGroupAlpha(canvasGroupOff, 1.0f);       
                onValueChanged.Invoke(false);
            }        
        }       

        void SetCanvasGroupAlpha(CanvasGroup obj,float alpha)
        {
            obj.alpha = alpha;
        }   
    }
}