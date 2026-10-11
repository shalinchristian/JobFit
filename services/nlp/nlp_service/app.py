import logging

from flask import Flask

from .routes import bp


def create_app(preload_model: bool = True) -> Flask:
    logging.basicConfig(level=logging.INFO)
    app = Flask(__name__)
    app.register_blueprint(bp)
    if preload_model:
        from .similarity import get_model
        get_model()
    return app
