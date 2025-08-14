using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RainbowArt.CleanFlatUI
{
    public class WindowResizeHandle : MonoBehaviour, IDragHandler, IBeginDragHandler
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

        [SerializeField]
        GameObject topHandle;

        [SerializeField]
        GameObject bottomHandle;

        [SerializeField]
        GameObject leftHandle;

        [SerializeField]
        GameObject rightHandle;

        [SerializeField]
        GameObject leftTopHandle;

        [SerializeField]
        GameObject rightTopHandle;

        [SerializeField]
        GameObject leftBottomHandle;

        [SerializeField]
        GameObject rightBottomHandle;

        ResizeableType curResizeableType = ResizeableType.None;
        RectTransform resizableRect;

        float minWidth = 100.0f;
        float minHeight = 100.0f;

        void Start()
        {
            resizableRect = GetComponent<RectTransform>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            GameObject curDraggingObj = eventData.pointerEnter;
            if(curDraggingObj == null)
            {
                return;
            }
            curResizeableType = GetCurResizeableType(curDraggingObj);
        }

        ResizeableType GetCurResizeableType(GameObject curDraggingObj)
        {
            ResizeableType type = ResizeableType.None;
            if (curDraggingObj == topHandle)
            {
                type = ResizeableType.Top;
            }
            else if(curDraggingObj == bottomHandle)
            {
                type = ResizeableType.Bottom;
            }
            else if (curDraggingObj == leftHandle)
            {
                type = ResizeableType.Left;
            }
            else if (curDraggingObj == rightHandle)
            {
                type = ResizeableType.Right;
            }
            else if (curDraggingObj == leftTopHandle)
            {
                type = ResizeableType.LeftTop;
            }
            else if (curDraggingObj == rightTopHandle)
            {
                type = ResizeableType.RightTop;
            }
            else if (curDraggingObj == leftBottomHandle)
            {
                type = ResizeableType.LeftBottom;
            }
            else if (curDraggingObj == rightBottomHandle)
            {
                type = ResizeableType.RightBottom;
            }
            return type;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (curResizeableType == ResizeableType.None)
            {
                return;
            }
            Vector2 curMousePos = Vector2.zero;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(resizableRect, eventData.position, eventData.enterEventCamera, out curMousePos))
            {
                return;
            }

            float resizableAreaWidth = resizableArea.rect.width;
            float resizableAreaHeight = resizableArea.rect.height;
            float resizableAreaPosX = resizableArea.anchoredPosition3D.x;
            float resizableAreaPosY = resizableArea.anchoredPosition3D.y;

            if ((int)(curResizeableType & ResizeableType.Top) != 0)
            {
                float offSetY = curMousePos.y - resizableAreaHeight / 2;
                resizableAreaHeight = resizableAreaHeight + offSetY;
                if (resizableAreaHeight < minHeight)
                {
                    resizableAreaHeight = minHeight;
                }
                else
                {
                    resizableAreaPosY = resizableAreaPosY + offSetY / 2;
                }

            }

            if ((int)(curResizeableType & ResizeableType.Bottom) != 0)
            {
                float offSetY = -(curMousePos.y + resizableAreaHeight / 2);
                resizableAreaHeight = resizableAreaHeight + offSetY;
                if (resizableAreaHeight < minHeight)
                {
                    resizableAreaHeight = minHeight;
                }
                else
                {
                    resizableAreaPosY = resizableAreaPosY - offSetY / 2;
                }
            }

            if ((int)(curResizeableType & ResizeableType.Right) != 0)
            {
                float offSetX = curMousePos.x - resizableAreaWidth / 2;
                resizableAreaWidth = resizableAreaWidth + offSetX;
                if (resizableAreaWidth < minWidth)
                {
                    resizableAreaWidth = minWidth;
                }
                else
                {
                    resizableAreaPosX = resizableAreaPosX + offSetX / 2;
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
            Vector3 resizableAreaPos = new Vector3(resizableAreaPosX, resizableAreaPosY, 0);
            resizableRect.anchoredPosition3D = resizableAreaPos;
        }        
    }
}
