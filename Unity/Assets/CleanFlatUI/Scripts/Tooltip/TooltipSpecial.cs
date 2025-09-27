using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
namespace RainbowArt.CleanFlatUI
{
    [ExecuteAlways]
    public class TooltipSpecial : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI description;

        [SerializeField]
        Animator animator; 

        float parentWidth;
        float parentHeight;

        enum Origin
        {
            Top = 0,
            RightTop,
            LeftTop,
            Bottom,
            RightBottom,
            LeftBottom         
        }           
        Origin origin = Origin.Top;     
         
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

        public void InitTooltip(Vector2 mousePosition, RectTransform areaScope)
        { 
            UpdateHeight();                 
            UpdatePosition(mousePosition,areaScope);
        }

        public void ShowTooltip()
        {
            gameObject.SetActive(true);    
            if(animator != null)
            {
                animator.enabled = false;
                animator.gameObject.transform.localScale = Vector3.one;
                animator.gameObject.transform.localEulerAngles = Vector3.zero;
            }   
            PlayAnimation(); 
        }

        public void HideTooltip()
        {
            gameObject.SetActive(false);    
        }   

        void Update()
        {
            if(bDelayedUpdate)
            {
                bDelayedUpdate = false;
                UpdateHeight();
            }
        }
          
        void UpdateHeight()
        {
            if(description != null)
            {
                RectTransform selfRect = GetComponent<RectTransform>();
                float finalHeight = description.preferredHeight + 40;
                selfRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);
            }
        }       

        void UpdatePosition(Vector2 mousePosition, RectTransform areaScope)
        {
            RectTransform selfRect = GetComponent<RectTransform>();
            selfRect.localPosition = new Vector3(mousePosition.x, mousePosition.y, 0);
            Vector3[] corners = new Vector3[4];
            selfRect.GetWorldCorners(corners);
            Vector3[] cornersInArea = new Vector3[4];
            float correctionX = 0;
            float correctionY = 0;
            for (int i = 0; i < 4; i++)
            {
                cornersInArea[i] = areaScope.InverseTransformPoint(corners[i]);
            }
            if (cornersInArea[2].x >= areaScope.rect.xMax)
            {
                if (cornersInArea[0].x - selfRect.rect.width / 2 < areaScope.rect.xMin)
                {
                    correctionX = cornersInArea[0].x - selfRect.rect.width / 2 - areaScope.rect.xMin;
                }
                if (cornersInArea[2].y >= areaScope.rect.yMax)
                {
                    origin = Origin.LeftBottom;
                    if (cornersInArea[0].y - selfRect.rect.height < areaScope.rect.yMin)
                    {
                        correctionY = cornersInArea[0].y - selfRect.rect.height - areaScope.rect.yMin;
                    }
                }
                else
                {
                    origin = Origin.LeftTop;
                }
            }
            else if (cornersInArea[0].x <= areaScope.rect.xMin)
            {
                if (cornersInArea[2].x + selfRect.rect.width / 2 > areaScope.rect.xMax)
                {
                    correctionX = cornersInArea[2].x + selfRect.rect.width / 2 - areaScope.rect.xMax;
                }
                if (cornersInArea[2].y >= areaScope.rect.yMax)
                {
                    origin = Origin.RightBottom;
                    if (cornersInArea[0].y - selfRect.rect.height < areaScope.rect.yMin)
                    {
                        correctionY = cornersInArea[0].y - selfRect.rect.height - areaScope.rect.yMin;
                    }
                }
                else
                {
                    origin = Origin.RightTop;
                }
            }
            else
            {
                if (cornersInArea[2].y >= areaScope.rect.yMax)
                {
                    if (cornersInArea[0].y - selfRect.rect.height < areaScope.rect.yMin)
                    {
                        correctionY = cornersInArea[0].y - selfRect.rect.height - areaScope.rect.yMin;
                    }
                    origin = Origin.Bottom;
                }
                else
                {
                    origin = Origin.Top;
                }
            }

            Vector3 pos = selfRect.localPosition;
            float selfWidth = selfRect.rect.width;
            float selfHeight = selfRect.rect.height;
            switch (origin)
            {
                case Origin.Top:
                {
                    break;
                }
                case Origin.RightTop:
                {
                    pos.x = pos.x + selfWidth / 2 - correctionX;
                    break;
                }
                case Origin.LeftTop:
                {
                    pos.x = pos.x - selfWidth / 2 - correctionX;
                    break;
                }
                case Origin.Bottom:
                {
                    pos.y = pos.y - selfHeight - correctionY;
                    break;
                }
                case Origin.RightBottom:
                {
                    pos.x = pos.x + selfWidth / 2 - correctionX;
                    pos.y = pos.y - selfHeight - correctionY;
                    break;
                }
                case Origin.LeftBottom:
                {
                    pos.x = pos.x - selfWidth / 2 - correctionX;
                    pos.y = pos.y - selfHeight - correctionY;
                    break;
                }
            }
            selfRect.localPosition = pos;
        }
        
        void PlayAnimation()
        {
            if(animator != null)
            {
                if(animator.enabled == false)
                {
                    animator.enabled = true;
                }
                animator.Play("Transition",0,0);  
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