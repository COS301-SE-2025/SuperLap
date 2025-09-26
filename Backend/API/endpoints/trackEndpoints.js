const express = require('express');

module.exports = function (db) {
  const router = express.Router();

  // TRACK ROUTES

  // Fetch all tracks
  router.get('/tracks', async (req, res) => {
    try {
      const tracks = await db.collection("tracks").find().toArray();
      res.json(tracks);
    } catch (error) {
      console.error(error);
      res.status(500).json({ message: "Failed to fetch tracks" });
    }
  });

  // Fetch a single track by name
  router.get('/tracks/name/:name', async (req, res) => {
    try {
      const trackName = req.params.name;
      const track = await db.collection("tracks").findOne({ name: trackName });
      res.json(track);
    } catch (error) {
      console.error(error);
      res.status(500).json({ message: "Failed to fetch track" });
    }
  });

  // Fetch all tracks by type
  router.get('/tracks/type/:type', async (req, res) => {
    try {
      const trackType = req.params.type;
      const tracks = await db.collection("tracks").find({ type: trackType }).toArray();
      res.json(tracks);
    } catch (error) {
      console.error(error);
      res.status(500).json({ message: "Failed to fetch tracks" });
    }
  });

  // Fetch all tracks by city
  router.get('/tracks/city/:city', async (req, res) => {
    try {
      const trackCity = req.params.city;
      const tracks = await db.collection("tracks").find({ city: trackCity }).toArray();
      res.status(201).json(tracks);
    } catch (error) {
      console.error(error);
      res.status(500).json({ message: "Failed to fetch tracks" });
    }
  });

  // Fetch all tracks by country
  router.get('/tracks/country/:country', async (req, res) => {
    try {
      const trackCountry = req.params.country;
      const tracks = await db.collection("tracks").find({ country: trackCountry }).toArray();
      res.status(201).json(tracks);
    } catch (error) {
      console.error(error);
      res.status(500).json({ message: "Failed to fetch tracks" });
    }
  });

  // Fetch all tracks by location
  router.get('/tracks/location/:location', async (req, res) => {
    try {
      const trackLocation = req.params.location;
      const tracks = await db.collection("tracks").find({ location: trackLocation }).toArray();
      res.status(201).json(tracks);
    } catch (error) {
      console.error(error);
      res.status(500).json({ message: "Failed to fetch tracks" });
    }
  });

  // Create a track
  router.post('/tracks', async (req, res) => {
    try {
      const {
        name,
        type,
        city,
        country,
        location,
        uploadedBy,
        image,
        description
      } = req.body;

      // Check if the track already exists
      const existingTrack = await db.collection("tracks").findOne({ name });
      if (existingTrack) {
        return res.status(400).json({ message: "Track already exists" });
      }

      const newTrack = {
        name,
        type,
        city,
        country,
        location,
        uploadedBy,
        image,
        description,
        dateUploaded: new Date().toISOString(), // Set date here
      };

      await db.collection("tracks").insertOne(newTrack);

      res.status(201).json({ message: "Track created successfully" });
    } catch (error) {
      console.error("Track creation error:", error);
      res.status(500).json({ message: "Error creating track" });
    }
  });


  // Update track
  router.put('/tracks/:name', async (req, res) => {
    const trackName = req.params.name;
    const updatedData = req.body;

    try {
      const result = await db.collection('tracks').updateOne({ name: trackName }, { $set: updatedData });

      if (result.modifiedCount === 0) {
        return res.status(404).json({ message: 'Track not found or data unchanged' });
      }

      res.json({ message: 'Track updated successfully' });
    } catch (error) {
      console.error('Update error:', error);
      res.status(500).json({ message: 'Failed to update track' });
    }
  });

  // Delete track
  router.delete('/tracks/:name', async (req, res) => {
    try {
      const trackName = req.params.name;
      await db.collection("tracks").deleteOne({ name: trackName });
      res.status(201).json({ message: "Track deleted successfully" });
    } catch (error) {
      console.error("Delete error:", error);
      res.status(500).json({ message: "Failed to delete track" });
    }
  });

  // Fetch track image
  router.get('/images/:name', async (req, res) => {
    try {
      const trackName = req.params.name;
      const track = await db.collection("tracks").findOne({ name: trackName });
      res.status(201).json(track.image);
    } catch (error) {
      console.error(error);
      res.status(500).json({ message: "Failed to fetch tracks" });
    }
  });

  return router;
}