using UnityEngine;

public class UserManager : MonoBehaviour
{
    private static UserManager _instance;
    public static UserManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("UserManager");
                _instance = go.AddComponent<UserManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public string Username { get; set; }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // persist across scenes
        }
        else if (_instance != this)
        {
            Destroy(gameObject); // prevent duplicates
        }
    }
}
