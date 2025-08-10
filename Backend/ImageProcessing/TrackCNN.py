import os
import numpy as np
import cv2
import matplotlib.pyplot as plt
import tensorflow as tf
from PIL import Image

# Create output folder for predictions
OUTPUT_DIR = "CNNoutput"
os.makedirs(OUTPUT_DIR, exist_ok=True)

def LoadData(frameObj=None, imgPath=None, maskPath=None, shape=128):
    if frameObj is None:
        frameObj = {'img': [], 'mask': []}
    imgNames = os.listdir(imgPath)
    maskNames = []

    # Generate mask base names from image names
    for mem in imgNames:
        mem = mem.split('_')[0]
        if mem not in maskNames:
            maskNames.append(mem)

    imgAddr = os.path.join(imgPath, '')
    maskAddr = os.path.join(maskPath, '')

    for i in range(len(imgNames)):
        try:
            img = plt.imread(imgAddr + maskNames[i] + '_sat.jpg')
            mask = plt.imread(maskAddr + maskNames[i] + '_mask.png')
        except:
            continue
        img = cv2.resize(img, (shape, shape))
        mask = cv2.resize(mask, (shape, shape))
        frameObj['img'].append(img)
        frameObj['mask'].append(mask[:, :, 0])  # binary mask channel 0

    return frameObj

def Conv2dBlock(inputTensor, numFilters, kernelSize=3, doBatchNorm=True):
    x = tf.keras.layers.Conv2D(filters=numFilters, kernel_size=(kernelSize, kernelSize),
                               kernel_initializer='he_normal', padding='same')(inputTensor)
    if doBatchNorm:
        x = tf.keras.layers.BatchNormalization()(x)
    x = tf.keras.layers.Activation('relu')(x)

    x = tf.keras.layers.Conv2D(filters=numFilters, kernel_size=(kernelSize, kernelSize),
                               kernel_initializer='he_normal', padding='same')(x)
    if doBatchNorm:
        x = tf.keras.layers.BatchNormalization()(x)
    x = tf.keras.layers.Activation('relu')(x)

    return x

def unetBlock(inputImage, numFilters=16, dropouts=0.1, doBatchNorm=True):
    c1 = Conv2dBlock(inputImage, numFilters * 1, kernelSize=3, doBatchNorm=doBatchNorm)
    p1 = tf.keras.layers.MaxPooling2D((2, 2))(c1)
    p1 = tf.keras.layers.Dropout(dropouts)(p1)

    c2 = Conv2dBlock(p1, numFilters * 2, kernelSize=3, doBatchNorm=doBatchNorm)
    p2 = tf.keras.layers.MaxPooling2D((2, 2))(c2)
    p2 = tf.keras.layers.Dropout(dropouts)(p2)

    c3 = Conv2dBlock(p2, numFilters * 4, kernelSize=3, doBatchNorm=doBatchNorm)
    p3 = tf.keras.layers.MaxPooling2D((2, 2))(c3)
    p3 = tf.keras.layers.Dropout(dropouts)(p3)

    c4 = Conv2dBlock(p3, numFilters * 8, kernelSize=3, doBatchNorm=doBatchNorm)
    p4 = tf.keras.layers.MaxPooling2D((2, 2))(c4)
    p4 = tf.keras.layers.Dropout(dropouts)(p4)

    c5 = Conv2dBlock(p4, numFilters * 16, kernelSize=3, doBatchNorm=doBatchNorm)

    u6 = tf.keras.layers.Conv2DTranspose(numFilters * 8, (3, 3), strides=(2, 2), padding='same')(c5)
    u6 = tf.keras.layers.concatenate([u6, c4])
    u6 = tf.keras.layers.Dropout(dropouts)(u6)
    c6 = Conv2dBlock(u6, numFilters * 8, kernelSize=3, doBatchNorm=doBatchNorm)

    u7 = tf.keras.layers.Conv2DTranspose(numFilters * 4, (3, 3), strides=(2, 2), padding='same')(c6)
    u7 = tf.keras.layers.concatenate([u7, c3])
    u7 = tf.keras.layers.Dropout(dropouts)(u7)
    c7 = Conv2dBlock(u7, numFilters * 4, kernelSize=3, doBatchNorm=doBatchNorm)

    u8 = tf.keras.layers.Conv2DTranspose(numFilters * 2, (3, 3), strides=(2, 2), padding='same')(c7)
    u8 = tf.keras.layers.concatenate([u8, c2])
    u8 = tf.keras.layers.Dropout(dropouts)(u8)
    c8 = Conv2dBlock(u8, numFilters * 2, kernelSize=3, doBatchNorm=doBatchNorm)

    u9 = tf.keras.layers.Conv2DTranspose(numFilters * 1, (3, 3), strides=(2, 2), padding='same')(c8)
    u9 = tf.keras.layers.concatenate([u9, c1])
    u9 = tf.keras.layers.Dropout(dropouts)(u9)
    c9 = Conv2dBlock(u9, numFilters * 1, kernelSize=3, doBatchNorm=doBatchNorm)

    output = tf.keras.layers.Conv2D(1, (1, 1), activation='sigmoid')(c9)
    model = tf.keras.Model(inputs=[inputImage], outputs=[output])
    return model

def build_unet_model(input_shape=(128, 128, 3), dropouts=0.07):
    inputs = tf.keras.layers.Input(input_shape)
    model = unetBlock(inputs, dropouts=dropouts)
    model.compile(optimizer='Adam', loss='binary_crossentropy', metrics=['accuracy'])
    print("U-Net model built and compiled.")
    return model

def train_model(model, train_img_path, train_mask_path, epochs=5, batch_size=16, shape=128):
    print(f"Loading training data from {train_img_path}")
    train_data = LoadData(imgPath=train_img_path, maskPath=train_mask_path, shape=shape)

    imgs = np.array(train_data['img'])
    masks = np.array(train_data['mask'])
    masks = np.expand_dims(masks, axis=-1)  # Add channel dimension for masks

    print(f"Training on {len(imgs)} images for {epochs} epochs")
    history = model.fit(imgs, masks, epochs=epochs, batch_size=batch_size, verbose=1)

    print("Training complete.")
    return history

def save_model(model, save_path='MapSegmentationGenerator.keras'):
    model.save(save_path)
    print(f"Model saved to {save_path}")

def load_model_from_file(model_path='MapSegmentationGenerator.keras'):
    model = tf.keras.models.load_model(model_path, compile=False)
    model.compile(optimizer='rmsprop', loss='binary_crossentropy', metrics=['accuracy'])
    print(f"Model loaded from {model_path}")
    return model

def preprocess_image(image_path, target_size=(128,128)):
    image = Image.open(image_path).convert('RGB')
    image = image.resize(target_size)
    image_np = np.array(image) / 255.0  # normalize to [0,1]
    return image_np

def predict_mask(model, image_np):
    input_tensor = np.expand_dims(image_np, axis=0)  # Add batch dimension
    prediction = model.predict(input_tensor)[0, :, :, 0]  # shape (128,128)
    return prediction

def save_prediction_image(pred_mask, output_path):
    pred_uint8 = (pred_mask * 255).astype(np.uint8)
    pred_img = Image.fromarray(pred_uint8)
    pred_img.save(output_path)
    print(f"Prediction saved to {output_path}")

def process_images(model, image_paths):
    results = {}
    for path in image_paths:
        print(f"Processing image: {path}")
        img_np = preprocess_image(path)
        pred_mask = predict_mask(model, img_np)

        base_name = os.path.splitext(os.path.basename(path))[0]
        output_file = os.path.join(OUTPUT_DIR, f"{base_name}_pred.png")
        save_prediction_image(pred_mask, output_file)

        results[path] = output_file
    return results

def visualize_results(predicted_mask, original_image, save_path=None):
    plt.figure(figsize=(10, 5))

    plt.subplot(1, 2, 1)
    plt.imshow(original_image)
    plt.title('Original Image')
    plt.axis('off')

    plt.subplot(1, 2, 2)
    plt.imshow(np.squeeze(predicted_mask), cmap='gray', vmin=0, vmax=1)
    plt.title('Predicted Mask')
    plt.axis('off')

    if save_path:
        plt.savefig(save_path)
        print(f"Visualization saved to {save_path}")
    else:
        plt.show()

if __name__ == "__main__":
    # Example usage as a script to train and predict

    # Paths (adjust these as needed)
    train_img_path = 'data/train'
    train_mask_path = 'data/train'
    test_image_paths = [
        'data/test/100393_sat.jpg',
        'data/test/206133_sat.jpg',
        'data/test/314381_sat.jpg',
        'data/test/429332_sat.jpg'
    ]

    # Build and train model
    model = build_unet_model()
    history = train_model(model, train_img_path, train_mask_path, epochs=5)

    # Save model after training
    save_model(model)

    # Load model for inference
    model = load_model_from_file()

    # Predict on test images
    results = process_images(model, test_image_paths)

    # Optional: visualize first prediction
    for img_path, pred_path in results.items():
        orig_img = Image.open(img_path)
        pred_img = Image.open(pred_path)
        visualize_results(np.array(pred_img)/255.0, orig_img)
        break  # visualize only first image
