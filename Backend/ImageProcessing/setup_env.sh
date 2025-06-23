#!/bin/bash

echo "🔧 Setting up Python virtual environment..."

# Check for Python
if ! command -v python3 &>/dev/null; then
  echo "❌ Python3 is not installed. Please install it first."
  exit 1
fi

# Create the virtual environment
python3 -m venv venv
echo "✅ Virtual environment created in /venv"

# Activate instructions
echo "💡 To activate it:"
echo "   On Linux/macOS: source venv/bin/activate"
echo "   On Windows:     venv\\Scripts\\activate"

# Install packages
source venv/bin/activate && pip install -r requirements.txt

echo "📦 Packages installed:"
pip freeze

echo "✅ Setup complete!"