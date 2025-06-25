const { app } = require('../app');

describe('Unit: Express App', () => {
    test('should have json middleware configured', () => {
        const jsonMiddleware = app._router.stack.some(
            layer => layer.name === 'jsonParser'
        );
        expect(jsonMiddleware).toBe(true);
    });

    test('should have Swagger routes setup', () => {
        const swaggerRoute = app._router.stack.some(
            layer => layer.route && layer.route.path === '/api-docs'
        );
        expect(swaggerRoute).toBe(true);
    });
});