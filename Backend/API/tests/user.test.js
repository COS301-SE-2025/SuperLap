const request = require('supertest');
const app = require('../app'); // Your Express app
const { clearDatabase } = require('./testHelpers');

describe('User API', () => {
    beforeEach(async () => {
        await clearDatabase(); // Clear the in-memory DB before each test
    });

    describe('GET /users', () => {
        it('should return an empty array when no users exist', async () => {
            const response = await request(app).get('/users');
            expect(response.status).toBe(200);
            expect(response.body).toEqual([]);
        });

        it('should return all users', async () => {
            // First create a test user
            await request(app)
                .post('/users')
                .send({ username: 'testuser', email: 'test@example.com' });

            const response = await request(app).get('/users');
            expect(response.status).toBe(200);
            expect(response.body.length).toBe(1);
            expect(response.body[0].username).toBe('testuser');
        });
    });

    describe('POST /users', () => {
        it('should create a new user', async () => {
            const newUser = { username: 'newuser', email: 'new@example.com' };
            const response = await request(app)
                .post('/users')
                .send(newUser);

            expect(response.status).toBe(201);
            expect(response.body.message).toBe('User created successfully');
        });

        it('should prevent duplicate usernames', async () => {
            const newUser = { username: 'duplicate', email: 'test@example.com' };

            // First create
            await request(app).post('/users').send(newUser);

            // Try to create again
            const response = await request(app)
                .post('/users')
                .send(newUser);

            expect(response.status).toBe(400);
            expect(response.body.message).toBe('Username already taken');
        });
    });

    describe('GET /users/:username', () => {
        it('should return a user by username', async () => {
            const testUser = { username: 'testget', email: 'get@example.com' };
            await request(app).post('/users').send(testUser);

            const response = await request(app).get('/users/testget');
            expect(response.status).toBe(200);
            expect(response.body.username).toBe('testget');
        });

        it('should return 404 for non-existent user', async () => {
            const response = await request(app).get('/users/nonexistent');
            expect(response.status).toBe(404);
        });
    });

    describe('PUT /users/:username', () => {
        it('should update a user', async () => {
            // Create a user first
            await request(app)
                .post('/users')
                .send({ username: 'toupdate', email: 'original@example.com' });

            // Update the user
            const updatedData = { email: 'updated@example.com' };
            const response = await request(app)
                .put('/users/toupdate')
                .send(updatedData);

            expect(response.status).toBe(200);
            expect(response.body.message).toBe('User updated successfully');

            // Verify the update
            const getResponse = await request(app).get('/users/toupdate');
            expect(getResponse.body.email).toBe('updated@example.com');
        });
    });

    describe('DELETE /users/:username', () => {
        it('should delete a user', async () => {
            // Create a user first
            await request(app)
                .post('/users')
                .send({ username: 'todelete', email: 'delete@example.com' });

            // Delete the user
            const response = await request(app).delete('/users/todelete');
            expect(response.status).toBe(201);
            expect(response.body.message).toBe('User deleted successfully');

            // Verify deletion
            const getResponse = await request(app).get('/users/todelete');
            expect(getResponse.status).toBe(404);
        });
    });
});