# Scripts Directory

This directory contains various utility scripts for the FeeNominal service.

## Available Script Categories

### [Admin Secret Management](./AdminSecretManagement/)

Scripts for managing admin API key secrets in AWS Secrets Manager for production deployment.

**Contents:**
- `Setup-AdminSecret.ps1` - PowerShell script for Windows environments
- `setup-admin-secret.sh` - Bash script for Linux/macOS environments
- `README.md` - Complete documentation and usage guide

**Purpose:**
- Create admin secrets in AWS Secrets Manager
- Generate secure random secrets for admin API key operations
- Support production deployment workflows

**Quick Start:**
```bash
# Navigate to the AdminSecretManagement directory
cd AdminSecretManagement

# Test with dry run (PowerShell)
.\Setup-AdminSecret.ps1 -DryRun

# Test with dry run (Bash)
./setup-admin-secret.sh -d
```

For detailed documentation and usage examples, see the [AdminSecretManagement README](./AdminSecretManagement/README.md).

---

## Future Script Categories

This directory is organized to accommodate additional script categories as needed:

- **Database Management** - Migration and seeding scripts
- **Deployment** - CI/CD and deployment automation
- **Monitoring** - Health checks and monitoring scripts
- **Testing** - Test data generation and validation scripts

## Contributing

When adding new scripts:

1. Create a dedicated subdirectory for the script category
2. Include appropriate documentation (README.md)
3. Follow the existing naming conventions
4. Add cross-platform support where possible (PowerShell + Bash)
5. Include proper error handling and validation 