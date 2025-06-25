using TMPro;
using System.Collections.Generic;
using UnityEngine;

public class TrackDropdownManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown trackDropdown;

    private void Start()
    {
        APIManager.Instance.GetAllTracks((success, message, tracks) =>
        {
            if (success && tracks != null)
            {
                trackDropdown.ClearOptions();

                List<string> options = new List<string>();
                foreach (var track in tracks)
                {
                    options.Add(track.name);
                }

                trackDropdown.AddOptions(options);
            }
            else
            {
                Debug.Log("Failed to load tracks: " + message);
            }
        });
    }
}
