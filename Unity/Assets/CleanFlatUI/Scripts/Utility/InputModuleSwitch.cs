using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RainbowArt.CleanFlatUI
{
    public class InputModuleSwitch : MonoBehaviour
    {
        void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            StandaloneInputModule inputModule = gameObject.GetComponent<StandaloneInputModule>();
            if(inputModule)
            {
                Destroy(inputModule);
            }
            UnityEngine.InputSystem.UI.InputSystemUIInputModule newInputModule = gameObject.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (newInputModule == null)
            {
                gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
#endif
        }
    }
}
