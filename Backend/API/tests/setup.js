// Test setup file
const { MongoClient } = require('mongodb');

// Increase timeout for all async operations
jest.setTimeout(30000);

// Add a global beforeAll to wait for MongoDB to be ready
beforeAll(async () => {
  // Wait for MongoDB to be ready with retry logic
  const maxRetries = 10;
  const retryDelay = 2000; // 2 seconds
  
  for (let i = 0; i < maxRetries; i++) {
    try {
      const client = await MongoClient.connect(process.env.MONGO_URI || 'mongodb://localhost:27017/Superlap_test');
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
});
