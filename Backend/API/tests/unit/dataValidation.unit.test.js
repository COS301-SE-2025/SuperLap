describe('Data Validation Unit Tests', function() {

    describe('Input Sanitization', function() {

        it('should handle special characters in usernames', function() {
            const testCases = [
                { input: 'normaluser', expected: true },
                { input: 'user_with_underscore', expected: true },
                { input: 'user-with-dash', expected: true },
                { input: 'user123', expected: true },
                { input: 'user@domain.com', expected: false }, // email-like
                { input: "'; DROP TABLE users; --", expected: false }, // SQL injection
                { input: '<script>alert("xss")</script>', expected: false }, // XSS
                { input: '', expected: false }, // empty
                { input: 'a'.repeat(1000), expected: false } // too long
            ];

            testCases.forEach(testCase => {
                const isValid = /^[a-zA-Z0-9_-]{1,50}$/.test(testCase.input);
                expect(isValid).toBe(testCase.expected);
            });
        });

        it('should validate email formats', function() {
            const validEmails = [
                'test@example.com',
                'user.name@domain.co.uk',
                'user+tag@example.org'
            ];

            const invalidEmails = [
                'notanemail',
                '@example.com',
                'user@',
                'user@@example.com',
                ''
            ];

            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

            validEmails.forEach(email => {
                expect(emailRegex.test(email)).toBe(true);
            });

            invalidEmails.forEach(email => {
                expect(emailRegex.test(email)).toBe(false);
            });
        });

        it('should validate lap time formats', function() {
            const validTimes = [
                '1:23.456',
                '2:15.789',
                '59.123',
                '1:00.000'
            ];

            const invalidTimes = [
                'invalid',
                '1:23:45',
                '-1:23.456',
                '1:23.45a'
            ];

            const timeRegex = /^(\d+:)?\d{1,2}\.\d{3}$/;

            validTimes.forEach(time => {
                expect(timeRegex.test(time)).toBe(true);
            });

            invalidTimes.forEach(time => {
                expect(timeRegex.test(time)).toBe(false);
            });
        });
    });

    describe('Data Type Validation', function() {
        
        it('should validate speed values', function() {
            const validSpeeds = [
                '180.5',
                '200',
                '0.0',
                '350.75'
            ];

            const invalidSpeeds = [
                'fast',
                '-100',
                '180.5.5',
                ''
            ];

            validSpeeds.forEach(speed => {
                const numSpeed = parseFloat(speed);
                expect(numSpeed).not.toBeNaN();
                expect(numSpeed).toBeGreaterThanOrEqual(0);
            });

            invalidSpeeds.forEach(speed => {
                const numSpeed = parseFloat(speed);
                expect(numSpeed < 0 || isNaN(numSpeed)).toBe(true);
            });
        });
    });
});
