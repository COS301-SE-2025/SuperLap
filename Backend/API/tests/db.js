// this is strictly for testing purposes
const { MongoClient } = require('mongodb');
require('dotenv').config();

let db;
let client;

async function connectToDb(databaseName = "Superlap") {
    client = await MongoClient.connect(uri);
    db = client.db(databaseName);
    console.log(`Connected to MongoDB: ${databaseName}`);
    return db;
}

async function closeDbConnection() {
    if (client) {
        await client.close();
        console.log("MongoDB connection closed");
    }
}

function getDb() {
    if (!db) throw new Error("Database not connected");
    return db;
}

module.exports = {
    connectToDb,
    closeDbConnection,
    getDb
};