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

    @patch('cv.imread')
    def test_loadImg_success(self, mock_imread):
        # Test img loading success
        mock_imread.return_value = self.test_image
        
        result = self.processor.loadImg('test_image.jpg')

        mock_imread.assert_called_once_with('test_image.jpg')
        np.testing.assert_array_equal(result, self.test_image)
        np.testing.assert_array_equal(self.processor.original_image, self.test_image)

    @patch('cv2.imread')
    def test_loadImg_failure(self, mock_imread):
        # Test img loading failure
        mock_imread.return_value = None

        with self.assertRaises(ValueError) as context:
            self.processor.loadImg('nonexistent.jpg')

        self.assertIn("Could not load image", str(context.exception))
    
    @patch('cv2.imshow')
    def test_processImg(self, mock_imshow):
        # Set up processor with a test image
        self.processor.original_image = self.test_image
        result = self.processor.processImg(self.test_image, show_debug=True)

        self.assertIn('processed_image', result)

        # Check processed_image is set
        self.assertIsNotNone(self.processor.processed_image)
        self.assertIsNotNone(self.processor.track_mask)

        # Verify debug was displayed
        mock_imshow.assert_called()

    @patch('cv2.imshow')
    @patch('cv2.findContours')
    @patch('cv2.Canny')
    def test_detectBoundaries_success(self, mock_canny, mock_findContours, mock_imshow):
        # Test successful boundary detection
        mock_canny.return_value = np.zeros((100, 100), dtype=np.uint8)

        # Create mock contours
        contour1 = np.array([[[10, 10]], [[20, 10]], [[20, 20]], [[10, 20]]])
        contour2 = np.array([[[15, 15]], [[18, 15]], [[18, 18]], [[15, 18]]])
        contour3 = np.array([[[12, 12]], [[17, 12]], [[17, 17]], [[12, 17]]])

        mock_findContours.return_value = ([contour1, contour2, contour3], None)

        # Set up processor
        self.processor.original_image = self.test_image

        result = self.processor.detectBoundaries(self.test_image, show_debug=True)

        self.assertIsNotNone(result)
        self.assertIn('outer', result)
        self.assertIn('inner', result)
        self.assertIsNotNone(self.processor.track_boundaries)

        # Verify mock calls
        mock_canny.assert_called_once()
        mock_findContours.assert_called_once()