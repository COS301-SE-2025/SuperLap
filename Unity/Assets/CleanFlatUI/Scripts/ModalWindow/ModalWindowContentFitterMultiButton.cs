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
public class ModalWindowContentFitterMultiButtonDemo : MonoBehaviour
{  
    //The ModalWindowContentFitterMultiButton Component.
    public ModalWindowContentFitterMultiButton m_ModalWindow; 
    void Start()
    {
        //Add OnFirst event listener.
        m_ModalWindow.OnFirst.AddListener(ModalWindowFirst);
        //Add OnSecond event listener.
        m_ModalWindow.OnSecond.AddListener(ModalWindowSecond);
        //Add OnThird event listener.
        m_ModalWindow.OnThird.AddListener(ModalWindowThird);
        //Add OnCancel event listener.
        m_ModalWindow.OnCancel.AddListener(ModalWindowCancel);  
         
        //Set the description.
        m_ModalWindow.DescriptionValue = "ModalWindowContentFitterMultiButton Demo.";  
        
        //Show Modal Window.
        m_ModalWindow.ShowModalWindow(); 
    }

    void ModalWindowFirst()
    {
        Debug.Log("ModalWindowFirst");
    }

    void ModalWindowSecond()
    {
        Debug.Log("ModalWindowSecond");
    }

    void ModalWindowThird()
    {
        Debug.Log("ModalWindowThird");
    }

    void ModalWindowCancel()
    {
        Debug.Log("ModalWindowCancel");
    }
}
*/

namespace RainbowArt.CleanFlatUI
{
    [ExecuteAlways]
    public class ModalWindowContentFitterMultiButton : MonoBehaviour
    {       
        [SerializeField]
        Image iconTitle;

        [SerializeField]
        TextMeshProUGUI title;

        [SerializeField]
        Button buttonClose; 

        [SerializeField]
        Button buttonFirst; 
        
        [SerializeField]
        Button buttonSecond;    

        [SerializeField]
        Button buttonThird;  

        [SerializeField]
        Animator animator;  

        [SerializeField]
        RectTransform view;

        [SerializeField]
        TextMeshProUGUI description;  

        [SerializeField]
        RectTransform buttonBar; 

        [Serializable]
        public class ModalWindowEvent : UnityEvent{ }

        [SerializeField]
        ModalWindowEvent onFirst = new ModalWindowEvent();         

        [SerializeField]
        ModalWindowEvent onSecond = new ModalWindowEvent(); 

        [SerializeField]
        ModalWindowEvent onThird = new ModalWindowEvent(); 

        [SerializeField]
        ModalWindowEvent onCancel = new ModalWindowEvent(); 

        IEnumerator diableCoroutine;
        float disableTime = 0.5f;
        float spacing = 20.0f; 
        float elapsedTime = 0f;
        bool bDelayedUpdate = false;
        
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

        public ModalWindowEvent OnFirst
        {
            get => onFirst;
            set
            {
                onFirst = value;
            }
        }          

        public ModalWindowEvent OnSecond
        {
            get => onSecond;
            set
            {
                onSecond = value;
            }
        }  

        public ModalWindowEvent OnThird
        {
            get => onThird;
            set
            {
                onThird = value;
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
            UpdateHeight();   
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
            if(buttonFirst != null)
            {
                buttonFirst.onClick.RemoveAllListeners();
                buttonFirst.onClick.AddListener(OnFirstClick);  
            }
            if(buttonSecond != null)
            {   
                buttonSecond.onClick.RemoveAllListeners();
                buttonSecond.onClick.AddListener(OnSecondClick);  
            }  
            if(buttonThird != null)
            {
                buttonThird.onClick.RemoveAllListeners();
                buttonThird.onClick.AddListener(OnThirdClick);  
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

        void OnFirstClick()
        {
            HideModalWindow(); 
            onFirst.Invoke();                  
        }

        void OnSecondClick()
        {            
            HideModalWindow();      
            onSecond.Invoke(); 
        }

        void OnThirdClick()
        {            
            HideModalWindow();    
            onThird.Invoke();   
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

        void Update()
        {
            if(bDelayedUpdate)
            {
                elapsedTime += Time.deltaTime;
                if (elapsedTime >= 0.1)
                {
                    bDelayedUpdate = false;
                    UpdateHeight();                                           
                }
            }
        }    
        
        void UpdateHeight()
        {
            if(description != null)
            {
                RectTransform descriptionRect= description.GetComponent<RectTransform>();
                float finalHeight = -descriptionRect.anchoredPosition3D.y + description.preferredHeight + spacing;         
                if(buttonBar != null)
                {
                    float buttonBarHeight = buttonBar.rect.height;                    
                    Vector3 pos = buttonBar.anchoredPosition3D;
                    pos.y = -finalHeight;
                    buttonBar.anchoredPosition3D = pos;  
                    finalHeight = finalHeight+ buttonBarHeight;
                }                
                view.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);

                float pivotY = view.pivot.y;
                if(pivotY == 0.5f)
                    return;            
                float viewHeight = view.rect.height;
                Vector3 viewPos = view.anchoredPosition3D;
                viewPos.y = viewHeight*(pivotY-0.5f);
                view.anchoredPosition3D = viewPos; 
            }          
        }      

        #if UNITY_EDITOR
        protected void OnValidate()
        {
            bDelayedUpdate = true;
        }
        #endif       
    }
}