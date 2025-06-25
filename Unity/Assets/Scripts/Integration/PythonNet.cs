using System;
using System.IO;
using UnityEngine;
using Python.Runtime;
using SystemDiagnostics = System.Diagnostics;

public class PythonNet
{
  private static PythonNet _instance;
  public static PythonNet Instance
  {
    get
    {
      if (_instance == null)
      {
        _instance = new PythonNet();
        _instance.Init();
      }
      return _instance;
    }
  }
  void Init()
  {
    // Initialize Python.NET
    try
    {
      string pythonDllPath = DetectPythonDLL();
      if (!string.IsNullOrEmpty(pythonDllPath))
      {
        Runtime.PythonDLL = pythonDllPath;
        Debug.Log($"Using Python DLL: {pythonDllPath}");
      }
      else
      {
        Debug.LogWarning("Could not automatically detect Python DLL. Using default Python.NET behavior.");
      }

      PythonEngine.Initialize();
      PythonEngine.BeginAllowThreads();
      Debug.Log("Python.NET initialized successfully.");
    }
    catch (Exception e)
    {
      Debug.LogError($"Failed to initialize Python.NET: {e.Message}");
    }
  }

  private string DetectPythonDLL()
  {
    try
    {
      if (Application.platform == RuntimePlatform.WindowsPlayer ||
          Application.platform == RuntimePlatform.WindowsEditor)
      {
        return DetectWindowsPythonDLL();
      }
      else if (Application.platform == RuntimePlatform.LinuxPlayer ||
               Application.platform == RuntimePlatform.LinuxEditor)
      {
        return DetectLinuxPythonDLL();
      }
      else
      {
        Debug.LogWarning($"Unsupported platform for automatic Python DLL detection: {Application.platform}");
        return null;
      }
    }
    catch (Exception e)
    {
      Debug.LogError($"Error detecting Python DLL: {e.Message}");
      return null;
    }
  }

  private string DetectWindowsPythonDLL()
  {
    // Common Python installation paths on Windows
    string[] commonPaths = {
            @"C:\Python3*\python3*.dll",
            @"C:\Program Files\Python3*\python3*.dll",
            @"C:\Program Files (x86)\Python3*\python3*.dll",
            @"C:\Users\{0}\AppData\Local\Programs\Python\Python3*\python3*.dll"
        };        // Try to get Python version and path using python command
    try
    {
      SystemDiagnostics.ProcessStartInfo startInfo = new SystemDiagnostics.ProcessStartInfo
      {
        FileName = "python",
        Arguments = "-c \"import sys; print(sys.executable); print(f'{sys.version_info.major}.{sys.version_info.minor}')\"",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        CreateNoWindow = true
      };

      using (SystemDiagnostics.Process process = SystemDiagnostics.Process.Start(startInfo))
      {
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
          string[] lines = output.Trim().Split('\n');
          if (lines.Length >= 2)
          {
            string pythonExePath = lines[0].Trim();
            string version = lines[1].Trim();

            // Try to find the DLL in the same directory as python.exe
            string pythonDir = Path.GetDirectoryName(pythonExePath);
            string dllPath = Path.Combine(pythonDir, $"python{version.Replace(".", "")}.dll");

            if (File.Exists(dllPath))
            {
              return dllPath;
            }

            // Alternative naming convention
            dllPath = Path.Combine(pythonDir, $"python{version[0]}{version[2]}.dll");
            if (File.Exists(dllPath))
            {
              return dllPath;
            }
          }
        }
      }
    }
    catch (Exception e)
    {
      Debug.LogWarning($"Could not detect Python via command line: {e.Message}");
    }

    // Fallback: search common installation directories
    foreach (string pathPattern in commonPaths)
    {
      try
      {
        string expandedPath = pathPattern.Replace("{0}", Environment.UserName);
        string directory = Path.GetDirectoryName(expandedPath);
        string pattern = Path.GetFileName(expandedPath);

        if (Directory.Exists(directory))
        {
          string[] files = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories);
          if (files.Length > 0)
          {
            return files[0]; // Return the first match
          }
        }
      }
      catch (Exception e)
      {
        Debug.LogWarning($"Error searching path {pathPattern}: {e.Message}");
      }
    }

    return null;
  }

  private string DetectLinuxPythonDLL()
  {        // Try to get Python version and library path using python command
    try
    {
      SystemDiagnostics.ProcessStartInfo startInfo = new SystemDiagnostics.ProcessStartInfo
      {
        FileName = "python3",
        Arguments = "-c \"import sys, sysconfig; print(f'{sys.version_info.major}.{sys.version_info.minor}'); print(sysconfig.get_config_var('LIBDIR'))\"",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        CreateNoWindow = true
      };

      using (SystemDiagnostics.Process process = SystemDiagnostics.Process.Start(startInfo))
      {
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
          string[] lines = output.Trim().Split('\n');
          if (lines.Length >= 2)
          {
            string version = lines[0].Trim();
            string libDir = lines[1].Trim();

            // Common naming patterns for Python shared libraries
            string[] patterns = {
                            $"libpython{version}.so.1.0",
                            $"libpython{version}.so",
                            $"libpython{version}m.so.1.0",
                            $"libpython{version}m.so"
                        };

            foreach (string pattern in patterns)
            {
              string dllPath = Path.Combine(libDir, pattern);
              if (File.Exists(dllPath))
              {
                return dllPath;
              }
            }
          }
        }
      }
    }
    catch (Exception e)
    {
      Debug.LogWarning($"Could not detect Python via python3 command: {e.Message}");
    }

    // Fallback: search common library directories
    string[] commonLibDirs = {
            "/usr/lib64",
            "/usr/lib/x86_64-linux-gnu",
            "/usr/lib",
            "/usr/local/lib",
            "/lib64",
            "/lib"
        };

    foreach (string libDir in commonLibDirs)
    {
      try
      {
        if (Directory.Exists(libDir))
        {
          // Search for Python shared libraries
          string[] files = Directory.GetFiles(libDir, "libpython*.so*", SearchOption.TopDirectoryOnly);

          // Prefer versioned libraries (e.g., libpython3.13.so.1.0)
          foreach (string file in files)
          {
            if (file.Contains(".so.") && (file.Contains("3.") || file.Contains("python3")))
            {
              return file;
            }
          }

          // Fallback to any Python library
          if (files.Length > 0)
          {
            return files[0];
          }
        }
      }
      catch (Exception e)
      {
        Debug.LogWarning($"Error searching directory {libDir}: {e.Message}");
      }
    }

    return null;
  }

  /// <summary>
  /// Execute a Python script and return its output as a string
  /// </summary>
  /// <param name="pythonScript">The Python code to execute</param>
  /// <returns>The output from the Python script, or null if execution failed</returns>
  public string RunPythonScript(string pythonScript)
  {
    try
    {
      using (Py.GIL())
      {
        using (var scope = Py.CreateScope())
        {
          // Redirect stdout to capture output
          scope.Exec(@"
import sys
from io import StringIO
_captured_output = StringIO()
_original_stdout = sys.stdout
sys.stdout = _captured_output
");

          // Execute the user's Python script
          scope.Exec(pythonScript);

          // Restore stdout and get the captured output
          scope.Exec(@"
sys.stdout = _original_stdout
_result = _captured_output.getvalue()
_captured_output.close()
");

          // Get the result
          var result = scope.Get("_result");
          return result.ToString();
        }
      }
    }
    catch (Exception e)
    {
      Debug.LogError($"Error executing Python script: {e.Message}");
      return null;
    }
  }

  /// <summary>
  /// Execute a Python script from a file and return its output
  /// </summary>
  /// <param name="scriptPath">Path to the Python script file</param>
  /// <returns>The output from the Python script, or null if execution failed</returns>
  public string RunPythonScriptFromFile(string scriptPath)
  {
    try
    {
      if (!File.Exists(scriptPath))
      {
        Debug.LogError($"Python script file not found: {scriptPath}");
        return null;
      }

      string scriptContent = File.ReadAllText(scriptPath);
      return RunPythonScript(scriptContent);
    }
    catch (Exception e)
    {
      Debug.LogError($"Error reading or executing Python script file '{scriptPath}': {e.Message}");
      return null;
    }
  }

  /// <summary>
  /// Execute a Python script and return a Python object
  /// </summary>
  /// <param name="pythonScript">The Python code to execute</param>
  /// <param name="returnVariable">The name of the variable to return</param>
  /// <returns>The Python object, or null if execution failed</returns>
  public PyObject RunPythonScriptWithReturn(string pythonScript, string returnVariable)
  {
    try
    {
      using (Py.GIL())
      {
        using (var scope = Py.CreateScope())
        {
          // Execute the user's Python script
          scope.Exec(pythonScript);

          // Get the specified variable
          if (scope.Contains(returnVariable))
          {
            return scope.Get(returnVariable);
          }
          else
          {
            Debug.LogError($"Variable '{returnVariable}' not found in Python scope");
            return null;
          }
        }
      }
    }
    catch (Exception e)
    {
      Debug.LogError($"Error executing Python script: {e.Message}");
      return null;
    }
  }

  /// <summary>
  /// Execute a Python function with parameters and return the result
  /// </summary>
  /// <param name="pythonScript">The Python code containing the function</param>
  /// <param name="functionName">The name of the function to call</param>
  /// <param name="parameters">Parameters to pass to the function</param>
  /// <returns>The function result as a Python object</returns>
  public PyObject CallPythonFunction(string pythonScript, string functionName, params object[] parameters)
  {
    try
    {
      using (Py.GIL())
      {
        using (var scope = Py.CreateScope())
        {
          // Execute the script to define the function
          scope.Exec(pythonScript);

          // Get the function
          if (scope.Contains(functionName))
          {
            var func = scope.Get(functionName);

            // Convert parameters to Python objects
            PyObject[] pyParams = new PyObject[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
              pyParams[i] = parameters[i].ToPython();
            }

            // Call the function
            return func.Invoke(pyParams);
          }
          else
          {
            Debug.LogError($"Function '{functionName}' not found in Python scope");
            return null;
          }
        }
      }
    }
    catch (Exception e)
    {
      Debug.LogError($"Error calling Python function: {e.Message}");
      return null;
    }
  }

  /// <summary>
  /// Import a Python module and return it
  /// </summary>
  /// <param name="moduleName">The name of the module to import</param>
  /// <returns>The imported module as a Python object</returns>
  public PyObject ImportModule(string moduleName)
  {
    try
    {
      using (Py.GIL())
      {
        return Py.Import(moduleName);
      }
    }
    catch (Exception e)
    {
      Debug.LogError($"Error importing module '{moduleName}': {e.Message}");
      return null;
    }
  }

  /// <summary>
  /// Check if Python.NET is properly initialized
  /// </summary>
  /// <returns>True if Python.NET is initialized, false otherwise</returns>
  public bool IsInitialized()
  {
    return PythonEngine.IsInitialized;
  }    /// <summary>
       /// Shutdown Python.NET (call this when your application is closing)
       /// </summary>
  public void Shutdown()
  {
    try
    {
      if (PythonEngine.IsInitialized)
      {
        PythonEngine.Shutdown();
        Debug.Log("Python.NET shutdown successfully.");
      }
    }
    catch (Exception e)
    {
      Debug.LogError($"Error during Python.NET shutdown: {e.Message}");
    }
  }
}
