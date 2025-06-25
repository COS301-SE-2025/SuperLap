const request = require('supertest');
const { connectToDb, closeDbConnection, app } = require('../app');

describe('Integration: MongoDB Connection & User API', () => {
    beforeAll(async () => {
        await connectToDb();
        // Clean up users collection before testing
        await app.locals.db.collection('users').deleteMany({});
    });

    afterAll(async () => {
        await closeDbConnection();
        await app.locals.db.client.close(); // force close
    });

    test('should attach db to app.locals', () => {
        const db = app.locals.db;
        expect(db).toBeDefined();
        expect(typeof db.collection).toBe('function');
    });

    test('GET /users should return empty array initially', async () => {
        const res = await request(app).get('/users');
        expect(res.statusCode).toBe(200);
        expect(Array.isArray(res.body)).toBe(true);
        expect(res.body.length).toBe(0);
    });

    test('POST /users should create a new user', async () => {
        const user = { username: 'testuser', email: 'test@example.com' };
        const res = await request(app).post('/users').send(user);
        expect(res.statusCode).toBe(201);
        expect(res.body.message).toBe('User created successfully');
    });

    test('POST /users should reject duplicate username', async () => {
        const user = { username: 'testuser', email: 'test2@example.com' };
        const res = await request(app).post('/users').send(user);
        expect(res.statusCode).toBe(400);
        expect(res.body.message).toBe('Username already taken');
    });

    test('GET /users should return array with the created user', async () => {
        const res = await request(app).get('/users');
        expect(res.statusCode).toBe(200);
        expect(res.body.length).toBe(1);
        expect(res.body[0].username).toBe('testuser');
    });

    test('GET /users/:username should return the specific user', async () => {
        const res = await request(app).get('/users/testuser');
        expect(res.statusCode).toBe(200);
        expect(res.body.username).toBe('testuser');
    });

    test('PUT /users/:username should update user data', async () => {
        const res = await request(app)
            .put('/users/testuser')
            .send({ email: 'updated@example.com' });

        expect(res.statusCode).toBe(200);
        expect(res.body.message).toBe('User updated successfully');

        // Verify update
        const userRes = await request(app).get('/users/testuser');
        expect(userRes.body.email).toBe('updated@example.com');
    });

    test('DELETE /users/:username should delete the user', async () => {
        const res = await request(app).delete('/users/testuser');
        expect(res.statusCode).toBe(201);
        expect(res.body.message).toBe('User deleted successfully');

        // Verify deletion
        const userRes = await request(app).get('/users/testuser');
        expect(userRes.body).toBeNull();
    });
});
