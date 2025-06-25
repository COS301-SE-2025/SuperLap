import unittest
import numpy as np
import cv2 as cv
import tempfile
import os
import struct
import json
import sys
from unittest.mock import patch, MagicMock, mock_open
from pathlib import Path

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))

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

    @patch('cv2.findContours')
    def test_detectBoundaries_insufficient_contours(self, mock_findContours):
        # Test boundary detection with insufficient contours.
        mock_findContours.return_value = ([np.array([[[10, 10]]])], None)  # Only 1 contour
        
        result = self.processor.detectBoundaries(self.test_image, show_debug=False)
        
        self.assertIsNone(result)

    def test_visualizeCenterline(self):
        # Test centerline visualization
        self.processor.original_image = self.test_image
        self.processor.centerline = self.test_points
        self.processor.centerline_smoothed = self.test_points
        
        result = self.processor.visualizeCenterline()
        
        self.assertIsInstance(result, np.ndarray)
        self.assertEqual(result.shape, self.test_image.shape)

    def test_visualizeCenterline_no_image(self):
        # Test visualization without original image
        result = self.processor.visualizeCenterline()
        self.assertIsNone(result)

    def test_saveCenterlineToBin(self):
        # Test saving centerline to binary file
        self.processor.centerline = self.test_points
        
        with tempfile.NamedTemporaryFile(delete=False) as tmp_file:
            tmp_path = tmp_file.name
        
        try:
            result = self.processor.saveCenterlineToBin(tmp_path)
            
            self.assertTrue(result)
            self.assertTrue(os.path.exists(tmp_path))
            
            # Verify file contents
            with open(tmp_path, 'rb') as f:
                num_points = struct.unpack('<I', f.read(4))[0]
                self.assertEqual(num_points, len(self.test_points))
                
                for i, (expected_x, expected_y) in enumerate(self.test_points):
                    x, y = struct.unpack('<ff', f.read(8))
                    self.assertAlmostEqual(x, expected_x, places=5)
                    self.assertAlmostEqual(y, expected_y, places=5)
        
        finally:
            if os.path.exists(tmp_path):
                os.unlink(tmp_path)

    def test_saveCenterlineToBin_no_points(self):
        # Test saving centerline with no points
        with tempfile.NamedTemporaryFile(delete=False) as tmp_file:
            tmp_path = tmp_file.name
        
        try:
            result = self.processor.saveCenterlineToBin(tmp_path)
            self.assertFalse(result)
        finally:
            if os.path.exists(tmp_path):
                os.unlink(tmp_path)

    def test_readEdgesFromBin(self):
        # Test reading edges from binary file
        # Create test data
        outer_points = [(10, 20), (30, 40), (50, 60)]
        inner_points = [(15, 25), (35, 45)]
        
        with tempfile.NamedTemporaryFile(delete=False) as tmp_file:
            tmp_path = tmp_file.name
            
            # Write test data
            with open(tmp_path, 'wb') as f:
                for points in [outer_points, inner_points]:
                    f.write(struct.pack('<I', len(points)))
                    for x, y in points:
                        f.write(struct.pack('<ff', float(x), float(y)))
        
        try:
            result = self.processor.readEdgesFromBin(tmp_path)
            
            self.assertIn('outer_boundary', result)
            self.assertIn('inner_boundary', result)
            self.assertEqual(len(result['outer_boundary']), len(outer_points))
            self.assertEqual(len(result['inner_boundary']), len(inner_points))
            
        finally:
            if os.path.exists(tmp_path):
                os.unlink(tmp_path)

    @patch('cv2.imwrite')
    @patch('os.makedirs')
    def test_drawEdgesFromBin(self, mock_makedirs, mock_imwrite):
        # Test drawing edges from binary file
        # Create test binary file
        outer_points = [(10, 20), (30, 40), (50, 60)]
        inner_points = [(15, 25), (35, 45)]
        
        with tempfile.NamedTemporaryFile(delete=False, suffix='.bin') as tmp_file:
            tmp_path = tmp_file.name
            
            with open(tmp_path, 'wb') as f:
                for points in [outer_points, inner_points]:
                    f.write(struct.pack('<I', len(points)))
                    for x, y in points:
                        f.write(struct.pack('<ff', float(x), float(y)))
        
        try:
            result = self.processor.drawEdgesFromBin(tmp_path)
            
            self.assertIsInstance(result, str)
            mock_imwrite.assert_called_once()
            
        finally:
            if os.path.exists(tmp_path):
                os.unlink(tmp_path)

    @patch('cv2.imwrite')
    @patch('os.makedirs')
    def test_saveProcessedImages(self, mock_makedirs, mock_imwrite):
        # Test saving processed images
        self.processor.original_image = self.test_image
        
        results = {
            'processed_image': self.test_image,
            'mask': self.test_mask
        }
        
        with tempfile.TemporaryDirectory() as tmp_dir:
            saved_files = self.processor.saveProcessedImages(
                results, tmp_dir, 'test_track'
            )
            
            self.assertIsInstance(saved_files, list)
            self.assertGreater(len(saved_files), 0)

class TestProcessTrackFunction(unittest.TestCase):
    # Test the processTrack function

    @patch('TrackProcessor.TrackProcessor')
    @patch('os.makedirs')
    @patch('builtins.open', new_callable=mock_open)
    @patch('json.dump')
    def test_processTrack_success(self, mock_json_dump, mock_file_open, 
                                 mock_makedirs, mock_processor_class):
        # Test successful track processing.

        # Mock the processor instance
        mock_processor = mock_processor_class.return_value
        mock_processor.loadImg.return_value = np.zeros((100, 100, 3))
        mock_processor.processImg.return_value = {'processed_image': np.zeros((100, 100))}
        mock_processor.detectBoundaries.return_value = {
            'outer': np.array([[[10, 10]], [[20, 20]]]),
            'inner': np.array([[[15, 15]], [[18, 18]]])
        }
        mock_processor.saveProcessedImages.return_value = ['file1.png', 'file2.png']
        mock_processor.centerline = None
        
        with tempfile.NamedTemporaryFile(suffix='.jpg') as tmp_img:
            result = processTrack(tmp_img.name, show_debug=False)
            
            self.assertIsNotNone(result)
            self.assertIn('processing_successful', result)
            self.assertTrue(result['processing_successful'])

    @patch('TrackProcessor.TrackProcessor')
    def test_processTrack_exception(self, mock_processor_class):
        """Test processTrack with exception."""
        mock_processor = mock_processor_class.return_value
        mock_processor.loadImg.side_effect = Exception("Test error")
        
        result = processTrack('nonexistent.jpg', show_debug=False)
        
        self.assertIsNone(result)

class TestProcessAllTracksFunction(unittest.TestCase):
    # Test the processAllTracks function

    @patch('glob.glob')
    @patch('TrackProcessor.processTrack')
    def test_processAllTracks_success(self, mock_processTrack, mock_glob):
        """Test processing multiple tracks."""
        mock_glob.return_value = ['track1.jpg', 'track2.png']
        mock_processTrack.return_value = {'processing_successful': True}
        
        results = processAllTracks(show_debug=False)
        
        self.assertEqual(len(results), 2)
        self.assertEqual(mock_processTrack.call_count, 2)

    @patch('glob.glob')
    def test_processAllTracks_no_files(self, mock_glob):
        # Test processing with no image files found.
        mock_glob.return_value = []
        
        results = processAllTracks(show_debug=False)
        
        self.assertEqual(len(results), 0)

class TestIntegration(unittest.TestCase):
    # Integration tests for the full pipeline

    def setUp(self):
        # Create test images
        self.test_dir = tempfile.mkdtemp()
        # Create test track img
        track_img = np.ones((200, 300, 3), dtype=np.uint8) * 255
        cv.ellipse(track_img, (150, 100), (120, 80), 0, 0, 360, (50, 50, 50), 20)
        cv.ellipse(track_img, (150, 100), (80, 50), 0, 0, 360, (0, 0, 0), 15)

        self.track_path = os.path.join(self.test_dir, 'test_track.png')
        cv.imwrite(self.track_path, track_img)

    def tearDown(self):
        import shutil
        shutil.rmtree(self.test_dir, ignore_errors=True)

    @patch('cv2.imshow')
    @patch('cv2.waitKey')
    @patch('cv2.destroyAllWindows')
    def test_full_pipeline(self, mock_destroy, mock_waitkey, mock_imshow):
        # Test the complete pipeline
        mock_waitkey.return_value = ord('q')
        
        with tempfile.TemporaryDirectory() as output_dir:
            result = processTrack(
                self.track_path, 
                output_dir, 
                show_debug=True,
                extract_centerline=False
            )
            
            self.assertIsNotNone(result)
            self.assertTrue(result['processing_successful'])
            self.assertGreater(len(result['processed_files']), 0)

if __name__ == '__main__':
    # Configure test discovery and execution
    unittest.main(verbosity=2)