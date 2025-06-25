require('dotenv').config();
const { connectToDb, closeDbConnection, app } = require('../app');

describe('Integration: MongoDB Connection', () => {
    beforeAll(async () => {
        await connectToDb();
    });

    afterAll(async () => {
        await closeDbConnection();
    });

    test('should attach db to app.locals', () => {
        const db = app.locals.db;
        expect(db).toBeDefined();
        expect(typeof db.collection).toBe('function'); // basic check
    });
});