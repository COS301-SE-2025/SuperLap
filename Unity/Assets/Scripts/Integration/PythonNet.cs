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
      // Automatically detect and set the Python DLL path
      string pythonDllPath = DetectPythonDLL();//@"C:\Users\milan\AppData\Local\Programs\Python\Python313\python313.dll";
      Runtime.PythonDLL = pythonDllPath;
      Debug.Log($"Using hardcoded Python DLL: {pythonDllPath}");

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
    // Strategy 1: Try to get Python path using python command
    string dllPath = DetectPythonDLLFromCommand();
    if (!string.IsNullOrEmpty(dllPath) && File.Exists(dllPath))
    {
      Debug.Log($"Found Python DLL via command: {dllPath}");
      return dllPath;
    }

    // Strategy 2: Search common installation directories
    dllPath = SearchCommonPythonDirectories();
    if (!string.IsNullOrEmpty(dllPath) && File.Exists(dllPath))
    {
      Debug.Log($"Found Python DLL in common directories: {dllPath}");
      return dllPath;
    }

    // Strategy 3: Search PATH environment variable
    dllPath = SearchPythonInPath();
    if (!string.IsNullOrEmpty(dllPath) && File.Exists(dllPath))
    {
      Debug.Log($"Found Python DLL via PATH: {dllPath}");
      return dllPath;
    }

    Debug.LogWarning("Could not detect Python DLL automatically");
    return null;
  }

  private string DetectPythonDLLFromCommand()
  {
    string[] pythonCommands = { "python", "python3", "py" };

    foreach (string pythonCmd in pythonCommands)
    {
      try
      {
        SystemDiagnostics.ProcessStartInfo startInfo = new SystemDiagnostics.ProcessStartInfo
        {
          FileName = pythonCmd,
          Arguments = "-c \"import sys, os; print(f'{sys.version_info.major}{sys.version_info.minor}'); print(sys.executable)\"",
          UseShellExecute = false,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          CreateNoWindow = true
        };

        using (SystemDiagnostics.Process process = SystemDiagnostics.Process.Start(startInfo))
        {
          string output = process.StandardOutput.ReadToEnd();
          string error = process.StandardError.ReadToEnd();
          process.WaitForExit();

          if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
          {
            string[] lines = output.Trim().Split('\n');
            if (lines.Length >= 2)
            {
              string version = lines[0].Trim();
              string executable = lines[1].Trim();

              // Get the directory containing python.exe
              string pythonDir = Path.GetDirectoryName(executable);
              
              // Try different DLL naming patterns
              string[] dllPatterns = {
                $"python{version}.dll",
                $"python{version[0]}{version[1]}.dll",
                "python3.dll",
                "python.dll"
              };

              foreach (string pattern in dllPatterns)
              {
                string dllPath = Path.Combine(pythonDir, pattern);
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
        Debug.LogWarning($"Could not detect Python via {pythonCmd} command: {e.Message}");
      }
    }

    return null;
  }

  private string SearchCommonPythonDirectories()
  {
    // Common Python installation directories
    string[] basePaths = {
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Programs\Python",
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Python",
      @"C:\Python",
      @"C:\Program Files\Python",
      @"C:\Program Files (x86)\Python",
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\AppData\Local\Programs\Python"
    };

    foreach (string basePath in basePaths)
    {
      try
      {
        if (Directory.Exists(basePath))
        {
          // Look for Python version directories (e.g., Python39, Python310, etc.)
          string[] versionDirs = Directory.GetDirectories(basePath, "Python*", SearchOption.TopDirectoryOnly);
          
          // Sort to prefer newer versions
          Array.Sort(versionDirs, (x, y) => string.Compare(y, x, StringComparison.OrdinalIgnoreCase));

          foreach (string versionDir in versionDirs)
          {
            string dllPath = FindPythonDLLInDirectory(versionDir);
            if (!string.IsNullOrEmpty(dllPath))
            {
              return dllPath;
            }
          }
        }
      }
      catch (Exception e)
      {
        Debug.LogWarning($"Error searching directory {basePath}: {e.Message}");
      }
    }

    return null;
  }

  private string FindPythonDLLInDirectory(string directory)
  {
    try
    {
      // Look for DLL files in the directory
      string[] dllFiles = Directory.GetFiles(directory, "python*.dll", SearchOption.TopDirectoryOnly);
      
      if (dllFiles.Length > 0)
      {
        // Prefer versioned DLLs (e.g., python39.dll over python.dll)
        foreach (string dll in dllFiles)
        {
          string fileName = Path.GetFileName(dll).ToLower();
          if (System.Text.RegularExpressions.Regex.IsMatch(fileName, @"python\d+\.dll"))
          {
            return dll;
          }
        }
        
        // Fallback to any python DLL
        return dllFiles[0];
      }
    }
    catch (Exception e)
    {
      Debug.LogWarning($"Error searching for DLL in directory {directory}: {e.Message}");
    }

    return null;
  }

  private string SearchPythonInPath()
  {
    try
    {
      string pathEnv = Environment.GetEnvironmentVariable("PATH");
      if (string.IsNullOrEmpty(pathEnv))
        return null;

      string[] pathDirs = pathEnv.Split(';');

      foreach (string pathDir in pathDirs)
      {
        try
        {
          if (Directory.Exists(pathDir))
          {
            // Look for python.exe in this directory
            string pythonExe = Path.Combine(pathDir, "python.exe");
            if (File.Exists(pythonExe))
            {
              string dllPath = FindPythonDLLInDirectory(pathDir);
              if (!string.IsNullOrEmpty(dllPath))
              {
                return dllPath;
              }
            }
          }
        }
        catch (Exception e)
        {
          Debug.LogWarning($"Error searching PATH directory {pathDir}: {e.Message}");
        }
      }
    }
    catch (Exception e)
    {
      Debug.LogWarning($"Error searching PATH environment variable: {e.Message}");
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
