using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

/*
//Set properties in C# example codes.
using RainbowArt.CleanFlatUI;
public class SliderRangeDemo : MonoBehaviour
{ 
    //The SliderRange Component. 
    public SliderRange m_Slider; 
    void Start()
    {
        //Add OnValue1Changed event listener.
        m_Slider.OnValue1Changed.AddListener(SliderRangeValue1Change);    
        //Add OnValue1Changed event listener.
        m_Slider.OnValue2Changed.AddListener(SliderRangeValue2Change); 
        //Set the minimum value.
        m_Slider.MinValue = 0; 
        //Set the maximum value.
        m_Slider.MaxValue = 1; 
        //Set whether to show the whole numbers.
        m_Slider.WholeNumbers = false; 
        //Set the value1.
        m_Slider.Value1 = 0.2f; 
        //Set the value2.
        m_Slider.Value2 = 0.8f; 
        //Set whether to show the text. 
        m_Slider.HasText = true; 
        //Set axis direction.
        m_Slider.Axis = SliderRange.AxisEnum.Horizontal;     
    }    
    public void SliderRangeValue1Change(float val)
    {
        Debug.Log("SliderRangeValue1Change, value: " + val);
    }  
    public void SliderRangeValue2Change(float val)
    {
        Debug.Log("SliderRangeValue2Change, value: " + val);
    }       
}
*/

namespace RainbowArt.CleanFlatUI
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class SliderRange : UIBehaviour, IDragHandler, IInitializePotentialDragHandler, ICanvasElement,IPointerDownHandler
    {
        [SerializeField]
        RectTransform fillRect;

        [SerializeField]
        RectTransform handle1Rect;

        [SerializeField]
        RectTransform handle2Rect;

        public enum AxisEnum
        {
            Horizontal = 0,
            Vertical = 1,
        };

        [SerializeField]
        AxisEnum axis = AxisEnum.Horizontal;

        [SerializeField]
        float minValue = 0;

        [SerializeField]
        float maxValue = 1;

        [SerializeField]
        bool wholeNumbers = false;

        [SerializeField]
        float value1;

        [SerializeField]
        float value2;

        [SerializeField]
        bool hasText = true;

        [SerializeField]
        TextMeshProUGUI text1; 

        [SerializeField]
        TextMeshProUGUI text2; 

        RectTransform fillContainerRect;
        RectTransform handleContainerRect;
        Vector2 offset = Vector2.zero;
        bool bDelayedUpdate = false;
        bool isDragingHandle1 = false;

        public AxisEnum Axis 
        { 
            get => axis;
            set 
            { 
                if(axis != value)
                {
                    axis = value;
                    UpdateGUI();
                }              
            } 
        }
       
        public float MinValue
        { 
            get => minValue;
            set
            {
                float newMinValue = value;
                if (wholeNumbers)
                {
                    newMinValue = Mathf.Round(newMinValue);
                }
                if (newMinValue != minValue)
                {
                    minValue = newMinValue;
                    SetValue1(value1);
                    UpdateGUI();
                }
            } 
        }
       
        public float MaxValue
        { 
            get => maxValue;
            set
            {
                float newMaxValue = value;
                if (wholeNumbers)
                {
                    newMaxValue = Mathf.Round(newMaxValue);
                }
                if (newMaxValue != maxValue)
                {
                    maxValue = newMaxValue;
                    SetValue1(value1);
                    UpdateGUI();
                }          
            } 
        }
       
        public bool WholeNumbers
        { 
            get => wholeNumbers;
            set
            { 
                if (wholeNumbers != value)
                {
                    wholeNumbers = value;
                    SetValue1(value1); 
                    UpdateGUI();
                }                 
            } 
        }
       
        public virtual float Value1
        {
            get
            {
                if (wholeNumbers)
                {
                    return Mathf.Round(value1);
                }
                return value1;
            }
            set
            {
                SetValue1(value);
            }
        }

        public virtual float Value2
        {
            get
            {
                if (wholeNumbers)
                {
                    return Mathf.Round(value2);
                }
                return value2;
            }
            set
            {
                SetValue2(value);
            }
        }

        public bool HasText
        {
            get => hasText;
            set
            {
                hasText = value;
                UpdateText();
            }
        }

        public virtual void SetValue1WithoutNotify(float input)
        {
            SetValue1(input, false);
        }

        public virtual void SetValue2WithoutNotify(float input)
        {
            SetValue2(input, false);
        }

        public float NormalizedValue1
        {
            get
            {
                if (Mathf.Approximately(MinValue, MaxValue))
                {
                    return 0;
                }
                return Mathf.InverseLerp(MinValue, MaxValue, Value1);
            }
            set
            {
                this.Value1 = Mathf.Lerp(MinValue, MaxValue, value);
                
            }
        }

        public float NormalizedValue2
        {
            get
            {
                if (Mathf.Approximately(MinValue, MaxValue))
                    return 0;
                return Mathf.InverseLerp(MinValue, MaxValue, Value2);
            }
            set
            {
                this.Value2 = Mathf.Lerp(MinValue, MaxValue, value);
            }
        }

        [Serializable]
        public class RangedSliderEvent : UnityEvent<float> { }

        [SerializeField]
        RangedSliderEvent onValue1Changed = new RangedSliderEvent();

        [SerializeField]
        RangedSliderEvent onValue2Changed = new RangedSliderEvent();
        
        public RangedSliderEvent OnValue1Changed 
        { 
            get => onValue1Changed;
            set 
            { 
                onValue1Changed = value; 
            } 
        }

        public RangedSliderEvent OnValue2Changed
        {
            get => onValue2Changed;
            set
            {
                onValue2Changed = value;
            }
        }

        protected SliderRange() { }

        #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (wholeNumbers)
            {
                minValue = Mathf.Round(minValue);
                maxValue = Mathf.Round(maxValue);
            }

            if (IsActive())
            {
                bDelayedUpdate = true;
            }

            if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) && !Application.isPlaying)
            {
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            }
            
            if((text1 != null) &&(text2 != null))
            {
                text1.gameObject.SetActive(hasText);
                text2.gameObject.SetActive(hasText);
            }
        }
        #endif 

        public virtual void Rebuild(CanvasUpdate executing)
        {
            #if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
            {
                OnValue1Changed.Invoke(Value1);
                OnValue2Changed.Invoke(Value2);
            }
            #endif
        }
      
        public virtual void LayoutComplete() { }       
        public virtual void GraphicUpdateComplete(){ }

        protected override void OnEnable()
        {
            base.OnEnable();
            fillContainerRect = fillRect.parent.GetComponent<RectTransform>();
            handleContainerRect = handle1Rect.parent.GetComponent<RectTransform>();
            SetValue1(value1, false);
            SetValue2(value2, false);
            UpdateGUI();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }
      
        protected virtual void Update()
        {
            if (bDelayedUpdate)
            {
                bDelayedUpdate = false;
                SetValue1(value1, false);
                SetValue2(value2, false);
                UpdateGUI();
            }
        }

        protected override void OnDidApplyAnimationProperties()
        {
            if (!IsActive())
            {
                return;
            }
            SetValue1(value1, false);
            SetValue2(value2, false);
            UpdateGUI();
        }
       
        protected virtual void SetValue1(float val, bool sendCallback = true)
        {
            float newValue = val;
            if (newValue > value2)
            {
                newValue = value2;
            }
            newValue = Mathf.Clamp(newValue, MinValue, MaxValue);
            if (wholeNumbers)
            {
                newValue = Mathf.Round(newValue);
            }
            if (value1 == newValue)
            {
                return;
            }                
            value1 = newValue;
            UpdateGUI();
            if (sendCallback)
            {
                onValue1Changed.Invoke(newValue);
            }
        }

        protected virtual void SetValue2(float val, bool sendCallback = true)
        {
            float newValue = val;
            if (newValue < value1)
            {
                newValue = value1;
            }
            newValue = Mathf.Clamp(newValue, MinValue, MaxValue);
            if (wholeNumbers)
            {
                newValue = Mathf.Round(newValue);
            }
            if (value2 == newValue)
            {
                return;
            }  
            value2 = newValue;
            UpdateGUI();
            if (sendCallback)
            {
                onValue2Changed.Invoke(newValue);
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (!IsActive())
            {
                return;
            }
            UpdateGUI();
        }

        void UpdateText()
        {
            if (text1 != null && text1.gameObject.activeSelf != hasText)
            {
                text1.gameObject.SetActive(hasText);
            }
            if (text2 != null && text2.gameObject.activeSelf != hasText)
            {
                text2.gameObject.SetActive(hasText);
            }
            if (hasText && (text1 != null) && (text2 != null))
            {
                float useValue1 = (float)Math.Round((double)value1, 1);
                text1.text = useValue1 + "";
                float useValue2 = (float)Math.Round((double)value2, 1);
                text2.text = useValue2 + "";
            }
        }

        void UpdateGUI()
        {
            if (fillContainerRect != null)
            {
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;
                anchorMin[(int)Axis] = NormalizedValue1;
                anchorMax[(int)Axis] = NormalizedValue2;                     
                fillRect.anchorMin = anchorMin;
                fillRect.anchorMax = anchorMax;
            }
            if (handleContainerRect != null)
            {
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;
                anchorMin[(int)Axis] = anchorMax[(int)Axis] = NormalizedValue1;
                handle1Rect.anchorMin = anchorMin;
                handle1Rect.anchorMax = anchorMax;

                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
                anchorMin[(int)Axis] = anchorMax[(int)Axis] = NormalizedValue2;
                handle2Rect.anchorMin = anchorMin;
                handle2Rect.anchorMax = anchorMax;
            }

            UpdateText();  
        }

        void UpdateDrag(PointerEventData eventData, Camera cam)
        {
            RectTransform clickRect = handleContainerRect ?? fillContainerRect;
            if (clickRect != null && clickRect.rect.size[(int)Axis] > 0)
            {
                Vector2 position = eventData.position;
                Vector2 localCursor;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(clickRect, position, cam, out localCursor))
                {
                    return;
                }
                localCursor -= clickRect.rect.position;
                float val = Mathf.Clamp01((localCursor - offset)[(int)Axis] / clickRect.rect.size[(int)Axis]);
                if(isDragingHandle1)
                {
                    NormalizedValue1 = val;
                }
                else
                {
                    NormalizedValue2 = val;
                }                
            }
        }

        bool MayDrag(PointerEventData eventData)
        {
            return IsActive() && eventData.button == PointerEventData.InputButton.Left;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
            {
                return;
            } 
            offset = Vector2.zero;
            if( RectTransformUtility.RectangleContainsScreenPoint(handle2Rect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera))
            {
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(handle2Rect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out localMousePos))
                {
                    offset = localMousePos;
                }
                isDragingHandle1 = false;
            }
            else if (RectTransformUtility.RectangleContainsScreenPoint(handle1Rect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera))
            {
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(handle1Rect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out localMousePos))
                {
                    offset = localMousePos;
                }
                isDragingHandle1 = true;
            }
            else
            {
                RectTransform clickRect = handleContainerRect;
                if (clickRect != null && clickRect.rect.size[(int)Axis] > 0)
                {
                    Vector2 position = eventData.position;
                    Vector2 localCursor;
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(clickRect, position, eventData.pressEventCamera, out localCursor))
                        return;
                    localCursor -= clickRect.rect.position;
                    float val = Mathf.Clamp01((localCursor - offset)[(int)Axis] / clickRect.rect.size[(int)Axis]);
                    if(Mathf.Abs(val - NormalizedValue1) <= Mathf.Abs(val - NormalizedValue2))
                    {
                        NormalizedValue1 = val;
                    }
                    else
                    {
                        NormalizedValue2 = val;
                    }

                }
            }
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
            {
                return;
            }
            UpdateDrag(eventData, eventData.pressEventCamera);
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = true;
        }
      
        public void SetDirection(AxisEnum direction, bool includeRectLayouts)
        {
            AxisEnum oldDir = Axis;
            Axis = direction;

            if (!includeRectLayouts)
            {
                return;
            }
            if (oldDir != Axis)
            {
                RectTransformUtility.FlipLayoutAxes(transform as RectTransform, true, true);
            }   
        }
    }
}
