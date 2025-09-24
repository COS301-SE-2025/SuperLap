const { MongoClient } = require('mongodb');

module.exports = async () => {
  // Set test environment variables
  process.env.NODE_ENV = 'test';
  process.env.MONGO_URI = process.env.MONGO_URI || 'mongodb://localhost:27017/Superlap_test';
  
  console.log('Global test setup started...');
  
  // Wait for MongoDB to be available
  const maxRetries = 30;
  const retryDelay = 2000;
  
  for (let i = 0; i < maxRetries; i++) {
    try {
      const client = await MongoClient.connect(process.env.MONGO_URI, {
        serverSelectionTimeoutMS: 5000,
        connectTimeoutMS: 5000,
      });
      
      // Test connection and create test database
      const db = client.db();
      await db.admin().ping();
      
      // Ensure collections exist
      const collections = ['users', 'tracks', 'racingData'];
      for (const collectionName of collections) {
        try {
          await db.createCollection(collectionName);
        } catch (error) {
          // Collection might already exist, which is fine
          if (!error.message.includes('already exists')) {
            console.warn(`Warning creating collection ${collectionName}:`, error.message);
          }
        }
      }
      
      await client.close();
      console.log('MongoDB is ready for testing');
      break;
    } catch (error) {
      if (i === maxRetries - 1) {
        console.error(`MongoDB not ready after ${maxRetries} attempts:`, error.message);
        throw error;
      }
      console.log(`Waiting for MongoDB... attempt ${i + 1}/${maxRetries}`);
      await new Promise(resolve => setTimeout(resolve, retryDelay));
    }
  }
  
  console.log('Global test setup completed');
};