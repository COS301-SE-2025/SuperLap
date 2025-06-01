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
just to test
# Creation:
Ensure you are in daemon
`cd daemon`

For development:
`npm run dev`

For production:
`npm start`

To run tests:
`npm test`

To build and run with Docker:
`npm run docker:build`
`npm run docker:run`

Or using Docker Compose:
`npm run docker:compose`

# Trouble shooting:

Error:
`Failed to start server: Error: listen EADDRINUSE: address already in use :::5000`

use 
`sudo lsof -i :5000`

example output:
`COMMAND   PID      USER   FD   TYPE DEVICE SIZE/OFF NODE NAME`
`node    10281 u21457752   29u  IPv6 170650      0t0  TCP *:5000 (LISTEN)`

then use the PID number
`kill -9 PID`

in the example above it would be
`kill -9 10281`

Try again.

# Testing

- Server Test (index.js and server.js)  **
- Storage Test (storageService.js)      **
- Collection Test (Collection.js)       
- Database Test (Database.js)           **
- Table Test (Table.js)                 
- User Test (User.js)                   
- Authentication Tests (authRoutes.js and authService.js) *
- Database Route Test (dbRoutes.js)     **

# Note
- I have used the singleton pattern with storage creation - to prevent overlapping issues, but this can cause funny issues with testing, please be aware.

please note that if you have never run these tests then you will need to uncomment the one test underserver.test.js and then comment it out again once you are done. if you don't it will say 1 test fails. i can't figure out how to fix this

note that there are too many tests to run npm test. use npm test -- -t "test name" to run individual tests.


docker-compose down --remove-orphans
docker-compose build --no-cache
docker-compose up
docker-compose up --build
npm --prefix daemon test

npm --prefix api test -- dbRouter.test.ts
npm --prefix api test -- db.test.ts
