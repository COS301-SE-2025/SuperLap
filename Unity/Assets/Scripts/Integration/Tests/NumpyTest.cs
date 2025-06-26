using UnityEngine;

public class NumpyTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string numpyTest = @"
import numpy as np
def run_numpy_test():
    a = np.array([1, 2, 3])
    b = np.array([4, 5, 6])
    c = a + b
    print('Numpy Test Result:', c)
run_numpy_test()
";

        string result = PythonNet.Instance.RunPythonScript(numpyTest);
        Debug.Log(result);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
