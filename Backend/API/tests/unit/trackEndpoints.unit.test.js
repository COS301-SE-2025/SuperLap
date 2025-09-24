describe('Track Endpoints Unit Tests', function() {
    let mockDb, mockCollection;
  
    beforeEach(function() {
        mockCollection = {
            findOne: jest.fn(),
            find: jest.fn().mockReturnValue({
                toArray: jest.fn()
            }),
            insertOne: jest.fn(),
            updateOne: jest.fn(),
            deleteOne: jest.fn()
        };
        
        mockDb = {
            collection: jest.fn().mockReturnValue(mockCollection)
        };
    });

});