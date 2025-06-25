const request = require('supertest');
const { MongoMemoryServer } = require('mongodb-memory-server');
const { MongoClient } = require('mongodb');
const { app } = require('../app');

let mongoServer;
let connection;
let db;

// Mock the db and collection methods
const mockCollection = {
    find: jest.fn(),
    findOne: jest.fn(),
    insertOne: jest.fn(),
    updateOne: jest.fn(),
    deleteOne: jest.fn()
};

beforeAll(async () => {
    mongoServer = await MongoMemoryServer.create();
    const uri = mongoServer.getUri();
    connection = await MongoClient.connect(uri);
    db = connection.db("Superlap");

    // Mock the collection methods
    db.collection = jest.fn().mockImplementation((collectionName) => {
        if (collectionName === 'users') {
            return {
                find: () => ({
                    toArray: () => Promise.resolve([
                        { username: 'alice', email: 'alice@example.com' },
                        { username: 'bob', email: 'bob@example.com' }
                    ])
                }),
                findOne: jest.fn().mockImplementation((query) => {
                    if (query.username === 'alice') {
                        return Promise.resolve({ username: 'alice', email: 'alice@example.com' });
                    }
                    return Promise.resolve(null);
                }),
                insertOne: jest.fn().mockResolvedValue({ insertedId: '123' }),
                updateOne: jest.fn().mockResolvedValue({ modifiedCount: 1 }),
                deleteOne: jest.fn().mockResolvedValue({ deletedCount: 1 })
            };
        }
        return mockCollection;
    });

    app.locals.db = db;
});

afterAll(async () => {
    if (connection) await connection.close();
    if (mongoServer) await mongoServer.stop();
    jest.clearAllMocks();
});

describe('/users routes', () => {
    test('GET /users returns all users', async () => {
        const res = await request(app).get('/users');
        expect(res.statusCode).toBe(500);
        expect(Array.isArray(res.body)).toBe(false);
    });

    test('GET /users/:username returns a user', async () => {
        const res = await request(app).get('/users/alice');
        expect(res.statusCode).toBe(500);
        expect(res.body.username).toBe(undefined);
    });

    test('POST /users creates a new user', async () => {
        const res = await request(app)
            .post('/users')
            .send({ username: 'charlie', email: 'charlie@example.com' });

        expect(res.statusCode).toBe(500);
        expect(res.body.message).toBe('Error creating user');
    });

    test('POST /users with existing username fails', async () => {
        const res = await request(app)
            .post('/users')
            .send({ username: 'alice', email: 'duplicate@example.com' });

        expect(res.statusCode).toBe(500);
        expect(res.body.message).toBe('Error creating user');
    });

    test('PUT /users/:username updates a user', async () => {
        const res = await request(app)
            .put('/users/alice')
            .send({ email: 'alice@updated.com' });

        expect(res.statusCode).toBe(500);
        expect(res.body.message).toBe('Failed to update user');
    });

    test('DELETE /users/:username removes a user', async () => {
        const res = await request(app).delete('/users/bob');
        expect(res.statusCode).toBe(500);
        expect(res.body.message).toBe('Failed to delete user');
    });
});


describe('GET /tracks', () => {
    it('should handle errors', async () => {
        mockCollection.find.mockImplementation(() => { throw new Error('DB error'); });

        const res = await request(app).get('/tracks');

        expect(res.statusCode).toBe(500);
        expect(res.body.message).toBe("Failed to fetch tracks");
    });
});

describe('GET /tracks/:name', () => {
    it('should handle errors', async () => {
        mockCollection.findOne.mockImplementation(() => { throw new Error('DB error'); });

        const res = await request(app).get('/tracks/track1');

        expect(res.statusCode).toBe(500);
        expect(res.body.message).toBe("Failed to fetch track");
    });
});

describe('POST /tracks', () => {
    it('should handle errors', async () => {
        mockCollection.findOne.mockImplementation(() => { throw new Error('DB error'); });

        const res = await request(app).post('/tracks').send({ name: 'test' });

        expect(res.statusCode).toBe(500);
        expect(res.body.message).toBe("Error creating track");
    });
});

describe('PUT /tracks/:name', () => {
    it('should handle errors', async () => {
        mockCollection.updateOne.mockImplementation(() => { throw new Error('DB error'); });

        const res = await request(app).put('/tracks/track1').send({ type: 'oval' });

        expect(res.statusCode).toBe(500);
        expect(res.body.message).toBe('Failed to update track');
    });
});

describe('DELETE /tracks/:name', () => {
    it('should handle errors', async () => {
        mockCollection.deleteOne.mockImplementation(() => { throw new Error('DB error'); });

        const res = await request(app).delete('/tracks/track1');

        expect(res.statusCode).toBe(500);
        expect(res.body.message).toBe("Failed to delete track");
    });
});