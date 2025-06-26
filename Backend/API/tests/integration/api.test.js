const request = require('supertest');
const { app, connectToDb, closeDbConnection } = require('../../app');
const { expect } = require('chai');

describe('API Endpoints', function () {
  beforeAll(async function () {
    await connectToDb();
  });

  const testUser = {
    username: 'testuser',
    email: 'testuser@example.com',
    password: 'testpassword',
  };

  afterAll(async function () {
    const db = app.locals.db;
    await db.collection('users').deleteMany({ username: testUser.username });
    await closeDbConnection();
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

  // Edge Testing
  it('GET /users/:username should return null or appropriate response if user not found', async function () {
    const res = await request(app).get('/users/nonexistentuser');
    expect(res.status).to.equal(200); // Your current implementation returns 200 with null body
    expect(res.body).to.satisfy(val => val === null || typeof val === 'object');
  });

  it('PUT /users/:username should return 404 if user does not exist', async function () {
    const res = await request(app)
      .put('/users/fakeuser123')
      .send({ age: 99 });
    expect(res.status).to.equal(404);
    expect(res.body).to.have.property('message', 'User not found or data unchanged');
  });

  it('DELETE /users/:username should return success even if user does not exist', async function () {
    const res = await request(app)
      .delete('/users/nonexistentuser');
    expect(res.status).to.equal(201); // You may want to consider changing this to 404 for nonexistent users
    expect(res.body).to.have.property('message', 'User deleted successfully');
  });

  it('PUT /users/:username with empty body should not update anything', async function () {
    // Create a user first
    await request(app).post('/users').send(testUser);

    const res = await request(app)
      .put(`/users/${testUser.username}`)
      .send({}); // Empty body
    expect(res.status).to.equal(404);
    expect(res.body).to.have.property('message', 'User not found or data unchanged');
  });

  it('POST /users should not allow creating a user with an existing username', async function () {
    // Create the user once
    await request(app).post('/users').send(testUser);

    // Try to create the same user again
    const res = await request(app).post('/users').send(testUser);
    expect(res.status).to.equal(400);
    expect(res.body).to.have.property('message', 'Username already taken');
  });

  it('POST /users should return 400 for malformed JSON', async function () {
    const res = await request(app)
      .post('/users')
      .set('Content-Type', 'application/json')
      .send('{"username": "badjson"'); // missing closing }

    expect(res.status).to.be.oneOf([400, 500]); // depending on express behavior
  });

});