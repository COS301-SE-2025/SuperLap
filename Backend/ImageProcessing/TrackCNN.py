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


# Phase 1 of the system: Load and preprocess data
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

def unet_block(inputImage, numFilters=16, droupouts=0.1, doBatchNorm=True):
    """
    Creates a U-Net model for semantic segmentation.
    """
    # Encoder path
    c1 = conv2d_block(inputImage, numFilters * 1, kernelSize=3, doBatchNorm=doBatchNorm)
    p1 = tf.keras.layers.MaxPooling2D((2, 2))(c1)
    p1 = tf.keras.layers.Dropout(droupouts)(p1)
    
    c2 = conv2d_block(p1, numFilters * 2, kernelSize=3, doBatchNorm=doBatchNorm)
    p2 = tf.keras.layers.MaxPooling2D((2, 2))(c2)
    p2 = tf.keras.layers.Dropout(droupouts)(p2)
    
    c3 = conv2d_block(p2, numFilters * 4, kernelSize=3, doBatchNorm=doBatchNorm)
    p3 = tf.keras.layers.MaxPooling2D((2, 2))(c3)
    p3 = tf.keras.layers.Dropout(droupouts)(p3)
    
    c4 = conv2d_block(p3, numFilters * 8, kernelSize=3, doBatchNorm=doBatchNorm)
    p4 = tf.keras.layers.MaxPooling2D((2, 2))(c4)
    p4 = tf.keras.layers.Dropout(droupouts)(p4)
    
    c5 = conv2d_block(p4, numFilters * 16, kernelSize=3, doBatchNorm=doBatchNorm)
    
    # Decoder path
    u6 = tf.keras.layers.Conv2DTranspose(numFilters*8, (3, 3), strides=(2, 2), padding='same')(c5)
    u6 = tf.keras.layers.concatenate([u6, c4])
    u6 = tf.keras.layers.Dropout(droupouts)(u6)
    c6 = conv2d_block(u6, numFilters * 8, kernelSize=3, doBatchNorm=doBatchNorm)
    
    u7 = tf.keras.layers.Conv2DTranspose(numFilters*4, (3, 3), strides=(2, 2), padding='same')(c6)
    u7 = tf.keras.layers.concatenate([u7, c3])
    u7 = tf.keras.layers.Dropout(droupouts)(u7)
    c7 = conv2d_block(u7, numFilters * 4, kernelSize=3, doBatchNorm=doBatchNorm)
    
    u8 = tf.keras.layers.Conv2DTranspose(numFilters*2, (3, 3), strides=(2, 2), padding='same')(c7)
    u8 = tf.keras.layers.concatenate([u8, c2])
    u8 = tf.keras.layers.Dropout(droupouts)(u8)
    c8 = conv2d_block(u8, numFilters * 2, kernelSize=3, doBatchNorm=doBatchNorm)
    
    u9 = tf.keras.layers.Conv2DTranspose(numFilters*1, (3, 3), strides=(2, 2), padding='same')(c8)
    u9 = tf.keras.layers.concatenate([u9, c1])
    u9 = tf.keras.layers.Dropout(droupouts)(u9)
    c9 = conv2d_block(u9, numFilters * 1, kernelSize=3, doBatchNorm=doBatchNorm)
    
    output = tf.keras.layers.Conv2D(1, (1, 1), activation='sigmoid')(c9)
    model = tf.keras.Model(inputs=[inputImage], outputs=[output])
    return model

def save_sample_images(frameObj, output_dir, num_samples=5):
    """
    Save sample images and masks to the output directory.
    """
    for i in range(min(num_samples, len(frameObj['img']))):
        fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(10, 5))
        
        ax1.imshow(frameObj['img'][i])
        ax1.set_title('Input Image')
        ax1.axis('off')
        
        ax2.imshow(frameObj['mask'][i], cmap='gray')
        ax2.set_title('Ground Truth Mask')
        ax2.axis('off')
        
        plt.tight_layout()
        plt.savefig(os.path.join(output_dir, f'sample_{i}.png'))
        plt.close()


#Phase 2 of the system: Train the model
def plot_training_history(history, output_dir):
    """Enhanced plotting function"""
    plt.figure(figsize=(12, 6))
    
    # Plot training & validation loss
    plt.subplot(1, 2, 1)
    plt.plot(history.history['loss'], label='Training Loss')
    if 'val_loss' in history.history:
        plt.plot(history.history['val_loss'], label='Validation Loss')
    plt.title('Model Loss')
    plt.xlabel('Epoch')
    plt.ylabel('Loss')
    plt.legend()
    plt.grid(True)
    
    # Plot training & validation accuracy
    plt.subplot(1, 2, 2)
    plt.plot(history.history['accuracy'], label='Training Accuracy')
    if 'val_accuracy' in history.history:
        plt.plot(history.history['val_accuracy'], label='Validation Accuracy')
    plt.title('Model Accuracy')
    plt.xlabel('Epoch')
    plt.ylabel('Accuracy')
    plt.legend()
    plt.grid(True)
    
    plt.tight_layout()
    plot_path = os.path.join(output_dir, 'training_metrics.png')
    plt.savefig(plot_path)
    plt.close()
    print(f"Training metrics plot saved to {plot_path}")

def train_model(model, x_train, y_train, epochs=83, output_file=None):
    """Train the model with progress bar in terminal and clean logs in file"""
    class DualOutput:
        def __init__(self, terminal, log_file):
            self.terminal = terminal
            self.log = log_file
            
        def write(self, message):
            self.terminal.write(message)
            # Only write complete lines (no progress bars) to log file
            if '\r' not in message and '\x1b' not in message:
                self.log.write(message)
                
        def flush(self):
            self.terminal.flush()
            self.log.flush()

    # Open log file if specified
    log_file = None
    original_stdout = sys.stdout
    if output_file:
        log_file = open(output_file, 'a')
        print("\n" + "="*50, file=log_file)
        print("TRAINING OUTPUT", file=log_file)
        print("="*50 + "\n", file=log_file)
        sys.stdout = DualOutput(original_stdout, log_file)

    # Train with both progress bar and clean epoch summaries
    history = model.fit(
        x_train,
        y_train,
        epochs=epochs,
        batch_size=32,
        validation_split=0.2,
        verbose=1  # Keep progress bar in terminal
    )

    # Print clean epoch summaries
    print("\nTraining Summary:")
    for epoch in range(epochs):
        epoch_log = (f"Epoch {epoch+1}/{epochs}\n"
                    f" - loss: {history.history['loss'][epoch]:.4f}\n"
                    f" - accuracy: {history.history['accuracy'][epoch]:.4f}")
        if 'val_loss' in history.history:
            epoch_log += (f"\n - val_loss: {history.history['val_loss'][epoch]:.4f}\n"
                         f" - val_accuracy: {history.history['val_accuracy'][epoch]:.4f}")
        print(epoch_log + "\n")

    # Clean up
    if output_file:
        log_file.close()
        sys.stdout = original_stdout
        print(f"\nTraining summary saved to {output_file}")

    return history


# Phase 3 of the system: Predictions
def predict16(valMap, model, shape=128):
    """
    Predicts the output for a batch of 16 images using the given model.

    Args:
        valMap (dict): Dictionary containing 'img' and 'mask' arrays
        model: Trained Keras model
        shape (int): Image shape (unused in this implementation but kept for compatibility)

    Returns:
        tuple: (predictions, processed_images, ground_truth_masks)
    """
    img = valMap['img'][0:16]
    mask = valMap['mask'][0:16]
    imgProc = np.array(img)  # Convert list to numpy array
    
    predictions = model.predict(imgProc)
    return predictions, imgProc, mask

def plot_predictions(img, predMask, groundTruth, output_dir, index):
    """
    Plots and saves comparison of input image, prediction, and ground truth.
    
    Args:
        img: Input image array
        predMask: Predicted mask array
        groundTruth: Ground truth mask array
        output_dir: Directory to save plots
        index: Index number for filename
    """
    plt.figure(figsize=(9, 9))
    
    plt.subplot(1, 3, 1)
    plt.imshow(img)
    plt.title('Aerial Image')
    plt.axis('off')
    
    plt.subplot(1, 3, 2)
    plt.imshow(predMask, cmap='gray')
    plt.title('Predicted Routes')
    plt.axis('off')
    
    plt.subplot(1, 3, 3)
    plt.imshow(groundTruth, cmap='gray')
    plt.title('Actual Routes')
    plt.axis('off')
    
    plt.tight_layout()
    plot_path = os.path.join(output_dir, f'prediction_comparison_{index}.png')
    plt.savefig(plot_path)
    plt.close()
    print(f"Saved prediction plot to {plot_path}")

def save_predictions(model, frameObjTrain, output_dir, num_samples=5):
    """
    Generate and save prediction visualizations for sample images.
    
    Args:
        model: Trained Keras model
        frameObjTrain: Dictionary containing training data
        output_dir: Directory to save outputs
        num_samples: Number of samples to visualize
    """
    # Make predictions
    predictions, actuals, masks = predict16(frameObjTrain, model)
    
    # Plot sample predictions
    sample_indices = [2, 10, 13, 5, 15]  # Example indices to visualize
    for i, idx in enumerate(sample_indices[:num_samples]):
        plot_predictions(
            actuals[idx],
            predictions[idx][:, :, 0],  # Remove channel dimension
            masks[idx],
            output_dir,
            i+1
        )
    
    # Save the model
    model_path = os.path.join(output_dir, 'MapSegmentationGenerator.keras')
    model.save(model_path)
    print(f"\nModel saved to {model_path}")


#Phase 4 of the system: Use the Model on Images
def load_saved_model(model_path):
    """
    Load and compile a saved Keras model.
    
    Args:
        model_path: Path to the saved .keras model file
        
    Returns:
        Loaded and compiled Keras model
    """
    model = tf.keras.models.load_model(model_path, compile=False)
    model.compile(optimizer='rmsprop', loss='binary_crossentropy', metrics=['accuracy'])
    print(f"Model loaded from {model_path}")
    return model

def predict_images(model, image_paths, target_size=(128, 128)):
    """
    Predict road masks for a list of input images.
    
    Args:
        model: Loaded Keras model
        image_paths: List of paths to input images
        target_size: Size to resize images to
        
    Returns:
        List of predictions (road masks)
    """
    predictions = []
    for path in image_paths:
        try:
            # Load and preprocess image
            img = Image.open(path)
            img = img.resize(target_size)
            img_array = np.array(img) / 255.0  # Normalize to [0,1]
            
            # Predict and store
            pred = model.predict(np.expand_dims(img_array, axis=0))
            predictions.append(pred)
        except Exception as e:
            print(f"Error processing {path}: {str(e)}")
            predictions.append(None)
    return predictions

def visualize_results(predicted_mask, original_image, save_dir=None, index=None):
    """
    Visualize and save comparison of original image and predicted mask.
    
    Args:
        predicted_mask: Model prediction array
        original_image: Original PIL Image or array
        save_dir: Directory to save visualization
        index: Optional index for filename
    """
    # Convert inputs to numpy arrays
    original_img = np.array(original_image)
    pred_mask = np.squeeze(np.array(predicted_mask))  # Remove extra dimensions
    
    # Create figure
    plt.figure(figsize=(12, 6))
    
    # Original image
    plt.subplot(1, 2, 1)
    plt.imshow(original_img)
    plt.title('Original Image')
    plt.axis('off')
    
    # Predicted mask with color
    plt.subplot(1, 2, 2)
    # Create blue-green mask (like previous implementation)
    colored_mask = np.zeros((*pred_mask.shape, 3))
    colored_mask[..., 1] = pred_mask * 0.7  # Green
    colored_mask[..., 2] = pred_mask * 0.9  # Blue
    plt.imshow(colored_mask)
    plt.title('Predicted Roads')
    plt.axis('off')
    
    # Save or show
    if save_dir and index is not None:
        os.makedirs(save_dir, exist_ok=True)
        save_path = os.path.join(save_dir, f'test_prediction_{index}.png')
        plt.savefig(save_path, bbox_inches='tight', dpi=150)
        plt.close()
        print(f"Saved test prediction visualization to {save_path}")
    else:
        plt.tight_layout()
        plt.show()
