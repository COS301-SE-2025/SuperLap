# Act Setup Script

This directory contains a setup script to download and install [nektos/act](https://github.com/nektos/act) - a tool that allows you to run GitHub Actions locally.

## Quick Start

```bash
# Run the setup script
./scripts/setup-act.sh
```

## What the script does

The `setup-act.sh` script will:

1. **Detect your platform** (Linux, macOS, Windows) and architecture (x86_64, arm64, etc.)
2. **Fetch the latest release** from the GitHub API
3. **Download the appropriate binary** for your system
4. **Install act to `./act` directory** in your project root
5. **Verify the installation** by running a version check
6. **Provide usage instructions** for getting started

## Features

- ✅ **Cross-platform support** (Linux, macOS, Windows)
- ✅ **Multi-architecture support** (x86_64, arm64, armv7, i386)
- ✅ **Automatic latest version detection**
- ✅ **Colored output** for better readability
- ✅ **Error handling** with proper exit codes
- ✅ **Installation verification**
- ✅ **Usage instructions** after installation

## After Installation

Once installed, you can use act in several ways:

### Option 1: Run from the act directory
```bash
cd ./act
./act --help
./act -l          # List workflows
./act             # Run all workflows
./act push        # Run push event workflows
```

### Option 2: Add to PATH (recommended but not required)
```bash
export PATH="$(pwd)/act:$PATH"
# Add to your ~/.bashrc or ~/.zshrc for permanent access
```

### Option 3: Create a symbolic link
```bash
sudo ln -sf $(pwd)/act/act /usr/local/bin/act
```

## Requirements

- `curl` (for downloading)
- Internet connection
- Appropriate permissions to create directories and files

## About Act

Act allows you to run your GitHub Actions locally for:
- **Fast Feedback** - Test workflow changes without committing/pushing
- **Local Task Runner** - Use GitHub Actions as a replacement for Makefiles

For more information, visit:
- 📚 [Act User Guide](https://nektosact.com/)
- 🐙 [GitHub Repository](https://github.com/nektos/act)
