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


});