const request = require('supertest');
const { app, connectToDb, closeDbConnection } = require('../../app');

describe('Track Routes Unit Tests', function () {
    let db;

    beforeAll(async function () {
        await connectToDb();
        db = app.locals.db;
    }, 30000);

    afterAll(async function () {
        await db.collection('tracks').deleteMany({ name: /^test/ });
        await db.collection('users').deleteMany({ username: /^test/ });
        await closeDbConnection();
    });

    const testUser = { username: 'testuser', email: 'test@example.com', password: 'password123' };
    const testTrack = {
        name: 'test-silverstone',
        type: 'circuit',
        city: 'Silverstone',
        country: 'UK',
        uploadedBy: 'testuser',
        description: 'Test track'
    };

    beforeEach(async function () {
        await request(app).post('/users').send(testUser);
    });

    afterEach(async function () {
        await db.collection('tracks').deleteMany({ name: /^test/ });
        await db.collection('users').deleteMany({ username: testUser.username });
    });

    describe('POST /tracks', function () {
        it('should create a track', async function () {
            const res = await request(app).post('/tracks').send(testTrack);
            expect(res.status).toBe(201);
        });

        it('should reject duplicate track', async function () {
            await request(app).post('/tracks').send(testTrack);
            const res = await request(app).post('/tracks').send(testTrack);
            expect(res.status).toBe(400);
        });
    });

    describe('GET /tracks', function () {
        it('should return all tracks', async function () {
            const res = await request(app).get('/tracks');
            expect(res.status).toBe(200);
            expect(Array.isArray(res.body)).toBe(true);
        });
    });

    describe('GET /tracks/:name', function () {
        beforeEach(async function () {
            await request(app).post('/tracks').send(testTrack);
        });

        it('should return specific track', async function () {
            const res = await request(app).get(`/tracks/${testTrack.name}`);
            expect(res.status).toBe(200);
            expect(res.body).toHaveProperty('name', testTrack.name);
        });
    });

    describe('DELETE /tracks/:name', function () {
        beforeEach(async function () {
            await request(app).post('/tracks').send(testTrack);
        });

        it('should delete track', async function () {
            const res = await request(app).delete(`/tracks/${testTrack.name}`);
            expect(res.status).toBe(201);
        });
    });
});