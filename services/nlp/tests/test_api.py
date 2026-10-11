import numpy as np
import pytest

from nlp_service import similarity
from nlp_service.app import create_app


class FakeModel:
    def encode(self, texts):
        return np.array([[1.0, 0.0] if "python" in t.lower() else [0.0, 1.0] for t in texts])


@pytest.fixture
def client(monkeypatch):
    monkeypatch.setattr(similarity, "_model", FakeModel())
    return create_app(preload_model=False).test_client()


def test_health(client):
    assert client.get("/health").get_json()["status"] == "ok"


def test_missing_fields(client):
    assert client.post("/similarity", json={"text1": "a"}).status_code == 400


def test_empty_text(client):
    assert client.post("/similarity", json={"text1": " ", "text2": "x"}).status_code == 400


def test_identical_meaning_scores_100(client):
    r = client.post("/similarity", json={"text1": "Python dev", "text2": "python job"})
    assert r.status_code == 200 and r.get_json()["score"] == 100.0


def test_unrelated_scores_0(client):
    r = client.post("/similarity", json={"text1": "Python dev", "text2": "chef"})
    assert r.get_json()["score"] == 0.0


def test_scale_score_clamps_negative():
    assert similarity.scale_score(-0.4) == 0.0
    assert similarity.scale_score(0.7843) == 78.43
