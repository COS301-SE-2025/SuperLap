const express = require('express');
const multer = require('multer');

// Configure multer for file uploads
const storage = multer.memoryStorage();
const upload = multer({
  storage: storage,
  fileFilter: (req, file, cb) => {
    // Only allow CSV files
    if (file.mimetype === 'text/csv' || file.originalname.endsWith('.csv')) {
      cb(null, true);
    } else {
      cb(new Error('Only CSV files are allowed'), false);
    }
  },
  limits: {
    fileSize: 50 * 1024 * 1024 // 50MB limit
  }
});

module.exports = function (db) {
  const router = express.Router();

    // RACING DATA ROUTES

    // Fetch all racing data records
    router.get('/racing-data', async (req, res) => {
        try {
            const racingData = await db.collection("racingData").find({}, {
                projection: { csvData: 0 } // Exclude the large base64 data from list view
            }).toArray();
            res.json(racingData);
        } catch (error) {
            console.error(error);
            res.status(500).json({ message: "Failed to fetch racing data" });
        }
    });

    // Fetch racing data by ID
    router.get('/racing-data/:id', async (req, res) => {
        try {
            const dataId = req.params.id;
            const racingData = await db.collection("racingData").findOne({ _id: dataId });
            
            if (!racingData) {
                return res.status(404).json({ message: "Racing data not found" });
            }
            
            res.json(racingData);
        } catch (error) {
            console.error(error);
            res.status(500).json({ message: "Failed to fetch racing data" });
        }
    });

    // Fetch racing data by track name
    router.get('/racing-data/track/:trackName', async (req, res) => {
        try {
            const trackName = req.params.trackName;
            const racingData = await db.collection("racingData").find(
                { trackName: trackName },
                { projection: { csvData: 0 } } // Exclude base64 data from list view
            ).toArray();
            res.json(racingData);
        } catch (error) {
            console.error(error);
            res.status(500).json({ message: "Failed to fetch racing data for track" });
        }
    });

    // Fetch racing data by user
    router.get('/racing-data/user/:userName', async (req, res) => {
        try {
            const userName = req.params.userName;
            const racingData = await db.collection("racingData").find(
                { userName: userName },
                { projection: { csvData: 0 } } // Exclude base64 data from list view
            ).toArray();
            res.json(racingData);
        } catch (error) {
            console.error(error);
            res.status(500).json({ message: "Failed to fetch racing data for player" });
        }
    });
}