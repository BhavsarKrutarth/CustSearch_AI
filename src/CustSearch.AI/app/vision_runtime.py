"""Explicit OpenCV/ONNX runtime adapter boundary for detector and tracker models."""

from pathlib import Path

import cv2
import numpy as np
import onnxruntime as ort


class OnnxPersonDetector:
    """Loads an approved ONNX detector; model deployment stays configuration-driven."""

    def __init__(self, model_path: Path) -> None:
        if not model_path.is_file():
            raise FileNotFoundError(model_path)
        self._session = ort.InferenceSession(str(model_path), providers=["CPUExecutionProvider"])

    @staticmethod
    def prepare(frame: np.ndarray, width: int, height: int) -> np.ndarray:
        """Resize a frame for model inference without persisting it."""

        resized = cv2.resize(frame, (width, height))
        return np.transpose(resized.astype(np.float32) / 255.0, (2, 0, 1))[None, ...]
