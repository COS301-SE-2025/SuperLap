using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RainbowArt.CleanFlatUI
{
    public class WindowResize : MonoBehaviour, IDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        enum ResizeableType
        {
            None = 0,
            Top = 1,
            Bottom = 2,
            Right = 4,
            Left = 8,
            LeftTop = Left|Top,
            RightTop = Right|Top,
            LeftBottom = Left|Bottom ,
            RightBottom = Right|Bottom,
        };

        [SerializeField]
        RectTransform resizableArea;

        float cursorScope = 20.0f;
        float minWidth = 100.0f;
        float minHeight = 100.0f;

        [SerializeField]
        Texture2D cursorHorizonal;

        [SerializeField]
        Texture2D cursorVertical;

        [SerializeField]
        Texture2D cursorDiagonalLeft;

        [SerializeField]
        Texture2D cursorDiagonalRight;

        [SerializeField]
        Vector2 mCursorHotSpot = new Vector2(16, 16);

        ResizeableType curResizeableType = ResizeableType.None;
        RectTransform resizableRect;
        Camera cachedEventCamera;
        bool isPressed = false;

        void Start()
        {
            resizableRect = GetComponent<RectTransform>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            cachedEventCamera = eventData.enterEventCamera;
        }

        public void OnPointerExit(PointerEventData eventData)
        {

        }

        public void OnPointerDown(PointerEventData eventData)
        {
            curResizeableType = GetCurResizeableType(eventData.position, eventData.pressEventCamera);
            isPressed = (curResizeableType != ResizeableType.None);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
        }

        void SetCursor(Texture2D texture, Vector2 hotspot, CursorMode cursorMode)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                Cursor.SetCursor(texture, hotspot, cursorMode);
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.mousePresent)
            {
                Cursor.SetCursor(texture, hotspot, cursorMode);
            }
#endif

        }

        void UpdateCursor()
        {
            if (curResizeableType == ResizeableType.None)
            {
                SetCursor(null, mCursorHotSpot, CursorMode.Auto);
            }
            else if (curResizeableType == ResizeableType.Left || curResizeableType == ResizeableType.Right)
            {
                SetCursor(cursorHorizonal, mCursorHotSpot, CursorMode.Auto);
            }
            else if (curResizeableType == ResizeableType.Top || curResizeableType == ResizeableType.Bottom)
            {
                SetCursor(cursorVertical, mCursorHotSpot, CursorMode.Auto);
            }
            else if (curResizeableType == ResizeableType.LeftTop || curResizeableType == ResizeableType.RightBottom)
            {
                SetCursor(cursorDiagonalLeft, mCursorHotSpot, CursorMode.Auto);
            }
            else if (curResizeableType == ResizeableType.RightTop || curResizeableType == ResizeableType.LeftBottom)
            {
                SetCursor(cursorDiagonalRight, mCursorHotSpot, CursorMode.Auto);
            }
        }

        void LateUpdate()
        {
            if(cachedEventCamera == null)
            {
                SetCursor(null, mCursorHotSpot, CursorMode.Auto);
                return;
            }

            if(isPressed)
            {
                UpdateCursor();
                return;
            }
#if ENABLE_INPUT_SYSTEM
            Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
            Vector2 mousePosition = Input.mousePosition;
#endif
            curResizeableType = GetCurResizeableType(mousePosition, cachedEventCamera);
            UpdateCursor();
        }

        ResizeableType GetCurResizeableType(Vector2 mousePosition,Camera eventCamera)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(resizableArea, mousePosition, eventCamera))
            {
                return ResizeableType.None;
            }

            Vector2 point;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(resizableArea, mousePosition, eventCamera, out point))
            {
                return ResizeableType.None;
            }

            float resizableAreaWidth = resizableArea.rect.width;
            float resizableAreaHeight = resizableArea.rect.height;
            ResizeableType ret = ResizeableType.None;
            if((resizableAreaWidth / 2) - Mathf.Abs(point.x) <= cursorScope)
            {
                if(point.x > 0)
                {
                    ret = ret | ResizeableType.Right;
                }
                else
                {
                    ret = ret | ResizeableType.Left;
                }
            }
            if((resizableAreaHeight / 2) - Mathf.Abs(point.y) <= cursorScope)
            {
                if(point.y > 0)
                {
                    ret = ret | ResizeableType.Top;
                }
                else
                {
                    ret = ret | ResizeableType.Bottom;
                }
            }
            return ret;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if(isPressed == false)
            {
                return;
            }
            Vector2 curMousePos = Vector2.zero;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(resizableRect, eventData.position, eventData.enterEventCamera, out curMousePos))
            {
                return;
            }

            float resizableAreaWidth = resizableArea.rect.width;
            float resizableAreaHeight = resizableArea.rect.height;
            float resizableAreaPosX = resizableArea.anchoredPosition3D.x;
            float resizableAreaPosY = resizableArea.anchoredPosition3D.y;            

            if( (int)(curResizeableType & ResizeableType.Top) !=0)
            {    
                float offSetY = curMousePos.y - resizableAreaHeight/2;
                resizableAreaHeight = resizableAreaHeight + offSetY;                    
                if(resizableAreaHeight < minHeight)
                {
                    resizableAreaHeight = minHeight;
                }
                else
                {
                    resizableAreaPosY = resizableAreaPosY + offSetY/2;   
                }                    

            }

            if((int)(curResizeableType & ResizeableType.Bottom) != 0)
            {
                float offSetY = - (curMousePos.y + resizableAreaHeight/2);
                resizableAreaHeight = resizableAreaHeight + offSetY;                    
                if(resizableAreaHeight < minHeight)
                {
                    resizableAreaHeight = minHeight;
                }
                else
                {
                    resizableAreaPosY = resizableAreaPosY - offSetY/2;
                }                    
            }

            if ((int)(curResizeableType & ResizeableType.Right) != 0)
            {
                float offSetX = curMousePos.x - resizableAreaWidth/2;
                resizableAreaWidth = resizableAreaWidth + offSetX;                    
                if(resizableAreaWidth < minWidth)
                {
                    resizableAreaWidth = minWidth;
                }
                else
                {
                    resizableAreaPosX = resizableAreaPosX + offSetX/2;
                }
            }

            if ((int)(curResizeableType & ResizeableType.Left) != 0)
            {
                float offSetX = -(curMousePos.x + resizableAreaWidth / 2);
                resizableAreaWidth = resizableAreaWidth + offSetX;
                if (resizableAreaWidth < minWidth)
                {
                    resizableAreaWidth = minWidth;
                }
                else
                {
                    resizableAreaPosX = resizableAreaPosX - offSetX / 2;
                }
            }

            resizableRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, resizableAreaWidth);
            resizableRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, resizableAreaHeight);
            Vector3 resizableAreaPos = new Vector3(resizableAreaPosX,resizableAreaPosY,0);            
            resizableRect.anchoredPosition3D = resizableAreaPos;  
        }
    }
}
