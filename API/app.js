const express = require('express');
const { MongoClient } = require('mongodb');
require('dotenv').config();
const { setupSwagger } = require('./swagger');

const app = express();
// Swagger setup
setupSwagger(app);

app.use(express.json());
const uri = process.env.MONGO_URI;
let db;
let client; // this will be used to close the connection later

async function connectToDb() {
  const client = await MongoClient.connect(uri);
  db = client.db("Superlap");
  app.locals.db = db;
  console.log("Connected to MongoDB");
}

async function closeDbConnection() {
  if (client) {
    await client.close();
    console.log("MongoDB connection closed");
  }
}

/**
 * @swagger
 * /:
 *   get:
 *     summary: Default route
 *     description: Returns a welcome message from Express.
 *     responses:
 *       200:
 *         description: A successful response
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 message:
 *                   type: string
 *                   example: Hello from Express!
 */

// Default route
app.get('/', async (req, res) => {
  try {
    res.json({ message: "Hello from Express!" });
  } catch (error) {
    console.log("GET error:", error);
    res.status(500).json({message:"Failed to fetch default route"});
  }
});

// USER ROUTES

/**
 * @swagger
 * /users:
 *   get:
 *     summary: Fetch all users
 *     description: Returns a list of all users.
 *     responses:
 *       200:
 *         description: A successful response
 *         content:
 *           application/json:
 *             schema:
 *               type: array
 *               items:
 *                 type: object
 *                 properties:
 *                   username:
 *                     type: string
 *                   email:
 *                     type: string
 */

// Fetch all users
app.get('/users', async (req, res) => {
  try {
    const users = await db.collection("users").find().toArray();
    res.json(users);
  } catch (error) {
    console.error("GET error:", error);
    res.status(500).json({message: "Failed to fetch users"});
  }
});

// Fetch a single user by username
app.get('/users/:username', async (req, res) => {
  try {
    const username = req.params.username;
    const user = await db.collection("users").findOne({username: username});
    res.json(user);
  } catch (error) {
    console.error("GET error:", error);
    res.status(500).json({message: "Failed to fetch user"});
  }
});

// Update a single user
app.put('/users/:username', async (req, res) => {
  const username = req.params.username;
  const updatedData = req.body;

  try {
    const result = await db.collection('users').updateOne({ username: username }, { $set: updatedData });

    if (result.modifiedCount === 0) {
      return res.status(404).json({ message: 'User not found or data unchanged' });
    }

    res.json({ message: 'User updated successfully' });
  } catch (error) {
    console.error('Update error:', error);
    res.status(500).json({ message: 'Failed to update user' });
  }
});

// Create a single user
app.post('/users', async (req, res) => {
  try {
    const newUser = req.body;
    // Check if the user already exists
    const existingUser = await db.collection("users").findOne({ username: newUser.username });
    if (existingUser) {
      return res.status(400).json({ message: "Username already taken" });
    }
    // Insert the new user into the database
    await (await db).collection("users").insertOne(newUser);
    res.status(201).json({ message: "User created successfully" });
  } catch (error) {
    console.error("Create error:", error);
    res.status(500).json({ message: "Error creating user" });
  }
});

// Delete a single user
app.delete('/users/:username', async (req, res) => {
  try {
    const username = req.params.username;
    const result = await db.collection("users").deleteOne({username: username});
    res.status(201).json({message: "User deleted successfully"});
  } catch (error) {
    console.error(error);
    res.status(500).json({message: "Failed to delete user"});
  }
});


// TRACK ROUTES

// Fetch all tracks
app.get('/tracks', async (req, res) => {
  try {
    const tracks = await db.collection("tracks").find().toArray();
    res.json(tracks);
  } catch (error) {
      console.error(error);
      res.status(500).json({message: "Failed to fetch tracks"});
  }
});

// Fetch a single track by name
app.get('/tracks/:name', async (req, res) => {
  try {
    const trackName = req.params.name;
    const track = await db.collection("tracks").findOne({name: trackName});
    res.json(track);
  } catch (error) {
    console.error(error);
    res.status(500).json({message: "Failed to fetch track"});
  }
});

// Fetch all tracks by type
app.get('/tracks/:type', async (req, res) => {
  try {
    const trackType = req.params.type;
    const tracks = await db.collection("tracks").find({type: trackType}).toArray();
    res.json(tracks);
  } catch (error) {
    console.error(error);
    res.status(500).json({message: "Failed to fetch tracks"});
  }
});

// Fetch all tracks by city
app.get('/tracks/:city', async (req, res) => {
  try {
    const trackCity = req.params.city;
    const tracks = await db.collection("tracks").find({city: trackCity}).toArray();
    res.status(201).json(tracks);
  } catch (error) {
    console.error(error);
    res.status(500).json({message: "Failed to fetch tracks"});
  }
});

// Fetch all tracks by country
app.get('/tracks/:country', async (req, res) => {
  try {
    const trackCountry = req.params.country;
    const tracks = await db.collection("tracks").find({country: trackCountry}).toArray();
    res.status(201).json(tracks);
  } catch (error) {
    console.error(error);
    res.status(500).json({message: "Failed to fetch tracks"});
  }
});

// Fetch all tracks by location
app.get('/tracks/:location', async (req, res) => {
  try {
    const trackLocation = req.params.location;
    const tracks = await db.collection("tracks").find({location: trackLocation}).toArray();
    res.status(201).json(tracks);
  } catch (error) {
    console.error(error);
    res.status(500).json({message: "Failed to fetch tracks"});
  }
});

// Create a track
app.post('/tracks', async (req, res) => {
  try {
    const newTrack = req.body;
    // Check if the track already exists
    const existingTrack = await db.collection("tracks").findOne({ name: newTrack.name });
    if (existingTrack) {
      return res.status(400).json({ message: "Track already exists" });
    }
    // Insert the new track into the database
    await db.collection("tracks").insertOne(newTrack);
    res.status(201).json({ message: "Track created successfully" });
  } catch (error) {
    res.status(500).json({ message: "Error creating track" });
  }
});

// Update track
app.put('/tracks/:name', async (req, res) => {
  const trackName = req.params.username;
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
app.delete('/tracks/:name', async (req, res) => {
  try {
    const trackName = req.params.name;
    await db.collection("tracks").deleteOne({name: trackName});
    res.status(201).json({ message: "Track deleted successfully" });
  } catch (error) {
    console.error("Delete error:", error);
    res.status(500).json({ message: "Failed to delete track" });
  }
});

// Fetch track image
app.get('/images/:name', async (req, res) => {
  try {
    const trackName = req.params.name;
    const track = await db.collection("tracks").findOne({name: trackLocation});
    res.status(201).json(track.image);
  } catch (error) {
    console.error(error);
    res.status(500).json({message: "Failed to fetch tracks"});
  }
});
// RACING LINE ROUTES


// TRAINING SESSION ROUTES


module.exports = { app, connectToDb, closeDbConnection };