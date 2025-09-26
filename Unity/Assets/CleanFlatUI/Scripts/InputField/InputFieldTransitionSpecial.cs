using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

namespace RainbowArt.CleanFlatUI
{
    public class InputFieldTransitionSpecial : MonoBehaviour
    {  
        [SerializeField]
        TMP_InputField inputField;

        [SerializeField]
        Animator animator;   
        
        EventTrigger eventTrigger;
        bool bDelayed = false;

        void Awake() 
        {
            ResetAnimation(animator);
            AddTriggersListener(inputField.gameObject,EventTriggerType.Select,InputFieldIn);
            inputField.onEndEdit.AddListener(InputFieldOut);
            inputField.onValueChanged.AddListener(InputFieldValueChanged);
        }    

        void OnEnable()
        {         
            UpdateGUI(false);  
        }

        void Update()
        {
            if(bDelayed)
            {
                bDelayed = false;
                UpdateGUI(false);
            }
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
            bDelayed = true;
        }

        public void InputFieldValueChanged(string value)
        {
            if((value.Length == 0)||(value.Length == 1))
            {
                UpdateGUI(true);
            }
        }

        public void UpdateGUI(bool bIn)
        {
            if(inputField.text.Length == 0)
            {
                if(bIn)
                {
                    PlayAnimation(animator, "In");
                }
                else
                {
                    PlayAnimation(animator, "Out");
                }                
            }  
            else
            {
                if(bIn)
                {
                    PlayAnimation(animator, "In Value");
                }
                else
                {
                    PlayAnimation(animator, "Out Value");
                } 
            }
        }        

        void PlayAnimation(Animator animator, string animStr)
        {
            if(animator != null)
            {
                if(animator.enabled == false)
                {
                    animator.enabled = true;
                }
                animator.Play(animStr, 0, 0);             
            }
        }

        void ResetAnimation(Animator animator)
        {
            if(animator != null)
            {
                animator.enabled = false;
            }
        }
    }
}