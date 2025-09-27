using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RainbowArt.CleanFlatUI
{
    public class WindowDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {        
        [SerializeField]
        RectTransform draggableArea;

        RectTransform cachedParentRect;
        RectTransform cachedSelfRect;
        bool isDraggableArea = false;
        Vector2 dragPosOffset;

        void Awake()
        {
            cachedSelfRect = GetComponent<RectTransform>();
            cachedParentRect = cachedSelfRect.parent as RectTransform;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if(RectTransformUtility.RectangleContainsScreenPoint(draggableArea, eventData.position, eventData.pressEventCamera))
            {
                Vector2 localPointerPos = Vector2.zero;
                Vector2 localPosition = cachedSelfRect.localPosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(cachedParentRect, eventData.position, eventData.pressEventCamera, out localPointerPos);
                dragPosOffset = localPosition - localPointerPos;
                isDraggableArea = true;
            }
            else
            {
                isDraggableArea = false;
            }            
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isDraggableArea = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if(isDraggableArea)
            {
                Vector2 localPointerPos = Vector2.zero;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(cachedParentRect, eventData.position, eventData.pressEventCamera, out localPointerPos))
                {
                    cachedSelfRect.localPosition = localPointerPos + dragPosOffset;
                }
            }
        }
    }
}