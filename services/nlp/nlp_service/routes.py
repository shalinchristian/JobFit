import logging

from flask import Blueprint, jsonify, request

from .config import MODEL_NAME
from . import similarity

logger = logging.getLogger(__name__)
bp = Blueprint("api", __name__)


@bp.route("/health", methods=["GET"])
def health():
    return jsonify({"status": "ok", "model": MODEL_NAME}), 200


@bp.route("/similarity", methods=["POST"])
def compute_similarity():
    """Body: {"text1": "...", "text2": "..."} -> {"score": 0-100}"""
    try:
        data = request.get_json(silent=True)
        if not data or "text1" not in data or "text2" not in data:
            return jsonify({"error": "Both 'text1' and 'text2' fields are required."}), 400

        text1 = str(data["text1"]).strip()
        text2 = str(data["text2"]).strip()
        if not text1 or not text2:
            return jsonify({"error": "Texts cannot be empty."}), 400

        score = similarity.compute_similarity(text1, text2)
        logger.info("Similarity score computed: %s", score)
        return jsonify({"score": score}), 200
    except Exception as e:
        logger.error("Error computing similarity: %s", e)
        return jsonify({"error": f"Internal server error: {e}"}), 500
