# Scripts Directory

This directory contains various setup scripts and utilities for the SuperLap project.

## Available Scripts and Tools

### 🎬 Act - GitHub Actions Local Runner
- **Script**: [`setup-act.sh`](./setup-act.sh)
- **Documentation**: [ACT.md](./ACT.md)
- **Description**: Setup script to download and install [nektos/act](https://github.com/nektos/act) for running GitHub Actions workflows locally
- **Usage**: `./scripts/setup-act.sh`

### Docker - Containerization Platform
- [**Build & Run**](#build-and-run-docker)

## Quick Start

To get started with any tool, simply run the corresponding setup script:

```bash
# Install Act for local GitHub Actions testing
./scripts/setup-act.sh
```

## Adding New Scripts

When adding new scripts to this directory, please:

1. **Create the script** with appropriate permissions (`chmod +x`)
2. **Add documentation** (either inline comments or separate `.md` file)
3. **Update this README** to include the new script
4. **Follow naming conventions**: `setup-<tool-name>.sh` for setup scripts

## Script Structure

Each setup script should:
- ✅ Include error handling (`set -e`)
- ✅ Provide colored output for better UX
- ✅ Verify installation after setup
- ✅ Show usage instructions
- ✅ Support cross-platform installation where possible

## Documentation Format

For complex tools, create a separate documentation file following this pattern:
- `TOOL_NAME.md` - Detailed documentation
- Include quick start, features, usage examples, and links to official docs

## Contributing

When contributing new scripts:
1. Test on multiple platforms if applicable
2. Include comprehensive error handling
3. Provide clear documentation
4. Update this index README

---

**Project**: SuperLap  
**Purpose**: Developer tooling and automation scripts

---

## Build and Run Docker

Unity can not be containerized, so that will not be run by Docker. But the backend (API and image processing) will be run by Docker.
Also this system doesn't need to use Docker, you can continue to code as normal without it, but please test once you have finished your code that it still runs with Docker.

Steps to run the system with Docker:
1. Ensure you have Docker installed on your machine.
2. Start up Docker.desktop or Docker service.
3. Open the terminal in the root directory of the project.
4. Run the following commands to build and run the Docker containers:

```bash
xhost +local:docker # For WSL and Linux users
set DISPLAY=host.docker.internal:0.0 # For Windows users

xhost +local:docker
docker-compose down --remove-orphans
docker-compose up --build
```

you could also run:

```bash
xhost +local:docker # For WSL and Linux users
set DISPLAY=host.docker.internal:0.0 # For Windows users

docker-compose down --remove-orphans
docker-compose build
docker-compose up
```

If you have run into **issues** try the following:

```bash
docker-compose down --remove-orphans
docker-compose build --no-cache
docker-compose up
```

Run the tests:
1. Run the docker.
2. Run the following commands in the terminal:
**Note:** To run a specific test use `npm test -- -t "test name"` to run individual tests.
**For example:**

```bash
npm --prefix api test -- dbRouter.test.ts
```

otherwise run all tests:

```bash
npm test
```

And to run the tests in each service:

```bash
npm --prefix api test
npm --prefix image-processing test
```
