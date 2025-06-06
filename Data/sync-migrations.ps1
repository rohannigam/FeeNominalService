# Script to maintain synchronization between different migration approaches
param(
    [string]$Action = "check",  # check, sync, or help
    [string]$Source = "traditional"  # traditional, evolve, or flyway
)

function Show-Help {
    Write-Host @"
Migration Synchronization Tool

Usage:
    .\sync-migrations.ps1 -Action <action> -Source <source>

Actions:
    check   - Check for differences between migration files
    sync    - Synchronize migration files based on source
    help    - Show this help message

Sources:
    traditional - Use Traditional SQL scripts as source
    evolve      - Use Evolve migrations as source
    flyway      - Use Flyway migrations as source

Examples:
    .\sync-migrations.ps1 -Action check -Source traditional
    .\sync-migrations.ps1 -Action sync -Source evolve
"@
}

function Get-FileHash {
    param([string]$FilePath)
    if (Test-Path $FilePath) {
        return (Get-FileHash -Path $FilePath -Algorithm SHA256).Hash
    }
    return $null
}

function Compare-Files {
    param(
        [string]$File1,
        [string]$File2
    )
    $hash1 = Get-FileHash $File1
    $hash2 = Get-FileHash $File2
    
    if ($hash1 -eq $null -or $hash2 -eq $null) {
        return $false
    }
    
    return $hash1 -eq $hash2
}

function Check-Synchronization {
    Write-Host "Checking synchronization status..."
    
    # Check Traditional vs Evolve
    $traditionalSchema = ".\Traditional\Step1_Schema.sql"
    $evolveLatest = Get-ChildItem ".\Evolve\migrations" -Filter "*.sql" | Sort-Object Name -Descending | Select-Object -First 1
    
    if ($evolveLatest) {
        $inSync = Compare-Files $traditionalSchema $evolveLatest.FullName
        $status = if ($inSync) { "In Sync" } else { "Out of Sync" }
        Write-Host "Traditional Schema vs Evolve Latest: $status"
    }
    
    # Check Traditional vs Flyway
    $flywayLatest = Get-ChildItem ".\Flyway\migrations" -Filter "*.sql" | Sort-Object Name -Descending | Select-Object -First 1
    
    if ($flywayLatest) {
        $inSync = Compare-Files $traditionalSchema $flywayLatest.FullName
        $status = if ($inSync) { "In Sync" } else { "Out of Sync" }
        Write-Host "Traditional Schema vs Flyway Latest: $status"
    }
}

function Sync-Migrations {
    param([string]$Source)
    
    Write-Host "Synchronizing migrations from $Source source..."
    
    switch ($Source.ToLower()) {
        "traditional" {
            # Copy from Traditional to Evolve and Flyway
            Copy-Item ".\Traditional\Step1_Schema.sql" ".\Evolve\migrations\V$(Get-Date -Format 'yyyyMMddHHmmss')__schema.sql"
            Copy-Item ".\Traditional\Step1_Schema.sql" ".\Flyway\migrations\V$(Get-Date -Format 'yyyyMMddHHmmss')__schema.sql"
        }
        "evolve" {
            # Copy from Evolve to Traditional and Flyway
            $evolveLatest = Get-ChildItem ".\Evolve\migrations" -Filter "*.sql" | Sort-Object Name -Descending | Select-Object -First 1
            if ($evolveLatest) {
                Copy-Item $evolveLatest.FullName ".\Traditional\Step1_Schema.sql"
                Copy-Item $evolveLatest.FullName ".\Flyway\migrations\V$(Get-Date -Format 'yyyyMMddHHmmss')__schema.sql"
            }
        }
        "flyway" {
            # Copy from Flyway to Traditional and Evolve
            $flywayLatest = Get-ChildItem ".\Flyway\migrations" -Filter "*.sql" | Sort-Object Name -Descending | Select-Object -First 1
            if ($flywayLatest) {
                Copy-Item $flywayLatest.FullName ".\Traditional\Step1_Schema.sql"
                Copy-Item $flywayLatest.FullName ".\Evolve\migrations\V$(Get-Date -Format 'yyyyMMddHHmmss')__schema.sql"
            }
        }
    }
}

# Main script execution
switch ($Action.ToLower()) {
    "check" {
        Check-Synchronization
    }
    "sync" {
        Sync-Migrations -Source $Source
    }
    "help" {
        Show-Help
    }
    default {
        Write-Host "Unknown action. Use -Action help for usage information."
    }
} 