const request = require('supertest');
const { app, connectToDb, closeDbConnection } = require('../../app');

describe('API Integration Tests', function () {
    let db;

    beforeAll(async function () {
        await connectToDb();
        db = app.locals.db;
    }, 30000);

    afterAll(async function () {
        await db.collection('racingData').deleteMany({ userName: /^integration/ });
        await db.collection('tracks').deleteMany({ name: /^integration/ });
        await db.collection('users').deleteMany({ username: /^integration/ });
        await closeDbConnection();
    });

    it('should support complete user workflow', async function () {
        const user = { username: 'integrationuser1', email: 'int1@example.com', password: 'password123' };
        const track = { name: 'integration-track-1', type: 'circuit', city: 'Test City', country: 'Test', uploadedBy: user.username };
        const csvData = Buffer.from('lap,time\n1,1:23.456').toString('base64');

        // Create user
        let res = await request(app).post('/users').send(user);
        expect(res.status).toBe(201);

        // Login user
        res = await request(app).post('/users/login').send({ username: user.username, password: user.password });
        expect(res.status).toBe(200);

        // Create track
        res = await request(app).post('/tracks').send(track);
        expect(res.status).toBe(201);

        // Upload racing data
        res = await request(app).post('/racing-data').send({
            trackName: track.name,
            userName: user.username,
            lapTime: '1:23.456',
            vehicleUsed: 'Test Car',
            fileName: 'test.csv',
            csvData: csvData
        });
        expect(res.status).toBe(201);
        const racingDataId = res.body.data._id;

        // Download CSV
        res = await request(app).get(`/racing-data/${racingDataId}/download`);
        expect(res.status).toBe(200);

        // Get statistics
        res = await request(app).get('/racing-data/stats/summary');
        expect(res.status).toBe(200);
        expect(res.body).toHaveProperty('totalRecords');
        
        // Cleanup
        await db.collection('racingData').deleteMany({ userName: user.username });
        await db.collection('tracks').deleteOne({ name: track.name });
        await db.collection('users').deleteOne({ username: user.username });
    });

    it('should handle cross-entity queries', async function () {
        const user = { username: 'integrationuser2', email: 'int2@example.com', password: 'password123' };
        const track = { name: 'integration-track-2', type: 'circuit', city: 'Test City', country: 'Test', uploadedBy: user.username };
        
        // Create user and track
        await request(app).post('/users').send(user);
        await request(app).post('/tracks').send(track);
        
        // Create racing data
        await request(app).post('/racing-data').send({
            trackName: track.name,
            userName: user.username,
            lapTime: '1:23.456',
            fileName: 'test.csv',
            csvData: Buffer.from('lap,time\n1,1:23.456').toString('base64')
        });

        // Query by user
        let res = await request(app).get(`/racing-data/user/${user.username}`);
        expect(res.status).toBe(200);
        expect(res.body.length).toBeGreaterThanOrEqual(1);

        // Query by track
        res = await request(app).get(`/racing-data/track/${track.name}`);
        expect(res.status).toBe(200);
        expect(res.body.length).toBeGreaterThanOrEqual(1);
        
        // Cleanup
        await db.collection('racingData').deleteMany({ userName: user.username });
        await db.collection('tracks').deleteOne({ name: track.name });
        await db.collection('users').deleteOne({ username: user.username });
    });
});