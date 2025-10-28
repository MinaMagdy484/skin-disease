# Simple Image Prediction Script for Skin Disease Classification
import tensorflow as tf
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import Dense, GlobalAveragePooling2D, Flatten
from tensorflow.keras.preprocessing import image
import numpy as np
import matplotlib.pyplot as plt
from PIL import Image
import os

# Suppress TensorFlow warnings
import logging
logging.getLogger("tensorflow").setLevel(logging.ERROR)

# ==================== CONFIGURATION ====================
# Change this to your image path
IMAGE_PATH = r'E:\valeri project skin\8.jpg'  # <-- CHANGE THIS

MODEL_WEIGHTS_PATH = r'E:\valeri project skin\new_model\skindect_model_weights.h5'
IMG_SIZE = (299, 299)

# Class names from your training
CLASS_NAMES = [
    '1. Enfeksiyonel',
    '2. Ekzama', 
    '3. Akne',
    '4. Pigment',
    '5. Benign',
    '6. Malign'
]
# =======================================================

def print_colored(msg, color='green'):
    """Print colored message"""
    colors = {
        'green': '\033[92m',
        'red': '\033[91m',
        'blue': '\033[94m',
        'yellow': '\033[93m',
        'cyan': '\033[96m',
        'end': '\033[0m'
    }
    print(f"{colors.get(color, '')}{msg}{colors['end']}")

def create_model():
    """Recreate the exact model architecture from training"""
    try:
        print_colored("Building model architecture...", 'cyan')
        
        # Create Xception base model (same as training)
        img_shape = (IMG_SIZE[0], IMG_SIZE[1], 3)
        
        xception_base = tf.keras.applications.xception.Xception(
            input_shape=img_shape,
            include_top=False,
            weights='imagenet'
        )
        
        # Build the sequential model (same as training notebook)
        model = Sequential()
        model.add(xception_base)
        model.add(GlobalAveragePooling2D())
        model.add(Flatten())
        model.add(Dense(1024, activation="relu"))
        model.add(Dense(512, activation="relu"))
        model.add(Dense(6, activation="softmax", name="classification"))
        
        print_colored("✓ Model architecture created!", 'green')
        return model
    
    except Exception as e:
        print_colored(f"✗ Error creating model: {e}", 'red')
        return None

def load_model_weights(model, weights_path):
    """Load pre-trained weights"""
    try:
        print_colored("Loading model weights...", 'cyan')
        
        # Check if weights file exists
        if not os.path.exists(weights_path):
            print_colored(f"✗ Weights file not found: {weights_path}", 'red')
            return False
        
        # Load weights
        model.load_weights(weights_path)
        
        # Compile model
        model.compile(
            optimizer=tf.keras.optimizers.SGD(learning_rate=0.0007, momentum=0.9),
            loss='categorical_crossentropy',
            metrics=['accuracy']
        )
        
        print_colored("✓ Model weights loaded successfully!", 'green')
        return True
    
    except Exception as e:
        print_colored(f"✗ Error loading weights: {e}", 'red')
        return False

def load_and_preprocess_image(img_path, target_size):
    """Load and preprocess image"""
    try:
        # Check if file exists
        if not os.path.exists(img_path):
            print_colored(f"✗ Image file not found: {img_path}", 'red')
            return None, None
        
        # Load image
        img = Image.open(img_path)
        print_colored(f"✓ Image loaded: {os.path.basename(img_path)}", 'green')
        print_colored(f"  Original size: {img.size}", 'cyan')
        
        # Convert to RGB if needed
        if img.mode != 'RGB':
            img = img.convert('RGB')
            print_colored("  Converted to RGB", 'cyan')
        
        # Keep original for display
        original_img = img.copy()
        
        # Resize image
        img = img.resize(target_size)
        
        # Convert to array
        img_array = image.img_to_array(img)
        
        # Expand dimensions (add batch dimension)
        img_array = np.expand_dims(img_array, axis=0)
        
        # No scaling - Xception expects 0-255 range (as per training)
        
        return img_array, original_img
    
    except Exception as e:
        print_colored(f"✗ Error processing image: {e}", 'red')
        return None, None

def predict_disease(model, img_array, class_names):
    """Make prediction"""
    try:
        print_colored("\nMaking prediction...", 'cyan')
        
        # Get predictions
        predictions = model.predict(img_array, verbose=0)
        
        # Get predicted class
        predicted_idx = np.argmax(predictions[0])
        predicted_class = class_names[predicted_idx]
        confidence = predictions[0][predicted_idx] * 100
        
        return predicted_class, confidence, predictions[0]
    
    except Exception as e:
        print_colored(f"✗ Error during prediction: {e}", 'red')
        return None, None, None

def display_results(original_img, predicted_class, confidence, all_predictions, class_names):
    """Display prediction results"""
    
    # Print results to console
    print_colored("\n" + "="*70, 'blue')
    print_colored("PREDICTION RESULTS", 'blue')
    print_colored("="*70, 'blue')
    print_colored(f"\n🔍 Detected Disease: {predicted_class}", 'green')
    print_colored(f"📊 Confidence: {confidence:.2f}%\n", 'green')
    
    # Show all predictions
    print_colored("All Class Probabilities:", 'yellow')
    print_colored("-"*70, 'yellow')
    
    # Sort predictions
    sorted_indices = np.argsort(all_predictions)[::-1]
    
    for rank, idx in enumerate(sorted_indices, 1):
        class_name = class_names[idx]
        prob = all_predictions[idx] * 100
        
        # Create visual bar
        bar_length = int(prob / 2)
        bar = "█" * bar_length
        
        # Color and symbol
        if rank == 1:
            color = 'green'
            symbol = "🏆"
        elif rank <= 3:
            color = 'cyan'
            symbol = f"{rank}."
        else:
            color = 'cyan'
            symbol = f"{rank}."
        
        print_colored(f"{symbol:3s} {class_name:20s} {bar:50s} {prob:6.2f}%", color)
    
    print_colored("="*70 + "\n", 'blue')
    
    # Create visualization
    try:
        fig = plt.figure(figsize=(16, 6))
        
        # Display original image
        ax1 = plt.subplot(1, 2, 1)
        ax1.imshow(original_img)
        ax1.axis('off')
        
        # Title color based on confidence
        if confidence > 70:
            title_color = 'green'
        elif confidence > 50:
            title_color = 'orange'
        else:
            title_color = 'red'
        
        ax1.set_title(
            f'Prediction: {predicted_class}\nConfidence: {confidence:.2f}%',
            fontsize=16, fontweight='bold', color=title_color, pad=20
        )
        
        # Display probability bar chart
        ax2 = plt.subplot(1, 2, 2)
        
        # Sort for display
        sorted_classes = [class_names[i] for i in sorted_indices]
        sorted_probs = [all_predictions[i] * 100 for i in sorted_indices]
        
        # Color bars
        colors_chart = ['#2ecc71' if i == 0 else '#3498db' for i in range(len(sorted_classes))]
        
        bars = ax2.barh(sorted_classes, sorted_probs, color=colors_chart)
        ax2.set_xlabel('Confidence (%)', fontsize=12, fontweight='bold')
        ax2.set_title('Prediction Probabilities', fontsize=14, fontweight='bold')
        ax2.set_xlim(0, 100)
        ax2.grid(axis='x', alpha=0.3)
        
        # Add percentage labels
        for bar, prob in zip(bars, sorted_probs):
            width = bar.get_width()
            ax2.text(width + 1, bar.get_y() + bar.get_height()/2,
                    f'{prob:.1f}%',
                    ha='left', va='center', fontweight='bold', fontsize=10)
        
        plt.tight_layout()
        plt.show()
        
    except Exception as e:
        print_colored(f"Note: Could not display visualization: {e}", 'yellow')

def main():
    """Main prediction function"""
    
    print_colored("\n" + "="*70, 'blue')
    print_colored("SKIN DISEASE PREDICTION SYSTEM", 'blue')
    print_colored("="*70 + "\n", 'blue')
    
    # Create model architecture
    model = create_model()
    if model is None:
        print_colored("\n✗ Failed to create model. Exiting...", 'red')
        return
    
    # Load weights
    if not load_model_weights(model, MODEL_WEIGHTS_PATH):
        print_colored("\n✗ Failed to load weights. Exiting...", 'red')
        return
    
    # Show model info
    print_colored("\nModel Information:", 'cyan')
    print_colored(f"  Input shape: {model.input_shape}", 'cyan')
    print_colored(f"  Output classes: {len(CLASS_NAMES)}", 'cyan')
    print_colored(f"  Total parameters: {model.count_params():,}", 'cyan')
    
    # Load and preprocess image
    print_colored(f"\nProcessing image...", 'cyan')
    img_array, original_img = load_and_preprocess_image(IMAGE_PATH, IMG_SIZE)
    
    if img_array is None:
        print_colored("\n✗ Failed to process image. Exiting...", 'red')
        return
    
    # Make prediction
    predicted_class, confidence, all_predictions = predict_disease(
        model, img_array, CLASS_NAMES
    )
    
    if predicted_class is None:
        print_colored("\n✗ Prediction failed. Exiting...", 'red')
        return
    
    # Display results
    display_results(original_img, predicted_class, confidence, all_predictions, CLASS_NAMES)
    
    print_colored("✓ Prediction completed successfully!", 'green')

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print_colored("\n\n✗ Interrupted by user", 'yellow')
    except Exception as e:
        print_colored(f"\n✗ Unexpected error: {e}", 'red')
        import traceback
        print_colored("\nFull error traceback:", 'yellow')
        traceback.print_exc()