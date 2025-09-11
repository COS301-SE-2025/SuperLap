using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowArt.CleanFlatUI
{
    public class TooltipDetermine : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        Tooltip tooltip;
        
        void Start()
        {
            tooltip.gameObject.SetActive(false);                     
        }
        
        void UpdatePosition()
        {
            RectTransform tooltipRect = tooltip.GetComponent<RectTransform>();
            RectTransform tooltipParentRect = tooltipRect.parent as RectTransform;
            if(tooltipParentRect == null)
            {
                return;
            }
            RectTransform UIRect = GetComponent<RectTransform>();
            float width = UIRect.rect.width;
            float hight = UIRect.rect.height;       
            Vector3[] corners = new Vector3[4];
            UIRect.GetWorldCorners(corners);
            Vector3[] cornersInArea = new Vector3[4];
            for(int i = 0; i < 4; i++)
            {
                cornersInArea[i] = tooltipParentRect.InverseTransformPoint(corners[i]); 
            } 
            Vector3 position = (cornersInArea[0] + cornersInArea[2])/2;
            tooltip.SetTooltipPosition(position,width,hight);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            UpdatePosition();
            tooltip.ShowTooltip();                      
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            tooltip.HideTooltip();  
        }       
    }
}