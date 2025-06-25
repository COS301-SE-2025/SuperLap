const express = require('express');
const { MongoClient } = require('mongodb');
require('dotenv').config();
const { setupSwagger } = require('../swagger');

const app = express();
// Swagger setup
setupSwagger(app);

app.use(express.json());
const uri = process.env.MONGO_URI;
let db;
let client; // this will be used to close the connection later

async function connectToDb() {
    try {
        client = await MongoClient.connect(uri);
        db = client.db("Superlap");
        app.locals.db = db;
        console.log("Connected to MongoDB");
        // Test the connection
        await db.command({ ping: 1 });
        console.log("Database ping successful");
    } catch (error) {
        console.error("Connection error:", error);
        process.exit(1); // Exit if we can't connect
    }
}

async function closeDbConnection() {
    if (client) {
        await client.close();
        console.log("MongoDB connection closed");
    }
}

// Connect to DB when starting the app
connectToDb().catch(console.error);

// Graceful shutdown
process.on('SIGINT', async () => {
    await closeDbConnection();
    process.exit();
});

// Default route
app.get('/', async (req, res) => {
    try {
        res.json({ message: "Hello from Express!" });
    } catch (error) {
        console.log("GET error:", error);
        res.status(500).json({ message: "Failed to fetch default route" });
    }
});

// USER ROUTES WITH SAFETY CHECKS

// Fetch all users (with projection to limit sensitive data)
app.get('/users', async (req, res) => {
    try {
        // Using projection to only return specific fields
        const users = await db.collection("users").find({}, {
            projection: {
                username: 1,
                email: 1,
                _id: 0 // Exclude MongoDB _id by default
            }
        }).limit(100).toArray(); // Added limit for safety
        res.json(users);
    } catch (error) {
        console.error("GET error:", error);
        res.status(500).json({ message: "Failed to fetch users" });
    }
});

// Fetch a single user by username
app.get('/users/:username', async (req, res) => {
    try {
        const username = req.params.username;
        // Using projection to limit returned data
        const user = await db.collection("users").findOne(
            { username: username },
            { projection: { password: 0 } } // Exclude sensitive fields
        );

        if (!user) {
            return res.status(404).json({ message: "User not found" });
        }

        res.json(user);
    } catch (error) {
        console.error("GET error:", error);
        res.status(500).json({ message: "Failed to fetch user" });
    }
});

// Update a single user (with validation)
app.put('/users/:username', async (req, res) => {
    const username = req.params.username;
    const updatedData = req.body;

    // Safety check - prevent updating all documents
    if (!username) {
        return res.status(400).json({ message: "Username is required" });
    }

    // Prevent updating sensitive/immutable fields
    if (updatedData._id || updatedData.username) {
        return res.status(400).json({ message: "Cannot update protected fields" });
    }

    try {
        const result = await db.collection('users').updateOne(
            { username: username },
            { $set: updatedData }
        );

        if (result.matchedCount === 0) {
            return res.status(404).json({ message: 'User not found' });
        }

        res.json({
            message: 'User updated successfully',
            modifiedCount: result.modifiedCount
        });
    } catch (error) {
        console.error('Update error:', error);
        res.status(500).json({ message: 'Failed to update user' });
    }
});

// Create a single user (with validation)
app.post('/users', async (req, res) => {
    try {
        const newUser = req.body;

        // Required fields check
        if (!newUser.username || !newUser.email) {
            return res.status(400).json({ message: "Username and email are required" });
        }

        // Check if the user already exists
        const existingUser = await db.collection("users").findOne({
            $or: [
                { username: newUser.username },
                { email: newUser.email }
            ]
        });

        if (existingUser) {
            return res.status(400).json({
                message: "User already exists",
                conflict: existingUser.username === newUser.username ? "username" : "email"
            });
        }

        // Add creation timestamp
        newUser.createdAt = new Date();

        // Insert the new user into the database
        const result = await db.collection("users").insertOne(newUser);

        res.status(201).json({
            message: "User created successfully",
            insertedId: result.insertedId
        });
    } catch (error) {
        console.error("Create error:", error);
        res.status(500).json({ message: "Error creating user" });
    }
});

// Delete a single user (with confirmation)
app.delete('/users/:username', async (req, res) => {
    try {
        const username = req.params.username;

        // Safety check - require confirmation in body
        if (!req.body.confirm || req.body.confirm !== "YES_DELETE") {
            return res.status(400).json({
                message: "Confirmation required. Send { confirm: 'YES_DELETE' } in request body"
            });
        }

        const result = await db.collection("users").deleteOne({ username: username });

        if (result.deletedCount === 0) {
            return res.status(404).json({ message: "User not found" });
        }

        res.json({
            message: "User deleted successfully",
            deletedCount: result.deletedCount
        });
    } catch (error) {
        console.error(error);
        res.status(500).json({ message: "Failed to delete user" });
    }
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
    console.log(`Server running on port ${PORT}`);
});