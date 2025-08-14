using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowArt.CleanFlatUI
{
    public class TooltipSnap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        TooltipSpecial tooltip;    
        
        RectTransform areaScope;
        Camera cachedEnterEventCamera;
        
        void Start ()
        {
            areaScope = GetComponent<RectTransform>();            
            tooltip.gameObject.SetActive(false); 
            UpdatePosition();
        }
        void Update()
        {
            if(tooltip.gameObject.activeSelf == true && cachedEnterEventCamera != null)
            {
#if ENABLE_INPUT_SYSTEM
                Vector2 mousePosition =  UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
                Vector2 mousePosition = Input.mousePosition;
#endif
                Vector2 localMousePos;
                if(RectTransformUtility.ScreenPointToLocalPointInRectangle(areaScope, mousePosition, cachedEnterEventCamera, out localMousePos))
                {
                    UpdatePosition();
                }
            }
        }

        void UpdatePosition()
        {
#if ENABLE_INPUT_SYSTEM
            Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
            Vector2 mousePosition = Input.mousePosition;
#endif
            Vector2 mousePos = Vector2.zero;
            RectTransform tooltipRect = tooltip.gameObject.GetComponent<RectTransform>();
            RectTransform tooltipParentRect = tooltipRect.parent as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(tooltipParentRect, mousePosition, cachedEnterEventCamera, out mousePos);
            tooltip.InitTooltip(mousePos,areaScope);                       
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            cachedEnterEventCamera = eventData.enterEventCamera;       
            tooltip.ShowTooltip();     
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            cachedEnterEventCamera = null;
            tooltip.HideTooltip();  
        }
    }
}