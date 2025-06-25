// This also for testing purposes
const { connectToDb, closeDbConnection, getDb } = require('./db');
const { MongoMemoryServer } = require('mongodb-memory-server');

let mongoServer;

beforeAll(async () => {
    // Use in-memory MongoDB for tests
    mongoServer = await MongoMemoryServer.create();
    const uri = mongoServer.getUri();
    process.env.MONGO_URI = uri; // Override the URI for tests

    await connectToDb("testdb");
});

afterAll(async () => {
    await closeDbConnection();
    await mongoServer.stop();
});

// Helper function for tests
function getTestDb() {
    return getDb();
}

module.exports = { getTestDb };