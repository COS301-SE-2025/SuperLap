using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowArt.CleanFlatUI
{
    public class ToggleSwap : MonoBehaviour 
    {
        [SerializeField]
        Toggle toggle;

        [SerializeField]
        Image background;
        
        [SerializeField]
        Image foreground;

        CanvasGroup canvasGroupBg;
        CanvasGroup canvasGroupFg;
        
        void Start () 
        {
            UpdateGUI();
        }

        void UpdateGUI()
        {
            if(toggle == null)
            {
                toggle = GetComponent<Toggle>();
            }
            if(canvasGroupBg == null)
            {
                canvasGroupBg = background.gameObject.GetComponent<CanvasGroup>();
            } 
            if(canvasGroupFg == null)
            {
                canvasGroupFg = foreground.gameObject.GetComponent<CanvasGroup>();
            } 
            toggle.onValueChanged.AddListener(ToggleValueChanged);
            ToggleValueChanged(toggle.isOn);
        }
    
        void ToggleValueChanged(bool value)
        {
            if(value)   
            {
                SetCanvasGroupAlpha(canvasGroupBg,0);
                SetCanvasGroupAlpha(canvasGroupFg,1);
            }
            else
            {
                SetCanvasGroupAlpha(canvasGroupBg,1);
                SetCanvasGroupAlpha(canvasGroupFg,0);
            }   
        }

        void SetCanvasGroupAlpha(CanvasGroup obj,float alpha)
        {
            obj.alpha = alpha;
        } 
    }
}