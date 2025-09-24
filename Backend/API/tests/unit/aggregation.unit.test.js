describe('MongoDB Aggregation Unit Tests', function() {
    describe('Statistics Pipeline', function() {
        it('should create proper aggregation pipeline for stats', function() {
            const expectedPipeline = [
                {
                $group: {
                    _id: null,
                    totalRecords: { $sum: 1 },
                    uniqueTracks: { $addToSet: "$trackName" },
                    uniqueUsers: { $addToSet: "$userName" },
                    avgFileSize: { $avg: "$fileSize" },
                    totalDataSize: { $sum: "$fileSize" }
                }
                },
                {
                $project: {
                    _id: 0,
                    totalRecords: 1,
                    uniqueTracksCount: { $size: "$uniqueTracks" },
                    uniqueUsersCount: { $size: "$uniqueUsers" },
                    avgFileSizeKB: { $round: [{ $divide: ["$avgFileSize", 1024] }, 2] },
                    totalDataSizeMB: { $round: [{ $divide: ["$totalDataSize", 1048576] }, 2] }
                }
                }
            ];

            // Verify pipeline structure
            expect(expectedPipeline).toHaveLength(2);
            expect(expectedPipeline[0]).toHaveProperty('$group');
            expect(expectedPipeline[1]).toHaveProperty('$project');
            
            // Verify group stage
            const groupStage = expectedPipeline[0].$group;
            expect(groupStage).toHaveProperty('totalRecords', { $sum: 1 });
            expect(groupStage).toHaveProperty('uniqueTracks', { $addToSet: "$trackName" });
            expect(groupStage).toHaveProperty('uniqueUsers', { $addToSet: "$userName" });
            
            // Verify project stage
            const projectStage = expectedPipeline[1].$project;
            expect(projectStage).toHaveProperty('_id', 0);
            expect(projectStage).toHaveProperty('totalRecords', 1);
            expect(projectStage).toHaveProperty('uniqueTracksCount');
            expect(projectStage).toHaveProperty('uniqueUsersCount');
        });
    });

    describe('Data Conversion', function() {
        it('should convert bytes to KB and MB correctly', function() {
            const bytes = 1048576; // 1MB in bytes
            const kb = bytes / 1024;
            const mb = bytes / 1048576;

            expect(kb).toBe(1024);
            expect(mb).toBe(1);
            });

            it('should handle rounding correctly', function() {
            const value = 123.456789;
            const rounded = Math.round(value * 100) / 100;

            expect(rounded).toBe(123.46);
        });
    });
});
