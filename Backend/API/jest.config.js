module.exports = {
  // Use Node.js environment for testing
  testEnvironment: 'node',
  // Test match patterns
  testMatch: [
    '**/tests/**/*.test.js',
    '**/tests/**/*.spec.js',
    '**/__tests__/**/*.js'
  ],

  // Setup file to run before tests
  setupFilesAfterEnv: ['<rootDir>/tests/setup.js'],

  // Coverage configuration
  collectCoverage: true,
  coverageDirectory: '<rootDir>/coverage',
  coverageReporters: ['html', 'lcov', 'text'],
  collectCoverageFrom: [
    'app.js',
    'endpoints/*.js',
    '!**/node_modules/**',
    '!**/tests/**',
    '!**/coverage/**'
  ],

  testTimeout: 60000, // 60 seconds default timeout
  verbose: true,
  
};
