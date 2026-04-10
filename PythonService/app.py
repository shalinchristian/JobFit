"""
Python Microservice: Semantic Similarity Engine
Uses sentence-transformers (all-MiniLM-L6-v2) to compute cosine similarity
between resume text and job description text.

Run with: python app.py
Listens on: http://localhost:5001
"""

from flask import Flask, request, jsonify
from sentence_transformers import SentenceTransformer
from sklearn.metrics.pairwise import cosine_similarity
import numpy as np
import logging

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = Flask(__name__)

# Load the model once at startup (downloads ~80MB on first run)
# all-MiniLM-L6-v2 is fast, lightweight, and produces great sentence embeddings
logger.info("Loading sentence-transformer model...")
model = SentenceTransformer("all-MiniLM-L6-v2")
logger.info("Model loaded successfully.")


@app.route("/health", methods=["GET"])
def health():
    """Simple health check endpoint."""
    return jsonify({"status": "ok", "model": "all-MiniLM-L6-v2"}), 200


@app.route("/similarity", methods=["POST"])
def compute_similarity():
    """
    Computes cosine similarity between two texts.

    Request Body (JSON):
        {
            "text1": "Resume text here...",
            "text2": "Job description text here..."
        }

    Response (JSON):
        {
            "score": 78.43   // similarity score scaled 0–100
        }
    """
    try:
        data = request.get_json()

        # Validate incoming payload
        if not data or "text1" not in data or "text2" not in data:
            return jsonify({"error": "Both 'text1' and 'text2' fields are required."}), 400

        text1 = data["text1"].strip()
        text2 = data["text2"].strip()

        if not text1 or not text2:
            return jsonify({"error": "Texts cannot be empty."}), 400

        # Encode both texts into dense vector embeddings
        # The model returns a 384-dimensional vector for each text
        logger.info("Encoding texts...")
        embeddings = model.encode([text1, text2])

        # Compute cosine similarity between the two embedding vectors
        # cosine_similarity returns a 2D matrix; we extract [0][1]
        similarity_matrix = cosine_similarity([embeddings[0]], [embeddings[1]])
        raw_score = float(similarity_matrix[0][0])  # value between -1 and 1

        # Scale to 0–100 for easier interpretation
        # Cosine similarity for text is typically in [0, 1] range
        score = round(max(0.0, raw_score) * 100, 2)

        logger.info(f"Similarity score computed: {score}")
        return jsonify({"score": score}), 200

    except Exception as e:
        logger.error(f"Error computing similarity: {str(e)}")
        return jsonify({"error": f"Internal server error: {str(e)}"}), 500


if __name__ == "__main__":
    # Run Flask dev server on port 5001
    # In production, use gunicorn: gunicorn -w 2 app:app -b 0.0.0.0:5001
    app.run(host="0.0.0.0", port=5001, debug=False)
