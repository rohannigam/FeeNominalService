# Script to handle database rollbacks using Evolve
param(
    [string]$TargetVersion = "",   # Version to roll back to (empty for one step)
    [string]$ConnectionString = "", # Database connection string
    [switch]$DryRun = $false       # If true, only show what would be done
)

# Configuration
$baseDir = Join-Path $PSScriptRoot "migrations"
$downDir = Join-Path $baseDir "down"
$containerName = "feenominal-db"

function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    else {
        $input | Write-Output
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

function Get-CurrentVersion {
    param([string]$ConnectionString)
    
    # Query the changelog table to get current version
    $query = "SELECT version FROM fee_nominal.changelog ORDER BY installed_on DESC LIMIT 1"
    $result = docker exec $containerName psql -U postgres -d feenominal -t -c $query
    return $result.Trim()
}

function Get-MigrationsToRollback {
    param(
        [string]$CurrentVersion,
        [string]$TargetVersion,
        [string]$DownDir
    )

    # Get all migration files
    $migrations = Get-ChildItem -Path $DownDir -Filter "V*__*_down.sql" | 
                 Where-Object { $_.Name -match 'V(\d+(\.\d+)?)__' } |
                 ForEach-Object {
                     $version = [regex]::Match($_.Name, 'V(\d+(\.\d+)?)__').Groups[1].Value
                     [PSCustomObject]@{
                         File = $_
                         Version = $version
                     }
                 }

    # Sort by version number (numeric comparison)
    $migrations = $migrations | Sort-Object { [int]$_.Version } -Descending

    # Filter migrations to rollback
    if ($TargetVersion) {
        $migrations = $migrations | Where-Object { 
            [int]$_.Version -gt [int]$TargetVersion -and 
            [int]$_.Version -le [int]$CurrentVersion 
        }
    } else {
        # If no target version, rollback one step
        $migrations = $migrations | Where-Object { 
            [int]$_.Version -eq [int]$CurrentVersion 
        }
    }

    return $migrations
}

function Execute-Rollback {
    param(
        [string]$MigrationFile,
        [string]$ConnectionString,
        [bool]$DryRun
    )

    Write-ColorOutput Green "Rolling back: $($MigrationFile.Name)"
    
    if ($DryRun) {
        Write-ColorOutput Yellow "Would execute: $($MigrationFile.FullName)"
        return
    }

    # Read the SQL content
    $sql = Get-Content $MigrationFile.FullName -Raw

    # Extract version from filename
    $version = [regex]::Match($MigrationFile.Name, 'V(\d+(\.\d+)?)__').Groups[1].Value

    # Execute the SQL using docker exec
    try {
        $tempFile = [System.IO.Path]::GetTempFileName()
        $sql | Out-File -FilePath $tempFile -Encoding UTF8
        docker cp $tempFile "${containerName}:/tmp/rollback.sql"
        docker exec $containerName psql -U postgres -d feenominal -f /tmp/rollback.sql
        Remove-Item $tempFile
        Write-ColorOutput Green "Successfully rolled back: $($MigrationFile.Name)"

        # Delete the version from the changelog table
        $deleteQuery = "DELETE FROM fee_nominal.changelog WHERE version = '$version';"
        docker exec $containerName psql -U postgres -d feenominal -c "$deleteQuery"
        Write-ColorOutput Yellow "Deleted version $version from changelog."
    }
    catch {
        Write-ColorOutput Red "Error rolling back $($MigrationFile.Name): $_"
        throw
    }
}

# Main script execution
Write-ColorOutput Cyan "Starting rollback process..."

# Validate connection string
if (-not $ConnectionString) {
    Write-ColorOutput Red "Connection string is required"
    exit 1
}

try {
    # Get current version
    $currentVersion = Get-CurrentVersion -ConnectionString $ConnectionString
    Write-ColorOutput Yellow "Current version: $currentVersion"

    # Get migrations to rollback
    $migrationsToRollback = Get-MigrationsToRollback -CurrentVersion $currentVersion -TargetVersion $TargetVersion -DownDir $downDir

    if (-not $migrationsToRollback) {
        Write-ColorOutput Yellow "No migrations to rollback"
        exit 0
    }

    Write-ColorOutput Yellow "Migrations to rollback:"
    $migrationsToRollback | ForEach-Object {
        Write-ColorOutput Yellow "  $($_.File.Name)"
    }

    # Confirm rollback
    if (-not $DryRun) {
        $confirmation = Read-Host "Do you want to proceed with the rollback? (y/n)"
        if ($confirmation -ne 'y') {
            Write-ColorOutput Yellow "Rollback cancelled"
            exit 0
        }
    }

    # Execute rollbacks
    foreach ($migration in $migrationsToRollback) {
        Execute-Rollback -MigrationFile $migration.File -ConnectionString $ConnectionString -DryRun $DryRun
    }

    Write-ColorOutput Green "Rollback completed successfully!"
}
catch {
    Write-ColorOutput Red "Error during rollback: $_"
    exit 1
} 