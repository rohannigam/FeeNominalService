#!/bin/bash

# Admin Secret Setup Script for FeeNominal Service
# This script creates admin API key secrets in AWS Secrets Manager for production deployment.

set -e

# Default values
SERVICE_NAMES=("scheduleforger" "paymentprocessor" "billingengine" "notificationservice")
REGION="us-east-1"
PROFILE=""
DRY_RUN=false

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# Function to show usage
show_usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -s, --services SERVICE1,SERVICE2,...  Comma-separated list of service names"
    echo "  -r, --region REGION                   AWS region (default: us-east-1)"
    echo "  -p, --profile PROFILE                 AWS profile to use"
    echo "  -d, --dry-run                         Show what would be created without creating"
    echo "  -h, --help                            Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 -s scheduleforger,paymentprocessor -r us-west-2"
    echo "  $0 --dry-run --services testservice"
    echo ""
    echo "Notes:"
    echo "  Requires AWS CLI to be installed and configured."
    echo "  Requires appropriate AWS permissions to create secrets."
}

# Function to generate a secure random secret
generate_secure_secret() {
    local length=${1:-64}
    local chars="ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-="
    local secret=""
    
    for ((i=0; i<length; i++)); do
        local random_index=$((RANDOM % ${#chars}))
        secret="${secret}${chars:$random_index:1}"
    done
    
    echo "$secret"
}

# Function to create admin secret in AWS Secrets Manager
create_admin_secret() {
    local service_name=$1
    local secret_value=$2
    local region=$3
    local profile=$4
    local dry_run=$5
    local base_secret_name=${6:-"feenominal/apikeys"}
    
    local secret_name="feenominal/admin/apikeys/${service_name}-admin-api-key-secret"
    local secret_description="Admin API key secret for ${service_name} service"
    
    # Create the secret JSON structure
    local secret_data=$(cat <<EOF
{
    "Secret": "${secret_value}",
    "Scope": "admin",
    "Status": "ACTIVE",
    "CreatedAt": "$(date -u +"%Y-%m-%dT%H:%M:%S.000Z")",
    "UpdatedAt": "$(date -u +"%Y-%m-%dT%H:%M:%S.000Z")",
    "IsRevoked": false,
    "LastRotated": null,
    "RevokedAt": null,
    "ExpiresAt": null
}
EOF
)
    
    if [ "$dry_run" = true ]; then
        print_status "$YELLOW" "DRY RUN: Would create secret '$secret_name'"
        print_status "$GRAY" "  Description: $secret_description"
        print_status "$GRAY" "  Secret Value: $secret_value"
        print_status "$GRAY" "  Region: $region"
        if [ -n "$profile" ]; then
            print_status "$GRAY" "  Profile: $profile"
        fi
        echo ""
        return 0
    fi
    
    # Build AWS CLI command
    local aws_cmd="aws secretsmanager create-secret"
    aws_cmd="$aws_cmd --name \"$secret_name\""
    aws_cmd="$aws_cmd --description \"$secret_description\""
    aws_cmd="$aws_cmd --secret-string '$secret_data'"
    aws_cmd="$aws_cmd --region $region"
    
    if [ -n "$profile" ]; then
        aws_cmd="$aws_cmd --profile $profile"
    fi
    
    print_status "$GREEN" "Creating secret '$secret_name'..."
    
    # Execute the command
    if eval $aws_cmd > /tmp/aws_result.json 2>&1; then
        local secret_arn=$(cat /tmp/aws_result.json | jq -r '.ARN')
        print_status "$GREEN" "✅ Successfully created secret '$secret_name'"
        print_status "$GRAY" "  Secret ARN: $secret_arn"
        rm -f /tmp/aws_result.json
        return 0
    else
        local error=$(cat /tmp/aws_result.json)
        print_status "$RED" "❌ Failed to create secret '$secret_name'"
        print_status "$RED" "  Error: $error"
        rm -f /tmp/aws_result.json
        return 1
    fi
}

# Function to check if secret already exists
check_secret_exists() {
    local service_name=$1
    local region=$2
    local profile=$3
    local base_secret_name=${4:-"feenominal/apikeys"}
    
    local secret_name="feenominal/admin/apikeys/${service_name}-admin-api-key-secret"
    
    local aws_cmd="aws secretsmanager describe-secret --secret-id \"$secret_name\" --region $region"
    if [ -n "$profile" ]; then
        aws_cmd="$aws_cmd --profile $profile"
    fi
    
    if eval $aws_cmd > /dev/null 2>&1; then
        return 0  # Secret exists
    else
        return 1  # Secret doesn't exist
    fi
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -s|--services)
            IFS=',' read -ra SERVICE_NAMES <<< "$2"
            shift 2
            ;;
        -r|--region)
            REGION="$2"
            shift 2
            ;;
        -p|--profile)
            PROFILE="$2"
            shift 2
            ;;
        -d|--dry-run)
            DRY_RUN=true
            shift
            ;;
        -h|--help)
            show_usage
            exit 0
            ;;
        *)
            print_status "$RED" "Unknown option: $1"
            show_usage
            exit 1
            ;;
    esac
done

# Main script execution
print_status "$CYAN" "🔧 Admin Secret Setup Script for FeeNominal Service"
print_status "$CYAN" "=================================================="
echo ""

# Validate AWS CLI installation
if ! command -v aws &> /dev/null; then
    print_status "$RED" "❌ AWS CLI is not installed or not in PATH"
    print_status "$YELLOW" "   Please install AWS CLI from: https://aws.amazon.com/cli/"
    exit 1
fi

aws_version=$(aws --version 2>&1)
print_status "$GREEN" "✅ AWS CLI found: $aws_version"

# Check if jq is available (for JSON parsing)
if ! command -v jq &> /dev/null; then
    print_status "$YELLOW" "⚠️  jq is not installed. Installing jq is recommended for better output formatting."
fi

# Display configuration
print_status "$YELLOW" "Configuration:"
print_status "$GRAY" "  Services: ${SERVICE_NAMES[*]}"
print_status "$GRAY" "  Region: $REGION"
print_status "$GRAY" "  Profile: ${PROFILE:-default}"
print_status "$GRAY" "  Dry Run: $DRY_RUN"
echo ""

if [ "$DRY_RUN" = true ]; then
    print_status "$YELLOW" "🔍 DRY RUN MODE - No secrets will be created"
    echo ""
fi

# Process each service
success_count=0
total_count=${#SERVICE_NAMES[@]}

for service_name in "${SERVICE_NAMES[@]}"; do
    print_status "$BLUE" "Processing service: $service_name"
    
    # Check if secret already exists
    if [ "$DRY_RUN" = false ]; then
        if check_secret_exists "$service_name" "$REGION" "$PROFILE" "feenominal/apikeys"; then
            print_status "$YELLOW" "⚠️  Secret already exists for '$service_name'. Skipping..."
            ((success_count++))
            continue
        fi
    fi
    
    # Generate secure secret
    secret_value=$(generate_secure_secret 64)
    
    # Create the secret
    if create_admin_secret "$service_name" "$secret_value" "$REGION" "$PROFILE" "$DRY_RUN" "feenominal/apikeys"; then
        ((success_count++))
    fi
    
    echo ""
done

# Summary
print_status "$CYAN" "=================================================="
print_status "$CYAN" "📊 Setup Summary:"
print_status "$GRAY" "  Total services processed: $total_count"
print_status "$GREEN" "  Successful: $success_count"
print_status "$RED" "  Failed: $((total_count - success_count))"
echo ""

if [ $success_count -eq $total_count ]; then
    print_status "$GREEN" "✅ All admin secrets have been set up successfully!"
else
    print_status "$YELLOW" "⚠️  Some secrets failed to be created. Please check the errors above."
fi

echo ""
print_status "$CYAN" "📝 Next Steps:"
print_status "$GRAY" "  1. Verify secrets in AWS Secrets Manager console"
print_status "$GRAY" "  2. Update your application configuration to use these secrets"
print_status "$GRAY" "  3. Test admin API key generation with the new secrets"
echo ""

if [ "$DRY_RUN" = false ]; then
    print_status "$RED" "🔐 IMPORTANT: Store the generated secret values securely!"
    print_status "$RED" "   These secrets are only shown during creation for verification."
fi 