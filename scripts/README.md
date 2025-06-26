# Scripts Directory

This directory contains various setup scripts and utilities for the SuperLap project.

## Available Scripts and Tools

### 🎬 Act - GitHub Actions Local Runner
- **Script**: [`setup-act.sh`](./setup-act.sh)
- **Documentation**: [ACT.md](./ACT.md)
- **Description**: Setup script to download and install [nektos/act](https://github.com/nektos/act) for running GitHub Actions workflows locally
- **Usage**: `./scripts/setup-act.sh`

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
