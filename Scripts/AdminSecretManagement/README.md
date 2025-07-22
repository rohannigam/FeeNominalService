# Admin Secret Setup Scripts

This directory contains scripts to set up admin API key secrets in AWS Secrets Manager for production deployment of the FeeNominal service.

## Overview

The FeeNominal service requires admin secrets to be pre-configured in AWS Secrets Manager before deployment. These secrets are used for:

- Admin API key generation
- Admin API key rotation
- Admin API key revocation

## Scripts Available

### PowerShell Script (Windows)
- **File**: `Setup-AdminSecret.ps1`
- **Usage**: Designed for Windows environments with PowerShell

### Bash Script (Linux/macOS)
- **File**: `setup-admin-secret.sh`
- **Usage**: Designed for Linux/macOS environments

## Prerequisites

1. **AWS CLI**: Must be installed and configured
   - Download from: https://aws.amazon.com/cli/
   - Configure with: `aws configure`

2. **AWS Permissions**: Your AWS credentials must have permissions to:
   - `secretsmanager:CreateSecret`
   - `secretsmanager:DescribeSecret`
   - `secretsmanager:GetSecretValue`

3. **Optional Dependencies**:
   - **jq** (for bash script): For better JSON output formatting
   - **PowerShell 5.1+** (for PowerShell script)

## Usage

### PowerShell (Windows)

```powershell
# Basic usage with default services
.\Setup-AdminSecret.ps1

# Specify custom services
.\Setup-AdminSecret.ps1 -ServiceNames @("scheduleforger", "paymentprocessor")

# Specify region and profile
.\Setup-AdminSecret.ps1 -Region "us-west-2" -Profile "production"

# Dry run to see what would be created
.\Setup-AdminSecret.ps1 -DryRun -ServiceNames @("testservice")

# Get help
.\Setup-AdminSecret.ps1 -Help
```

### Bash (Linux/macOS)

```bash
# Make script executable
chmod +x setup-admin-secret.sh

# Basic usage with default services
./setup-admin-secret.sh

# Specify custom services
./setup-admin-secret.sh -s scheduleforger,paymentprocessor

# Specify region and profile
./setup-admin-secret.sh -r us-west-2 -p production

# Dry run to see what would be created
./setup-admin-secret.sh -d -s testservice

# Get help
./setup-admin-secret.sh -h
```

## Parameters

| Parameter | PowerShell | Bash | Description |
|-----------|------------|------|-------------|
| Services | `-ServiceNames` | `-s, --services` | Comma-separated list of service names |
| Region | `-Region` | `-r, --region` | AWS region (default: us-east-1) |
| Profile | `-Profile` | `-p, --profile` | AWS profile to use |
| Dry Run | `-DryRun` | `-d, --dry-run` | Show what would be created without creating |
| Help | `-Help` | `-h, --help` | Show help message |

## Default Services

The scripts create admin secrets for these services by default:
- `scheduleforger`
- `paymentprocessor`
- `billingengine`
- `notificationservice`

## Secret Structure

Each admin secret is created with the following structure:

```json
{
    "Secret": "generated-secure-secret-value",
    "Scope": "admin",
    "Status": "ACTIVE",
    "CreatedAt": "2024-01-01T00:00:00.000Z",
    "UpdatedAt": "2024-01-01T00:00:00.000Z",
    "IsRevoked": false,
    "LastRotated": null,
    "RevokedAt": null,
    "ExpiresAt": null
}
```

## Secret Naming Convention

Secrets are created with the following naming pattern:
```
feenominal/admin/apikeys/{serviceName}-admin-api-key-secret
```

This pattern is configurable via the `AWS:SecretsManager:AdminSecretNameFormat` setting in your appsettings.json file.

Examples:
- `feenominal/admin/apikeys/scheduleforger-admin-api-key-secret`
- `feenominal/admin/apikeys/paymentprocessor-admin-api-key-secret`

**Note**: The admin secret name format is configurable via the `AWS:SecretsManager:AdminSecretNameFormat` setting in your appsettings.json file.

## Security Features

1. **Secure Secret Generation**: Uses cryptographically secure random generation
2. **Secret Length**: Generates 64-character secrets by default
3. **Character Set**: Includes uppercase, lowercase, numbers, and special characters
4. **Dry Run Mode**: Test the script without creating actual secrets
5. **Duplicate Prevention**: Checks if secrets already exist before creating

## Deployment Workflow

### Pre-Production Setup

1. **Test the script**:
   ```bash
   ./setup-admin-secret.sh -d -s testservice
   ```

2. **Create secrets for your services**:
   ```bash
   ./setup-admin-secret.sh -s yourservice1,yourservice2 -r your-aws-region
   ```

3. **Verify in AWS Console**:
   - Go to AWS Secrets Manager
   - Verify secrets were created with correct names
   - Check secret values are properly formatted

### Production Deployment

1. **Run the script** with your production services:
   ```bash
   ./setup-admin-secret.sh -s production-service1,production-service2 -r us-east-1 -p production
   ```

2. **Deploy your application** with the configured secrets

3. **Test admin API key generation** using the new secrets

## Troubleshooting

### Common Issues

1. **AWS CLI not found**:
   - Install AWS CLI from https://aws.amazon.com/cli/
   - Ensure it's in your PATH

2. **Permission denied**:
   - Check your AWS credentials: `aws sts get-caller-identity`
   - Verify you have the required Secrets Manager permissions

3. **Secret already exists**:
   - The script will skip existing secrets
   - Use dry run mode to see what would be created

4. **Invalid region**:
   - Verify the region exists: `aws ec2 describe-regions`
   - Use a valid AWS region

### Error Messages

- **"AWS CLI not found"**: Install AWS CLI
- **"Permission denied"**: Check AWS credentials and permissions
- **"Secret already exists"**: Normal behavior, secret will be skipped
- **"Invalid secret name format"**: Check service name format

## Best Practices

1. **Use Dry Run First**: Always test with `-d` or `--dry-run` before production
2. **Secure Storage**: Store generated secret values securely
3. **Environment Separation**: Use different AWS profiles for different environments
4. **Regular Rotation**: Plan for regular secret rotation
5. **Monitoring**: Monitor secret usage and access patterns

## Integration with CI/CD

You can integrate these scripts into your CI/CD pipeline:

```yaml
# Example GitHub Actions step
- name: Setup Admin Secrets
  run: |
    chmod +x scripts/setup-admin-secret.sh
    ./scripts/setup-admin-secret.sh -s ${{ secrets.SERVICES }} -r ${{ secrets.AWS_REGION }}
  env:
    AWS_ACCESS_KEY_ID: ${{ secrets.AWS_ACCESS_KEY_ID }}
    AWS_SECRET_ACCESS_KEY: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
```

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review AWS CLI documentation
3. Check AWS Secrets Manager documentation
4. Contact your DevOps team 