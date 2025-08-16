import os
import sys
import numpy as np
import cv2
import matplotlib.pyplot as plt
import tensorflow as tf
from PIL import Image
from datetime import datetime

# Create output directory if it doesn't exist
output_dir = "LaneDetectionOutput"
os.makedirs(output_dir, exist_ok=True)

def save_model_summary(model, filename):
    """Save model summary to a text file"""
    original_stdout = sys.stdout
    with open(filename, 'w') as f:
        sys.stdout = f
        model.summary()
        sys.stdout = original_stdout

def preprocess_lane_image(img, target_size=(128, 128)):
    """
    Enhanced preprocessing specifically for lane detection
    """
    # Resize image
    img = cv2.resize(img, target_size)
    
    # Convert to RGB if needed
    if len(img.shape) == 3 and img.shape[2] == 3:
        img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    
    # Normalize to [0, 1]
    img = img.astype(np.float32) / 255.0
    
    # Optional: Apply histogram equalization for better contrast
    # This can help detect faint lane markings
    if len(img.shape) == 3:
        # Convert to YUV and equalize Y channel
        img_yuv = cv2.cvtColor((img * 255).astype(np.uint8), cv2.COLOR_RGB2YUV)
        img_yuv[:,:,0] = cv2.equalizeHist(img_yuv[:,:,0])
        img = cv2.cvtColor(img_yuv, cv2.COLOR_YUV2RGB).astype(np.float32) / 255.0
    
    return img

def load_lane_data(frameObj=None, imgPath=None, maskPath=None, shape=128):
    """
    Load lane detection training data with enhanced preprocessing
    
    Expected file naming:
    - Images: lane_001.jpg, lane_002.jpg, etc.
    - Masks: lane_001_mask.png, lane_002_mask.png, etc.
    """
    if frameObj is None:
        frameObj = {'img': [], 'mask': []}
    
    imgNames = sorted([f for f in os.listdir(imgPath) if f.endswith(('.jpg', '.jpeg', '.png'))])
    
    imgAddr = imgPath + '/'
    maskAddr = maskPath + '/'
    
    processed_count = 0
    
    for img_name in imgNames:
        try:
            # Extract base name for corresponding mask
            base_name = os.path.splitext(img_name)[0]
            mask_name = f"{base_name}_mask.png"
            
            # Load image and mask
            img_path = imgAddr + img_name
            mask_path = maskAddr + mask_name
            
            if not os.path.exists(mask_path):
                print(f"Warning: Mask not found for {img_name}, skipping...")
                continue
            
            img = plt.imread(img_path)
            mask = plt.imread(mask_path)
            
            # Preprocess image with lane-specific enhancements
            img = preprocess_lane_image(img, (shape, shape))
            
            # Process mask
            mask = cv2.resize(mask, (shape, shape))
            
            # Ensure mask is binary (0 or 1)
            if len(mask.shape) == 3:
                mask = mask[:, :, 0]  # Take first channel if RGB
            
            # Normalize mask to [0, 1] and ensure binary values
            mask = (mask > 0.5).astype(np.float32)
            
            frameObj['img'].append(img)
            frameObj['mask'].append(mask)
            processed_count += 1
            
        except Exception as e:
            print(f"Error loading {img_name}: {str(e)}")
            continue
    
    print(f"Successfully loaded {processed_count} lane training samples")
    return frameObj

def conv2d_block(inputTensor, numFilters, kernelSize=3, doBatchNorm=True):
    """
    Enhanced convolutional block with improved initialization for lane detection
    """
    x = tf.keras.layers.Conv2D(
        filters=numFilters, 
        kernel_size=(kernelSize, kernelSize),
        kernel_initializer='he_normal', 
        padding='same',
        activation=None  # Will add activation after batch norm
    )(inputTensor)
    
    if doBatchNorm:
        x = tf.keras.layers.BatchNormalization()(x)
    
    x = tf.keras.layers.Activation('relu')(x)
    
    # Second convolution
    x = tf.keras.layers.Conv2D(
        filters=numFilters, 
        kernel_size=(kernelSize, kernelSize),
        kernel_initializer='he_normal', 
        padding='same',
        activation=None
    )(x)
    
    if doBatchNorm:
        x = tf.keras.layers.BatchNormalization()(x)
    
    x = tf.keras.layers.Activation('relu')(x)
    
    return x

def lane_unet_model(input_shape=(128, 128, 3), numFilters=32, dropout_rate=0.1):
    """
    Enhanced U-Net architecture optimized for lane detection
    """
    inputs = tf.keras.layers.Input(input_shape)
    
    # Encoder path with increased filters for better feature extraction
    c1 = conv2d_block(inputs, numFilters * 1, kernelSize=3, doBatchNorm=True)
    p1 = tf.keras.layers.MaxPooling2D((2, 2))(c1)
    p1 = tf.keras.layers.Dropout(dropout_rate)(p1)
    
    c2 = conv2d_block(p1, numFilters * 2, kernelSize=3, doBatchNorm=True)
    p2 = tf.keras.layers.MaxPooling2D((2, 2))(c2)
    p2 = tf.keras.layers.Dropout(dropout_rate)(p2)
    
    c3 = conv2d_block(p2, numFilters * 4, kernelSize=3, doBatchNorm=True)
    p3 = tf.keras.layers.MaxPooling2D((2, 2))(c3)
    p3 = tf.keras.layers.Dropout(dropout_rate)(p3)
    
    c4 = conv2d_block(p3, numFilters * 8, kernelSize=3, doBatchNorm=True)
    p4 = tf.keras.layers.MaxPooling2D((2, 2))(c4)
    p4 = tf.keras.layers.Dropout(dropout_rate)(p4)
    
    # Bottleneck with more filters for complex feature representation
    c5 = conv2d_block(p4, numFilters * 16, kernelSize=3, doBatchNorm=True)
    
    # Decoder path with skip connections
    u6 = tf.keras.layers.Conv2DTranspose(numFilters * 8, (3, 3), strides=(2, 2), padding='same')(c5)
    u6 = tf.keras.layers.concatenate([u6, c4])
    u6 = tf.keras.layers.Dropout(dropout_rate)(u6)
    c6 = conv2d_block(u6, numFilters * 8, kernelSize=3, doBatchNorm=True)
    
    u7 = tf.keras.layers.Conv2DTranspose(numFilters * 4, (3, 3), strides=(2, 2), padding='same')(c6)
    u7 = tf.keras.layers.concatenate([u7, c3])
    u7 = tf.keras.layers.Dropout(dropout_rate)(u7)
    c7 = conv2d_block(u7, numFilters * 4, kernelSize=3, doBatchNorm=True)
    
    u8 = tf.keras.layers.Conv2DTranspose(numFilters * 2, (3, 3), strides=(2, 2), padding='same')(c7)
    u8 = tf.keras.layers.concatenate([u8, c2])
    u8 = tf.keras.layers.Dropout(dropout_rate)(u8)
    c8 = conv2d_block(u8, numFilters * 2, kernelSize=3, doBatchNorm=True)
    
    u9 = tf.keras.layers.Conv2DTranspose(numFilters * 1, (3, 3), strides=(2, 2), padding='same')(c8)
    u9 = tf.keras.layers.concatenate([u9, c1])
    u9 = tf.keras.layers.Dropout(dropout_rate)(u9)
    c9 = conv2d_block(u9, numFilters * 1, kernelSize=3, doBatchNorm=True)
    
    # Output layer with sigmoid activation for binary classification
    outputs = tf.keras.layers.Conv2D(1, (1, 1), activation='sigmoid', name='lane_output')(c9)
    
    model = tf.keras.Model(inputs=[inputs], outputs=[outputs], name='LaneDetectionUNet')
    return model

def dice_coefficient(y_true, y_pred, smooth=1e-7):
    """
    Dice coefficient for evaluating segmentation performance
    Better for lane detection than standard accuracy
    """
    y_true_f = tf.keras.backend.flatten(y_true)
    y_pred_f = tf.keras.backend.flatten(y_pred)
    intersection = tf.keras.backend.sum(y_true_f * y_pred_f)
    return (2. * intersection + smooth) / (tf.keras.backend.sum(y_true_f) + tf.keras.backend.sum(y_pred_f) + smooth)

def dice_loss(y_true, y_pred):
    """
    Dice loss function - better for segmentation tasks with class imbalance
    """
    return 1 - dice_coefficient(y_true, y_pred)

def combined_loss(y_true, y_pred, alpha=0.7):
    """
    Combined loss function: weighted combination of dice loss and binary crossentropy
    """
    dice = dice_loss(y_true, y_pred)
    bce = tf.keras.losses.binary_crossentropy(y_true, y_pred)
    return alpha * dice + (1 - alpha) * bce

def train_lane_model(model, x_train, y_train, epochs=50, batch_size=16, validation_split=0.2, output_file=None):
    """
    Enhanced training function with better callbacks and monitoring
    """
    # Define callbacks
    callbacks = [
        tf.keras.callbacks.EarlyStopping(
            monitor='val_loss',
            patience=10,
            restore_best_weights=True,
            verbose=1
        ),
        tf.keras.callbacks.ReduceLROnPlateau(
            monitor='val_loss',
            factor=0.5,
            patience=5,
            min_lr=1e-7,
            verbose=1
        )
    ]
    
    # Add model checkpoint
    checkpoint_path = os.path.join(output_dir, 'best_lane_model.keras')
    callbacks.append(
        tf.keras.callbacks.ModelCheckpoint(
            checkpoint_path,
            monitor='val_dice_coefficient',
            save_best_only=True,
            mode='max',
            verbose=1
        )
    )
    
    print(f"Starting training for {epochs} epochs...")
    print(f"Training samples: {len(x_train)}")
    print(f"Validation split: {validation_split}")
    print(f"Batch size: {batch_size}")
    
    history = model.fit(
        x_train, y_train,
        epochs=epochs,
        batch_size=batch_size,
        validation_split=validation_split,
        callbacks=callbacks,
        verbose=1
    )
    
    # Save training history
    if output_file:
        with open(output_file, 'a') as f:
            f.write(f"\n{'='*50}\n")
            f.write("LANE DETECTION TRAINING COMPLETED\n")
            f.write(f"{'='*50}\n")
            f.write(f"Final Training Loss: {history.history['loss'][-1]:.4f}\n")
            f.write(f"Final Validation Loss: {history.history['val_loss'][-1]:.4f}\n")
            f.write(f"Final Training Dice: {history.history['dice_coefficient'][-1]:.4f}\n")
            f.write(f"Final Validation Dice: {history.history['val_dice_coefficient'][-1]:.4f}\n")
    
    return history

def visualize_lane_predictions(model, test_images, test_masks, num_samples=5):
    """
    Visualize lane detection predictions
    """
    indices = np.random.choice(len(test_images), min(num_samples, len(test_images)), replace=False)
    
    fig, axes = plt.subplots(num_samples, 3, figsize=(15, 5 * num_samples))
    if num_samples == 1:
        axes = axes.reshape(1, -1)
    
    for i, idx in enumerate(indices):
        img = test_images[idx]
        true_mask = test_masks[idx]
        
        # Get prediction
        pred_mask = model.predict(np.expand_dims(img, axis=0))[0]
        pred_mask = (pred_mask > 0.5).astype(np.float32)
        
        # Plot original image
        axes[i, 0].imshow(img)
        axes[i, 0].set_title('Original Image')
        axes[i, 0].axis('off')
        
        # Plot predicted lanes
        axes[i, 1].imshow(pred_mask[:, :, 0], cmap='hot')
        axes[i, 1].set_title('Predicted Lanes')
        axes[i, 1].axis('off')
        
        # Plot ground truth
        axes[i, 2].imshow(true_mask, cmap='hot')
        axes[i, 2].set_title('Ground Truth')
        axes[i, 2].axis('off')
    
    plt.tight_layout()
    plt.savefig(os.path.join(output_dir, 'lane_predictions_sample.png'), dpi=150, bbox_inches='tight')
    plt.close()
    print(f"Sample predictions saved to {output_dir}/lane_predictions_sample.png")

def evaluate_lane_model(model, x_test, y_test, output_file=None):
    """
    Comprehensive evaluation of lane detection model
    """
    print("Evaluating lane detection model...")
    
    # Get predictions
    predictions = model.predict(x_test, batch_size=16, verbose=1)
    pred_binary = (predictions > 0.5).astype(np.float32)
    
    # Calculate metrics
    dice_scores = []
    iou_scores = []
    precision_scores = []
    recall_scores = []
    
    for i in range(len(y_test)):
        y_true = y_test[i].flatten()
        y_pred = pred_binary[i].flatten()
        
        # Dice coefficient
        intersection = np.sum(y_true * y_pred)
        dice = (2. * intersection) / (np.sum(y_true) + np.sum(y_pred) + 1e-7)
        dice_scores.append(dice)
        
        # IoU
        union = np.sum(np.logical_or(y_true > 0.5, y_pred > 0.5))
        iou = intersection / (union + 1e-7)
        iou_scores.append(iou)
        
        # Precision and Recall
        tp = np.sum((y_true > 0.5) & (y_pred > 0.5))
        fp = np.sum((y_true <= 0.5) & (y_pred > 0.5))
        fn = np.sum((y_true > 0.5) & (y_pred <= 0.5))
        
        precision = tp / (tp + fp + 1e-7)
        recall = tp / (tp + fn + 1e-7)
        
        precision_scores.append(precision)
        recall_scores.append(recall)
    
    # Calculate averages
    avg_dice = np.mean(dice_scores)
    avg_iou = np.mean(iou_scores)
    avg_precision = np.mean(precision_scores)
    avg_recall = np.mean(recall_scores)
    avg_f1 = 2 * (avg_precision * avg_recall) / (avg_precision + avg_recall + 1e-7)
    
    results = f"""
{'='*60}
LANE DETECTION MODEL EVALUATION
{'='*60}
Number of test samples: {len(y_test)}
Average Dice Coefficient: {avg_dice:.4f}
Average IoU: {avg_iou:.4f}
Average Precision: {avg_precision:.4f}
Average Recall: {avg_recall:.4f}
Average F1-Score: {avg_f1:.4f}
{'='*60}
"""
    
    print(results)
    
    if output_file:
        with open(output_file, 'a') as f:
            f.write(results)
    
    return {
        'dice': avg_dice,
        'iou': avg_iou,
        'precision': avg_precision,
        'recall': avg_recall,
        'f1': avg_f1
    }

def predict_lanes_on_image(model, image_path, output_path=None):
    """
    Predict lane markings on a single image
    """
    try:
        # Load and preprocess image
        img = Image.open(image_path).convert('RGB')
        original_size = img.size
        
        # Resize for model input
        img_resized = img.resize((128, 128))
        img_array = preprocess_lane_image(np.array(img_resized))
        
        # Predict
        prediction = model.predict(np.expand_dims(img_array, axis=0))[0]
        lane_mask = (prediction > 0.5).astype(np.uint8)[:, :, 0]
        
        # Resize prediction back to original size
        lane_mask_resized = cv2.resize(lane_mask, original_size, interpolation=cv2.INTER_NEAREST)
        
        # Create visualization
        fig, axes = plt.subplots(1, 3, figsize=(15, 5))
        
        # Original image
        axes[0].imshow(img)
        axes[0].set_title('Original Image')
        axes[0].axis('off')
        
        # Lane prediction overlay
        img_with_lanes = np.array(img)
        lane_overlay = np.zeros_like(img_with_lanes)
        lane_overlay[lane_mask_resized > 0] = [255, 0, 0]  # Red lanes
        
        # Blend images
        blended = cv2.addWeighted(img_with_lanes, 0.7, lane_overlay, 0.3, 0)
        axes[1].imshow(blended)
        axes[1].set_title('Lanes Overlay')
        axes[1].axis('off')
        
        # Lane mask only
        axes[2].imshow(lane_mask_resized, cmap='hot')
        axes[2].set_title('Detected Lanes')
        axes[2].axis('off')
        
        plt.tight_layout()
        
        if output_path:
            plt.savefig(output_path, dpi=150, bbox_inches='tight')
            print(f"Lane prediction saved to {output_path}")
        else:
            plt.show()
        
        plt.close()
        
        return lane_mask_resized
        
    except Exception as e:
        print(f"Error predicting lanes on image: {str(e)}")
        return None

def main():
    """
    Main function for lane detection training and evaluation
    """
    print("Starting Lane Detection Pipeline...")
    
    # Initialize data containers
    frameObjTrain = {'img': [], 'mask': []}
    
    # Data paths - adjust these to your lane dataset
    train_img_path = "data/train_lanes"  # Directory with lane images
    train_mask_path = "data/train_masks"  # Directory with corresponding masks
    
    # Check if data directories exist
    if not os.path.exists(train_img_path):
        print(f"Training images directory not found: {train_img_path}")
        print("Please organize your lane data as follows:")
        print("data/train/ - contains lane images (lane_001.jpg, lane_002.jpg, etc.)")
        print("data/train_masks/ - contains corresponding masks (lane_001_mask.png, etc.)")
        return
    
    if not os.path.exists(train_mask_path):
        print(f"Training masks directory not found: {train_mask_path}")
        return
    
    # Load training data
    try:
        frameObjTrain = load_lane_data(frameObjTrain, train_img_path, train_mask_path)
        if len(frameObjTrain['img']) == 0:
            print("No training data loaded. Please check your data paths and file naming.")
            return
    except Exception as e:
        print(f"Error loading training data: {str(e)}")
        return
    
    # Convert to numpy arrays
    x_train = np.array(frameObjTrain['img'])
    y_train = np.array(frameObjTrain['mask'])
    y_train = np.expand_dims(y_train, axis=-1)  # Add channel dimension
    
    print(f"Training data shape: {x_train.shape}")
    print(f"Training masks shape: {y_train.shape}")
    
    # Build and compile lane detection model
    model = lane_unet_model(input_shape=(128, 128, 3), numFilters=32, dropout_rate=0.1)
    
    # Compile with combined loss and custom metrics
    model.compile(
        optimizer=tf.keras.optimizers.Adam(learning_rate=1e-3),
        loss=combined_loss,
        metrics=['accuracy', dice_coefficient]
    )
    
    # Save model summary
    summary_file = os.path.join(output_dir, 'lane_detection_model_info.txt')
    save_model_summary(model, summary_file)
    print(f"Model summary saved to {summary_file}")
    
    # Train model
    print("\nStarting model training...")
    history = train_lane_model(
        model, x_train, y_train, 
        epochs=50,  # Adjust as needed
        batch_size=16,
        validation_split=0.2,
        output_file=summary_file
    )
    
    # Save final model
    final_model_path = os.path.join(output_dir, 'LaneDetectionModel.keras')
    model.save(final_model_path)
    print(f"Final model saved to {final_model_path}")
    
    # Create validation set for evaluation
    val_split = int(0.8 * len(x_train))
    x_val = x_train[val_split:]
    y_val = y_train[val_split:]
    
    if len(x_val) > 0:
        # Evaluate model
        metrics = evaluate_lane_model(model, x_val, y_val, summary_file)
        
        # Generate sample predictions
        visualize_lane_predictions(model, x_val, y_val[:, :, :, 0], num_samples=3)
    
    print(f"\nLane detection pipeline completed!")
    print(f"Results saved in: {output_dir}")
    
    # Test on custom image if available
    test_image_path = "testimage/test_lane.jpg"
    if os.path.exists(test_image_path):
        print(f"\nTesting on custom image: {test_image_path}")
        output_prediction_path = os.path.join(output_dir, "custom_lane_prediction.png")
        predict_lanes_on_image(model, test_image_path, output_prediction_path)

if __name__ == "__main__":
    main()