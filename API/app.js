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

module.exports = { app, connectToDb };