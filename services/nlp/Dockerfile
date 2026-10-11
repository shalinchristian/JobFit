FROM python:3.10-slim

WORKDIR /app

# Hugging Face model cache lives on a volume (see docker-compose.yml) so the ~80 MB model
# is downloaded once, not on every container start.
ENV HF_HOME=/models \
    PYTHONUNBUFFERED=1

COPY requirements.txt .
# CPU-only torch first: avoids the multi-GB CUDA wheels sentence-transformers would otherwise pull.
RUN pip install --no-cache-dir --index-url https://download.pytorch.org/whl/cpu torch \
 && pip install --no-cache-dir -r requirements.txt

# Optional: bake the model into the image (docker compose build --build-arg PRELOAD_MODEL=1).
ARG PRELOAD_MODEL=0
RUN if [ "$PRELOAD_MODEL" = "1" ]; then \
      python -c "from sentence_transformers import SentenceTransformer; SentenceTransformer('all-MiniLM-L6-v2')"; \
    fi

COPY . .

EXPOSE 5001
# Production server; create_app() loads the model when each worker starts.
CMD ["gunicorn", "-w", "2", "-b", "0.0.0.0:5001", "--timeout", "120", "nlp_service.app:create_app()"]
