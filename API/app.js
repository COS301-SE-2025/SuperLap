const express = require('express');
const { MongoClient } = require('mongodb');
require('dotenv').config();

const app = express();
app.use(express.json());
const uri = process.env.MONGO_URI;
let db;

async function connectToDb() {
  const client = await MongoClient.connect(uri);
  db = client.db("Superlap");
  app.locals.db = db;
  console.log("Connected to MongoDB");
}

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

// TRACK ROUTES



// RACING LINE ROUTES



// TRAINING SESSION ROUTES





module.exports = { app, connectToDb };