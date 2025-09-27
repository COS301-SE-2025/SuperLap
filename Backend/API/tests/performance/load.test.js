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
        await db.collection('racingData').deleteMany({ userName: /^perf/ });
        await closeDbConnection();
    });

    describe('Concurrent Operations', function () {
        it('should handle 50 concurrent user creations', async function () {
            const promises = Array(50).fill(0).map((_, i) =>
                request(app).post('/users').send({
                    username: `perfuser${i}_${Date.now()}`, // Add timestamp to ensure uniqueness
                    email: `perf${i}_${Date.now()}@example.com`,
                    password: 'password123'
                })
            );

            const start = Date.now();
            const results = await Promise.all(promises);
            const duration = Date.now() - start;

            const successCount = results.filter(res => res.status === 201).length;
            expect(successCount).toBe(50);
            expect(duration).toBeLessThan(10000); // Should complete within 10 seconds
            
            // Cleanup - get usernames from successful responses
            const usernames = results
                .filter(res => res.status === 201)
                .map((_, i) => `perfuser${i}_${Date.now()}`);
            
            if (usernames.length > 0) {
                await db.collection('users').deleteMany({ 
                    username: { $in: usernames } 
                });
            }
        });

        it('should handle 100 concurrent GET requests', async function () {
            // Create a test user first
            const testUser = { 
                username: `perfgetuser_${Date.now()}`, 
                email: `perfget_${Date.now()}@example.com`, 
                password: 'password123' 
            };
            await request(app).post('/users').send(testUser);

            const promises = Array(100).fill(0).map(() => request(app).get('/users'));

            const start = Date.now();
            const results = await Promise.all(promises);
            const duration = Date.now() - start;

            const successCount = results.filter(res => res.status === 200).length;
            expect(successCount).toBe(100);
            expect(duration).toBeLessThan(5000); // Should complete within 5 seconds
            
            // Cleanup
            await db.collection('users').deleteOne({ username: testUser.username });
        });
    });

    describe('Large Data Upload', function () {
        it('should handle large CSV upload efficiently', async function () {
            const timestamp = Date.now();
            const testUser = { 
                username: `perfuploaduser_${timestamp}`, 
                email: `perfupload_${timestamp}@example.com`, 
                password: 'password123' 
            };
            const testTrack = { 
                name: `perf-track-${timestamp}`, 
                type: 'circuit', 
                city: 'Test', 
                country: 'Test', 
                uploadedBy: testUser.username 
            };

            // Create user and track
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);

            const largeCsv = 'lap,time,speed\n' + Array(3000).fill(0).map((_, i) => `${i+1},1:23.${String(i).padStart(3, '0')},180`).join('\n');
            const largeCsvBase64 = Buffer.from(largeCsv).toString('base64');

            const start = Date.now();
            const res = await request(app).post('/racing-data').send({
                trackName: testTrack.name,
                userName: testUser.username,
                lapTime: '1:23.456',
                fileName: 'large.csv',
                csvData: largeCsvBase64
            });
            const duration = Date.now() - start;

            expect(res.status).toBe(201);
            expect(duration).toBeLessThan(10000); // Should complete within 10 seconds
            
            // Cleanup
            await db.collection('racingData').deleteMany({ userName: testUser.username });
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });
    });
});