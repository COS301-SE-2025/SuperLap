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
public class ModalWindowProgressBarLoopDemo : MonoBehaviour
{  
    //The ModalWindowProgressBarLoop Component.
    public ModalWindowProgressBarLoop m_ModalWindow; 
    //Update Coroutine.
    IEnumerator updateCoroutine;
    
    void Start()
    {
        //Add OnFinish event listener.
        m_ModalWindow.OnFinish.AddListener(ModalWindowFinish); 

        //Set the description.
        m_ModalWindow.DescriptionValue = "ModalWindowProgressBarLoop Demo."; 

        //Show Modal Window.
        m_ModalWindow.ShowModalWindow(); 
        //Update progressbar progress.    
        UpdateProgress();
    }    

    void ModalWindowFinish()
    {            
        Debug.Log("ModalWindowFinish");
    }

    void UpdateProgress()
    {
        if(updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }    
        updateCoroutine = UpdateTransition();              
        StartCoroutine(updateCoroutine);   
    }

    IEnumerator UpdateTransition()
    {  
        yield return new WaitForSeconds(2.5f);   
        m_ModalWindow.FinishProgress();
    }    
}
*/

namespace RainbowArt.CleanFlatUI
{
    public class ModalWindowProgressBarLoop : MonoBehaviour
    {       
        [SerializeField]
        Image iconTitle;

        [SerializeField]
        TextMeshProUGUI title;

        [SerializeField]
        TextMeshProUGUI description;     

        [SerializeField]
        Animator animator;  

        [SerializeField]
        ProgressBarLoop progressBar;

        [Serializable]
        public class ModalWindowEvent : UnityEvent{ }

        [SerializeField]
        ModalWindowEvent onFinish = new ModalWindowEvent(); 

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
        
        public ModalWindowEvent OnFinish
        {
            get => onFinish;
            set
            {
                onFinish = value;
            }
        }  

        IEnumerator diableCoroutine;
        float disableTime = 0.5f;

        public void ShowModalWindow()
        {
            gameObject.SetActive(true);       
            InitAnimation();   
            PlayAnimation(true); 
        }
        
        public void FinishProgress()
        {                                  
            HideModalWindow(); 
            onFinish.Invoke();             
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

        void OnProgressFinish()
        {
            onFinish.Invoke();
            HideModalWindow();            
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