# Backend/ImageProcessing/Dockerfile
FROM python:3.10-slim

# Install system dependencies required by OpenCV and Qt
RUN apt-get update && apt-get install -y \
    libgl1 \
    libglib2.0-0 \
    libxcb-icccm4 \
    libxcb-image0 \
    libxcb-keysyms1 \
    libxcb-randr0 \
    libxcb-render-util0 \
    libxcb-shape0 \
    libxcb-xfixes0 \
    libxcb-xinerama0 \
    libxcb-xkb1 \
    xkb-data \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Install Python dependencies
COPY requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

# Copy source files
COPY . .

# Run the main script
CMD ["python", "TrackProcessor.py"]