FROM python:3.10-slim

# Set working directory
WORKDIR /app

# Install system dependencies for OpenCV + Qt GUI
RUN apt-get update && apt-get install -y \
    libgl1 \
    libglib2.0-0 \
    libx11-xcb1 \
    libxext6 \
    libxrender1 \
    libqt5widgets5 \
    libxcb-xinerama0 \
    && rm -rf /var/lib/apt/lists/*

# Install Python dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy app files
COPY . .

# Default command
CMD ["python", "TrackProcessor.py"]