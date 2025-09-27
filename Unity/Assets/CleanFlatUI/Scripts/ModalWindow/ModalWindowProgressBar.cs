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
public class ModalWindowProgressBarDemo : MonoBehaviour
{  
    //The ModalWindowProgressBar Component.
    public ModalWindowProgressBar m_ModalWindow; 
    //Update Coroutine.
    IEnumerator updateCoroutine;

    void Start()
    {
        //Add OnFinish event listener.
        m_ModalWindow.OnFinish.AddListener(ModalWindowFinish);  
        //Add OnCancel event listener.
        m_ModalWindow.OnCancel.AddListener(ModalWindowCancel);  

        //Set the description.
        m_ModalWindow.DescriptionValue = "ModalWindowProgressBar Demo."; 
                 
        //Show Modal Window.
        m_ModalWindow.ShowModalWindow();   
        //Update progressbar progress.    
        UpdateProgress();
    }    

    void ModalWindowFinish()
    {            
        Debug.Log("ModalWindowFinish");
    }

    void ModalWindowCancel()
    {
        Debug.Log("ModalWindowCancel");
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
        float curProgress = 0f;
        float maxProgress = 100.0f;  
        while (curProgress <= maxProgress) 
        {
            m_ModalWindow.SetProgress(curProgress);
            curProgress++;              
            yield return new WaitForSeconds(0.1f);
        }      
        m_ModalWindow.FinishProgress();
    }     
}
*/

namespace RainbowArt.CleanFlatUI
{
    public class ModalWindowProgressBar : MonoBehaviour
    {       
        [SerializeField]
        Image iconTitle;

        [SerializeField]
        TextMeshProUGUI title;

        [SerializeField]
        Button buttonClose; 

        [SerializeField]
        TextMeshProUGUI description;     

        [SerializeField]
        Animator animator;  

        [SerializeField]
        ProgressBar progressBar;

        [Serializable]
        public class ModalWindowEvent : UnityEvent{ }

        [SerializeField]
        ModalWindowEvent onCancel = new ModalWindowEvent();  

        [SerializeField]
        ModalWindowEvent onFinish = new ModalWindowEvent();  

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

        public ModalWindowEvent OnCancel
        {
            get => onCancel;
            set
            {
                onCancel = value;
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

        public void ShowModalWindow()
        {
            gameObject.SetActive(true);     
            InitButtons();  
            InitAnimation();   
            PlayAnimation(true); 
        }           

        public void SetProgress(float progress)
        {
            progressBar.CurrentValue = progress;
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

         void InitButtons()
        {
            if(buttonClose != null)
            {
                buttonClose.onClick.RemoveAllListeners(); 
                buttonClose.onClick.AddListener(OnCloseClick);  
            }             
        }

        void OnCloseClick()
        {
            onCancel.Invoke();
            HideModalWindow();                               
        }

        void OnProgressFinish()
        {            
            HideModalWindow();        
            onFinish.Invoke();    
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