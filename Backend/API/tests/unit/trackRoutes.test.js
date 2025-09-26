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

    const baseUser = { email: 'test@example.com', password: 'password123' };
    const baseTrack = {
        type: 'circuit',
        city: 'Silverstone',
        country: 'UK',
        description: 'Test track'
    };

    describe('POST /tracks', function () {
        it('should create a track', async function () {
            const testUser = { ...baseUser, username: 'testuser1' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-1', uploadedBy: testUser.username };
            
            // Create user first
            await request(app).post('/users').send(testUser);
            
            const res = await request(app).post('/tracks').send(testTrack);
            expect(res.status).toBe(201);
            expect(res.body).toHaveProperty('message', 'Track created successfully');
            
            // Verify track was created with all fields
            const createdTrack = await db.collection('tracks').findOne({ name: testTrack.name });
            expect(createdTrack.dateUploaded).toBeDefined();
            expect(createdTrack.location).toBe(`${testTrack.city}, ${testTrack.country}`);
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should create track with default values', async function () {
            const testUser = { ...baseUser, username: 'testuser2' };
            const minimalTrack = { name: 'test-minimal-track' };
            
            await request(app).post('/users').send(testUser);
            
            const res = await request(app).post('/tracks').send(minimalTrack);
            expect(res.status).toBe(201);
            
            // Verify defaults were applied
            const createdTrack = await db.collection('tracks').findOne({ name: minimalTrack.name });
            expect(createdTrack.uploadedBy).toBe('testuser'); // Default value
            expect(createdTrack.image).toBe('');
            expect(createdTrack.description).toBe('');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('tracks').deleteOne({ name: minimalTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should reject duplicate track', async function () {
            const testUser = { ...baseUser, username: 'testuser3' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-3', uploadedBy: testUser.username };
            
            // Create user first
            await request(app).post('/users').send(testUser);
            
            // Create first track
            const res1 = await request(app).post('/tracks').send(testTrack);
            expect(res1.status).toBe(201);
            
            // Try to create duplicate track
            const res2 = await request(app).post('/tracks').send(testTrack);
            expect(res2.status).toBe(400);
            expect(res2.body).toHaveProperty('message', 'Track already exists');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
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
        it('should return specific track', async function () {
            const testUser = { ...baseUser, username: 'testuser4' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-4', uploadedBy: testUser.username };
            
            // Create user and track
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            
            const res = await request(app).get(`/tracks/${testTrack.name}`);
            expect(res.status).toBe(200);
            expect(res.body).toHaveProperty('name', testTrack.name);
            expect(res.body).toHaveProperty('type', testTrack.type);
            expect(res.body).toHaveProperty('city', testTrack.city);
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should return 404 for non-existent track', async function () {
            const res = await request(app).get('/tracks/nonexistent-track');
            expect(res.status).toBe(404);
            expect(res.body).toHaveProperty('message', 'Track not found');
        });
    });

    describe('GET /tracks/type/:type', function () {
        it('should return tracks by type', async function () {
            const testUser = { ...baseUser, username: 'testuser5' };
            const testTrack = { ...baseTrack, name: 'test-circuit-5', type: 'circuit', uploadedBy: testUser.username };
            
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            
            const res = await request(app).get('/tracks/type/circuit');
            expect(res.status).toBe(200);
            expect(Array.isArray(res.body)).toBe(true);
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });
    });

    describe('GET /tracks/city/:city', function () {
        it('should return tracks by city', async function () {
            const testUser = { ...baseUser, username: 'testuser6' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-6', city: 'TestCity', uploadedBy: testUser.username };
            
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            
            const res = await request(app).get('/tracks/city/TestCity');
            expect(res.status).toBe(200);
            expect(Array.isArray(res.body)).toBe(true);
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });
    });

    describe('GET /tracks/country/:country', function () {
        it('should return tracks by country', async function () {
            const testUser = { ...baseUser, username: 'testuser7' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-7', country: 'TestCountry', uploadedBy: testUser.username };
            
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            
            const res = await request(app).get('/tracks/country/TestCountry');
            expect(res.status).toBe(200);
            expect(Array.isArray(res.body)).toBe(true);
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });
    });

    describe('GET /tracks/location/:location', function () {
        it('should return tracks by location', async function () {
            const testUser = { ...baseUser, username: 'testuser8' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-8', location: 'TestLocation', uploadedBy: testUser.username };
            
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            
            const res = await request(app).get('/tracks/location/TestLocation');
            expect(res.status).toBe(200);
            expect(Array.isArray(res.body)).toBe(true);
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });
    });

    describe('PUT /tracks/:name', function () {
        it('should update track', async function () {
            const testUser = { ...baseUser, username: 'testuser9' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-9', uploadedBy: testUser.username };
            
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            
            const updateData = { description: 'Updated description', city: 'New City' };
            const res = await request(app).put(`/tracks/${testTrack.name}`).send(updateData);
            
            expect(res.status).toBe(200);
            expect(res.body).toHaveProperty('message', 'Track updated successfully');
            
            // Verify update
            const updatedTrack = await db.collection('tracks').findOne({ name: testTrack.name });
            expect(updatedTrack.description).toBe('Updated description');
            expect(updatedTrack.city).toBe('New City');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should return 404 for non-existent track', async function () {
            const res = await request(app).put('/tracks/nonexistent-track').send({ description: 'test' });
            expect(res.status).toBe(404);
            expect(res.body).toHaveProperty('message', 'Track not found');
        });

        it('should ignore name changes in update', async function () {
            const testUser = { ...baseUser, username: 'testuser10' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-10', uploadedBy: testUser.username };
            
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            
            const res = await request(app).put(`/tracks/${testTrack.name}`).send({ name: 'hacked-name', description: 'test' });
            expect(res.status).toBe(200);
            
            // Verify name unchanged
            const updatedTrack = await db.collection('tracks').findOne({ name: testTrack.name });
            expect(updatedTrack.name).toBe(testTrack.name);
            expect(updatedTrack.description).toBe('test');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });
    });

    describe('DELETE /tracks/:name', function () {
        it('should delete track', async function () {
            const testUser = { ...baseUser, username: 'testuser11' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-11', uploadedBy: testUser.username };
            
            // Create user and track
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            
            const res = await request(app).delete(`/tracks/${testTrack.name}`);
            expect(res.status).toBe(200);
            expect(res.body).toHaveProperty('message', 'Track deleted successfully');
            
            // Verify deletion
            const deletedTrack = await db.collection('tracks').findOne({ name: testTrack.name });
            expect(deletedTrack).toBeNull();
            
            // Cleanup user
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should return 404 for non-existent track', async function () {
            const res = await request(app).delete('/tracks/nonexistent-track');
            expect(res.status).toBe(404);
            expect(res.body).toHaveProperty('message', 'Track not found');
        });
    });

    describe('GET /images/:name', function () {
        it('should return track image', async function () {
            const testUser = { ...baseUser, username: 'testuser12' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-12', uploadedBy: testUser.username, image: 'base64imagedata' };
            
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            
            const res = await request(app).get(`/images/${testTrack.name}`);
            expect(res.status).toBe(200);
            expect(res.body).toBe('base64imagedata');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should return 404 for track without image', async function () {
            const testUser = { ...baseUser, username: 'testuser13' };
            const testTrack = { ...baseTrack, name: 'test-silverstone-13', uploadedBy: testUser.username, image: '' };
            
            await request(app).post('/users').send(testUser);
            await request(app).post('/tracks').send(testTrack);
            
            const res = await request(app).get(`/images/${testTrack.name}`);
            expect(res.status).toBe(404);
            expect(res.body).toHaveProperty('message', 'Track image not found');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('tracks').deleteOne({ name: testTrack.name });
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should return 404 for non-existent track', async function () {
            const res = await request(app).get('/images/nonexistent-track');
            expect(res.status).toBe(404);
            expect(res.body).toHaveProperty('message', 'Track image not found');
        });
    });
});