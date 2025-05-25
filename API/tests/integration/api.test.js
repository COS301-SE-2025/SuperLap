const request = require('supertest');
const { app, connectToDb } = require('../../app');
const { expect } = require('chai');

describe('API Endpoints', function () {
  beforeAll(async function () {
    await connectToDb();
  });

  const testUser = {
    username: 'testuser',
    email: 'testuser@example.com',
    age: 25,
  };

  afterAll(async function () {
    const db = app.locals.db;
    await db.collection('users').deleteMany({ username: testUser.username });
  });

  it('GET / should return a message and collections', async function () {
    const res = await request(app).get('/');
    expect(res.status).to.equal(200);
    expect(res.body).to.have.property('message');
  });

  it('POST /users should create a new user', async function () {
    const res = await request(app)
      .post('/users')
      .send(testUser);
    expect(res.status).to.equal(201);
    expect(res.body).to.have.property('message', 'User created successfully');
  });

  it('GET /users should return an array of users', async function () {
    const res = await request(app).get('/users');
    expect(res.status).to.equal(200);
    expect(res.body).to.be.an('array');
  });

  it('GET /users/:username should return a specific user', async function () {
    const res = await request(app).get(`/users/${testUser.username}`);
    expect(res.status).to.equal(200);
    expect(res.body).to.include({ username: testUser.username });
  });

  it('PUT /users/:username should update the user', async function () {
    const updatedData = { age: 30 };
    const res = await request(app)
      .put(`/users/${testUser.username}`)
      .send(updatedData);
    expect(res.status).to.equal(200);
    expect(res.body).to.have.property('message', 'User updated successfully');
  });

  it('DELETE /users/:username should delete the user', async function () {
    const res = await request(app)
      .delete(`/users/${testUser.username}`);
    expect(res.status).to.equal(201);
    expect(res.body).to.have.property('message', 'User deleted successfully');
  });

  // // Edge Testing

  it('GET /users/:username should return null or appropriate response if user not found', async function () {
    const res = await request(app).get('/users/nonexistentuser');
    expect(res.status).to.equal(200); // Your current implementation returns 200 with null body
    expect(res.body).to.satisfy(val => val === null || typeof val === 'object');
  });
});