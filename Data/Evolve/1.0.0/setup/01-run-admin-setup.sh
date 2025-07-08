#!/bin/bash
# 01-run-admin-setup.sh
# Run admin setup script during Docker initialization
# This script runs the M1_0_0_000__admin_setup.sql script

set -e

echo "=== Running Admin Setup (M1_0_0_000) ==="
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -f /docker-entrypoint-initdb.d/M1_0_0_000__admin_setup.sql
echo "✓ Admin setup completed" 