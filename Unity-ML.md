# **Python Installation for Unity ML-Agents**

This guide provides a streamlined process for installing the Python prerequisites required for the Unity ML-Agents Toolkit.

## **Prerequisites**

Before you begin, ensure you have Python installed on your system. If you plan to use conda, make sure you have Anaconda or Miniconda installed.  
You can download Python from the official [Python website](https://www.python.org/downloads/) or Anaconda from the [Anaconda Distribution page](https://www.anaconda.com/products/distribution).

## **Installation Steps**

It is highly recommended to use a dedicated virtual environment to avoid conflicts with other packages. Below are two options for creating one.

### **Option A: Using venv (Standard Python)**

1. **Create and Activate a Virtual Environment**  
   Open your terminal or command prompt and run the following commands.  
   * To create the environment (replace venv with your preferred name):  
     python \-m venv venv

   * To activate the environment:  
     * **Windows (Command Prompt):**  
       venv\\Scripts\\activate

     * **Windows (PowerShell):**  
       venv\\Scripts\\Activate.ps1

     * **macOS / Linux:**  
       source venv/bin/activate

2. **Install the mlagents Package**  
   Proceed to the "Install Package" step below.

### **Option B: Using conda**

1. **Create and Activate a Conda Environment**  
   Open your Anaconda Prompt or terminal.  
   * To create the environment (we'll name it mlagents-env and use Python 3.9, a commonly supported version):  
     conda create \--name mlagents-env python=3.9

     You will be prompted to proceed (y/n), type y and press Enter.  
   * To activate the new environment:  
     conda activate mlagents-env

2. **Install the mlagents Package**  
   Proceed to the "Install Package" step below.

### **Install Package (After Activating Environment)**

With your chosen virtual environment (venv or conda) activated, install the ml-agents package and its dependencies (including PyTorch) using pip.  
pip install mlagents

The installation is now complete. You can proceed with setting up your Unity project.

## **Video Tutorial**

If you encounter any issues or prefer a visual guide, this video tutorial walks through the complete setup process:  
[Unity ML-Agents \- Installation and Setup Tutorial](https://www.youtube.com/watch?v=bT3SV1SLqHA)