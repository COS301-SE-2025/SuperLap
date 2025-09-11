// Test setup file
const { MongoClient } = require('mongodb');

// Increase timeout for all async operations
jest.setTimeout(60000); // Increased to 60 seconds

// Add a global beforeAll to wait for MongoDB to be ready
beforeAll(async () => {
  // Wait for MongoDB to be ready with retry logic
  const maxRetries = 20; // Increased retries
  const retryDelay = 3000; // 3 seconds
  
  for (let i = 0; i < maxRetries; i++) {
    try {
      const client = await MongoClient.connect(process.env.MONGO_URI || 'mongodb://localhost:27017/Superlap_test', {
        serverSelectionTimeoutMS: 5000,
        connectTimeoutMS: 5000,
      });
      await client.db().admin().ping(); // Test the connection
      await client.close();
      console.log('MongoDB is ready for testing');
      break;
    } catch (error) {
      if (i === maxRetries - 1) {
        throw new Error(`MongoDB not ready after ${maxRetries} attempts: ${error.message}`);
      }
      console.log(`Waiting for MongoDB... attempt ${i + 1}/${maxRetries}`);
      await new Promise(resolve => setTimeout(resolve, retryDelay));
    }
  }
}, 120000); // 2 minute timeout for the setup
