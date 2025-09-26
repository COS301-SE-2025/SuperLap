const request = require('supertest');
const { app, connectToDb, closeDbConnection } = require('../../app');

describe('Racing Data Routes Unit Tests', function () {
    let db;

    beforeAll(async function () {
        await connectToDb();
        db = app.locals.db;
    }, 30000);

    afterAll(async function () {
        await db.collection('racingData').deleteMany({ userName: /^test/ });
        await db.collection('tracks').deleteMany({ name: /^test/ });
        await db.collection('users').deleteMany({ username: /^test/ });
        await closeDbConnection();
    });

    const testUser = { username: 'testuser', email: 'test@example.com', password: 'password123' };
    const testTrack = { name: 'test-silverstone', type: 'circuit', city: 'Silverstone', country: 'UK', uploadedBy: 'testuser' };
    const testCsvData = 'lap,time,speed\n1,1:23.456,180.5';
    const testCsvBase64 = Buffer.from(testCsvData).toString('base64');

    beforeEach(async function () {
        await request(app).post('/users').send(testUser);
        await request(app).post('/tracks').send(testTrack);
    });

    afterEach(async function () {
        await db.collection('racingData').deleteMany({ userName: testUser.username });
        await db.collection('tracks').deleteMany({ name: testTrack.name });
        await db.collection('users').deleteMany({ username: testUser.username });
    });

    describe('POST /racing-data', function () {
        it('should create racing data', async function () {
            const racingData = {
                trackName: testTrack.name,
                userName: testUser.username,
                lapTime: '1:23.456',
                vehicleUsed: 'Test Car',
                fileName: 'test.csv',
                csvData: testCsvBase64
            };

            const res = await request(app).post('/racing-data').send(racingData);
            expect(res.status).toBe(201);
            expect(res.body.data).toHaveProperty('trackName', testTrack.name);
        });

        it('should reject missing CSV data', async function () {
            const racingData = {
                trackName: testTrack.name,
                userName: testUser.username,
                lapTime: '1:23.456'
            };

            const res = await request(app).post('/racing-data').send(racingData);
            expect(res.status).toBe(400);
        });
    });

    describe('GET /racing-data', function () {
        it('should return all racing data', async function () {
            const res = await request(app).get('/racing-data');
            expect(res.status).toBe(200);
            expect(Array.isArray(res.body)).toBe(true);
        });
    });

    describe('POST /racing-data/upload', function () {
        it('should upload CSV file', async function () {
            const res = await request(app)
                .post('/racing-data/upload')
                .field('trackName', testTrack.name)
                .field('userName', testUser.username)
                .field('vehicleUsed', 'Test Car')
                .attach('csvFile', Buffer.from(testCsvData), 'test.csv');

            expect(res.status).toBe(201);
        });

        it('should reject non-CSV file', async function () {
            const res = await request(app)
                .post('/racing-data/upload')
                .field('trackName', testTrack.name)
                .field('userName', testUser.username)
                .attach('csvFile', Buffer.from('not csv'), 'test.txt');

            expect(res.status).toBe(400);
        });
    });
});