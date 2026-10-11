"""Semantic similarity between two texts using sentence-transformers."""
import logging

from sklearn.metrics.pairwise import cosine_similarity

logger = logging.getLogger(__name__)

_model = None


def get_model():
    """Load the model lazily, once (downloads ~80MB on first run)."""
    global _model
    if _model is None:
        from sentence_transformers import SentenceTransformer
        from .config import MODEL_NAME
        logger.info("Loading sentence-transformer model %s...", MODEL_NAME)
        _model = SentenceTransformer(MODEL_NAME)
        logger.info("Model loaded successfully.")
    return _model


def scale_score(raw_score: float) -> float:
    """Clamp cosine similarity at 0 and scale to 0-100."""
    return round(max(0.0, float(raw_score)) * 100, 2)


def compute_similarity(text1: str, text2: str, model=None) -> float:
    model = model or get_model()
    embeddings = model.encode([text1, text2])
    raw = cosine_similarity([embeddings[0]], [embeddings[1]])[0][0]
    return scale_score(raw)
