"""
Python Microservice: Semantic Similarity Engine.
Run with: python app.py  (listens on http://localhost:5001)
In production: gunicorn -w 2 "nlp_service.app:create_app()" -b 0.0.0.0:5001
"""
from nlp_service.app import create_app
from nlp_service.config import HOST, PORT

app = create_app()

if __name__ == "__main__":
    app.run(host=HOST, port=PORT, debug=False)
