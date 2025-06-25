const express = require('express');
require('dotenv').config();
const { MongoClient } = require('mongodb');
const { setupSwagger } = require('./swagger');

const app = express();
// Swagger setup
setupSwagger(app);

app.use(express.json());
const uri = process.env.MONGO_URI;
let db;
let client; // this will be used to close the connection later

async function connectToDb() {
  client = await MongoClient.connect(uri);
  db = client.db("Superlap");
  app.locals.db = db;

  const trackRouter = require('./endpoints/trackEndpoints')(db);
  const userRouter = require('./endpoints/userEndpoints')(db);

  app.use('', trackRouter);
  app.use('', userRouter);

  console.log("Connected to MongoDB");
}


async function closeDbConnection() {
  if (client) {
    await client.close();
    console.log("MongoDB connection closed");
  }
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

module.exports = { app, connectToDb, closeDbConnection};