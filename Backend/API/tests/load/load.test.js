const request = require('supertest');
const { app, connectToDb, closeDbConnection } = require('../../app');
const { expect } = require('chai');

describe('Load Tests', function () {
    let db;
    
    beforeAll(async function () {
        await connectToDb();
        db = app.locals.db;
    }, 60000);

    afterAll(async function () {
        await db.collection('users').deleteMany({ username: /^loadtest/ });
        await db.collection('tracks').deleteMany({ name: /^loadtest/ });
        await db.collection('racingData').deleteMany({ userName: /^loadtest/ });
        await closeDbConnection();
    });

    const testCsvData = 'lap,time,speed\n1,1:23.456,180.5\n2,1:24.123,179.8';
    const testCsvBase64 = Buffer.from(testCsvData).toString('base64');

    describe('Concurrent User Creation', function () {
        it('should handle 100 concurrent user registrations', async function () {
        const userPromises = Array(100).fill(0).map((_, i) => 
            request(app)
            .post('/users')
            .send({
                username: `loadtest_user_${i}`,
                email: `loadtest${i}@example.com`,
                password: 'loadtestpass123'
            })
        );

        const startTime = Date.now();
        const results = await Promise.all(userPromises);
        const endTime = Date.now();
        const duration = endTime - startTime;

        console.log(`100 user creations took ${duration}ms`);
        
        const successCount = results.filter(res => res.status === 201).length;
        expect(successCount).toBe(100);
        expect(duration).toBeLessThan(10000); // Should complete within 10 seconds
        });
    });

    describe('Concurrent Data Retrieval', function () {
        beforeAll(async function () {
        // Create test data first
        await request(app).post('/users').send({
            username: 'loadtest_user_main',
            email: 'loadtest@example.com',
            password: 'password123'
        });

        const trackPromises = Array(20).fill(0).map((_, i) =>
            request(app).post('/tracks').send({
            name: `loadtest-track-${i}`,
            type: 'circuit',
            city: `City${i}`,
            country: 'Test Country',
            uploadedBy: 'loadtest_user_main'
            })
        );
        await Promise.all(trackPromises);
        });

        it('should handle 200 concurrent GET requests for all tracks', async function () {
        const getPromises = Array(200).fill(0).map(() => 
            request(app).get('/tracks')
        );

        const startTime = Date.now();
        const results = await Promise.all(getPromises);
        const endTime = Date.now();
        const duration = endTime - startTime;

        console.log(`200 concurrent GET requests took ${duration}ms`);

        const successCount = results.filter(res => res.status === 200).length;
        expect(successCount).toBe(200);
        expect(duration).toBeLessThan(15000); // Should complete within 15 seconds

        // Verify data consistency
        results.forEach(res => {
            expect(res.body).toBeInstanceOf(Array);
            expect(res.body.length).toBeGreaterThanOrEqual(20);
        });
        });

        it('should handle mixed read/write operations', async function () {
        const operations = [];

        // 50% reads, 30% writes, 20% updates
        for (let i = 0; i < 100; i++) {
            const rand = Math.random();
            if (rand < 0.5) {
            // Read operation
            operations.push(request(app).get('/tracks'));
            } else if (rand < 0.8) {
            // Write operation
            operations.push(
                request(app).post('/tracks').send({
                name: `loadtest-mixed-${i}-${Date.now()}`,
                type: 'circuit',
                city: 'LoadTest City',
                country: 'Test Country',
                uploadedBy: 'loadtest_user_main'
                })
            );
            } else {
            // Update operation
            operations.push(
                request(app)
                .put('/tracks/loadtest-track-0')
                .send({ description: `Updated at ${Date.now()}` })
            );
            }
        }

        const startTime = Date.now();
        const results = await Promise.all(operations);
        const endTime = Date.now();
        const duration = endTime - startTime;

        console.log(`100 mixed operations took ${duration}ms`);

        const successCount = results.filter(res => 
            res.status === 200 || res.status === 201
        ).length;
        
        expect(successCount).toBeGreaterThan(80); // Allow some failures in mixed load
        expect(duration).toBeLessThan(20000); // Should complete within 20 seconds
        });
    });

    describe('Large Data Upload Load Test', function () {
        beforeAll(async function () {
        await request(app).post('/users').send({
            username: 'loadtest_upload_user',
            email: 'uploadtest@example.com',
            password: 'password123'
        });

        await request(app).post('/tracks').send({
            name: 'loadtest-upload-track',
            type: 'circuit',
            city: 'Upload City',
            country: 'Test Country',
            uploadedBy: 'loadtest_upload_user'
        });
        });

        it('should handle 50 concurrent large CSV uploads', async function () {
        // Create a larger CSV dataset for stress testing
        const largeCsvRows = Array(1000).fill(0).map((_, i) => 
            `${i+1},1:${23 + (i % 60)}.${String(i % 1000).padStart(3, '0')},${180 + (i % 50)}.${i % 10}`
        );
        const largeCsvData = 'lap,time,speed\n' + largeCsvRows.join('\n');
        const largeCsvBase64 = Buffer.from(largeCsvData).toString('base64');

        const uploadPromises = Array(50).fill(0).map((_, i) =>
            request(app)
            .post('/racing-data')
            .send({
                trackName: 'loadtest-upload-track',
                userName: 'loadtest_upload_user',
                lapTime: `1:23.${String(i).padStart(3, '0')}`,
                vehicleUsed: `Load Test Car ${i}`,
                fileName: `loadtest-data-${i}.csv`,
                csvData: largeCsvBase64,
                description: `Load test upload ${i}`
            })
        );

        const startTime = Date.now();
        const results = await Promise.all(uploadPromises);
        const endTime = Date.now();
        const duration = endTime - startTime;

        console.log(`50 large CSV uploads took ${duration}ms`);
        console.log(`Average time per upload: ${duration / 50}ms`);

        const successCount = results.filter(res => res.status === 201).length;
        expect(successCount).toBe(50);
        expect(duration).toBeLessThan(30000); // Should complete within 30 seconds
        });
    });

    describe('Database Query Performance', function () {
        beforeAll(async function () {
        // Create substantial test dataset
        await request(app).post('/users').send({
            username: 'loadtest_query_user',
            email: 'querytest@example.com',
            password: 'password123'
        });

        // Create tracks
        const trackPromises = Array(10).fill(0).map((_, i) =>
            request(app).post('/tracks').send({
            name: `loadtest-query-track-${i}`,
            type: i % 2 === 0 ? 'circuit' : 'street',
            city: `Query City ${i}`,
            country: 'Test Country',
            uploadedBy: 'loadtest_query_user'
            })
        );
        await Promise.all(trackPromises);

        // Create racing data
        const racingDataPromises = [];
        for (let track = 0; track < 10; track++) {
            for (let session = 0; session < 20; session++) {
            racingDataPromises.push(
                request(app).post('/racing-data').send({
                trackName: `loadtest-query-track-${track}`,
                userName: 'loadtest_query_user',
                lapTime: `1:${20 + (session % 40)}.${String(session * 17).padStart(3, '0')}`,
                vehicleUsed: `Query Car ${session % 5}`,
                fileName: `query-data-${track}-${session}.csv`,
                csvData: testCsvBase64,
                description: `Query test data ${track}-${session}`
                })
            );
            }
        }
        await Promise.all(racingDataPromises);
        }, 120000); // Increased timeout for data creation

        it('should perform complex queries efficiently', async function () {
        const queries = [
            // Get all racing data
            () => request(app).get('/racing-data'),
            
            // Get data by user
            () => request(app).get('/racing-data/user/loadtest_query_user'),
            
            // Get data by track
            () => request(app).get('/racing-data/track/loadtest-query-track-0'),
            
            // Get statistics
            () => request(app).get('/racing-data/stats/summary'),
            
            // Get all tracks
            () => request(app).get('/tracks'),
            
            // Get specific track
            () => request(app).get('/tracks/loadtest-query-track-1')
        ];

        const iterations = 20;
        const results = [];

        for (const query of queries) {
            const queryPromises = Array(iterations).fill(0).map(() => query());
            
            const startTime = Date.now();
            const queryResults = await Promise.all(queryPromises);
            const endTime = Date.now();
            const duration = endTime - startTime;

            const successCount = queryResults.filter(res => res.status === 200).length;
            
            results.push({
            query: query.toString(),
            duration,
            averageTime: duration / iterations,
            successRate: (successCount / iterations) * 100
            });
        }

        console.log('Query Performance Results:');
        results.forEach(result => {
            console.log(`Average time: ${result.averageTime.toFixed(2)}ms, Success rate: ${result.successRate}%`);
        });

        // All queries should complete successfully
        results.forEach(result => {
            expect(result.successRate).toBe(100);
            expect(result.averageTime).toBeLessThan(1000); // Average query should be under 1 second
        });
        });
    });

    describe('Memory and Resource Usage', function () {
        it('should not leak memory during sustained operations', async function () {
        const initialMemory = process.memoryUsage();
        
        // Perform sustained operations
        for (let batch = 0; batch < 10; batch++) {
            const promises = Array(50).fill(0).map((_, i) => 
            request(app).get('/tracks')
            );
            await Promise.all(promises);
            
            // Force garbage collection if available
            if (global.gc) {
            global.gc();
            }
        }

        const finalMemory = process.memoryUsage();
        const memoryIncrease = finalMemory.heapUsed - initialMemory.heapUsed;
        const memoryIncreaseKB = memoryIncrease / 1024;

        console.log(`Memory increase: ${memoryIncreaseKB.toFixed(2)} KB`);
        
        // Memory increase should be reasonable (less than 10MB)
        expect(memoryIncrease).toBeLessThan(10 * 1024 * 1024);
        });
    });

    describe('Error Rate Under Load', function () {
        it('should maintain low error rates under high load', async function () {
        const totalRequests = 500;
        const concurrentBatches = 50;
        const batchSize = totalRequests / concurrentBatches;

        let totalErrors = 0;
        const startTime = Date.now();

        for (let batch = 0; batch < concurrentBatches; batch++) {
            const batchPromises = Array(batchSize).fill(0).map((_, i) => {
            const requestType = i % 4;
            switch (requestType) {
                case 0:
                return request(app).get('/users');
                case 1:
                return request(app).get('/tracks');
                case 2:
                return request(app).get('/racing-data');
                case 3:
                return request(app).get('/racing-data/stats/summary');
                default:
                return request(app).get('/');
            }
            });

            const batchResults = await Promise.all(
            batchPromises.map(p => p.catch(err => ({ status: 500, error: err })))
            );

            const batchErrors = batchResults.filter(res => 
            res.status >= 400 || res.error
            ).length;
            
            totalErrors += batchErrors;
        }

        const endTime = Date.now();
        const duration = endTime - startTime;
        const errorRate = (totalErrors / totalRequests) * 100;

        console.log(`Load test completed: ${totalRequests} requests in ${duration}ms`);
        console.log(`Error rate: ${errorRate.toFixed(2)}%`);
        console.log(`Requests per second: ${((totalRequests / duration) * 1000).toFixed(2)}`);

        // Error rate should be less than 5%
        expect(errorRate).toBeLessThan(5);
        
        // Should handle at least 10 requests per second
        expect((totalRequests / duration) * 1000).toBeGreaterThan(10);
        });
    });
});