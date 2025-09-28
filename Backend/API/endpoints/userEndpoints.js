const express = require('express');
const bcrypt = require('bcrypt');
const { ObjectId } = require('mongodb');

module.exports = function(db) {
  const router = express.Router();

  async function hashPassword(password) {
    const saltRounds = 10;
    return await bcrypt.hash(password, saltRounds);
  }

  async function comparePassword(password, hashedPassword) {
    return await bcrypt.compare(password, hashedPassword);
  }

  // USER ROUTES

  // Fetch all users
  router.get('/users', async (req, res) => {
    try {
      const users = await db.collection("users").find({}, { projection: { passwordHash: 0 } }).toArray();
      res.status(200).json(users);
    } catch (error) {
      console.error("GET error:", error);
      res.status(500).json({message: "Failed to fetch users"});
    }
  });

  // Fetch a single user by username
  router.get('/users/:username', async (req, res) => {
    try {
      const username = req.params.username;
      const user = await db.collection("users").findOne(
        {username: username}, 
        { projection: { passwordHash: 0 } }
      );
      
      if (!user) {
        return res.status(404).json({ message: "User not found" });
      }
      
      res.status(200).json(user);
    } catch (error) {
      console.error("GET error:", error);
      res.status(500).json({message: "Failed to fetch user"});
    }
  });

  // Login a user
  router.post('/users/login', async (req, res) => {
    const { username, password } = req.body;

    if (!username || !password) {
      return res.status(400).json({ message: "Username and password are required" });
    }

    try {
      const user = await db.collection("users").findOne({ username: username });
      if (!user) {
        return res.status(401).json({ message: 'Invalid credentials' });
      }
      
      const isPasswordValid = await comparePassword(password, user.passwordHash);
      if (!isPasswordValid) {
        return res.status(401).json({ message: 'Invalid credentials' });
      }
      
      // Exclude passwordHash from the response
      const { passwordHash, ...userWithoutPassword } = user;
      res.status(200).json(userWithoutPassword);
    } catch (error) {
      console.error('Login error:', error);
      res.status(500).json({ message: 'Failed to login user' });
    }
  });

  // Update a single user
  router.put('/users/:username', async (req, res) => {
    const username = req.params.username;
    const updatedData = req.body;

    try {
      // Don't allow password updates through this endpoint
      if (updatedData.password) {
        delete updatedData.password;
      }
      if (updatedData.passwordHash) {
        delete updatedData.passwordHash;
      }

      const result = await db.collection('users').updateOne(
        { username: username }, 
        { $set: updatedData }
      );

      if (result.matchedCount === 0) {
        return res.status(404).json({ message: 'User not found' });
      }

      res.status(200).json({ message: 'User updated successfully' });
    } catch (error) {
      console.error('Update error:', error);
      res.status(500).json({ message: 'Failed to update user' });
    }
  });

  // Create a single user
  router.post('/users', async (req, res) => {
    try {
      let { username, email, password } = req.body;
      
      // Check if the user already exists
      const existingUser = await db.collection("users").findOne({ username });
      if (existingUser) {
        return res.status(400).json({ message: "Username already taken" });
      }

      const passwordHash = await hashPassword(password);
      const createdAt = new Date().toISOString();

      const newUser = { 
        username, 
        email, 
        passwordHash, 
        createdAt,
        updatedAt: createdAt
      };

      await db.collection("users").insertOne(newUser);

      res.status(201).json({
        message: "User created successfully"
      });
    } catch (error) {
      console.error("Create error:", error);
      res.status(500).json({ message: "Error creating user" });
    }
  });

  // Delete a single user
  router.delete('/users/:username', async (req, res) => {
    try {
      const username = req.params.username;
      const result = await db.collection("users").deleteOne({username: username});
      
      if (result.deletedCount === 0) {
        return res.status(404).json({ message: "User not found" });
      }
      
      res.status(200).json({message: "User deleted successfully"});
    } catch (error) {
      console.error(error);
      res.status(500).json({message: "Failed to delete user"});
    }
  });

  return router;
}