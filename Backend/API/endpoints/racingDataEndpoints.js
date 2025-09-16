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

    // Upload racing data (CSV file)
    router.post('/racing-data/upload', upload.single('csvFile'), async (req, res) => {
        try {
            if (!req.file) {
                return res.status(400).json({ message: "No CSV file uploaded" });
            }

            const {
                trackName,
                userName,
                sessionType,
                lapTime,
                vehicleUsed,
                gameVersion,
                description
            } = req.body;

            // Convert CSV file to base64
            const csvBase64 = req.file.buffer.toString('base64');

            // Create unique ID based on timestamp and user
            const recordId = `${userName}_${trackName}_${Date.now()}`;

            const newRacingData = {
                _id: recordId,
                trackName: trackName || 'Unknown',
                userName: userName || 'Anonymous',
                sessionType: sessionType || 'Practice', // Practice, Qualifying, Race
                lapTime: lapTime || null,
                vehicleUsed: vehicleUsed || 'Unknown',
                gameVersion: gameVersion || 'MotoGP18',
                description: description || '',
                fileName: req.file.originalname,
                fileSize: req.file.size,
                csvData: csvBase64,
                dateUploaded: new Date().toISOString(),
                uploadedBy: userName || 'System'
            };

            await db.collection("racingData").insertOne(newRacingData);

            // Return response without the large base64 data
            const { csvData, ...responseData } = newRacingData;
            res.status(201).json({ 
                message: "Racing data uploaded successfully",
                data: responseData
            });
        } catch (error) {
            console.error("Racing data upload error:", error);
            if (error.code === 'LIMIT_FILE_SIZE') {
                res.status(400).json({ message: "File too large. Maximum size is 50MB." });
            } else if (error.message === 'Only CSV files are allowed') {
                res.status(400).json({ message: "Only CSV files are allowed" });
            } else {
                res.status(500).json({ message: "Error uploading racing data" });
            }
        }
    });

    // Create racing data with base64 string (alternative to file upload)
    router.post('/racing-data', async (req, res) => {
        try {
            const {
                trackName,
                userName,
                sessionType,
                lapTime,
                vehicleUsed,
                gameVersion,
                description,
                fileName,
                csvData // Expecting base64 encoded string
            } = req.body;

            if (!csvData) {
                return res.status(400).json({ message: "CSV data (base64) is required" });
            }

            // Create unique ID
            const recordId = `${userName || 'anonymous'}_${trackName || 'unknown'}_${Date.now()}`;

            const newRacingData = {
                _id: recordId,
                trackName: trackName || 'Unknown',
                userName: userName || 'Anonymous',
                sessionType: sessionType || 'Practice',
                lapTime: lapTime || null,
                vehicleUsed: vehicleUsed || 'Unknown',
                gameVersion: gameVersion || 'MotoGP18',
                description: description || '',
                fileName: fileName || 'racing_data.csv',
                fileSize: Buffer.from(csvData, 'base64').length,
                csvData: csvData,
                dateUploaded: new Date().toISOString(),
                uploadedBy: userName || 'Anonymous'
            };

            await db.collection("racingData").insertOne(newRacingData);

            // Return response without the large base64 data
            const { csvData: _, ...responseData } = newRacingData;
            res.status(201).json({ 
                message: "Racing data created successfully",
                data: responseData
            });
        } catch (error) {
            console.error("Racing data creation error:", error);
            res.status(500).json({ message: "Error creating racing data" });
        }
    });

    // Download CSV data (decode base64 and return as CSV)
    router.get('/racing-data/:id/download', async (req, res) => {
        try {
            const dataId = req.params.id;
            const racingData = await db.collection("racingData").findOne({ _id: dataId });

            if (!racingData) {
                return res.status(404).json({ message: "Racing data not found" });
            }

            // Decode base64 to CSV
            const csvBuffer = Buffer.from(racingData.csvData, 'base64');
            
            res.setHeader('Content-Type', 'text/csv');
            res.setHeader('Content-Disposition', `attachment; filename="${racingData.fileName}"`);
            res.send(csvBuffer);
        } catch (error) {
            console.error("Download error:", error);
            res.status(500).json({ message: "Failed to download racing data" });
        }
    });

    // Update racing data metadata (not the CSV data itself)
    router.put('/racing-data/:id', async (req, res) => {
        try {
            const dataId = req.params.id;
            const allowedUpdates = ['trackName', 'playerName', 'sessionType', 'lapTime', 'vehicleUsed', 'description'];
            const updateData = {};

            // Only allow specific fields to be updated
            Object.keys(req.body).forEach(key => {
                if (allowedUpdates.includes(key)) {
                    updateData[key] = req.body[key];
                }
            });

            if (Object.keys(updateData).length === 0) {
                return res.status(400).json({ message: "No valid fields to update" });
            }

            updateData.lastModified = new Date().toISOString();

            const result = await db.collection('racingData').updateOne(
                { _id: dataId }, 
                { $set: updateData }
            );

            if (result.modifiedCount === 0) {
                return res.status(404).json({ message: 'Racing data not found or no changes made' });
            }

            res.json({ message: 'Racing data updated successfully' });
        } catch (error) {
            console.error('Update error:', error);
            res.status(500).json({ message: 'Failed to update racing data' });
        }
    });
}