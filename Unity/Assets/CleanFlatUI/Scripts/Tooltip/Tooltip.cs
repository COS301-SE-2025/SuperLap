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
    public class Tooltip : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI description;

        [SerializeField]
        RectTransform arrowRect;

        [SerializeField]
        uint distance = 0;

        [SerializeField]
        Animator animator;      
          
        public enum Origin
        {
            Top = 0,
            Bottom,
            Left,
            Right 
        }

        [SerializeField]
        Origin origin = Origin.Top;

        bool bDelayedUpdate = false;

        public uint Distance
        {
            get => distance;
            set
            {
                distance = value;
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

        public void SetTooltipPosition(Vector3 position,float width, float height)
        {
            RectTransform tooltipRect = GetComponent<RectTransform>();
            float halfParentRectWidth = width/2;
            float halfParentRectHeight = height/2;
            float tooltipPosX = position.x;
            float tooltipPosY = position.y;
            switch (origin)
            {
                case Origin.Top:
                {
                    tooltipPosY = position.y + halfParentRectHeight + distance;                 
                    break;
                }
                case Origin.Bottom:
                {
                    tooltipPosY = position.y - halfParentRectHeight - distance; 
                    break;
                }
                case Origin.Left:
                {
                    tooltipPosX = position.x - halfParentRectWidth - distance;
                    break;
                }
                case Origin.Right:
                {
                    tooltipPosX = position.x + halfParentRectWidth  + distance;
                    break;
                } 
            }  
            Vector3 tooltipPos = new Vector3(tooltipPosX,tooltipPosY,0);            
            tooltipRect.anchoredPosition3D = tooltipPos;
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
            UpdateHeight();
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
                RectTransform tooltipRect = GetComponent<RectTransform>();
                float finalHeight = description.preferredHeight;
                float arrowHeight = 0f;
                if(arrowRect != null)
                {
                    arrowHeight = arrowRect.rect.height;
                }
                switch (origin)
                {
                    case Origin.Top:
                    case Origin.Bottom:
                    {
                        finalHeight = finalHeight + arrowHeight +40;               
                        break;
                    }
                    case Origin.Left:
                    case Origin.Right:
                    {
                        finalHeight = finalHeight + 40;
                        break;
                    }
                }  
                tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);
            }
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