import os
import sys
import numpy as np
import cv2
import matplotlib.pyplot as plt
import tensorflow as tf
from PIL import Image

# Create output directory if it doesn't exist
output_dir = "CNNoutput"
os.makedirs(output_dir, exist_ok=True)

def save_model_summary(model, filename):
    """Save model summary to a text file"""
    original_stdout = sys.stdout  # Save a reference to the original standard output
    with open(filename, 'w') as f:
        sys.stdout = f  # Change the standard output to the file we created
        model.summary()
        sys.stdout = original_stdout  # Reset the standard output to its original value
