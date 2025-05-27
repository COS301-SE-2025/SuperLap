#!/bin/bash

# Setup script for nektos/act - Run GitHub Actions locally
# This script downloads and installs act to the ./act directory

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
REPO="nektos/act"
INSTALL_DIR="./act"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

echo -e "${BLUE}🚀 Setting up nektos/act - Run GitHub Actions locally${NC}"
echo "=================================================="

# Function to detect OS and architecture
detect_platform() {
    local os arch
    
    case "$(uname -s)" in
        Linux*)     os="Linux";;
        Darwin*)    os="Darwin";;
        CYGWIN*|MINGW*|MSYS*) os="Windows";;
        *)          echo -e "${RED}❌ Unsupported OS: $(uname -s)${NC}"; exit 1;;
    esac
    
    case "$(uname -m)" in
        x86_64|amd64)   arch="x86_64";;
        arm64|aarch64)  arch="arm64";;
        armv7l)         arch="armv7";;
        i386|i686)      arch="i386";;
        *)              echo -e "${RED}❌ Unsupported architecture: $(uname -m)${NC}"; exit 1;;
    esac
    
    echo "${os}_${arch}"
}

# Function to get latest release version
get_latest_version() {
    echo -e "${YELLOW}🔍 Fetching latest release version...${NC}" >&2
    local version
    version=$(curl -s "https://api.github.com/repos/${REPO}/releases/latest" | grep '"tag_name":' | sed -E 's/.*"([^"]+)".*/\1/')
    
    if [ -z "$version" ]; then
        echo -e "${RED}❌ Failed to fetch latest version${NC}" >&2
        exit 1
    fi
    
    echo -e "${GREEN}✅ Latest version: ${version}${NC}" >&2
    echo "$version"
}

# Function to download and install act
install_act() {
    local version="$1"
    local platform="$2"
    local download_url filename
    
    # Construct download URL based on platform
    case "$platform" in
        Linux_x86_64)
            filename="act_${platform}.tar.gz"
            ;;
        Linux_arm64)
            filename="act_${platform}.tar.gz"
            ;;
        Linux_armv7)
            filename="act_${platform}.tar.gz"
            ;;
        Linux_i386)
            filename="act_${platform}.tar.gz"
            ;;
        Darwin_x86_64)
            filename="act_${platform}.tar.gz"
            ;;
        Darwin_arm64)
            filename="act_${platform}.tar.gz"
            ;;
        Windows_x86_64)
            filename="act_${platform}.zip"
            ;;
        Windows_arm64)
            filename="act_${platform}.zip"
            ;;
        Windows_i386)
            filename="act_${platform}.zip"
            ;;
        *)
            echo -e "${RED}❌ No binary available for platform: $platform${NC}"
            exit 1
            ;;
    esac
    
    download_url="https://github.com/${REPO}/releases/download/${version}/${filename}"
    
    echo -e "${YELLOW}📦 Downloading act ${version} for ${platform}...${NC}"
    echo "URL: $download_url"
    
    # Create install directory
    cd "$PROJECT_ROOT"
    mkdir -p "$INSTALL_DIR"
    cd "$INSTALL_DIR"
    
    # Download the file
    if ! curl -L -o "$filename" "$download_url"; then
        echo -e "${RED}❌ Failed to download act${NC}"
        exit 1
    fi
    
    echo -e "${YELLOW}📂 Extracting act...${NC}"
    
    # Extract based on file type
    if [[ "$filename" == *.tar.gz ]]; then
        tar -xzf "$filename"
    elif [[ "$filename" == *.zip ]]; then
        unzip -o "$filename"
    fi
    
    # Remove archive
    rm "$filename"
    
    # Make executable (for Unix-like systems)
    if [[ "$platform" != Windows_* ]]; then
        chmod +x act
    fi
    
    echo -e "${GREEN}✅ act installed successfully to ${INSTALL_DIR}${NC}"
}

# Function to verify installation
verify_installation() {
    cd "$PROJECT_ROOT/$INSTALL_DIR"
    
    if [[ -f "act" ]] || [[ -f "act.exe" ]]; then
        echo -e "${YELLOW}🔍 Verifying installation...${NC}"
        
        local act_binary="./act"
        if [[ -f "act.exe" ]]; then
            act_binary="./act.exe"
        fi
        
        if $act_binary --version; then
            echo -e "${GREEN}✅ act is working correctly!${NC}"
        else
            echo -e "${YELLOW}⚠️  act binary found but version check failed${NC}"
        fi
    else
        echo -e "${RED}❌ act binary not found after installation${NC}"
        exit 1
    fi
}

# Function to show usage instructions
show_usage() {
    echo ""
    echo -e "${BLUE}📖 Usage Instructions:${NC}"
    echo "======================"
    echo ""
    echo "To use act, you can:"
    echo ""
    echo "1. Run from the act directory:"
    echo "   cd ./act && ./act"
    echo ""
    echo "2. Add to your PATH (recommended):"
    echo "   export PATH=\"\$(pwd)/act:\$PATH\""
    echo "   # Add this line to your ~/.bashrc or ~/.zshrc for permanent access"
    echo ""
    echo "3. Create a symbolic link:"
    echo "   sudo ln -sf \$(pwd)/act/act /usr/local/bin/act"
    echo ""
    echo -e "${YELLOW}💡 Quick Start:${NC}"
    echo "   cd ./act"
    echo "   ./act --help                    # Show help"
    echo "   ./act -l                        # List available workflows"
    echo "   ./act                           # Run all workflows"
    echo "   ./act push                      # Run 'push' event workflows"
    echo ""
    echo -e "${BLUE}📚 Documentation:${NC}"
    echo "   Visit: https://nektosact.com/"
    echo "   GitHub: https://github.com/nektos/act"
}

# Main execution
main() {
    echo -e "${YELLOW}🔧 Detecting platform...${NC}"
    local platform
    platform=$(detect_platform)
    echo -e "${GREEN}✅ Platform detected: ${platform}${NC}"
    
    local version
    version=$(get_latest_version)
    
    echo ""
    echo -e "${YELLOW}⬇️  Installing act...${NC}"
    install_act "$version" "$platform"
    
    echo ""
    verify_installation
    
    echo ""
    show_usage
    
    echo ""
    echo -e "${GREEN}🎉 Setup complete! Happy GitHub Actions testing! 🎉${NC}"
}

# Check if curl is available
if ! command -v curl &> /dev/null; then
    echo -e "${RED}❌ curl is required but not installed. Please install curl first.${NC}"
    exit 1
fi

# Check if we're in a git repository (optional check)
if [[ ! -d "$PROJECT_ROOT/.git" ]]; then
    echo -e "${YELLOW}⚠️  Warning: Not in a git repository. act works best in projects with GitHub Actions workflows.${NC}"
    echo "Continue anyway? (y/N)"
    read -r response
    if [[ ! "$response" =~ ^[Yy]$ ]]; then
        echo "Setup cancelled."
        exit 0
    fi
fi

# Run main function
main "$@"
