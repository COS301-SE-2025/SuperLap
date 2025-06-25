import unittest
import numpy as np
import cv2 as cv
import tempfile
import os
import struct
import json
from unittest.mock import patch, MagicMock, mock_open
from pathlib import Path
import sys

from TrackProcessor import TrackProcessor, processTrack, processAllTracks

class TestTrackProcessor(unittest.TestCase):
    
    def setUp(self):
        # Set up test image before each test
        self.processor = TrackProcessor()

        self.test_image = np.zeros((100, 100, 3), dtype=np.uint8)
        cv.rectangle(self.test_image, (20, 30), (80, 70), (50, 50, 50), -1)
        cv.rectangle(self.test_image, (30, 40), (70, 60), 255, -1)

        self.test_mask = np.zeros((100, 100), dtype=np.uint8)
        cv.rectangle(self.test_mask, (30, 40), (70, 60), 255, -1)

        self.test_points = [(10, 10), (20, 15), (30, 20), (40, 25), (50, 30)]

    def tearDown(self):
        cv.destroyAllWindows()

    def test_init(self):
        processor = TrackProcessor()
        self.assertIsNone(processor.original_image)
        self.assertIsNone(processor.processed_image)
        self.assertIsNone(processor.track_mask)
        self.assertIsNone(processor.track_boundaries)
        self.assertIsNone(processor.centerline)
        self.assertIsNone(processor.centerline_smoothed)