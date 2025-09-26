const request = require('supertest');
const { app, connectToDb, closeDbConnection } = require('../../app');

describe('User Routes Unit Tests', function () {
    let db;

    beforeAll(async function () {
        await connectToDb();
        db = app.locals.db;
    }, 30000);

    afterAll(async function () {
        await db.collection('users').deleteMany({ username: /^test/ });
        await closeDbConnection();
    });

    const testUser = {
        username: 'testuser',
        email: 'test@example.com',
        password: 'password123'
    };

    describe('POST /users', function () {
        afterEach(async function () {
            await db.collection('users').deleteMany({ username: testUser.username });
        });

        it('should create a user successfully', async function () {
            const res = await request(app)
                .post('/users')
                .send(testUser);
            
            expect(res.status).toBe(201);
            expect(res.body).toHaveProperty('message', 'User created successfully');
        });

        it('should reject duplicate username', async function () {
            await request(app).post('/users').send(testUser);
            const res = await request(app).post('/users').send(testUser);
            
            expect(res.status).toBe(400);
            expect(res.body).toHaveProperty('message', 'Username already taken');
        });
    });

    describe('GET /users', function () {
        it('should return all users', async function () {
            const res = await request(app).get('/users');
            expect(res.status).toBe(200);
            expect(Array.isArray(res.body)).toBe(true);
        });
    });

    describe('GET /users/:username', function () {
        beforeEach(async function () {
            await request(app).post('/users').send(testUser);
        });

        afterEach(async function () {
            await db.collection('users').deleteMany({ username: testUser.username });
        });

        it('should return specific user', async function () {
            const res = await request(app).get(`/users/${testUser.username}`);
            expect(res.status).toBe(200);
            expect(res.body).toHaveProperty('username', testUser.username);
        });
    });

    describe('POST /users/login', function () {
        beforeEach(async function () {
            await request(app).post('/users').send(testUser);
        });

        afterEach(async function () {
            await db.collection('users').deleteMany({ username: testUser.username });
        });

        it('should login with correct credentials', async function () {
            const res = await request(app)
                .post('/users/login')
                .send({ username: testUser.username, password: testUser.password });
            
            expect(res.status).toBe(200);
            expect(res.body).toHaveProperty('username', testUser.username);
        });

        it('should reject invalid password', async function () {
            const res = await request(app)
                .post('/users/login')
                .send({ username: testUser.username, password: 'wrongpassword' });
            
            expect(res.status).toBe(401);
        });
    });

    describe('PUT /users/:username', function () {
        beforeEach(async function () {
            await request(app).post('/users').send(testUser);
        });

        afterEach(async function () {
            await db.collection('users').deleteMany({ username: testUser.username });
        });

        it('should update user', async function () {
            const res = await request(app)
                .put(`/users/${testUser.username}`)
                .send({ email: 'newemail@example.com' });
            
            expect(res.status).toBe(200);
        });
    });

    describe('DELETE /users/:username', function () {
        beforeEach(async function () {
            await request(app).post('/users').send(testUser);
        });

        it('should delete user', async function () {
            const res = await request(app).delete(`/users/${testUser.username}`);
            expect(res.status).toBe(201);
        });
    });
});