#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Setup admin secrets in AWS Secrets Manager for production deployment.

.DESCRIPTION
    This script creates admin API key secrets in AWS Secrets Manager for the specified services.
    It generates secure random secrets and stores them in the proper format for the FeeNominal service.

.PARAMETER ServiceNames
    Array of service names to create admin secrets for. Defaults to common services.

.PARAMETER Region
    AWS region where secrets will be stored. Defaults to us-east-1.

.PARAMETER Profile
    AWS profile to use for authentication. If not specified, uses default profile.

.PARAMETER DryRun
    If specified, shows what would be created without actually creating secrets.

.EXAMPLE
    .\Setup-AdminSecret.ps1 -ServiceNames @("scheduleforger", "paymentprocessor") -Region us-west-2

.EXAMPLE
    .\Setup-AdminSecret.ps1 -DryRun -ServiceNames @("testservice")

.NOTES
    Requires AWS CLI to be installed and configured.
    Requires appropriate AWS permissions to create secrets.
#>

param(
    [string[]]$ServiceNames = @("scheduleforger", "paymentprocessor", "billingengine", "notificationservice"),
    [string]$Region = "us-east-1",
    [string]$Profile = "",
    [switch]$DryRun
)

# Function to generate a secure random secret
function New-SecureSecret {
    param([int]$Length = 64)
    
    $chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-="
    $random = [System.Random]::new()
    $secret = ""
    
    for ($i = 0; $i -lt $Length; $i++) {
        $secret += $chars[$random.Next($chars.Length)]
    }
    
    return $secret
}

# Function to create admin secret in AWS Secrets Manager
function New-AdminSecret {
    param(
        [string]$ServiceName,
        [string]$SecretValue,
        [string]$Region,
        [string]$Profile,
        [bool]$DryRun,
        [string]$BaseSecretName = "feenominal/apikeys"
    )
    
    $secretName = "feenominal/admin/apikeys/$ServiceName-admin-api-key-secret"
    $secretDescription = "Admin API key secret for $ServiceName service"
    
    # Create the secret JSON structure
    $secretData = @{
        Secret = $SecretValue
        Scope = "admin"
        Status = "ACTIVE"
        CreatedAt = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        UpdatedAt = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        IsRevoked = $false
        LastRotated = $null
        RevokedAt = $null
        ExpiresAt = $null
    } | ConvertTo-Json -Depth 10
    
    if ($DryRun) {
        Write-Host "DRY RUN: Would create secret '$secretName'" -ForegroundColor Yellow
        Write-Host "  Description: $secretDescription" -ForegroundColor Gray
        Write-Host "  Secret Value: $SecretValue" -ForegroundColor Gray
        Write-Host "  Region: $Region" -ForegroundColor Gray
        Write-Host "  Base Secret Name: $BaseSecretName" -ForegroundColor Gray
        if ($Profile) {
            Write-Host "  Profile: $Profile" -ForegroundColor Gray
        }
        Write-Host ""
        return
    }
    
    try {
        # Build AWS CLI command
        $awsCmd = "aws secretsmanager create-secret"
        $awsCmd += " --name `"$secretName`""
        $awsCmd += " --description `"$secretDescription`""
        $awsCmd += " --secret-string `"$secretData`""
        $awsCmd += " --region $Region"
        
        if ($Profile) {
            $awsCmd += " --profile $Profile"
        }
        
        Write-Host "Creating secret '$secretName'..." -ForegroundColor Green
        
        # Execute the command
        $result = Invoke-Expression $awsCmd 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Successfully created secret '$secretName'" -ForegroundColor Green
            Write-Host "  Secret ARN: $($result | ConvertFrom-Json | Select-Object -ExpandProperty ARN)" -ForegroundColor Gray
        } else {
            Write-Host "❌ Failed to create secret '$secretName'" -ForegroundColor Red
            Write-Host "  Error: $result" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "❌ Exception occurred while creating secret '$secretName'" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
    
    return $true
}

# Function to check if secret already exists
function Test-SecretExists {
    param(
        [string]$ServiceName,
        [string]$Region,
        [string]$Profile,
        [string]$BaseSecretName = "feenominal/apikeys"
    )
    
    $secretName = "feenominal/admin/apikeys/$ServiceName-admin-api-key-secret"
    
    try {
        $awsCmd = "aws secretsmanager describe-secret --secret-id `"$secretName`" --region $Region"
        if ($Profile) {
            $awsCmd += " --profile $Profile"
        }
        
        $result = Invoke-Expression $awsCmd 2>&1
        
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $false
    }
}

# Main script execution
Write-Host "🔧 Admin Secret Setup Script for FeeNominal Service" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Validate AWS CLI installation
try {
    $awsVersion = aws --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "AWS CLI not found"
    }
    Write-Host "✅ AWS CLI found: $awsVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ AWS CLI is not installed or not in PATH" -ForegroundColor Red
    Write-Host "   Please install AWS CLI from: https://aws.amazon.com/cli/" -ForegroundColor Yellow
    exit 1
}

# Display configuration
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Services: $($ServiceNames -join ', ')" -ForegroundColor Gray
Write-Host "  Region: $Region" -ForegroundColor Gray
Write-Host "  Profile: $(if ($Profile) { $Profile } else { 'default' })" -ForegroundColor Gray
Write-Host "  Dry Run: $DryRun" -ForegroundColor Gray
Write-Host ""

if ($DryRun) {
    Write-Host "🔍 DRY RUN MODE - No secrets will be created" -ForegroundColor Yellow
    Write-Host ""
}

# Process each service
$successCount = 0
$totalCount = $ServiceNames.Count

foreach ($serviceName in $ServiceNames) {
    Write-Host "Processing service: $serviceName" -ForegroundColor White
    
    # Check if secret already exists
    if (-not $DryRun) {
        $exists = Test-SecretExists -ServiceName $serviceName -Region $Region -Profile $Profile -BaseSecretName "feenominal/apikeys"
        if ($exists) {
            Write-Host "⚠️  Secret already exists for '$serviceName'. Skipping..." -ForegroundColor Yellow
            $successCount++
            continue
        }
    }
    
    # Generate secure secret
    $secretValue = New-SecureSecret -Length 64
    
    # Create the secret
    $result = New-AdminSecret -ServiceName $serviceName -SecretValue $secretValue -Region $Region -Profile $Profile -DryRun $DryRun -BaseSecretName "feenominal/apikeys"
    
    if ($result -ne $false) {
        $successCount++
    }
    
    Write-Host ""
}

# Summary
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "📊 Setup Summary:" -ForegroundColor Cyan
Write-Host "  Total services processed: $totalCount" -ForegroundColor Gray
Write-Host "  Successful: $successCount" -ForegroundColor Green
Write-Host "  Failed: $($totalCount - $successCount)" -ForegroundColor Red
Write-Host ""

if ($successCount -eq $totalCount) {
    Write-Host "✅ All admin secrets have been set up successfully!" -ForegroundColor Green
} else {
    Write-Host "⚠️  Some secrets failed to be created. Please check the errors above." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "📝 Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Verify secrets in AWS Secrets Manager console" -ForegroundColor Gray
Write-Host "  2. Update your application configuration to use these secrets" -ForegroundColor Gray
Write-Host "  3. Test admin API key generation with the new secrets" -ForegroundColor Gray
Write-Host ""

if (-not $DryRun) {
    Write-Host "🔐 IMPORTANT: Store the generated secret values securely!" -ForegroundColor Red
    Write-Host "   These secrets are only shown during creation for verification." -ForegroundColor Red
} 