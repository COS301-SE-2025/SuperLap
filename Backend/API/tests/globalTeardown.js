const { MongoClient } = require('mongodb');

module.exports = async () => {
  console.log('Global test teardown started...');
  
  try {
    // Connect to test database and clean up
    const client = await MongoClient.connect(process.env.MONGO_URI, {
      serverSelectionTimeoutMS: 5000,
      connectTimeoutMS: 5000,
    });
    
    const db = client.db();
    
    // Clean up test data (anything with 'test' prefix)
    const collections = ['users', 'tracks', 'racingData'];
    
    for (const collectionName of collections) {
      try {
        const collection = db.collection(collectionName);
        
        // Delete test data based on collection
        if (collectionName === 'users') {
          await collection.deleteMany({ username: /^test/ });
        } else if (collectionName === 'tracks') {
          await collection.deleteMany({ name: /^test/ });
        } else if (collectionName === 'racingData') {
          await collection.deleteMany({ userName: /^test/ });
        }
        
        console.log(`Cleaned up test data from ${collectionName}`);
      } catch (error) {
        console.warn(`Warning cleaning up ${collectionName}:`, error.message);
      }
    }
    
    await client.close();
    console.log('Test database cleanup completed');
  } catch (error) {
    console.warn('Warning during test cleanup:', error.message);
  }
  
  console.log('Global test teardown completed');
};