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