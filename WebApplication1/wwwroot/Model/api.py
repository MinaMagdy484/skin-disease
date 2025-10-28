from flask import Flask, request, jsonify
from flask_cors import CORS
import tensorflow as tf
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import Dense, GlobalAveragePooling2D, Flatten
from tensorflow.keras.preprocessing import image
import numpy as np
from PIL import Image
import io
import os
import logging

# Suppress TensorFlow warnings
logging.getLogger("tensorflow").setLevel(logging.ERROR)

# ==================== CONFIGURATION ====================
MODEL_WEIGHTS_PATH = r'E:\valeri project skin\new_model\skindect_model_weights.h5'
IMG_SIZE = (299, 299)

CLASS_NAMES = [
    '1. Enfeksiyonel',
    '2. Ekzama', 
    '3. Akne',
    '4. Pigment',
    '5. Benign',
    '6. Malign'
]
# =======================================================

app = Flask(__name__)
CORS(app)  # Enable CORS for all routes

# Global model variable
model = None

def create_model():
    """Recreate the exact model architecture from training"""
    try:
        print("Building model architecture...")
        
        img_shape = (IMG_SIZE[0], IMG_SIZE[1], 3)
        
        xception_base = tf.keras.applications.xception.Xception(
            input_shape=img_shape,
            include_top=False,
            weights='imagenet'
        )
        
        model = Sequential()
        model.add(xception_base)
        model.add(GlobalAveragePooling2D())
        model.add(Flatten())
        model.add(Dense(1024, activation="relu"))
        model.add(Dense(512, activation="relu"))
        model.add(Dense(6, activation="softmax", name="classification"))
        
        print("✓ Model architecture created!")
        return model
    
    except Exception as e:
        print(f"✗ Error creating model: {e}")
        return None

def load_model_weights(mdl, weights_path):
    """Load pre-trained weights"""
    try:
        print("Loading model weights...")
        
        if not os.path.exists(weights_path):
            print(f"✗ Weights file not found: {weights_path}")
            return False
        
        mdl.load_weights(weights_path)
        
        mdl.compile(
            optimizer=tf.keras.optimizers.SGD(learning_rate=0.0007, momentum=0.9),
            loss='categorical_crossentropy',
            metrics=['accuracy']
        )
        
        print("✓ Model weights loaded successfully!")
        return True
    
    except Exception as e:
        print(f"✗ Error loading weights: {e}")
        return False

def initialize_model():
    """Initialize model"""
    global model
    
    print("\n" + "="*70)
    print("INITIALIZING SKIN DISEASE PREDICTION API")
    print("="*70 + "\n")
    
    model = create_model()
    if model is None:
        print("✗ Failed to create model")
        return False
    
    if not load_model_weights(model, MODEL_WEIGHTS_PATH):
        print("✗ Failed to load weights")
        model = None
        return False
    
    print(f"\n✓ API Ready!")
    print(f"  Model loaded with {model.count_params():,} parameters")
    print(f"  Serving on http://0.0.0.0:5000")
    print("="*70 + "\n")
    return True

def preprocess_image(img_file):
    """Preprocess uploaded image"""
    try:
        # Read image from file upload
        img = Image.open(io.BytesIO(img_file.read()))
        
        # Convert to RGB if needed
        if img.mode != 'RGB':
            img = img.convert('RGB')
        
        # Resize image
        img = img.resize(IMG_SIZE)
        
        # Convert to array
        img_array = image.img_to_array(img)
        
        # Expand dimensions (add batch dimension)
        img_array = np.expand_dims(img_array, axis=0)
        
        return img_array
    
    except Exception as e:
        raise ValueError(f"Error processing image: {str(e)}")

@app.route('/', methods=['GET'])
def home():
    """API home endpoint"""
    return jsonify({
        'message': 'Skin Disease Prediction API',
        'version': '1.0',
        'status': 'running',
        'model_loaded': model is not None,
        'endpoints': {
            '/': 'GET - API information',
            '/predict': 'POST - Upload image for prediction (multipart/form-data with key "file")',
            '/health': 'GET - Check API health status',
            '/classes': 'GET - Get available disease classes'
        },
        'usage': {
            'curl': 'curl -X POST -F "file=@image.jpg" http://localhost:5000/predict',
            'python': 'requests.post("http://localhost:5000/predict", files={"file": open("image.jpg", "rb")})'
        }
    })

@app.route('/health', methods=['GET'])
def health():
    """Health check endpoint"""
    if model is None:
        return jsonify({
            'status': 'error',
            'message': 'Model not loaded',
            'model_loaded': False
        }), 503
    
    return jsonify({
        'status': 'healthy',
        'model_loaded': True,
        'classes_count': len(CLASS_NAMES),
        'image_size': IMG_SIZE
    })

@app.route('/classes', methods=['GET'])
def get_classes():
    """Get available disease classes"""
    return jsonify({
        'classes': CLASS_NAMES,
        'count': len(CLASS_NAMES)
    })

@app.route('/predict', methods=['POST'])
def predict():
    """Predict disease from uploaded image"""
    
    # Check if model is loaded
    if model is None:
        return jsonify({
            'error': 'Model not loaded. Please restart the server.'
        }), 503
    
    # Check if file is present in request
    if 'file' not in request.files:
        return jsonify({
            'error': 'No file uploaded. Please send image with key "file"',
            'usage': 'curl -X POST -F "file=@image.jpg" http://localhost:5000/predict'
        }), 400
    
    file = request.files['file']
    
    # Check if file is selected
    if file.filename == '':
        return jsonify({
            'error': 'No file selected'
        }), 400
    
    # Check file extension
    allowed_extensions = {'png', 'jpg', 'jpeg', 'gif', 'bmp'}
    file_ext = file.filename.rsplit('.', 1)[1].lower() if '.' in file.filename else ''
    
    if file_ext not in allowed_extensions:
        return jsonify({
            'error': f'Invalid file type. Allowed: {", ".join(allowed_extensions)}'
        }), 400
    
    try:
        # Preprocess image
        img_array = preprocess_image(file)
        
        # Make prediction
        predictions = model.predict(img_array, verbose=0)
        
        # Get predicted class
        predicted_idx = np.argmax(predictions[0])
        predicted_class = CLASS_NAMES[predicted_idx]
        confidence = float(predictions[0][predicted_idx] * 100)
        
        # Get all predictions
        all_predictions = {
            CLASS_NAMES[i]: float(predictions[0][i] * 100)
            for i in range(len(CLASS_NAMES))
        }
        
        # Sort predictions by confidence
        sorted_predictions = dict(
            sorted(all_predictions.items(), key=lambda x: x[1], reverse=True)
        )
        
        # Prepare response
        response = {
            'success': True,
            'predicted_class': predicted_class,
            'confidence': round(confidence, 2),
            'all_predictions': sorted_predictions,
            'image_size': IMG_SIZE,
            'model_info': {
                'architecture': 'Xception',
                'classes': len(CLASS_NAMES)
            }
        }
        
        return jsonify(response), 200
    
    except ValueError as ve:
        return jsonify({
            'error': str(ve)
        }), 400
    
    except Exception as e:
        return jsonify({
            'error': 'Prediction failed',
            'details': str(e)
        }), 500

@app.errorhandler(404)
def not_found(error):
    return jsonify({
        'error': 'Endpoint not found',
        'message': 'Please check the API documentation',
        'available_endpoints': [
            'GET /',
            'GET /health',
            'GET /classes',
            'POST /predict'
        ]
    }), 404

@app.errorhandler(500)
def internal_error(error):
    return jsonify({
        'error': 'Internal server error',
        'message': str(error)
    }), 500

if __name__ == '__main__':
    # Initialize model at startup
    if not initialize_model():
        print("\n✗ Failed to initialize model. Exiting...")
        exit(1)
    
    # Run Flask app
    print("\n🚀 Starting Flask server...")
    print("📍 API available at: http://localhost:5000")
    print("📖 Documentation: http://localhost:5000/")
    print("\nPress CTRL+C to stop\n")
    
    app.run(
        host='0.0.0.0',
        port=5000,
        debug=False,
        threaded=True
    )