# Backend/API/Dockerfile

FROM node:18

# Create app directory
WORKDIR /app

# Copy package.json and install dependencies
COPY package.json ./
RUN npm install

# Copy the rest of the source code
COPY . .

# Expose the port your app runs on
EXPOSE 3000

# Run the app
CMD ["npm", "start"]