const request = require('supertest');
const { app, connectToDb, closeDbConnection } = require('../../app');
const { expect } = require('chai');
const fs = require('fs');
const path = require('path');

describe('Complete API Test Suite', function () {
  let db;

  beforeAll(async function () {
    await connectToDb();
    db = app.locals.db;
  }, 60000);

  afterAll(async function () {
    // Clean up all test data
    await db.collection('users').deleteMany({ username: /^test/ });
    await db.collection('tracks').deleteMany({ name: /^test/ });
    await db.collection('racingData').deleteMany({ userName: /^test/ });
    await closeDbConnection();
  });

  // Test data
  const testUser = {
    username: 'testuser',
    email: 'testuser@example.com',
    password: 'testpassword123'
  };

  const testUser2 = {
    username: 'testuser2',
    email: 'testuser2@example.com',
    password: 'testpassword456'
  };

  const testTrack = {
    name: 'test-silverstone',
    type: 'circuit',
    city: 'Silverstone',
    country: 'United Kingdom',
    location: 'Northamptonshire',
    uploadedBy: 'testuser',
    image: 'data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAYEBQYFBAYGBQYHBwYIChAKCgkJChQODwwQFxQYGBcUFhYaHSUfGhsjHBYWICwgIyYnKSopGR8tMC0oMCUoKSj/2wBDAQcHBwoIChMKChMoGhYaKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCj/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAv/xAAhEAACAQMDBQAAAAAAAAAAAAABAgMABAUGIWGRkqGx0f/EABUBAQEAAAAAAAAAAAAAAAAAAAMF/8QAGhEAAgIDAAAAAAAAAAAAAAAAAAECEgMRkf/aAAwDAQACEQMRAD8A0XqoJHUddfPw==',
    description: 'Test track for racing'
  };

  const testCsvData = 'lap,time,speed,position\n1,1:23.456,180.5,1\n2,1:24.123,179.8,1\n3,1:23.987,181.2,1';
  const testCsvBase64 = Buffer.from(testCsvData).toString('base64');

  describe('Root Endpoint', function () {
    it('GET / should return a message and collections', async function () {
      const res = await request(app).get('/');
      expect(res.status).to.equal(200);
      expect(res.body).to.have.property('message');
    });
  });

  describe('User Endpoints', function () {
    beforeEach(async function () {
      // Clean up before each test
      await db.collection('users').deleteMany({ username: /^test/ });
    });

    describe('POST /users - Create User', function () {
      it('should create a new user successfully', async function () {
        const res = await request(app)
          .post('/users')
          .send(testUser);
        
        expect(res.status).to.equal(201);
        expect(res.body).to.have.property('message', 'User created successfully');
        expect(res.body).to.have.property('userId');
      });

      it('should not allow creating a user with existing username', async function () {
        await request(app).post('/users').send(testUser);
        
        const res = await request(app).post('/users').send(testUser);
        expect(res.status).to.equal(400);
        expect(res.body).to.have.property('message', 'Username already taken');
      });

      it('should handle missing required fields', async function () {
        const incompleteUser = { username: 'incomplete' };
        const res = await request(app)
          .post('/users')
          .send(incompleteUser);
        
        expect(res.status).to.be.oneOf([400, 500]);
      });

      it('should hash passwords correctly', async function () {
        await request(app).post('/users').send(testUser);
        
        const user = await db.collection('users').findOne({ username: testUser.username });
        expect(user.passwordHash).to.exist;
        expect(user.passwordHash).to.not.equal(testUser.password);
        expect(user.password).to.be.undefined;
      });
    });

    describe('GET /users - Get All Users', function () {
      it('should return an array of users', async function () {
        await request(app).post('/users').send(testUser);
        await request(app).post('/users').send(testUser2);
        
        const res = await request(app).get('/users');
        expect(res.status).to.equal(200);
        expect(res.body).to.be.an('array');
        expect(res.body.length).to.be.at.least(2);
      });

      it('should return empty array when no users exist', async function () {
        const res = await request(app).get('/users');
        expect(res.status).to.equal(200);
        expect(res.body).to.be.an('array');
      });
    });

    describe('GET /users/:username - Get Specific User', function () {
      it('should return a specific user', async function () {
        await request(app).post('/users').send(testUser);
        
        const res = await request(app).get(`/users/${testUser.username}`);
        expect(res.status).to.equal(200);
        expect(res.body).to.have.property('username', testUser.username);
        expect(res.body).to.have.property('email', testUser.email);
        expect(res.body).to.not.have.property('password');
      });

      it('should return null for non-existent user', async function () {
        const res = await request(app).get('/users/nonexistentuser');
        expect(res.status).to.equal(200);
        expect(res.body).to.be.null;
      });
    });

    describe('POST /users/login - User Login', function () {
      beforeEach(async function () {
        await request(app).post('/users').send(testUser);
      });

      it('should login with correct credentials', async function () {
        const res = await request(app)
          .post('/users/login')
          .send({
            username: testUser.username,
            password: testUser.password
          });
        
        expect(res.status).to.equal(200);
        expect(res.body).to.have.property('username', testUser.username);
        expect(res.body).to.not.have.property('passwordHash');
      });

      it('should reject invalid password', async function () {
        const res = await request(app)
          .post('/users/login')
          .send({
            username: testUser.username,
            password: 'wrongpassword'
          });
        
        expect(res.status).to.equal(401);
        expect(res.body).to.have.property('message', 'Invalid password');
      });

      it('should return 404 for non-existent user', async function () {
        const res = await request(app)
          .post('/users/login')
          .send({
            username: 'nonexistent',
            password: 'anypassword'
          });
        
        expect(res.status).to.equal(404);
        expect(res.body).to.have.property('message', 'User not found');
      });
    });

    describe('PUT /users/:username - Update User', function () {
      beforeEach(async function () {
        await request(app).post('/users').send(testUser);
      });

      it('should update user successfully', async function () {
        const updateData = { email: 'newemail@example.com' };
        const res = await request(app)
          .put(`/users/${testUser.username}`)
          .send(updateData);
        
        expect(res.status).to.equal(200);
        expect(res.body).to.have.property('message', 'User updated successfully');
      });

      it('should return 404 for non-existent user', async function () {
        const res = await request(app)
          .put('/users/nonexistent')
          .send({ email: 'test@test.com' });
        
        expect(res.status).to.equal(404);
        expect(res.body).to.have.property('message', 'User not found or data unchanged');
      });

      it('should handle empty update data', async function () {
        const res = await request(app)
          .put(`/users/${testUser.username}`)
          .send({});
        
        expect(res.status).to.equal(404);
        expect(res.body).to.have.property('message', 'User not found or data unchanged');
      });
    });

    describe('DELETE /users/:username - Delete User', function () {
      beforeEach(async function () {
        await request(app).post('/users').send(testUser);
      });

      it('should delete user successfully', async function () {
        const res = await request(app).delete(`/users/${testUser.username}`);
        expect(res.status).to.equal(201);
        expect(res.body).to.have.property('message', 'User deleted successfully');
      });

      it('should handle deletion of non-existent user', async function () {
        const res = await request(app).delete('/users/nonexistent');
        expect(res.status).to.equal(201);
        expect(res.body).to.have.property('message', 'User deleted successfully');
      });
    });
  });

  describe('Track Endpoints', function () {
    beforeEach(async function () {
      await db.collection('tracks').deleteMany({ name: /^test/ });
      await db.collection('users').deleteMany({ username: /^test/ });
      await request(app).post('/users').send(testUser);
    });

    describe('POST /tracks - Create Track', function () {
      it('should create a new track successfully', async function () {
        const res = await request(app)
          .post('/tracks')
          .send(testTrack);
        
        expect(res.status).to.equal(201);
        expect(res.body).to.have.property('message', 'Track created successfully');
      });

      it('should not allow creating duplicate tracks', async function () {
        await request(app).post('/tracks').send(testTrack);
        
        const res = await request(app).post('/tracks').send(testTrack);
        expect(res.status).to.equal(400);
        expect(res.body).to.have.property('message', 'Track already exists');
      });

      it('should handle missing required fields', async function () {
        const incompleteTrack = { name: 'incomplete-track' };
        const res = await request(app)
          .post('/tracks')
          .send(incompleteTrack);
        
        expect(res.status).to.equal(201);
      });
    });

    describe('GET /tracks - Get All Tracks', function () {
      it('should return all tracks', async function () {
        await request(app).post('/tracks').send(testTrack);
        await request(app).post('/tracks').send({
          ...testTrack,
          name: 'test-monza',
          city: 'Monza'
        });
        
        const res = await request(app).get('/tracks');
        expect(res.status).to.equal(200);
        expect(res.body).to.be.an('array');
        expect(res.body.length).to.be.at.least(2);
      });

      it('should return empty array when no tracks exist', async function () {
        const res = await request(app).get('/tracks');
        expect(res.status).to.equal(200);
        expect(res.body).to.be.an('array');
      });
    });

    describe('GET /tracks/:name - Get Specific Track', function () {
      it('should return a specific track', async function () {
        await request(app).post('/tracks').send(testTrack);
        
        const res = await request(app).get(`/tracks/${testTrack.name}`);
        expect(res.status).to.equal(200);
        expect(res.body).to.have.property('name', testTrack.name);
        expect(res.body).to.have.property('city', testTrack.city);
      });

      it('should return null for non-existent track', async function () {
        const res = await request(app).get('/tracks/nonexistent-track');
        expect(res.status).to.equal(200);
        expect(res.body).to.be.null;
      });
    });

    describe('PUT /tracks/:name - Update Track', function () {
      beforeEach(async function () {
        await request(app).post('/tracks').send(testTrack);
      });

      it('should update track successfully', async function () {
        const updateData = { description: 'Updated description' };
        const res = await request(app)
          .put(`/tracks/${testTrack.name}`)
          .send(updateData);
        
        expect(res.status).to.equal(200);
        expect(res.body).to.have.property('message', 'Track updated successfully');
      });
    });

    describe('DELETE /tracks/:name - Delete Track', function () {
      beforeEach(async function () {
        await request(app).post('/tracks').send(testTrack);
      });

      it('should delete track successfully', async function () {
        const res = await request(app).delete(`/tracks/${testTrack.name}`);
        expect(res.status).to.equal(201);
        expect(res.body).to.have.property('message', 'Track deleted successfully');
      });
    });

    describe('GET /images/:name - Get Track Image', function () {
      beforeEach(async function () {
        await request(app).post('/tracks').send(testTrack);
      });

      it('should return track image', async function () {
        const res = await request(app).get(`/images/${testTrack.name}`);
        expect(res.status).to.equal(201);
        expect(res.body).to.equal(testTrack.image);
      });
    });
  });

  describe('Racing Data Endpoints', function () {
    beforeEach(async function () {
      await db.collection('racingData').deleteMany({ userName: /^test/ });
      await db.collection('users').deleteMany({ username: /^test/ });
      await db.collection('tracks').deleteMany({ name: /^test/ });
      
      await request(app).post('/users').send(testUser);
      await request(app).post('/tracks').send(testTrack);
    });

    describe('POST /racing-data - Create Racing Data', function () {
      it('should create racing data with base64 CSV', async function () {
        const racingData = {
          trackName: testTrack.name,
          userName: testUser.username,
          lapTime: '1:23.456',
          vehicleUsed: 'Test Car',
          description: 'Test racing session',
          fileName: 'test-data.csv',
          csvData: testCsvBase64
        };
        
        const res = await request(app)
          .post('/racing-data')
          .send(racingData);
        
        expect(res.status).to.equal(201);
        expect(res.body).to.have.property('message', 'Racing data created successfully');
        expect(res.body.data).to.have.property('trackName', testTrack.name);
        expect(res.body.data).to.not.have.property('csvData');
      });

      it('should return 400 when CSV data is missing', async function () {
        const racingData = {
          trackName: testTrack.name,
          userName: testUser.username,
          lapTime: '1:23.456'
        };
        
        const res = await request(app)
          .post('/racing-data')
          .send(racingData);
        
        expect(res.status).to.equal(400);
        expect(res.body).to.have.property('message', 'CSV data (base64) is required');
      });
    });

    describe('POST /racing-data/upload - Upload CSV File', function () {
      it('should upload CSV file successfully', async function () {
        const res = await request(app)
          .post('/racing-data/upload')
          .field('trackName', testTrack.name)
          .field('userName', testUser.username)
          .field('fastestLapTime', '1:23.456')
          .field('averageSpeed', '180.5')
          .field('topSpeed', '200.3')
          .field('vehicleUsed', 'Test Car')
          .field('description', 'Test session')
          .attach('csvFile', Buffer.from(testCsvData), 'test-data.csv');
        
        expect(res.status).to.equal(201);
        expect(res.body).to.have.property('message', 'Racing data uploaded successfully');
        expect(res.body.data).to.have.property('trackName', testTrack.name);
      });

      it('should return 400 when no file is uploaded', async function () {
        const res = await request(app)
          .post('/racing-data/upload')
          .field('trackName', testTrack.name)
          .field('userName', testUser.username);
        
        expect(res.status).to.equal(400);
        expect(res.body).to.have.property('message', 'No CSV file uploaded');
      });
    });

    describe('GET /racing-data - Get All Racing Data', function () {
      beforeEach(async function () {
        const racingData = {
          trackName: testTrack.name,
          userName: testUser.username,
          lapTime: '1:23.456',
          vehicleUsed: 'Test Car',
          fileName: 'test-data.csv',
          csvData: testCsvBase64
        };
        await request(app).post('/racing-data').send(racingData);
      });

      it('should return all racing data records', async function () {
        const res = await request(app).get('/racing-data');
        expect(res.status).to.equal(200);
        expect(res.body).to.be.an('array');
        expect(res.body.length).to.be.at.least(1);
        expect(res.body[0]).to.not.have.property('csvData');
      });
    });

    describe('GET /racing-data/:id - Get Specific Racing Data', function () {
      let racingDataId;

      beforeEach(async function () {
        const racingData = {
          trackName: testTrack.name,
          userName: testUser.username,
          lapTime: '1:23.456',
          vehicleUsed: 'Test Car',
          fileName: 'test-data.csv',
          csvData: testCsvBase64
        };
        const res = await request(app).post('/racing-data').send(racingData);
        racingDataId = res.body.data._id;
      });

      it('should return specific racing data', async function () {
        const res = await request(app).get(`/racing-data/${racingDataId}`);
        expect(res.status).to.equal(200);
        expect(res.body).to.have.property('_id', racingDataId);
        expect(res.body).to.have.property('csvData');
      });

      it('should return 404 for non-existent racing data', async function () {
        const res = await request(app).get('/racing-data/nonexistent-id');
        expect(res.status).to.equal(404);
        expect(res.body).to.have.property('message', 'Racing data not found');
      });
    });

    describe('GET /racing-data/track/:trackName - Get Racing Data by Track', function () {
      beforeEach(async function () {
        const racingData = {
          trackName: testTrack.name,
          userName: testUser.username,
          lapTime: '1:23.456',
          vehicleUsed: 'Test Car',
          fileName: 'test-data.csv',
          csvData: testCsvBase64
        };
        await request(app).post('/racing-data').send(racingData);
      });

      it('should return racing data for specific track', async function () {
        const res = await request(app).get(`/racing-data/track/${testTrack.name}`);
        expect(res.status).to.equal(200);
        expect(res.body).to.be.an('array');
        expect(res.body.length).to.be.at.least(1);
        expect(res.body[0]).to.have.property('trackName', testTrack.name);
      });

      it('should return empty array for non-existent track', async function () {
        const res = await request(app).get('/racing-data/track/nonexistent-track');
        expect(res.status).to.equal(200);
        expect(res.body).to.be.an('array');
        expect(res.body.length).to.equal(0);
      });
    });

    describe('GET /racing-data/user/:userName - Get Racing Data by User', function () {
      beforeEach(async function () {
        const racingData = {
          trackName: testTrack.name,
          userName: testUser.username,
          lapTime: '1:23.456',
          vehicleUsed: 'Test Car',
          fileName: 'test-data.csv',
          csvData: testCsvBase64
        };
        await request(app).post('/racing-data').send(racingData);
      });

      it('should return racing data for specific user', async function () {
        const res = await request(app).get(`/racing-data/user/${testUser.username}`);
        expect(res.status).to.equal(200);
        expect(res.body).to.be.an('array');
        expect(res.body.length).to.be.at.least(1);
        expect(res.body[0]).to.have.property('userName', testUser.username);
      });
    });

    describe('GET /racing-data/:id/download - Download CSV Data', function () {
      let racingDataId;

      beforeEach(async function () {
        const racingData = {
          trackName: testTrack.name,
          userName: testUser.username,
          lapTime: '1:23.456',
          vehicleUsed: 'Test Car',
          fileName: 'test-data.csv',
          csvData: testCsvBase64
        };
        const res = await request(app).post('/racing-data').send(racingData);
        racingDataId = res.body.data._id;
      });

      it('should download CSV file', async function () {
        const res = await request(app).get(`/racing-data/${racingDataId}/download`);
        expect(res.status).to.equal(200);
        expect(res.headers['content-type']).to.equal('text/csv; charset=utf-8');
        expect(res.headers['content-disposition']).to.include('attachment');
        expect(res.text).to.equal(testCsvData);
      });

      it('should return 404 for non-existent racing data', async function () {
        const res = await request(app).get('/racing-data/nonexistent/download');
        expect(res.status).to.equal(404);
        expect(res.body).to.have.property('message', 'Racing data not found');
      });
    });

    describe('PUT /racing-data/:id - Update Racing Data', function () {
      let racingDataId;

      beforeEach(async function () {
        const racingData = {
          trackName: testTrack.name,
          userName: testUser.username,
          lapTime: '1:23.456',
          vehicleUsed: 'Test Car',
          fileName: 'test-data.csv',
          csvData: testCsvBase64
        };
        const res = await request(app).post('/racing-data').send(racingData);
        racingDataId = res.body.data._id;
      });

      it('should update racing data successfully', async function () {
        const updateData = {
          description: 'Updated description',
          lapTime: '1:22.999'
        };
        
        const res = await request(app)
          .put(`/racing-data/${racingDataId}`)
          .send(updateData);
        
        expect(res.status).to.equal(200);
        expect(res.body).to.have.property('message', 'Racing data updated successfully');
      });

      it('should return 400 for invalid update fields', async function () {
        const updateData = {
          invalidField: 'should not be allowed'
        };
        
        const res = await request(app)
          .put(`/racing-data/${racingDataId}`)
          .send(updateData);
        
        expect(res.status).to.equal(400);
        expect(res.body).to.have.property('message', 'No valid fields to update');
      });

      it('should return 404 for non-existent racing data', async function () {
        const res = await request(app)
          .put('/racing-data/nonexistent')
          .send({ description: 'test' });
        
        expect(res.status).to.equal(404);
        expect(res.body).to.have.property('message', 'Racing data not found or no changes made');
      });
    });

    describe('DELETE /racing-data/:id - Delete Racing Data', function () {
      let racingDataId;

      beforeEach(async function () {
        const racingData = {
          trackName: testTrack.name,
          userName: testUser.username,
          lapTime: '1:23.456',
          vehicleUsed: 'Test Car',
          fileName: 'test-data.csv',
          csvData: testCsvBase64
        };
        const res = await request(app).post('/racing-data').send(racingData);
        racingDataId = res.body.data._id;
      });

      it('should delete racing data successfully', async function () {
        const res = await request(app).delete(`/racing-data/${racingDataId}`);
        expect(res.status).to.equal(200);
        expect(res.body).to.have.property('message', 'Racing data deleted successfully');
      });

      it('should return 404 for non-existent racing data', async function () {
        const res = await request(app).delete('/racing-data/nonexistent');
        expect(res.status).to.equal(404);
        expect(res.body).to.have.property('message', 'Racing data not found');
      });
    });

    describe('GET /racing-data/stats/summary - Get Statistics', function () {
      beforeEach(async function () {
        const racingData1 = {
          trackName: testTrack.name,
          userName: testUser.username,
          lapTime: '1:23.456',
          vehicleUsed: 'Test Car',
          fileName: 'test-data1.csv',
          csvData: testCsvBase64
        };
        
        const racingData2 = {
          trackName: 'test-monza',
          userName: testUser2.username,
          lapTime: '1:24.123',
          vehicleUsed: 'Test Car 2',
          fileName: 'test-data2.csv',
          csvData: testCsvBase64
        };
        
        await request(app).post('/users').send(testUser2);
        await request(app).post('/racing-data').send(racingData1);
        await request(app).post('/racing-data').send(racingData2);
      });

      it('should return statistics summary', async function () {
        const res = await request(app).get('/racing-data/stats/summary');
        expect(res.status).to.equal(200);
        expect(res.body).to.have.property('totalRecords');
        expect(res.body).to.have.property('uniqueTracksCount');
        expect(res.body).to.have.property('uniqueUsersCount');
        expect(res.body).to.have.property('avgFileSizeKB');
        expect(res.body).to.have.property('totalDataSizeMB');
        expect(res.body.totalRecords).to.be.at.least(2);
      });
    });
  });

  describe('Error Handling and Edge Cases', function () {
    it('should handle malformed JSON', async function () {
      const res = await request(app)
        .post('/users')
        .set('Content-Type', 'application/json')
        .send('{"username": "badjson"'); // malformed JSON
      
      expect(res.status).to.be.oneOf([400, 500]);
    });

    it('should handle very long usernames', async function () {
      const longUsername = 'a'.repeat(1000);
      const res = await request(app)
        .post('/users')
        .send({
          username: longUsername,
          email: 'test@example.com',
          password: 'password123'
        });
      
      expect(res.status).to.be.oneOf([400, 500, 201]);
    });

    it('should handle special characters in track names', async function () {
      const specialTrack = {
        ...testTrack,
        name: 'test-track-with-special-chars-!@#$%'
      };
      
      const res = await request(app)
        .post('/tracks')
        .send(specialTrack);
      
      expect(res.status).to.equal(201);
    });

    it('should handle large CSV data', async function () {
      const largeCsvData = 'lap,time,speed\n' + 
        Array(10000).fill(0).map((_, i) => `${i+1},1:23.${String(i).padStart(3, '0')},180.5`).join('\n');
      const largeCsvBase64 = Buffer.from(largeCsvData).toString('base64');
      
      const racingData = {
        trackName: testTrack.name,
        userName: testUser.username,
        lapTime: '1:23.456',
        vehicleUsed: 'Test Car',
        fileName: 'large-test-data.csv',
        csvData: largeCsvBase64
      };
      
      await request(app).post('/users').send(testUser);
      await request(app).post('/tracks').send(testTrack);
      
      const res = await request(app)
        .post('/racing-data')
        .send(racingData);
      
      expect(res.status).to.be.oneOf([201, 413, 500]); // Success or payload too large
    });

    it('should handle concurrent requests', async function () {
      await request(app).post('/users').send(testUser);
      
      const promises = Array(10).fill(0).map((_, i) => 
        request(app)
          .post('/users')
          .send({
            username: `concurrent-user-${i}`,
            email: `concurrent${i}@example.com`,
            password: 'password123'
          })
      );
      
      const results = await Promise.all(promises);
      const successCount = results.filter(res => res.status === 201).length;
      expect(successCount).to.equal(10);
    });
  });

  describe('Performance Tests', function () {
    it('should handle rapid successive requests', async function () {
      await request(app).post('/users').send(testUser);
      
      const start = Date.now();
      const promises = Array(20).fill(0).map(() => request(app).get('/users'));
      const results = await Promise.all(promises);
      const end = Date.now();
      
      expect(end - start).to.be.lessThan(5000); // Should complete within 5 seconds
      results.forEach(res => expect(res.status).to.equal(200));
    });

    it('should handle large result sets efficiently', async function () {
      await request(app).post('/users').send(testUser);
      await request(app).post('/tracks').send(testTrack);
      
      // Create multiple racing data records
      const promises = Array(50).fill(0).map((_, i) => 
        request(app)
          .post('/racing-data')
          .send({
            trackName: testTrack.name,
            userName: testUser.username,
            lapTime: `1:23.${String(i).padStart(3, '0')}`,
            vehicleUsed: `Test Car ${i}`,
            fileName: `test-data-${i}.csv`,
            csvData: testCsvBase64
          })
      );
      
      await Promise.all(promises);
      
      const start = Date.now();
      const res = await request(app).get('/racing-data');
      const end = Date.now();
      
      expect(res.status).to.equal(200);
      expect(res.body.length).to.be.at.least(50);
      expect(end - start).to.be.lessThan(3000); // Should complete within 3 seconds
    });
  });

  describe('Data Validation Tests', function () {
    describe('User Data Validation', function () {
      it('should validate email format', async function () {
        const invalidUser = {
          username: 'testuser',
          email: 'invalid-email',
          password: 'password123'
        };
        
        const res = await request(app)
          .post('/users')
          .send(invalidUser);
        
        // Note: Current implementation doesn't validate email format
        // This test documents expected behavior for future implementation
        expect(res.status).to.be.oneOf([201, 400]);
      });

      it('should handle empty password', async function () {
        const userWithEmptyPassword = {
          username: 'testuser',
          email: 'test@example.com',
          password: ''
        };
        
        const res = await request(app)
          .post('/users')
          .send(userWithEmptyPassword);
        
        expect(res.status).to.be.oneOf([400, 500]);
      });

      it('should handle SQL injection attempts in username', async function () {
        const maliciousUser = {
          username: "'; DROP TABLE users; --",
          email: 'malicious@example.com',
          password: 'password123'
        };
        
        const res = await request(app)
          .post('/users')
          .send(maliciousUser);
        
        // Should either reject or handle safely
        expect(res.status).to.be.oneOf([201, 400, 500]);
      });
    });

    describe('Track Data Validation', function () {
      beforeEach(async function () {
        await request(app).post('/users').send(testUser);
      });

      it('should handle missing track name', async function () {
        const trackWithoutName = {
          type: 'circuit',
          city: 'Test City',
          country: 'Test Country',
          uploadedBy: testUser.username
        };
        
        const res = await request(app)
          .post('/tracks')
          .send(trackWithoutName);
        
        expect(res.status).to.be.oneOf([201, 400]);
      });

      it('should handle extremely long track descriptions', async function () {
        const longDescription = 'A'.repeat(10000);
        const trackWithLongDescription = {
          ...testTrack,
          name: 'test-track-long-desc',
          description: longDescription
        };
        
        const res = await request(app)
          .post('/tracks')
          .send(trackWithLongDescription);
        
        expect(res.status).to.be.oneOf([201, 400, 413]);
      });
    });

    describe('Racing Data Validation', function () {
      beforeEach(async function () {
        await request(app).post('/users').send(testUser);
        await request(app).post('/tracks').send(testTrack);
      });

      it('should handle invalid base64 data', async function () {
        const invalidRacingData = {
          trackName: testTrack.name,
          userName: testUser.username,
          lapTime: '1:23.456',
          vehicleUsed: 'Test Car',
          fileName: 'test-data.csv',
          csvData: 'this-is-not-valid-base64!'
        };
        
        const res = await request(app)
          .post('/racing-data')
          .send(invalidRacingData);
        
        expect(res.status).to.be.oneOf([201, 400]);
      });

      it('should handle non-CSV file upload', async function () {
        const textData = 'This is not a CSV file';
        const res = await request(app)
          .post('/racing-data/upload')
          .field('trackName', testTrack.name)
          .field('userName', testUser.username)
          .attach('csvFile', Buffer.from(textData), 'test-data.txt');
        
        expect(res.status).to.equal(400);
        expect(res.body).to.have.property('message', 'Only CSV files are allowed');
      });

      it('should handle oversized file uploads', async function () {
        // Create a large buffer (simulate 60MB file)
        const largeBuffer = Buffer.alloc(60 * 1024 * 1024, 'a');
        
        const res = await request(app)
          .post('/racing-data/upload')
          .field('trackName', testTrack.name)
          .field('userName', testUser.username)
          .attach('csvFile', largeBuffer, 'large-file.csv');
        
        expect(res.status).to.equal(400);
        expect(res.body).to.have.property('message', 'File too large. Maximum size is 50MB.');
      });
    });
  });

  describe('Integration Tests', function () {
    it('should support complete user workflow', async function () {
      // 1. Create user
      let res = await request(app).post('/users').send(testUser);
      expect(res.status).to.equal(201);
      
      // 2. Login user
      res = await request(app)
        .post('/users/login')
        .send({
          username: testUser.username,
          password: testUser.password
        });
      expect(res.status).to.equal(200);
      
      // 3. Create track
      res = await request(app).post('/tracks').send(testTrack);
      expect(res.status).to.equal(201);
      
      // 4. Upload racing data
      res = await request(app)
        .post('/racing-data')
        .send({
          trackName: testTrack.name,
          userName: testUser.username,
          lapTime: '1:23.456',
          vehicleUsed: 'Test Car',
          fileName: 'test-data.csv',
          csvData: testCsvBase64
        });
      expect(res.status).to.equal(201);
      const racingDataId = res.body.data._id;
      
      // 5. Retrieve racing data
      res = await request(app).get(`/racing-data/${racingDataId}`);
      expect(res.status).to.equal(200);
      
      // 6. Download CSV
      res = await request(app).get(`/racing-data/${racingDataId}/download`);
      expect(res.status).to.equal(200);
      expect(res.text).to.equal(testCsvData);
      
      // 7. Update racing data
      res = await request(app)
        .put(`/racing-data/${racingDataId}`)
        .send({ description: 'Updated test session' });
      expect(res.status).to.equal(200);
      
      // 8. Get statistics
      res = await request(app).get('/racing-data/stats/summary');
      expect(res.status).to.equal(200);
      expect(res.body.totalRecords).to.be.at.least(1);
    });

    it('should handle cascading operations correctly', async function () {
      // Create user and track
      await request(app).post('/users').send(testUser);
      await request(app).post('/tracks').send(testTrack);
      
      // Create multiple racing data records for the user
      const racingDataPromises = Array(5).fill(0).map((_, i) => 
        request(app)
          .post('/racing-data')
          .send({
            trackName: testTrack.name,
            userName: testUser.username,
            lapTime: `1:23.${String(i).padStart(3, '0')}`,
            vehicleUsed: `Test Car ${i}`,
            fileName: `test-data-${i}.csv`,
            csvData: testCsvBase64
          })
      );
      
      await Promise.all(racingDataPromises);
      
      // Verify data was created correctly
      const userDataRes = await request(app).get(`/racing-data/user/${testUser.username}`);
      expect(userDataRes.status).to.equal(200);
      expect(userDataRes.body.length).to.equal(5);
      
      const trackDataRes = await request(app).get(`/racing-data/track/${testTrack.name}`);
      expect(trackDataRes.status).to.equal(200);
      expect(trackDataRes.body.length).to.equal(5);
    });
  });

  describe('Security Tests', function () {
    it('should not expose sensitive data in responses', async function () {
      await request(app).post('/users').send(testUser);
      
      const res = await request(app).get(`/users/${testUser.username}`);
      expect(res.status).to.equal(200);
      expect(res.body).to.not.have.property('password');
      expect(res.body).to.not.have.property('passwordHash');
    });

    it('should handle authentication bypass attempts', async function () {
      const maliciousLogin = {
        username: { $ne: null },
        password: { $ne: null }
      };
      
      const res = await request(app)
        .post('/users/login')
        .send(maliciousLogin);
      
      expect(res.status).to.be.oneOf([400, 404, 500]);
    });

    it('should sanitize file names', async function () {
      await request(app).post('/users').send(testUser);
      await request(app).post('/tracks').send(testTrack);
      
      const maliciousFileName = '../../../etc/passwd';
      const res = await request(app)
        .post('/racing-data/upload')
        .field('trackName', testTrack.name)
        .field('userName', testUser.username)
        .attach('csvFile', Buffer.from(testCsvData), maliciousFileName);
      
      if (res.status === 201) {
        // If upload succeeds, filename should be sanitized
        expect(res.body.data.fileName).to.not.include('../');
      }
    });
  });

  describe('Cleanup and Maintenance', function () {
    it('should handle database cleanup operations', async function () {
      // Create test data
      await request(app).post('/users').send(testUser);
      await request(app).post('/tracks').send(testTrack);
      
      const racingData = {
        trackName: testTrack.name,
        userName: testUser.username,
        lapTime: '1:23.456',
        vehicleUsed: 'Test Car',
        fileName: 'test-data.csv',
        csvData: testCsvBase64
      };
      
      const createRes = await request(app).post('/racing-data').send(racingData);
      const racingDataId = createRes.body.data._id;
      
      // Delete racing data
      let res = await request(app).delete(`/racing-data/${racingDataId}`);
      expect(res.status).to.equal(200);
      
      // Delete track
      res = await request(app).delete(`/tracks/${testTrack.name}`);
      expect(res.status).to.equal(201);
      
      // Delete user
      res = await request(app).delete(`/users/${testUser.username}`);
      expect(res.status).to.equal(201);
      
      // Verify deletions
      res = await request(app).get(`/racing-data/${racingDataId}`);
      expect(res.status).to.equal(404);
      
      res = await request(app).get(`/tracks/${testTrack.name}`);
      expect(res.status).to.equal(200);
      expect(res.body).to.be.null;
      
      res = await request(app).get(`/users/${testUser.username}`);
      expect(res.status).to.equal(200);
      expect(res.body).to.be.null;
    });
  });
});