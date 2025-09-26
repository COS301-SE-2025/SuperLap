const request = require('supertest');
const { app } = require('../../app');

/**
 * Test data generators and utilities
 */
class TestHelpers {
    /**
     * Generate test user data
     * @param {number} index - Index for unique naming
     * @returns {Object} User object
     */
    static generateTestUser(index = 0) {
        return {
        username: `testuser${index}`,
        email: `testuser${index}@example.com`,
        password: `testpassword${index}`
        };
    }

    /**
     * Generate test track data
     * @param {number} index - Index for unique naming
     * @param {string} uploadedBy - User who uploaded the track
     * @returns {Object} Track object
     */
    static generateTestTrack(index = 0, uploadedBy = 'testuser') {
        const tracks = [
        { name: `test-silverstone-${index}`, city: 'Silverstone', country: 'United Kingdom' },
        { name: `test-monza-${index}`, city: 'Monza', country: 'Italy' },
        { name: `test-spa-${index}`, city: 'Spa-Francorchamps', country: 'Belgium' },
        { name: `test-suzuka-${index}`, city: 'Suzuka', country: 'Japan' },
        { name: `test-monaco-${index}`, city: 'Monaco', country: 'Monaco' }
        ];

        const baseTrack = tracks[index % tracks.length];
        
        return {
        ...baseTrack,
        type: 'circuit',
        location: baseTrack.city,
        uploadedBy,
        image: this.generateBase64Image(),
        description: `Test track ${index} - ${baseTrack.name}`
        };
    }

    /**
     * Generate test racing data
     * @param {string} trackName - Name of the track
     * @param {string} userName - Name of the user
     * @param {number} index - Index for unique data
     * @returns {Object} Racing data object
     */
    static generateTestRacingData(trackName, userName, index = 0) {
        return {
        trackName,
        userName,
        lapTime: `1:${23 + (index % 10)}.${String(index * 123).slice(-3)}`,
        vehicleUsed: `Test Car ${index % 5}`,
        description: `Test racing session ${index}`,
        fileName: `test-data-${index}.csv`,
        csvData: this.generateTestCsvBase64(index)
        };
    }

    /**
     * Generate test CSV data in base64 format
     * @param {number} laps - Number of laps to generate
     * @returns {string} Base64 encoded CSV data
     */
    static generateTestCsvBase64(laps = 10) {
        const headers = 'lap,time,speed,position,sector1,sector2,sector3';
        const rows = Array(laps).fill(0).map((_, i) => {
        const lap = i + 1;
        const minutes = 1;
        const seconds = 23 + (i % 10);
        const milliseconds = String(456 + (i * 17)).slice(-3);
        const speed = 180 + (i % 20);
        const sector1 = `0:${20 + (i % 5)}.${String(123 + i).slice(-3)}`;
        const sector2 = `0:${25 + (i % 7)}.${String(234 + i).slice(-3)}`;
        const sector3 = `0:${18 + (i % 4)}.${String(345 + i).slice(-3)}`;
        
        return `${lap},${minutes}:${seconds}.${milliseconds},${speed}.${i % 10},1,${sector1},${sector2},${sector3}`;
        });
        
        const csvData = [headers, ...rows].join('\n');
        return Buffer.from(csvData).toString('base64');
    }

    /**
     * Generate a small base64 image for testing
     * @returns {string} Base64 encoded image data
     */
    static generateBase64Image() {
        return 'data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAYEBQYFBAYGBQYHBwYIChAKCgkJChQODwwQFxQYGBcUFhYaHSUfGhsjHBYWICwgIyYnKSopGR8tMC0oMCUoKSj/2wBDAQcHBwoIChMKChMoGhYaKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCj/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAv/xAAhEAACAQMDBQAAAAAAAAAAAAABAgMABAUGIWGRkqGx0f/EABUBAQEAAAAAAAAAAAAAAAAAAAMF/8QAGhEAAgIDAAAAAAAAAAAAAAAAAAECEgMRkf/aAAwDAQACEQMRAD8A0XqoJHUddfPw==';
    }

    /**
     * Create a test user and return the response
     * @param {number} index - Index for unique naming
     * @returns {Promise<Object>} Request response
     */
    static async createTestUser(index = 0) {
        const userData = this.generateTestUser(index);
        const response = await request(app)
        .post('/users')
        .send(userData);
        return { response, userData };
    }

    /**
     * Create a test track and return the response
     * @param {number} index - Index for unique naming
     * @param {string} uploadedBy - User who uploaded the track
     * @returns {Promise<Object>} Request response
     */
    static async createTestTrack(index = 0, uploadedBy = 'testuser') {
        const trackData = this.generateTestTrack(index, uploadedBy);
        const response = await request(app)
        .post('/tracks')
        .send(trackData);
        return { response, trackData };
    }

    /**
     * Create test racing data and return the response
     * @param {string} trackName - Name of the track
     * @param {string} userName - Name of the user
     * @param {number} index - Index for unique data
     * @returns {Promise<Object>} Request response
     */
    static async createTestRacingData(trackName, userName, index = 0) {
        const racingData = this.generateTestRacingData(trackName, userName, index);
        const response = await request(app)
        .post('/racing-data')
        .send(racingData);
        return { response, racingData };
    }

    /**
     * Setup complete test environment with user, track, and racing data
     * @param {number} index - Index for unique naming
     * @returns {Promise<Object>} All created test data
     */
    static async setupTestEnvironment(index = 0) {
        const { response: userResponse, userData } = await this.createTestUser(index);
        if (userResponse.status !== 201) {
        throw new Error(`Failed to create test user: ${userResponse.body.message}`);
        }

        const { response: trackResponse, trackData } = await this.createTestTrack(index, userData.username);
        if (trackResponse.status !== 201) {
        throw new Error(`Failed to create test track: ${trackResponse.body.message}`);
        }

        const { response: racingDataResponse, racingData } = await this.createTestRacingData(
        trackData.name, 
        userData.username, 
        index
        );
        if (racingDataResponse.status !== 201) {
        throw new Error(`Failed to create test racing data: ${racingDataResponse.body.message}`);
        }

        return {
        user: { response: userResponse, data: userData },
        track: { response: trackResponse, data: trackData },
        racingData: { 
            response: racingDataResponse, 
            data: racingData,
            id: racingDataResponse.body.data._id
        }
        };
    }

    /**
     * Clean up test data
     * @param {Object} testData - Test data from setupTestEnvironment
     */
    static async cleanupTestEnvironment(testData) {
        const cleanupPromises = [];

        if (testData.racingData && testData.racingData.id) {
        cleanupPromises.push(
            request(app).delete(`/racing-data/${testData.racingData.id}`)
        );
        }

        if (testData.track && testData.track.data.name) {
        cleanupPromises.push(
            request(app).delete(`/tracks/${testData.track.data.name}`)
        );
        }

        if (testData.user && testData.user.data.username) {
        cleanupPromises.push(
            request(app).delete(`/users/${testData.user.data.username}`)
        );
        }

        await Promise.allSettled(cleanupPromises);
    }

    /**
     * Wait for a specified amount of time
     * @param {number} ms - Milliseconds to wait
     * @returns {Promise<void>}
     */
    static async wait(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    /**
     * Generate random string for testing
     * @param {number} length - Length of the string
     * @returns {string} Random string
     */
    static generateRandomString(length = 10) {
        const chars = 'abcdefghijklmnopqrstuvwxyz0123456789';
        let result = '';
        for (let i = 0; i < length; i++) {
        result += chars.charAt(Math.floor(Math.random() * chars.length));
        }
        return result;
    }

    /**
     * Generate invalid data for negative testing
     * @param {string} type - Type of invalid data to generate
     * @returns {Object} Invalid data object
     */
    static generateInvalidData(type) {
        switch (type) {
        case 'user':
            return {
            username: '', // Empty username
            email: 'invalid-email', // Invalid email format
            password: '123' // Too short password
            };
        case 'track':
            return {
            name: '', // Empty name
            type: 'invalid-type',
            city: null,
            country: undefined
            };
        case 'racingData':
            return {
            trackName: '',
            userName: '',
            lapTime: 'invalid-time',
            csvData: 'not-base64!'
            };
        default:
            return {};
        }
    }

    /**
     * Validate response structure
     * @param {Object} response - HTTP response
     * @param {Object} expectedStructure - Expected response structure
     * @returns {boolean} Whether response matches expected structure
     */
    static validateResponseStructure(response, expectedStructure) {
        const validateObject = (obj, structure) => {
        for (const key in structure) {
            if (!obj.hasOwnProperty(key)) {
            return false;
            }
            
            if (typeof structure[key] === 'object' && structure[key] !== null) {
            if (!validateObject(obj[key], structure[key])) {
                return false;
            }
            } else if (typeof obj[key] !== structure[key]) {
            return false;
            }
        }
        return true;
        };

        return validateObject(response.body, expectedStructure);
    }

    /**
     * Create multiple test entities
     * @param {string} type - Type of entity (user, track, racingData)
     * @param {number} count - Number of entities to create
     * @param {Object} options - Additional options
     * @returns {Promise<Array>} Array of created entities
     */
    static async createMultipleTestEntities(type, count, options = {}) {
        const entities = [];
        
        for (let i = 0; i < count; i++) {
        let result;
        
        switch (type) {
            case 'user':
            result = await this.createTestUser(options.startIndex ? options.startIndex + i : i);
            break;
            case 'track':
            result = await this.createTestTrack(
                options.startIndex ? options.startIndex + i : i, 
                options.uploadedBy || 'testuser'
            );
            break;
            case 'racingData':
            result = await this.createTestRacingData(
                options.trackName || 'test-track',
                options.userName || 'testuser',
                options.startIndex ? options.startIndex + i : i
            );
            break;
            default:
            throw new Error(`Unsupported entity type: ${type}`);
        }
        
        entities.push(result);
        }
        
        return entities;
    }

    /**
     * Performance measurement utilities
     */
    static startPerformanceTimer() {
        return {
        startTime: Date.now(),
        startMemory: process.memoryUsage()
        };
    }

    static endPerformanceTimer(timer) {
        const endTime = Date.now();
        const endMemory = process.memoryUsage();
        
        return {
        duration: endTime - timer.startTime,
        memoryDelta: {
            heapUsed: endMemory.heapUsed - timer.startMemory.heapUsed,
            heapTotal: endMemory.heapTotal - timer.startMemory.heapTotal,
            external: endMemory.external - timer.startMemory.external,
            rss: endMemory.rss - timer.startMemory.rss
        }
        };
    }

    /**
     * Database utilities
     */
    static async getDbStats(db) {
        const collections = ['users', 'tracks', 'racingData'];
        const stats = {};
        
        for (const collection of collections) {
        const count = await db.collection(collection).countDocuments();
        const size = await db.collection(collection).stats().catch(() => ({ size: 0 }));
        stats[collection] = { count, size: size.size || 0 };
        }
        
        return stats;
    }

    /**
     * Error simulation utilities
     */
    static simulateNetworkError() {
        const error = new Error('Network Error');
        error.code = 'ECONNREFUSED';
        return error;
    }

    static simulateTimeoutError() {
        const error = new Error('Timeout Error');
        error.code = 'ETIMEDOUT';
        return error;
    }

    static simulateDatabaseError() {
        const error = new Error('Database Error');
        error.name = 'MongoNetworkError';
        return error;
    }
}

module.exports = TestHelpers;