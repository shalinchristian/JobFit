import os

MODEL_NAME = os.getenv("MODEL_NAME", "all-MiniLM-L6-v2")
HOST = os.getenv("HOST", "0.0.0.0")
PORT = int(os.getenv("PORT", "5001"))
