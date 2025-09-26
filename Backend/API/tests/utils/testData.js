/**
 * Test data constants and fixtures
 */
module.exports = {
    // Valid test data
    validUser: {
        username: 'validtestuser',
        email: 'valid@example.com',
        password: 'validpassword123'
    },

    validTrack: {
        name: 'valid-test-track',
        type: 'circuit',
        city: 'Test City',
        country: 'Test Country',
        location: 'Test Location',
        uploadedBy: 'validtestuser',
        description: 'A valid test track'
    },

    validRacingData: {
        trackName: 'valid-test-track',
        userName: 'validtestuser',
        lapTime: '1:23.456',
        vehicleUsed: 'Test Car',
        description: 'Valid test session'
    },

    // Invalid test data for negative testing
    invalidUsers: [
        { username: '', email: 'test@example.com', password: 'password123' }, // Empty username
        { username: 'test', email: '', password: 'password123' }, // Empty email
        { username: 'test', email: 'test@example.com', password: '' }, // Empty password
        { username: 'test', email: 'invalid-email', password: 'password123' }, // Invalid email
        { email: 'test@example.com', password: 'password123' }, // Missing username
        { username: 'test', password: 'password123' }, // Missing email
        { username: 'test', email: 'test@example.com' } // Missing password
    ],

    invalidTracks: [
        { type: 'circuit', city: 'Test', country: 'Test' }, // Missing name
        { name: '', type: 'circuit', city: 'Test', country: 'Test' }, // Empty name
        { name: 'test', city: 'Test', country: 'Test' }, // Missing type
        { name: 'test', type: '', city: 'Test', country: 'Test' } // Empty type
    ],

    invalidRacingData: [
        { userName: 'test', lapTime: '1:23.456' }, // Missing trackName
        { trackName: 'test', lapTime: '1:23.456' }, // Missing userName
        { trackName: 'test', userName: 'test' }, // Missing csvData
        { trackName: 'test', userName: 'test', csvData: 'invalid-base64!' } // Invalid base64
    ],

    // Sample CSV data
    sampleCsvData: {
        simple: 'lap,time\n1,1:23.456\n2,1:24.123\n3,1:23.987',
        detailed: 'lap,time,speed,position,sector1,sector2,sector3,fuel,tire_temp\n1,1:23.456,180.5,1,0:23.123,0:35.456,0:24.877,95.5,85.2\n2,1:24.123,179.8,1,0:23.567,0:35.789,0:24.767,95.2,86.1',
        large: Array(1000).fill(0).map((_, i) => 
        i === 0 ? 'lap,time,speed' : `${i},1:${23 + (i % 60)}.${String(i).padStart(3, '0')},${180 + (i % 20)}`
        ).join('\n')
    },

    // HTTP status codes for testing
    statusCodes: {
        success: {
        OK: 200,
        CREATED: 201,
        NO_CONTENT: 204
        },
        clientError: {
        BAD_REQUEST: 400,
        UNAUTHORIZED: 401,
        FORBIDDEN: 403,
        NOT_FOUND: 404,
        CONFLICT: 409,
        PAYLOAD_TOO_LARGE: 413
        },
        serverError: {
        INTERNAL_SERVER_ERROR: 500,
        BAD_GATEWAY: 502,
        SERVICE_UNAVAILABLE: 503
        }
    },

    // Expected response structures
    responseStructures: {
        user: {
        username: 'string',
        email: 'string',
        createdAt: 'string'
        },
        track: {
        name: 'string',
        type: 'string',
        city: 'string',
        country: 'string',
        dateUploaded: 'string'
        },
        racingData: {
        _id: 'string',
        trackName: 'string',
        userName: 'string',
        fileName: 'string',
        dateUploaded: 'string'
        },
        error: {
        message: 'string'
        }
    },

    // Performance benchmarks
    performanceBenchmarks: {
        singleRequest: 1000, // Max 1 second for single request
        batchRequest: 5000, // Max 5 seconds for batch requests
        largeDataUpload: 10000, // Max 10 seconds for large uploads
        complexQuery: 2000 // Max 2 seconds for complex queries
    },

    // Load testing configurations
    loadTestConfigs: {
        light: { users: 10, requests: 100, duration: 10000 },
        medium: { users: 50, requests: 500, duration: 30000 },
        heavy: { users: 100, requests: 1000, duration: 60000 }
    }
};