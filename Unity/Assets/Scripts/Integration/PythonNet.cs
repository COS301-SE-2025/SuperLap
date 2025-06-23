using System;
using UnityEngine;
using Python.Runtime; 

public class PythonNet : MonoBehaviour
{    void Start()
    {
        // Initialize Python.NET
        try
        {
            // Set the Python DLL path for Fedora
            Runtime.PythonDLL = "/usr/lib64/libpython3.13.so.1.0";
            
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();
            Debug.Log("Python.NET initialized successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Python.NET: {e.Message}");
        }
    }

}
