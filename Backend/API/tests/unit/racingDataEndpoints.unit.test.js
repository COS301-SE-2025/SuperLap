describe('Racing Data Endpoints Unit Tests', function() {
    describe('CSV Data Processing', function() {
        it('should encode and decode base64 correctly', function() {
            const csvData = 'lap,time,speed\n1,1:23.456,180.5\n2,1:24.123,179.8';
            const base64Encoded = Buffer.from(csvData).toString('base64');
            const base64Decoded = Buffer.from(base64Encoded, 'base64').toString();
            
            expect(base64Encoded).toBeDefined();
            expect(base64Encoded).not.toBe(csvData);
            expect(base64Decoded).toBe(csvData);
        });

        it('should calculate file size correctly', function() {
            const csvData = 'lap,time,speed\n1,1:23.456,180.5';
            const base64Encoded = Buffer.from(csvData).toString('base64');
            const calculatedSize = Buffer.from(base64Encoded, 'base64').length;
            
            expect(calculatedSize).toBe(csvData.length);
        });
    });
});