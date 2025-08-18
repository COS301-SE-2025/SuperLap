using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

/*
//Set properties in C# example codes.
using RainbowArt.CleanFlatUI;
public class ModalWindowDemo : MonoBehaviour
{  
    //The ModalWindow Component.
    public ModalWindow m_ModalWindow; 
    void Start()
    {
        //Add OnConfirm event listener.
        m_ModalWindow.OnConfirm.AddListener(ModalWindowConfirm);  
        //Add OnCancel event listener.
        m_ModalWindow.OnCancel.AddListener(ModalWindowCancel); 

        //Set the description.
        m_ModalWindow.DescriptionValue = "ModalWindow Demo.";   
            
        //Show Modal Window.
        m_ModalWindow.ShowModalWindow(); 
    }    

    void ModalWindowConfirm()
    {            
        Debug.Log("ModalWindowConfirm");
    }

    void ModalWindowCancel()
    {
        Debug.Log("ModalWindowCancel");
    }
}
*/

namespace RainbowArt.CleanFlatUI
{
    public class ModalWindow : MonoBehaviour
    {       
        [SerializeField]
        Image iconTitle;

        [SerializeField]
        TextMeshProUGUI title;

        [SerializeField]
        Button buttonClose; 

        [SerializeField]
        Button buttonConfirm; 
        
        [SerializeField]
        Button buttonCancel;    

        [SerializeField]
        Animator animator;   

        [SerializeField]
        TextMeshProUGUI description;    

        [Serializable]
        public class ModalWindowEvent : UnityEvent{ }

        [SerializeField]
        ModalWindowEvent onConfirm = new ModalWindowEvent();         

        [SerializeField]
        ModalWindowEvent onCancel = new ModalWindowEvent();    

        IEnumerator diableCoroutine;
        float disableTime = 0.5f;

        public string DescriptionValue
        {
            get
            {
                if(description != null)
                {
                    return description.text;
                }
                return "";               
            }
            set
            {
                if(description != null)
                {
                    description.text = value;
                }  
            }
        } 
        
        public string TitleValue
        {
            get
            {
                if(title != null)
                {
                    return title.text;
                }
                return "";               
            }
            set
            {
                if(title != null)
                {
                    title.text = value;
                }  
            }
        } 

        public Sprite IconValue
        {
            get
            {
                if(iconTitle != null)
                {
                    return iconTitle.sprite;
                }
                return null;           
            }
            set
            {
                if(iconTitle != null)
                {
                    if(value != null)
                    {
                        iconTitle.gameObject.SetActive(true);
                        iconTitle.sprite = value;
                    }
                    else
                    {
                        iconTitle.gameObject.SetActive(false);
                        iconTitle.sprite = null;
                    }                    
                }                
            }
        }   

        public ModalWindowEvent OnConfirm
        {
            get => onConfirm;
            set
            {
                onConfirm = value;
            }
        }   

        public ModalWindowEvent OnCancel
        {
            get => onCancel;
            set
            {
                onCancel = value;
            }
        } 

        public void ShowModalWindow()
        {
            gameObject.SetActive(true);  
            InitButtons();    
            InitAnimation();     
            PlayAnimation(true); 
        }
        
        public void HideModalWindow()
        {
            PlayAnimation(false);    
            if(animator != null)
            {
                if(diableCoroutine != null)
                {
                    StopCoroutine(diableCoroutine);
                    diableCoroutine = null;
                }    
                diableCoroutine = DisableTransition();              
                StartCoroutine(diableCoroutine); 
            }  
            else
            {
                gameObject.SetActive(false);
            }          
        }

        IEnumerator DisableTransition()
        {
            yield return new WaitForSeconds(disableTime);
            gameObject.SetActive(false);         
        }  
      
        void InitButtons()
        {
            if(buttonClose != null)
            {
                buttonClose.onClick.RemoveAllListeners(); 
                buttonClose.onClick.AddListener(OnCloseClick);  
            }
            if(buttonConfirm != null)
            {
                buttonConfirm.onClick.RemoveAllListeners();
                buttonConfirm.onClick.AddListener(OnConfirmClick);  
            }
            if(buttonCancel != null)
            {   
                buttonCancel.onClick.RemoveAllListeners();
                buttonCancel.onClick.AddListener(OnCancelClick);  
            }            
        }

        void OnCloseClick()
        {
            OnCancelClick();           
        }

        void OnCancelClick()
        {            
            HideModalWindow();    
            onCancel.Invoke();        
        }

        void OnConfirmClick()
        {           
            HideModalWindow();    
            onConfirm.Invoke();   
        }

        void InitAnimation()
        {
            if(animator != null)
            {
                animator.enabled = false;
                animator.gameObject.transform.localScale = Vector3.one;
                animator.gameObject.transform.localEulerAngles = Vector3.zero;
            } 
        }
        
        void PlayAnimation(bool bShow)
        {
            if(animator != null)
            {
                if(animator.enabled == false)
                {
                    animator.enabled = true;
                }
                if(bShow)
                {
                    animator.Play("In",0,0);  
                }
                else
                {
                    animator.Play("Out",0,0);  
                }
            }            
        }         
    }
}