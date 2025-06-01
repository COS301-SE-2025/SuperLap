# Backend/ImageProcessing/Dockerfile
FROM python:3.10-slim

# Set working directory
WORKDIR /app

# Copy requirements and install dependencies first (layer caching)
COPY requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

# Copy the rest of the source code
COPY . .

# Default command to run the script
CMD ["python", "TrackProcessor.py"]
