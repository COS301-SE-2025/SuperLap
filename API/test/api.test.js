const request = require('supertest');
const { app, connectToDb } = require('../app');
const { expect } = require('chai');

describe('API Endpoints', function () {
  before(async function () {
    await connectToDb();
  });

  

});