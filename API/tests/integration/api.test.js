const request = require('supertest');
const { app, connectToDb } = require('../../app');
const { expect } = require('chai');

describe('API Endpoints', function () {
  beforeAll(async function () {
    await connectToDb();
  });

  it('GET / should return a message and collections', async function () {
    const res = await request(app).get('/');
    expect(res.status).to.equal(200);
    expect(res.body).to.have.property('message');
  });

});