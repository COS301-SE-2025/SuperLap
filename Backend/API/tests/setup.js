const { MongoClient } = require('mongodb');

jest.setTimeout(30000);

beforeAll(async () => {
    const maxRetries = 5;
    const retryDelay = 2000;
    
    for (let i = 0; i < maxRetries; i++) {
        try {
            const client = await MongoClient.connect(process.env.MONGO_URI || 'mongodb://localhost:27017/Superlap_test', {
                serverSelectionTimeoutMS: 5000,
            });
            await client.db().admin().ping();
            await client.close();
            console.log('MongoDB ready for testing');
            return;
        } catch (error) {
            if (i === maxRetries - 1) {
                console.error('MongoDB connection failed after retries:', error);
                throw error;
            }
            console.log(`Waiting for MongoDB... attempt ${i + 1}/${maxRetries}`);
            await new Promise(resolve => setTimeout(resolve, retryDelay));
        }
    }
});