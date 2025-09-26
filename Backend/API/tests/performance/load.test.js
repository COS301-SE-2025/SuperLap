const request = require('supertest');
const { app, connectToDb, closeDbConnection } = require('../../app');

describe('Performance Tests', function () {
    let db;

    beforeAll(async function () {
        await connectToDb();
        db = app.locals.db;
    }, 60000);

    afterAll(async function () {
        await db.collection('users').deleteMany({ username: /^perf/ });
        await db.collection('tracks').deleteMany({ name: /^perf/ });
        await closeDbConnection();
    });

    describe('Concurrent Operations', function () {
        it('should handle 50 concurrent user creations', async function () {
            const promises = Array(50).fill(0).map((_, i) =>
                request(app).post('/users').send({
                    username: `perfuser${i}`,
                    email: `perf${i}@example.com`,
                    password: 'password123'
                })
            );

            const start = Date.now();
            const results = await Promise.all(promises);
            const duration = Date.now() - start;

            const successCount = results.filter(res => res.status === 201).length;
            expect(successCount).toBe(50);
            expect(duration).toBeLessThan(10000); // Should complete within 10 seconds
        });

        it('should handle 100 concurrent GET requests', async function () {
            await request(app).post('/users').send({ username: 'perfuser', email: 'perf@example.com', password: 'password123' });

            const promises = Array(100).fill(0).map(() => request(app).get('/users'));

            const start = Date.now();
            const results = await Promise.all(promises);
            const duration = Date.now() - start;

            const successCount = results.filter(res => res.status === 200).length;
            expect(successCount).toBe(100);
            expect(duration).toBeLessThan(5000); // Should complete within 5 seconds
        });
    });

    describe('Large Data Upload', function () {
        beforeEach(async function () {
            await request(app).post('/users').send({ username: 'perfuploaduser', email: 'perfupload@example.com', password: 'password123' });
            await request(app).post('/tracks').send({ name: 'perf-track', type: 'circuit', city: 'Test', country: 'Test', uploadedBy: 'perfuploaduser' });
        });

        afterEach(async function () {
            await db.collection('racingData').deleteMany({ userName: 'perfuploaduser' });
        });

        it('should handle large CSV upload efficiently', async function () {
            const largeCsv = 'lap,time,speed\n' + Array(5000).fill(0).map((_, i) => `${i+1},1:23.${String(i).padStart(3, '0')},180`).join('\n');
            const largeCsvBase64 = Buffer.from(largeCsv).toString('base64');

            const start = Date.now();
            const res = await request(app).post('/racing-data').send({
                trackName: 'perf-track',
                userName: 'perfuploaduser',
                lapTime: '1:23.456',
                fileName: 'large.csv',
                csvData: largeCsvBase64
            });
            const duration = Date.now() - start;

            expect(res.status).toBe(201);
            expect(duration).toBeLessThan(5000); // Should complete within 5 seconds
        });
    });
});