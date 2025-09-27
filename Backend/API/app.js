const express = require('express');
require('dotenv').config();
const { MongoClient } = require('mongodb');
const { setupSwagger } = require('./swagger');

const app = express();
// Swagger setup
setupSwagger(app);
app.use(express.json({ limit: '10mb' }));

app.use(express.json());
const uri = process.env.MONGO_URI;
let db;
let client; // this will be used to close the connection later

async function connectToDb() {
  const maxRetries = 5;
  const retryDelay = 2000; // 2 seconds
  
  for (let i = 0; i < maxRetries; i++) {
    try {
      client = await MongoClient.connect(uri, {
        serverSelectionTimeoutMS: 10000, // 10 second timeout
        connectTimeoutMS: 10000,
      });
      db = client.db("Superlap");
      app.locals.db = db;

      const trackRouter = require('./endpoints/trackEndpoints')(db);
      const userRouter = require('./endpoints/userEndpoints')(db);
      const racingDataRouter = require('./endpoints/racingDataEndpoints')(db);

      app.use('', trackRouter);
      app.use('', userRouter);
      app.use('', racingDataRouter);

      console.log("Connected to MongoDB");
      return;
    } catch (error) {
      console.log(`MongoDB connection attempt ${i + 1}/${maxRetries} failed:`, error.message);
      if (i === maxRetries - 1) {
        throw error;
      }
      await new Promise(resolve => setTimeout(resolve, retryDelay));
    }
  }
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