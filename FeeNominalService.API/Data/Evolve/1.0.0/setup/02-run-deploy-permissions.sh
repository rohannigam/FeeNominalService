#!/bin/bash
# 02-run-deploy-permissions.sh
# Run deployment permissions script during Docker initialization
# This script runs the M1_0_0_001__grant_deploy_permissions.sql script

set -e

echo "=== Running Deployment Permissions Setup (M1_0_0_001) ==="
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -f /docker-entrypoint-initdb.d/M1_0_0_001__grant_deploy_permissions.sql
echo "✓ Deployment permissions setup completed" 