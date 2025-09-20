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
public class SliderCircularDemo : MonoBehaviour
{  
    //The SliderCircular Component.
    public SliderCircular m_Slider;
    void Start()
    {
        //Add OnValueChanged event listener.
        m_Slider.OnValueChanged.AddListener(SliderCircularValueChange);
        //Set the minimum value.
        m_Slider.MinValue = 0;
        //Set the maximum value.
        m_Slider.MaxValue = 1; 
        //Set whether to show the whole numbers.
        m_Slider.WholeNumbers = false; 
        //Set the value.
        m_Slider.Value = 0.8; 
        //Set whether to show the text.  
        m_Slider.HasText = true;         
        //Set whether to fill clockwise.
        //m_Slider.Clockwise = true;   
        //Set the fill start position which include four positions such as Top, Bottom, Left, Right.  
        m_Slider.CurFillOrigin = SliderCircular.FillOrigin.Top;

    } 
    public void SliderCircularValueChange(float val)
    {
        Debug.Log("SliderCircularValueChange, value: " + val);
    }       
}
*/

namespace RainbowArt.CleanFlatUI
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class SliderCircular : UIBehaviour, IDragHandler, IInitializePotentialDragHandler, ICanvasElement, IPointerDownHandler
    {        
        public enum FillOrigin
        {
            Top = 0,
            Right,
            Bottom,
            Left,
        }   
        
        [SerializeField]
        FillOrigin fillOrigin = FillOrigin.Top;    

        [SerializeField]          
        Image fillImage;

        [SerializeField]
        RectTransform handleRect;

        [SerializeField]
        RectTransform handleRootRect;

        [SerializeField]
        bool clockwise = true;

        [SerializeField]
        float minValue = 0;

        [SerializeField]
        float maxValue = 1;

        [SerializeField]
        bool wholeNumbers = false;

        [SerializeField]
        float value;

        [SerializeField]
        bool hasText = true;

        [SerializeField]
        TextMeshProUGUI text; 

        Vector2 offset = Vector2.zero;
        bool bDelayedUpdate = false;
        RectTransform fillImageRect;   

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
                    SetValue(this.value);
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
                    SetValue(this.value);
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
                    SetValue(this.value);
                    UpdateGUI();
                }
            }
        }
       
        public virtual float Value
        {
            get
            {
                if (wholeNumbers)
                {
                    return Mathf.Round(value);
                }
                return value;
            }
            set
            {
                SetValue(value);
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

        public bool Clockwise
        {
            get => clockwise;
            set
            {
                if (clockwise == value)
                {
                    return;
                }
                clockwise = value;
                UpdateGUI();
            }
        }

        public FillOrigin CurFillOrigin
        {
            get => fillOrigin;
            set
            {
                if (fillOrigin == value)
                {
                    return;
                }
                fillOrigin = value;
                UpdateGUI();
            }
        }  

        public virtual void SetValueWithoutNotify(float input)
        {
            SetValue(input, false);
        }

        public float NormalizedValue
        {
            get
            {
                if (Mathf.Approximately(MinValue, MaxValue))
                {
                    return 0;
                }
                return Mathf.InverseLerp(MinValue, MaxValue, Value);
            }
            set
            {
                this.Value = Mathf.Lerp(MinValue, MaxValue, value);
            }
        }

        [Serializable]
        public class SliderCircularEvent : UnityEvent<float> {}

        [SerializeField]        
        SliderCircularEvent onValueChanged = new SliderCircularEvent();
        
        public SliderCircularEvent OnValueChanged
        {
            get => onValueChanged;
            set
            {
                onValueChanged = value;
            }
        }

        protected SliderCircular() { }

        public virtual void Rebuild(CanvasUpdate executing)
        {
            #if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
            {
                OnValueChanged.Invoke(Value);
            }
            #endif
        }

        public virtual void LayoutComplete() { }
        public virtual void GraphicUpdateComplete() { }

        protected override void OnEnable()
        {
            base.OnEnable();
            fillImageRect = fillImage.GetComponent<RectTransform>();
            UpdateFillImageOrign();
            SetValue(value, false);
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
                UpdateFillImageOrign();
                SetValue(value, false);
                UpdateGUI();
            }
        }

        protected override void OnDidApplyAnimationProperties()
        {
            if (!IsActive())
            {
                return;
            }
            SetValue(value, false);
            UpdateGUI();
        }

        protected virtual void SetValue(float val, bool sendCallback = true)
        {
            float newValue = val;
            newValue = Mathf.Clamp(newValue, MinValue, MaxValue);
            if (wholeNumbers)
            {
                newValue = Mathf.Round(newValue);
            }
            if (value == newValue)
            {
                return;
            }
            value = newValue;
            UpdateGUI();
            if (sendCallback)
            {
                onValueChanged.Invoke(newValue);
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

        void UpdateGUI()
        {
            float curNormalizedValue = NormalizedValue;
            fillImage.fillAmount = curNormalizedValue;
            if(clockwise)
            {
                float finalAngle = -curNormalizedValue * 360 - (int)fillOrigin * 90;
                handleRootRect.localEulerAngles = new Vector3(0, 0, finalAngle);
            }
            else
            {
                float finalAngle = curNormalizedValue * 360 - (int)fillOrigin * 90;
                handleRootRect.localEulerAngles = new Vector3(0, 0, finalAngle);
            }
            UpdateText();
        }

        void UpdateText()
        {
            if (text != null && text.gameObject.activeSelf != hasText)
            {
                text.gameObject.SetActive(hasText);
            }
            if (hasText && (text != null))
            {
                float useValue = (float)Math.Round((double)value, 1);
                text.text = useValue + "";
            }
        }

        float GetAngleWithFillOrign(Vector2 pos)
        {
            Vector2 fillOrignVector = GetOriginVector(fillOrigin);
            if(clockwise)
            {
                float curAngle = Vector2.SignedAngle(fillOrignVector, pos);
                if (curAngle > 0)
                {
                    curAngle = 360 - curAngle;
                }
                else
                {
                    curAngle = -curAngle;
                }
                return curAngle;
            }
            else
            {
                float curAngle = Vector2.SignedAngle(fillOrignVector, pos);
                if (curAngle > 0)
                {
                    return curAngle;
                }
                else
                {
                    curAngle = 360 + curAngle;
                }
                return curAngle;
            }            
        }

        Vector2 GetOriginVector(FillOrigin origin)
        {
            if (origin == FillOrigin.Top)
            {
                return new Vector2(0, 1);
            }
            else if (origin == FillOrigin.Bottom)
            {
                return new Vector2(0, -1);
            }
            else if (origin == FillOrigin.Left)
            {
                return new Vector2(-1, 0);
            }
            else if (origin == FillOrigin.Right)
            {
                return new Vector2(1, 0);
            }
            return Vector2.zero;
        }

        void UpdateFillImageOrign()
        {
            if(fillOrigin == FillOrigin.Top)
            {
                fillImage.fillOrigin = (int)Image.Origin360.Top;
            }
            else if (fillOrigin == FillOrigin.Bottom)
            {
                fillImage.fillOrigin = (int)Image.Origin360.Bottom;
            }
            else if (fillOrigin == FillOrigin.Left)
            {
                fillImage.fillOrigin = (int)Image.Origin360.Left;
            }
            else if (fillOrigin == FillOrigin.Right)
            {
                fillImage.fillOrigin = (int)Image.Origin360.Right;
            }
            fillImage.fillClockwise = clockwise;
        }

        void UpdateDrag(PointerEventData eventData, Camera cam)
        {
            RectTransform clickRect = fillImageRect;
            Vector2 position = eventData.position;
            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(fillImageRect, position, cam, out localCursor))
            {
                return;
            }
            float curAngle = GetAngleWithFillOrign(localCursor);
            float val = curAngle / 360;
            NormalizedValue = val;
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
            if (RectTransformUtility.RectangleContainsScreenPoint(handleRect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera))
            {
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(handleRect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out localMousePos))
                {
                    offset = localMousePos;
                }
            }
            else
            {
                RectTransform clickRect = fillImageRect;
                Vector2 position = eventData.position;
                Vector2 localCursor;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(fillImageRect, position, eventData.pressEventCamera, out localCursor))
                {
                    return;
                }
                float curAngle = GetAngleWithFillOrign(localCursor);
                float val = curAngle / 360;
                NormalizedValue = val;
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

            if(text != null)
            {
                text.gameObject.SetActive(hasText);
            }
        }
        #endif 
    }
}
