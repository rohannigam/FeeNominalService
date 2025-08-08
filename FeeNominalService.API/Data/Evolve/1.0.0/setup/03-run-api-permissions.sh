#!/bin/bash
# 03-run-api-permissions.sh
# Run API permissions script during Docker initialization
# This script runs the M1_0_0_002__grant_api_permissions.sql script

set -e

echo "=== Running API Permissions Setup (M1_0_0_002) ==="
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -f /docker-entrypoint-initdb.d/M1_0_0_002__grant_api_permissions.sql
echo "✓ API permissions setup completed" 