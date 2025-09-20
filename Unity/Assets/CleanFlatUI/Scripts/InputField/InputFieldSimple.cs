using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

namespace RainbowArt.CleanFlatUI
{
    public class InputFieldSimple : MonoBehaviour
    {  
        [SerializeField]
        TMP_InputField inputField;           

        [SerializeField]
        Image background;

        [SerializeField]
        Image foreground;
        
        CanvasGroup canvasGroupBg;
        CanvasGroup canvasGroupFg;
        EventTrigger eventTrigger; 

        void Awake() 
        {
            AddTriggersListener(inputField.gameObject,EventTriggerType.Select,InputFieldIn);
            inputField.onEndEdit.AddListener(InputFieldOut);
            inputField.onValueChanged.AddListener(InputFieldValueChanged);
        }   

        void OnEnable()
        {   
            UpdateGUI(false);             
        }

        void AddTriggersListener(GameObject obj, EventTriggerType eventID, UnityAction<BaseEventData> action)
        {
            EventTrigger trigger = obj.GetComponent<EventTrigger>();
            if(trigger == null)
            {
                trigger = obj.AddComponent<EventTrigger>();
            }    
            if(trigger.triggers.Count == 0)
            {
                trigger.triggers = new List<EventTrigger.Entry>();
            }    
            UnityAction<BaseEventData> callback = new UnityAction<BaseEventData>(action);
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = eventID;
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
        }

        public void InputFieldIn(BaseEventData data) 
        {
            UpdateGUI(true);
        }

        public void InputFieldOut(string value)
        {
            UpdateGUI(false);
        }

        public void InputFieldValueChanged(string value)
        {
            if((value.Length == 0)||(value.Length == 1))
            {
                UpdateGUI(true);
            }
        }

        void InitCanvasGroup()
        {
            if(canvasGroupBg == null)
            {
                canvasGroupBg = background.gameObject.GetComponent<CanvasGroup>();
            } 
            if(canvasGroupFg == null)
            {
                canvasGroupFg = foreground.gameObject.GetComponent<CanvasGroup>();
            } 
        }

        public void UpdateGUI(bool bIn)
        {      
            InitCanvasGroup();      
            if(inputField.text.Length == 0)
            {
                if(!bIn)
                {
                    SetCanvasGroupAlpha(canvasGroupFg, 0f);  
                }
                else
                {
                    SetCanvasGroupAlpha(canvasGroupFg, 0f);  
                }                
            }  
            else
            {
                if(!bIn)
                {
                    SetCanvasGroupAlpha(canvasGroupFg, 1.0f);  
                }
                else
                {
                    SetCanvasGroupAlpha(canvasGroupFg, 1.0f);  
                }                               
            }
        }     

        void SetCanvasGroupAlpha(CanvasGroup obj,float alpha)
        {
            obj.alpha = alpha;
        }  
    }
}