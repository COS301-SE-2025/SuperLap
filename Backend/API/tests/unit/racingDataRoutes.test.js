const request = require('supertest');
const { app, connectToDb, closeDbConnection } = require('../../app');
const { ObjectId } = require('mongodb');

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

    const baseUser = { email: 'test@example.com', password: 'password123' };
    const baseTrack = { type: 'circuit', city: 'Silverstone', country: 'UK' };
    const testCsvData = 'lap,time,speed\n1,1:23.456,180.5\n2,1:24.123,179.8';
    const testCsvBase64 = Buffer.from(testCsvData).toString('base64');

    describe('POST /racing-data', function () {
        it('should create racing data', async function () {
            const testUser = { ...baseUser, username: 'testuser1' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-1', uploadedBy: testUser.username };
            
            // Create user and track
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);

            const racingData = {
                trackName: testTrack.name,
                userName: testUser.username,
                fastestLapTime: '1:23.456',
                topSpeed: '308',
                averageSpeed: '187',
                vehicleUsed: 'Test Car',
                description: 'Test lap data',
                fileName: 'test.csv',
                csvData: testCsvBase64
            };

            const res = await request(app).post('/racing-data').send(racingData);
            expect(res.status).toBe(201);
            expect(res.body).toHaveProperty('message', 'Racing data created successfully');
            expect(res.body.data).toHaveProperty('trackName', testTrack.name);
            expect(res.body.data).toHaveProperty('userName', testUser.username);
            expect(res.body.data).toHaveProperty('dateUploaded');
            expect(res.body.data.csvData).toBeUndefined(); // Should not include csvData in response
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('racingData').deleteMany({ userName: testUser.username });
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should create racing data with default values', async function () {
            const testUser = { ...baseUser, username: 'testuser2' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-2', uploadedBy: testUser.username };
            
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);

            const racingData = {
                csvData: testCsvBase64
            };

            const res = await request(app).post('/racing-data').send(racingData);
            expect(res.status).toBe(201);
            expect(res.body.data.trackName).toBe('Unknown');
            expect(res.body.data.userName).toBe('Anonymous');
            expect(res.body.data.vehicleUsed).toBe('Unknown');
            expect(res.body.data.fileName).toBe('racing_data.csv');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('racingData').deleteMany({ userName: 'Anonymous' });
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should reject missing CSV data', async function () {
            const testUser = { ...baseUser, username: 'testuser3' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-3', uploadedBy: testUser.username };
            
            // Create user and track
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);

            const racingData = {
                trackName: testTrack.name,
                userName: testUser.username,
                fastestLapTime: '1:23.456'
            };

            const res = await request(app).post('/racing-data').send(racingData);
            expect(res.status).toBe(400);
            expect(res.body).toHaveProperty('message', 'CSV data (base64) is required');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should reject CSV data that is too large', async function () {
            const oneMB = 1024 * 1024;
            const largeData = 'x'.repeat(2 * oneMB); // 2MB
            const largeCsvBase64 = Buffer.from(largeData).toString('base64');

            const racingData = {
                trackName: 'test-track',
                userName: 'testuser',
                csvData: largeCsvBase64
            };

            const res = await request(app).post('/racing-data').send(racingData);
            expect(res.status).toBe(413);
            expect(res.body).toHaveProperty('message', 'CSV data too large. Maximum size is 1MB.');
    });
});

    describe('GET /racing-data', function () {
        it('should return all racing data', async function () {
            const res = await request(app).get('/racing-data');
            expect(res.status).toBe(200);
            expect(Array.isArray(res.body)).toBe(true);
        });

        it('should not include csvData in list response', async function () {
            const testUser = { ...baseUser, username: 'testuser4' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-4', uploadedBy: testUser.username };
            
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            await request(app).post('/racing-data').send({
                trackName: testTrack.name,
                userName: testUser.username,
                csvData: testCsvBase64
            });

            const res = await request(app).get('/racing-data');
            expect(res.status).toBe(200);
            
            const userRecord = res.body.find(r => r.userName === testUser.username);
            if (userRecord) {
                expect(userRecord.csvData).toBeUndefined();
            }
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('racingData').deleteMany({ userName: testUser.username });
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });
    });

    describe('GET /racing-data/:id', function () {
        it('should return racing data by ObjectId', async function () {
            const testUser = { ...baseUser, username: 'testuser5' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-5', uploadedBy: testUser.username };
            
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            
            const createRes = await request(app).post('/racing-data').send({
                trackName: testTrack.name,
                userName: testUser.username,
                csvData: testCsvBase64
            });
            
            const racingDataId = createRes.body.data._id;
            
            const res = await request(app).get(`/racing-data/${racingDataId}`);
            expect(res.status).toBe(200);
            expect(res.body).toHaveProperty('_id', racingDataId);
            expect(res.body).toHaveProperty('csvData');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('racingData').deleteMany({ userName: testUser.username });
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should return racing data by string ID', async function () {
            const customId = 'custom-string-id-123';
            const testUser = { ...baseUser, username: 'testuser6' };
            
            await request(app).post('/users').send(testUser);
            
            // Insert directly into database with custom string ID
            await db.collection('racingData').insertOne({
                _id: customId,
                trackName: 'test-track',
                userName: testUser.username,
                csvData: testCsvBase64,
                dateUploaded: new Date().toISOString()
            });
            
            const res = await request(app).get(`/racing-data/${customId}`);
            expect(res.status).toBe(200);
            expect(res.body).toHaveProperty('_id', customId);
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('racingData').deleteOne({ _id: customId });
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should return 404 for non-existent racing data', async function () {
            const res = await request(app).get(`/racing-data/${new ObjectId()}`);
            expect(res.status).toBe(404);
            expect(res.body).toHaveProperty('message', 'Racing data not found');
        });
    });

    describe('GET /racing-data/track/:trackName', function () {
        it('should return racing data for specific track', async function () {
            const testUser = { ...baseUser, username: 'testuser7' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-7', uploadedBy: testUser.username };
            
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            await request(app).post('/racing-data').send({
                trackName: testTrack.name,
                userName: testUser.username,
                csvData: testCsvBase64
            });

            const res = await request(app).get(`/racing-data/track/${testTrack.name}`);
            expect(res.status).toBe(200);
            expect(Array.isArray(res.body)).toBe(true);
            expect(res.body.length).toBeGreaterThan(0);
            expect(res.body[0]).toHaveProperty('trackName', testTrack.name);
            expect(res.body[0].csvData).toBeUndefined();
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('racingData').deleteMany({ userName: testUser.username });
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });
    });

    describe('GET /racing-data/user/:userName', function () {
        it('should return racing data for specific user', async function () {
            const testUser = { ...baseUser, username: 'testuser8' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-8', uploadedBy: testUser.username };
            
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            await request(app).post('/racing-data').send({
                trackName: testTrack.name,
                userName: testUser.username,
                csvData: testCsvBase64
            });

            const res = await request(app).get(`/racing-data/user/${testUser.username}`);
            expect(res.status).toBe(200);
            expect(Array.isArray(res.body)).toBe(true);
            expect(res.body.length).toBeGreaterThan(0);
            expect(res.body[0]).toHaveProperty('userName', testUser.username);
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('racingData').deleteMany({ userName: testUser.username });
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });
    });

    describe('GET /racing-data/user/:userName/last', function () {
        it('should return last racing data for user', async function () {
            const testUser = { ...baseUser, username: 'testuser9' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-9', uploadedBy: testUser.username };
            
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            
            // Create two records
            await request(app).post('/racing-data').send({
                trackName: testTrack.name,
                userName: testUser.username,
                fastestLapTime: '1:25.000',
                topSpeed: '302',
                averageSpeed: '193',
                csvData: testCsvBase64
            });
            
            await new Promise(resolve => setTimeout(resolve, 100));
            
            await request(app).post('/racing-data').send({
                trackName: testTrack.name,
                userName: testUser.username,
                fastestLapTime: '1:23.000',
                topSpeed: '300',
                averageSpeed: '183',
                csvData: testCsvBase64
            });

            const res = await request(app).get(`/racing-data/user/${testUser.username}/last`);
            expect(res.status).toBe(200);
            expect(res.body).toHaveProperty('userName', testUser.username);
            expect(res.body).toHaveProperty('fastestLapTime', '1:23.000');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('racingData').deleteMany({ userName: testUser.username });
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should return 404 when no data found for user', async function () {
            const res = await request(app).get('/racing-data/user/nonexistentuser/last');
            expect(res.status).toBe(404);
            expect(res.body).toHaveProperty('message', 'No racing data found for user');
        });
    });

    describe('POST /racing-data/upload', function () {
        it('should upload CSV file', async function () {
            const testUser = { ...baseUser, username: 'testuser10' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-10', uploadedBy: testUser.username };
            
            // Create user and track
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);

            const res = await request(app)
                .post('/racing-data/upload')
                .field('trackName', testTrack.name)
                .field('userName', testUser.username)
                .field('vehicleUsed', 'Test Car')
                .field('description', 'Test upload')
                .attach('csvFile', Buffer.from(testCsvData), 'test.csv');

            expect(res.status).toBe(201);
            expect(res.body).toHaveProperty('message', 'Racing data uploaded successfully');
            expect(res.body.data).toHaveProperty('fileName', 'test.csv');
            expect(res.body.data).toHaveProperty('fileSize');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('racingData').deleteMany({ userName: testUser.username });
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should reject non-CSV file', async function () {
            const testUser = { ...baseUser, username: 'testuser11' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-11', uploadedBy: testUser.username };
            
            // Create user and track
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);

            const res = await request(app)
                .post('/racing-data/upload')
                .field('trackName', testTrack.name)
                .field('userName', testUser.username)
                .attach('csvFile', Buffer.from('not csv'), 'test.txt');

            expect(res.status).toBe(400);
            expect(res.body).toHaveProperty('message', 'Only CSV files are allowed');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should reject request without file', async function () {
            const res = await request(app)
                .post('/racing-data/upload')
                .field('trackName', 'test-track')
                .field('userName', 'testuser');

            expect(res.status).toBe(400);
            expect(res.body).toHaveProperty('message', 'No CSV file uploaded');
        });

        it('should reject file too large', async function () {
            const testUser = { ...baseUser, username: 'testuser12' };
            
            await request(app).post('/users').send(testUser);
            
            // Create a file larger than 100MB
            const largeData = Buffer.alloc(101 * 1024 * 1024, 'x');

            const res = await request(app)
                .post('/racing-data/upload')
                .field('trackName', 'test-track')
                .field('userName', testUser.username)
                .attach('csvFile', largeData, 'large.csv');

            expect(res.status).toBe(400);
            expect(res.body).toHaveProperty('message', 'File too large. Maximum size is 100MB.');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('users').deleteOne({ username: testUser.username });
        });
    });

    describe('GET /racing-data/:id/download', function () {
        it('should download CSV data', async function () {
            const testUser = { ...baseUser, username: 'testuser13' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-13', uploadedBy: testUser.username };
            
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            
            const createRes = await request(app).post('/racing-data').send({
                trackName: testTrack.name,
                userName: testUser.username,
                fileName: 'download-test.csv',
                csvData: testCsvBase64
            });
            
            const racingDataId = createRes.body.data._id;
            
            const res = await request(app).get(`/racing-data/${racingDataId}/download`);
            expect(res.status).toBe(200);
            expect(res.headers['content-type']).toBe('text/csv');
            expect(res.headers['content-disposition']).toContain('download-test.csv');
            expect(res.text).toBe(testCsvData);
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('racingData').deleteMany({ userName: testUser.username });
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should return 404 for non-existent racing data', async function () {
            const res = await request(app).get(`/racing-data/${new ObjectId()}/download`);
            expect(res.status).toBe(404);
            expect(res.body).toHaveProperty('message', 'Racing data not found');
        });
    });

    describe('PUT /racing-data/:id', function () {
        it('should update racing data metadata', async function () {
            const testUser = { ...baseUser, username: 'testuser14' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-14', uploadedBy: testUser.username };
            
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            
            const createRes = await request(app).post('/racing-data').send({
                trackName: testTrack.name,
                userName: testUser.username,
                csvData: testCsvBase64
            });
            
            const racingDataId = createRes.body.data._id;
            
            const updateData = {
                description: 'Updated description',
                fastestLapTime: '1:22.000',
                vehicleUsed: 'Updated Car'
            };
            
            const res = await request(app).put(`/racing-data/${racingDataId}`).send(updateData);
            expect(res.status).toBe(200);
            expect(res.body).toHaveProperty('message', 'Racing data updated successfully');

            // Verify update
            const updatedData = await db.collection('racingData').findOne({ _id: new ObjectId(racingDataId) });
            expect(updatedData.description).toBe('Updated description');
            expect(updatedData.fastestLapTime).toBe('1:22.000');
            expect(updatedData.lastModified).toBeDefined();
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('racingData').deleteMany({ userName: testUser.username });
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should return 404 for non-existent racing data', async function () {
            const res = await request(app).put(`/racing-data/${new ObjectId()}`).send({ description: 'test' });
            expect(res.status).toBe(404);
            expect(res.body).toHaveProperty('message', 'Racing data not found');
        });

        it('should reject update with no valid fields', async function () {
            const testUser = { ...baseUser, username: 'testuser15' };
            
            await request(app).post('/users').send(testUser);
            
            const createRes = await request(app).post('/racing-data').send({
                userName: testUser.username,
                csvData: testCsvBase64
            });
            
            const racingDataId = createRes.body.data._id;
            
            const res = await request(app).put(`/racing-data/${racingDataId}`).send({ invalidField: 'test' });
            expect(res.status).toBe(400);
            expect(res.body).toHaveProperty('message', 'No valid fields to update');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('racingData').deleteMany({ userName: testUser.username });
            await db.collection('users').deleteOne({ username: testUser.username });
        });
    });

    describe('DELETE /racing-data/:id', function () {
        it('should delete racing data', async function () {
            const testUser = { ...baseUser, username: 'testuser16' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-16', uploadedBy: testUser.username };
            
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            
            const createRes = await request(app).post('/racing-data').send({
                trackName: testTrack.name,
                userName: testUser.username,
                csvData: testCsvBase64
            });
            
            const racingDataId = createRes.body.data._id;
            
            const res = await request(app).delete(`/racing-data/${racingDataId}`);
            expect(res.status).toBe(200);
            expect(res.body).toHaveProperty('message', 'Racing data deleted successfully');
            
            // Verify deletion
            const deletedData = await db.collection('racingData').findOne({ _id: new ObjectId(racingDataId) });
            expect(deletedData).toBeNull();
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should return 404 for non-existent racing data', async function () {
            const res = await request(app).delete(`/racing-data/${new ObjectId()}`);
            expect(res.status).toBe(404);
            expect(res.body).toHaveProperty('message', 'Racing data not found');
        });
    });

    describe('GET /racing-data/stats/summary', function () {
        it('should return racing data statistics', async function () {
            const res = await request(app).get('/racing-data/stats/summary');
            expect(res.status).toBe(200);
            expect(res.body).toHaveProperty('totalRecords');
            expect(res.body).toHaveProperty('uniqueTracksCount');
            expect(res.body).toHaveProperty('uniqueUsersCount');
            expect(res.body).toHaveProperty('avgFileSizeKB');
            expect(res.body).toHaveProperty('totalDataSizeMB');
        });

        it('should return default stats when no data exists', async function () {
            // Clear all racing data temporarily
            await db.collection('racingData').deleteMany({});
            
            const res = await request(app).get('/racing-data/stats/summary');
            expect(res.status).toBe(200);
            expect(res.body).toEqual({
                totalRecords: 0,
                uniqueTracksCount: 0,
                uniqueUsersCount: 0,
                avgFileSizeKB: 0,
                totalDataSizeMB: 0
            });
        });
    });
});