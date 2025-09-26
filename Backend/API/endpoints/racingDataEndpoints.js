const express = require('express');
const multer = require('multer');
const { ObjectId } = require('mongodb');

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
      res.status(200).json(racingData);
    } catch (error) {
      console.error(error);
      res.status(500).json({ message: "Failed to fetch racing data" });
    }
  });

  // Fetch racing data by ID
  router.get('/racing-data/:id', async (req, res) => {
    try {
      const dataId = req.params.id;
      let query;
      
      // Check if it's a valid ObjectId
      if (ObjectId.isValid(dataId)) {
        query = { _id: new ObjectId(dataId) };
      } else {
        query = { _id: dataId };
      }
      
      const racingData = await db.collection("racingData").findOne(query);
      
      if (!racingData) {
        return res.status(404).json({ message: "Racing data not found" });
      }
      res.status(200).json(racingData);
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
      res.status(200).json(racingData);
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
      res.status(200).json(racingData);
    } catch (error) {
      console.error(error);
      res.status(500).json({ message: "Failed to fetch racing data for user" });
    }
  });

  // Fetch last track data uploaded by user
  router.get('/racing-data/user/:userName/last', async (req, res) => {
    try {
      const userName = req.params.userName;
      const lastRacingData = await db.collection("racingData").findOne(
        { userName: userName },
        { sort: { dateUploaded: -1 }, projection: { csvData: 0 } } // Exclude base64 data
      );
      
      if (!lastRacingData) {
        return res.status(404).json({ message: "No racing data found for user" });
      }
      
      res.status(200).json(lastRacingData);
    } catch (error) {
      console.error(error);
      res.status(500).json({ message: "Failed to fetch last racing data for user" });
    }
  });

  // Upload racing data (CSV file) with proper error handling
  router.post('/racing-data/upload', (req, res, next) => {
    upload.single('csvFile')(req, res, function (err) {
      if (err) {
        if (err instanceof multer.MulterError) {
          if (err.code === 'LIMIT_FILE_SIZE') {
            return res.status(400).json({ message: "File too large. Maximum size is 50MB." });
          }
        } else if (err.message === 'Only CSV files are allowed') {
          return res.status(400).json({ message: "Only CSV files are allowed" });
        }
        return res.status(500).json({ message: "Error uploading racing data" });
      }
      next();
    });
  }, async (req, res) => {
    try {
      if (!req.file) {
        return res.status(400).json({ message: "No CSV file uploaded" });
      }

      const {
        trackName,
        userName,
        vehicleUsed,
        description
      } = req.body;

      // Convert CSV file to base64
      const csvBase64 = req.file.buffer.toString('base64');

      // Create unique ID based on timestamp and user
      const recordId = new ObjectId();

      const newRacingData = {
        _id: recordId,
        trackName: trackName || 'Unknown',
        userName: userName || 'Anonymous',
        vehicleUsed: vehicleUsed || 'Unknown',
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
      res.status(500).json({ message: "Error uploading racing data" });
    }
  });

  // Create racing data with base64 string (alternative to file upload)
  router.post('/racing-data', async (req, res) => {
    try {
      const {
        trackName,
        userName,
        lapTime,
        vehicleUsed,
        description,
        fileName,
        csvData // Expecting base64 encoded string
      } = req.body;

      if (!csvData) {
        return res.status(400).json({ message: "CSV data (base64) is required" });
      }

      // Create unique ID
      const recordId = new ObjectId();

      const newRacingData = {
        _id: recordId,
        trackName: trackName || 'Unknown',
        userName: userName || 'Anonymous',
        lapTime: lapTime || null,
        vehicleUsed: vehicleUsed || 'Unknown',
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
      let query;
      
      if (ObjectId.isValid(dataId)) {
        query = { _id: new ObjectId(dataId) };
      } else {
        query = { _id: dataId };
      }
      
      const racingData = await db.collection("racingData").findOne(query);

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
      let query;
      
      if (ObjectId.isValid(dataId)) {
        query = { _id: new ObjectId(dataId) };
      } else {
        query = { _id: dataId };
      }
      
      const allowedUpdates = ['trackName', 'userName', 'vehicleUsed', 'description', 'lapTime'];
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
        query, 
        { $set: updateData }
      );

      if (result.matchedCount === 0) {
        return res.status(404).json({ message: 'Racing data not found' });
      }

      res.status(200).json({ message: 'Racing data updated successfully' });
    } catch (error) {
      console.error('Update error:', error);
      res.status(500).json({ message: 'Failed to update racing data' });
    }
  });

  // Delete racing data
  router.delete('/racing-data/:id', async (req, res) => {
    try {
      const dataId = req.params.id;
      let query;
      
      if (ObjectId.isValid(dataId)) {
        query = { _id: new ObjectId(dataId) };
      } else {
        query = { _id: dataId };
      }
      
      const result = await db.collection("racingData").deleteOne(query);

      if (result.deletedCount === 0) {
        return res.status(404).json({ message: "Racing data not found" });
      }

      res.status(200).json({ message: "Racing data deleted successfully" });
    } catch (error) {
      console.error("Delete error:", error);
      res.status(500).json({ message: "Failed to delete racing data" });
    }
  });

  // Get racing data statistics
  router.get('/racing-data/stats/summary', async (req, res) => {
    try {
      const pipeline = [
        {
          $group: {
            _id: null,
            totalRecords: { $sum: 1 },
            uniqueTracks: { $addToSet: "$trackName" },
            uniqueUsers: { $addToSet: "$userName" },
            avgFileSize: { $avg: "$fileSize" },
            totalDataSize: { $sum: "$fileSize" }
          }
        },
        {
          $project: {
            _id: 0,
            totalRecords: 1,
            uniqueTracksCount: { $size: "$uniqueTracks" },
            uniqueUsersCount: { $size: "$uniqueUsers" },
            avgFileSizeKB: { $round: [{ $divide: ["$avgFileSize", 1024] }, 2] },
            totalDataSizeMB: { $round: [{ $divide: ["$totalDataSize", 1048576] }, 2] }
          }
        }
      ];

      const stats = await db.collection("racingData").aggregate(pipeline).toArray();
      res.status(200).json(stats[0] || {
        totalRecords: 0,
        uniqueTracksCount: 0,
        uniqueUsersCount: 0,
        avgFileSizeKB: 0,
        totalDataSizeMB: 0
      });
    } catch (error) {
      console.error("Stats error:", error);
      res.status(500).json({ message: "Failed to fetch racing data statistics" });
    }
  });

  return router;
}