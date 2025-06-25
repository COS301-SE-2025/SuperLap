// tests/user.test.js
const { getTestDb } = require('./setup');
const UserService = require('../services/userService'); // Your service layer

describe('User Service', () => {
    let db;

    beforeEach(() => {
        db = getTestDb();
        // You can seed test data here if needed
    });

    afterEach(async () => {
        // Clean up only the collections used in tests
        await db.collection('users').deleteMany({});
    });

    test('should create a user', async () => {
        const user = { name: "Test User", email: "test@example.com" };
        const result = await UserService.createUser(db, user);

        expect(result.insertedId).toBeDefined();

        // Verify only one document exists (our test data)
        const count = await db.collection('users').countDocuments();
        expect(count).toBe(1);
    });
});