FROM python:3.10-slim

WORKDIR /app

# Install GUI and Qt dependencies (no qt5-default)
RUN apt-get update && apt-get install -y \
    libgl1 \
    libglib2.0-0 \
    libsm6 \
    libxext6 \
    libxrender-dev \
    libxkbcommon-x11-0 \
    libxcb-xinerama0 \
    libxcb-randr0 \
    libxcb-shape0 \
    libxcb-xfixes0 \
    libxcb1 \
    libx11-xcb1 \
    libqt5widgets5 \
    libqt5gui5 \
    libqt5core5a \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

CMD ["python", "TrackProcessor.py"]
