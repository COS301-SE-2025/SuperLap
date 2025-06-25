// tests/testHelpers.js
const request = require('supertest');
const app = require('../app'); // Your Express app
const { getTestDb } = require('./setup');

async function createTestUser(userData = {}) {
    const db = getTestDb();
    const defaultUser = {
        username: 'testuser',
        email: 'test@example.com',
        ...userData
    };
    await db.collection('users').insertOne(defaultUser);
    return defaultUser;
}

module.exports = {
    request,
    app,
    createTestUser
};