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

def load_data(frameObj=None, imgPath=None, maskPath=None, shape=128):
    """
    Load data from image and mask directories and resize them to the specified shape.

    Args:
        frameObj (dict): Dictionary to store the loaded images and masks.
        imgPath (str): Path to the directory containing the images.
        maskPath (str): Path to the directory containing the masks.
        shape (int): Desired shape for resizing the images and masks.

    Returns:
        dict: Dictionary containing the loaded and resized images and masks.
    """
    imgNames = os.listdir(imgPath)
    maskNames = []

    # Generate mask names
    for mem in imgNames:
        mem = mem.split('_')[0]
        if mem not in maskNames:
            maskNames.append(mem)

    imgAddr = imgPath + '/'
    maskAddr = maskPath + '/'

    for i in range(len(imgNames)):
        try:
            img = plt.imread(imgAddr + maskNames[i] + '_sat.jpg')
            mask = plt.imread(maskAddr + maskNames[i] + '_mask.png')
        except:
            continue
        
        img = cv2.resize(img, (shape, shape))
        mask = cv2.resize(mask, (shape, shape))
        frameObj['img'].append(img)
        frameObj['mask'].append(mask[:, :, 0])  # binary mask is in channel 0

    return frameObj

def conv2d_block(inputTensor, numFilters, kernelSize=3, doBatchNorm=True):
    """
    Creates a convolutional block with two Conv2D layers, batch norm, and ReLU activation.
    """
    x = tf.keras.layers.Conv2D(
        filters=numFilters, 
        kernel_size=(kernelSize, kernelSize),
        kernel_initializer='he_normal', 
        padding='same'
    )(inputTensor)
    
    if doBatchNorm:
        x = tf.keras.layers.BatchNormalization()(x)
        
    x = tf.keras.layers.Activation('relu')(x)
    
    x = tf.keras.layers.Conv2D(
        filters=numFilters, 
        kernel_size=(kernelSize, kernelSize),
        kernel_initializer='he_normal', 
        padding='same'
    )(x)
    
    if doBatchNorm:
        x = tf.keras.layers.BatchNormalization()(x)
        
    x = tf.keras.layers.Activation('relu')(x)
    
    return x

