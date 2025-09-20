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
public class NotificationDemo : MonoBehaviour
{  
    //The Notification Component.
    public Notification m_Notification; 
    void Start()
    {    
        //Add OnCancel event listener.
        m_Notification.OnCancel.AddListener(NotificationCancel);  

        //Set the showing time.  
        m_Notification.ShowTime = 2.0f;  
        //Set the origin point relative to the canvas.
        m_Notification.CurOrigin = Notification.Origin.TopCenter;
        //Set the offset X relative to the reference origin point.
        m_Notification.OffsetX = 0;
        //Set the offset Y relative to the reference origin point.
        m_Notification.OffsetY = -100;
        //Set the description.
        m_Notification.DescriptionValue = "Notification Demo.";
        
        //Show Notification.
        m_Notification.ShowNotification();
    }        

    void NotificationCancel()
    {
        Debug.Log("NotificationCancel");
    }
}
*/

namespace RainbowArt.CleanFlatUI
{
    public class Notification : MonoBehaviour
    {      
        [SerializeField]
        Image icon;
        
        [SerializeField]
        TextMeshProUGUI title;  

        [SerializeField]
        TextMeshProUGUI description;

        [SerializeField]
        Animator animator;

        [SerializeField]
        float showTime = 2.0f;

        [SerializeField]
        float offsetX = 0f;

        [SerializeField]
        float offsetY = 0f;    

        public enum Origin
        {
            TopLeft,
            TopCenter,
            TopRight,
            BottomLeft,
            BottomCenter,
            BottomRight
        }

        [SerializeField]
        Origin origin = Origin.TopCenter;

        [SerializeField]
        Button buttonClose; 

        [Serializable]
        public class NotificationEvent : UnityEvent{ }

        [SerializeField]   
        NotificationEvent onCancel = new NotificationEvent();     

        float disableTime = 1.0f;        
        List<Canvas> tempCanvasList = new List<Canvas>();   
        IEnumerator transitionCoroutine;
        IEnumerator diableCoroutine;

        Vector3? initAnchoredPosition;
        Vector3 InitPosition
        {
            get
            {
                if(initAnchoredPosition == null)
                {
                    initAnchoredPosition = GetComponent<RectTransform>().anchoredPosition3D;
                }
                return initAnchoredPosition ?? Vector3.zero;
            }
        }

        public float ShowTime
        {
            get => showTime;
            set
            {
                showTime = value;
            }
        }

        public Origin CurOrigin
        {
            get => origin;
            set
            {                
                origin = value;
            }
        }    

        public float OffsetX
        {
            get => offsetX;
            set
            {                
                offsetX = value;
            }
        }    

        public float OffsetY
        {
            get => offsetY;
            set
            {                
                offsetY = value;
            }
        }  

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
                if(icon != null)
                {
                    return icon.sprite;
                }
                return null;           
            }
            set
            {
                if(icon != null)
                {
                    if(value != null)
                    {
                        icon.gameObject.SetActive(true);
                        icon.sprite = value;
                    }
                    else
                    {
                        icon.gameObject.SetActive(false);
                        icon.sprite = null;
                    }                    
                }                
            }
        }    

        public NotificationEvent OnCancel
        {
            get => onCancel;
            set
            {
                onCancel = value;
            }
        }  

        public void ShowNotification()
        {
            gameObject.SetActive(true);  
            InitButtons();
            InitAnimation();                
            UpdatePosition();
            if(animator != null)
            {
                PlayAnimation(true); 
            }
            StartTransition(true);
        }

        public void HideNotification()
        {
            StartTransition(false);
        }   
       
        void UpdatePosition()
        {
            tempCanvasList.Clear();
            GetComponentsInParent(false, tempCanvasList);
            if (tempCanvasList.Count == 0)
            {
                return;
            }
            Canvas rootCanvas = tempCanvasList[tempCanvasList.Count - 1];
            for (int i = 0; i < tempCanvasList.Count; i++)
            {
                if (tempCanvasList[i].isRootCanvas)
                {
                    rootCanvas = tempCanvasList[i];
                    break;
                }
            }
            tempCanvasList.Clear();
            RectTransform canvasTrans = rootCanvas.GetComponent<RectTransform>();
            RectTransform rectTrans = GetComponent<RectTransform>();                       
            Vector3[] corners = new Vector3[4];
            canvasTrans.GetWorldCorners(corners); 
            Vector3 canvasCornerLeftBottom = rectTrans.parent.InverseTransformPoint(corners[0]);
            Vector3 canvasCornerRightTop = rectTrans.parent.InverseTransformPoint(corners[2]);
            rectTrans.anchoredPosition3D = InitPosition; 
            Vector3 pos = rectTrans.localPosition;
            float canvasMinX = canvasCornerLeftBottom.x;
            float canvasMaxX = canvasCornerRightTop.x;
            float canvasMinY = canvasCornerLeftBottom.y;
            float canvasMaxY = canvasCornerRightTop.y;
            
            switch (origin)
            {                
                case Origin.TopCenter:
                {
                    pos.x = (canvasMinX + canvasMaxX) / 2 + offsetX;
                    pos.y = canvasMaxY - rectTrans.rect.height/2 + offsetY; 
                    break;
                }
                case Origin.BottomCenter:
                {
                    pos.x = (canvasMinX + canvasMaxX) / 2 + offsetX;
                    pos.y = canvasMinY + rectTrans.rect.height/2 + offsetY;
                    break;
                }
                case Origin.TopLeft:
                {                    
                    pos.x = canvasMinX + rectTrans.rect.width/2 + offsetX; 
                    pos.y = canvasMaxY - rectTrans.rect.height/2 + offsetY; 
                    break;
                }
                case Origin.BottomLeft:
                {
                    pos.x = canvasMinX + rectTrans.rect.width/2 + offsetX; 
                    pos.y = canvasMinY + rectTrans.rect.height/2 + offsetY;
                    break;
                }
                case Origin.TopRight:
                {
                    pos.x = canvasMaxX - rectTrans.rect.width/2 + offsetX; 
                    pos.y = canvasMaxY - rectTrans.rect.height/2 + offsetY; 
                    break;
                }
                case Origin.BottomRight:
                {
                    pos.x = canvasMaxX - rectTrans.rect.width/2 + offsetX; 
                    pos.y = canvasMinY + rectTrans.rect.height/2 + offsetY;
                    break;
                }
            }
            float minX = canvasMinX + rectTrans.rect.width/2;
            float maxX = canvasMaxX - rectTrans.rect.width/2;
            float minY = canvasMinY + rectTrans.rect.height/2;
            float maxY = canvasMaxY - rectTrans.rect.height/2;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            rectTrans.localPosition = pos;     
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

        void StartTransition(bool bShow)
        {
            if(bShow)
            {
                if(transitionCoroutine != null)
                {
                    StopCoroutine(transitionCoroutine);
                    transitionCoroutine = null;
                }    
                transitionCoroutine = UpdateTransition();              
                StartCoroutine(transitionCoroutine); 
            }
            else
            {
                if(diableCoroutine != null)
                {
                    StopCoroutine(diableCoroutine);
                    diableCoroutine = null;
                }    
                diableCoroutine = DisableTransition();              
                StartCoroutine(diableCoroutine); 
            }           
        }

        IEnumerator UpdateTransition()
        {
            yield return new WaitForSeconds(showTime);
            if(animator != null)
            {
                PlayAnimation(false);
                yield return new WaitForSeconds(disableTime);
            }              
            gameObject.SetActive(false);      
        }

        IEnumerator DisableTransition()
        {
            if(animator != null)
            {
                PlayAnimation(false);
                yield return new WaitForSeconds(disableTime);
            }  
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
            if(transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }  
            HideNotification();    
            onCancel.Invoke();              
        }   
    }
}