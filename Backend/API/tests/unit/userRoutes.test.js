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

    // Use unique usernames for each test to avoid collisions
    const baseUser = {
        email: 'test@example.com',
        password: 'password123'
    };

    describe('POST /users', function () {
        it('should create a user successfully', async function () {
            const testUser = { ...baseUser, username: 'testuser1' };
            
            const res = await request(app)
                .post('/users')
                .send(testUser);
            
            expect(res.status).toBe(201);
            expect(res.body).toHaveProperty('message', 'User created successfully');
            
            // Verify user was created with hashed password
            const createdUser = await db.collection('users').findOne({ username: testUser.username });
            expect(createdUser).toBeTruthy();
            expect(createdUser.passwordHash).toBeDefined();
            expect(createdUser.passwordHash).not.toBe(testUser.password);
            expect(createdUser.createdAt).toBeDefined();
            expect(createdUser.updatedAt).toBeDefined();
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should reject duplicate username', async function () {
            const testUser = { ...baseUser, username: 'testuser2' };
            
            // Create first user
            const res1 = await request(app).post('/users').send(testUser);
            expect(res1.status).toBe(201);
            
            // Try to create duplicate user
            const res2 = await request(app).post('/users').send(testUser);
            expect(res2.status).toBe(400);
            expect(res2.body).toHaveProperty('message', 'Username already taken');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should handle missing required fields', async function () {
            const res1 = await request(app).post('/users').send({});
            expect(res1.status).toBe(500);
            
            const res2 = await request(app).post('/users').send({ username: 'testuser3' });
            expect(res2.status).toBe(500);
            
            const res3 = await request(app).post('/users').send({ username: 'testuser3', email: 'test@example.com' });
            expect(res3.status).toBe(500);
        });
    });

    describe('GET /users', function () {
        it('should return all users', async function () {
            const res = await request(app).get('/users');
            expect(res.status).toBe(200);
            expect(Array.isArray(res.body)).toBe(true);
        });

        it('should not include password hashes in response', async function () {
            const testUser = { ...baseUser, username: 'testuser4' };
            await request(app).post('/users').send(testUser);
            
            const res = await request(app).get('/users');
            expect(res.status).toBe(200);
            
            const user = res.body.find(u => u.username === testUser.username);
            if (user) {
                expect(user.passwordHash).toBeUndefined();
            }
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('users').deleteOne({ username: testUser.username });
        });
    });

    describe('GET /users/:username', function () {
        it('should return specific user', async function () {
            const testUser = { ...baseUser, username: 'testuser5' };
            
            // Create user
            await request(app).post('/users').send(testUser);
            
            const res = await request(app).get(`/users/${testUser.username}`);
            expect(res.status).toBe(200);
            expect(res.body).toHaveProperty('username', testUser.username);
            expect(res.body).toHaveProperty('email', testUser.email);
            expect(res.body.passwordHash).toBeUndefined();
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should return 404 for non-existent user', async function () {
            const res = await request(app).get('/users/nonexistentuser');
            expect(res.status).toBe(404);
            expect(res.body).toHaveProperty('message', 'User not found');
        });
    });

    describe('POST /users/login', function () {
        it('should login with correct credentials', async function () {
            const testUser = { ...baseUser, username: 'testuser6' };
            
            // Create user
            await request(app).post('/users').send(testUser);
            
            const res = await request(app)
                .post('/users/login')
                .send({ username: testUser.username, password: testUser.password });
            
            expect(res.status).toBe(200);
            expect(res.body).toHaveProperty('username', testUser.username);
            expect(res.body.passwordHash).toBeUndefined();
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should reject invalid password', async function () {
            const testUser = { ...baseUser, username: 'testuser7' };
            
            // Create user
            await request(app).post('/users').send(testUser);
            
            const res = await request(app)
                .post('/users/login')
                .send({ username: testUser.username, password: 'wrongpassword' });
            
            expect(res.status).toBe(401);
            expect(res.body).toHaveProperty('message', 'Invalid credentials');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should reject non-existent username', async function () {
            const res = await request(app)
                .post('/users/login')
                .send({ username: 'nonexistentuser', password: 'password123' });
            
            expect(res.status).toBe(401);
            expect(res.body).toHaveProperty('message', 'Invalid credentials');
        });

        it('should reject missing username or password', async function () {
            const res1 = await request(app)
                .post('/users/login')
                .send({ password: 'password123' });
            expect(res1.status).toBe(400);
            expect(res1.body).toHaveProperty('message', 'Username and password are required');
            
            const res2 = await request(app)
                .post('/users/login')
                .send({ username: 'testuser' });
            expect(res2.status).toBe(400);
            expect(res2.body).toHaveProperty('message', 'Username and password are required');
            
            const res3 = await request(app)
                .post('/users/login')
                .send({});
            expect(res3.status).toBe(400);
        });
    });

    describe('PUT /users/:username', function () {
        it('should update user', async function () {
            const testUser = { ...baseUser, username: 'testuser8' };
            
            // Create user
            await request(app).post('/users').send(testUser);
            
            const res = await request(app)
                .put(`/users/${testUser.username}`)
                .send({ email: 'newemail@example.com' });
            
            expect(res.status).toBe(200);
            expect(res.body).toHaveProperty('message', 'User updated successfully');
            
            // Verify update
            const updatedUser = await db.collection('users').findOne({ username: testUser.username });
            expect(updatedUser.email).toBe('newemail@example.com');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('users').deleteOne({ username: testUser.username });
        });

        it('should return 404 for non-existent user', async function () {
            const res = await request(app)
                .put('/users/nonexistentuser')
                .send({ email: 'newemail@example.com' });
            
            expect(res.status).toBe(404);
            expect(res.body).toHaveProperty('message', 'User not found');
        });

        it('should ignore password fields in update', async function () {
            const testUser = { ...baseUser, username: 'testuser9' };
            
            // Create user
            await request(app).post('/users').send(testUser);
            
            const originalUser = await db.collection('users').findOne({ username: testUser.username });
            const originalHash = originalUser.passwordHash;
            
            const res = await request(app)
                .put(`/users/${testUser.username}`)
                .send({ 
                    email: 'newemail@example.com',
                    password: 'newpassword',
                    passwordHash: 'hackattempt'
                });
            
            expect(res.status).toBe(200);
            
            // Verify password hash unchanged
            const updatedUser = await db.collection('users').findOne({ username: testUser.username });
            expect(updatedUser.passwordHash).toBe(originalHash);
            expect(updatedUser.email).toBe('newemail@example.com');
            
            // Cleanup
            await new Promise(resolve => setTimeout(resolve, 100));
            await db.collection('users').deleteOne({ username: testUser.username });
        });
    });

    describe('DELETE /users/:username', function () {
        it('should delete user', async function () {
            const testUser = { ...baseUser, username: 'testuser10' };
            
            // Create user
            await request(app).post('/users').send(testUser);
            
            const res = await request(app).delete(`/users/${testUser.username}`);
            expect(res.status).toBe(200);
            expect(res.body).toHaveProperty('message', 'User deleted successfully');
            
            // Verify deletion
            const deletedUser = await db.collection('users').findOne({ username: testUser.username });
            expect(deletedUser).toBeNull();
        });

        it('should return 404 for non-existent user', async function () {
            const res = await request(app).delete('/users/nonexistentuser');
            expect(res.status).toBe(404);
            expect(res.body).toHaveProperty('message', 'User not found');
        });
    });
});