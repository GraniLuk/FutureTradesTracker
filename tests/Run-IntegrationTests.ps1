#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Helper script to run integration tests for Crypto Position Analysis

.DESCRIPTION
    This script provides easy commands to run various types of integration tests
    with proper filtering and logging options.

.PARAMETER TestType
    The type of tests to run: All, BingX, Bybit, Positions, Balance, TradeHistory, Performance, ErrorHandling

.PARAMETER Exchange
    Specific exchange to test: BingX, Bybit, or All

.PARAMETER VerboseOutput
    Enable verbose logging output

.PARAMETER SetupCredentials
    Launch the user secrets setup process

.EXAMPLE
    .\Run-IntegrationTests.ps1 -TestType BingX
    Run all BingX integration tests

.EXAMPLE
    .\Run-IntegrationTests.ps1 -TestType Positions -Exchange BingX
    Run only position tests for BingX

.EXAMPLE
    .\Run-IntegrationTests.ps1 -TestType All -VerboseOutput
    Run all integration tests with verbose output

.EXAMPLE
    .\Run-IntegrationTests.ps1 -SetupCredentials
    Setup user secrets for API credentials
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("All", "BingX", "Bybit", "Positions", "Balance", "TradeHistory", "Performance", "ErrorHandling", "CrossExchange")]
    [string]$TestType = "All",
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("BingX", "Bybit", "All")]
    [string]$Exchange = "All",
    
    [Parameter(Mandatory = $false)]
    [switch]$VerboseOutput,
    
    [Parameter(Mandatory = $false)]
    [switch]$SetupCredentials
)

# Colors for output - using strings directly to avoid variable conflicts

function Write-Header {
    param([string]$Title)
    Write-Host "=" * 60 -ForegroundColor "Green"
    Write-Host $Title -ForegroundColor "Green"
    Write-Host "=" * 60 -ForegroundColor "Green"
}

function Write-Section {
    param([string]$Title)
    Write-Host "`n--- $Title ---" -ForegroundColor "Yellow"
}

function Setup-UserSecrets {
    Write-Header "Setting up User Secrets for API Credentials"
    
    $projectPath = Join-Path $PSScriptRoot "FutureTradesTracker.Tests.csproj"
    
    if (-not (Test-Path $projectPath)) {
        Write-Host "Error: Test project not found at $projectPath" -ForegroundColor "Red"
        return
    }
    
    Write-Host "🔐 IMPORTANT: This will store your API credentials securely using .NET User Secrets" -ForegroundColor "Yellow"
    Write-Host "Your credentials will NOT be stored in the project files or Git repository" -ForegroundColor "Yellow"
    Write-Host ""
    
    Write-Host "Initializing user secrets..." -ForegroundColor "Cyan"
    dotnet user-secrets init --project $projectPath
    
    Write-Host "`nPlease enter your API credentials:" -ForegroundColor "Yellow"
    Write-Host "(Press Enter to skip an exchange if you don't have credentials)" -ForegroundColor "Gray"
    
    # BingX Credentials
    Write-Host "`n--- BingX API Credentials ---" -ForegroundColor "Magenta"
    $bingXApiKey = Read-Host -Prompt "Enter your BingX API Key (or press Enter to skip)"
    
    if (-not [string]::IsNullOrWhiteSpace($bingXApiKey)) {
        $bingXSecretKey = Read-Host -Prompt "Enter your BingX Secret Key" -AsSecureString
        $bingXSecretKeyPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($bingXSecretKey))
        
        dotnet user-secrets set "BingXApi:ApiKey" $bingXApiKey --project $projectPath
        dotnet user-secrets set "BingXApi:SecretKey" $bingXSecretKeyPlain --project $projectPath
        Write-Host "✅ BingX credentials saved" -ForegroundColor "Green"
    } else {
        Write-Host "⏭️  Skipping BingX credentials" -ForegroundColor "Yellow"
    }
    
    # Bybit Credentials
    Write-Host "`n--- Bybit API Credentials ---" -ForegroundColor "Magenta"
    $bybitApiKey = Read-Host -Prompt "Enter your Bybit API Key (or press Enter to skip)"
    
    if (-not [string]::IsNullOrWhiteSpace($bybitApiKey)) {
        $bybitSecretKey = Read-Host -Prompt "Enter your Bybit Secret Key" -AsSecureString
        $bybitSecretKeyPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($bybitSecretKey))
        
        dotnet user-secrets set "BybitApi:ApiKey" $bybitApiKey --project $projectPath
        dotnet user-secrets set "BybitApi:SecretKey" $bybitSecretKeyPlain --project $projectPath
        Write-Host "✅ Bybit credentials saved" -ForegroundColor "Green"
    } else {
        Write-Host "⏭️  Skipping Bybit credentials" -ForegroundColor "Yellow"
    }
    
    Write-Host "`n🎉 Credentials setup complete!" -ForegroundColor "Green"
    Write-Host "You can now run integration tests with:" -ForegroundColor "Cyan"
    Write-Host "  .\Run-IntegrationTests.ps1 -TestType All" -ForegroundColor "White"
    Write-Host ""
    Write-Host "To verify your setup:" -ForegroundColor "Cyan"
    Write-Host "  dotnet user-secrets list --project $projectPath" -ForegroundColor "White"
    Write-Host ""
    Write-Host "🔒 Security Notes:" -ForegroundColor "Yellow"
    Write-Host "- Your credentials are stored securely on your local machine" -ForegroundColor "Gray"
    Write-Host "- They are NOT stored in project files or Git repository" -ForegroundColor "Gray"
    Write-Host "- Each developer must set up their own credentials" -ForegroundColor "Gray"
}

function Build-TestFilter {
    param([string]$TestType, [string]$Exchange)
    
    $filters = @()
    
    # Add category filter for integration tests
    $filters += "TestCategory=Integration"
    
    # Add exchange filter
    if ($Exchange -ne "All") {
        $filters += "TestCategory=$Exchange"
    }
    
    # Add test type filter
    if ($TestType -ne "All") {
        switch ($TestType) {
            "Positions" { $filters += "TestCategory=Positions" }
            "Balance" { $filters += "TestCategory=Balance" }
            "TradeHistory" { $filters += "TestCategory=TradeHistory" }
            "Performance" { $filters += "TestCategory=Performance" }
            "ErrorHandling" { $filters += "TestCategory=ErrorHandling" }
            "CrossExchange" { $filters += "TestCategory=CrossExchange" }
            "BingX" { $filters += "TestCategory=BingX" }
            "Bybit" { $filters += "TestCategory=Bybit" }
        }
    }
    
    return $filters -join "&"
}

function Run-IntegrationTests {
    param([string]$TestType, [string]$Exchange, [bool]$VerboseOutput)
    
    Write-Header "Running Integration Tests for Crypto Position Analysis"
    
    # Build test filter
    $filter = Build-TestFilter -TestType $TestType -Exchange $Exchange
    
    Write-Section "Test Configuration"
    Write-Host "Test Type: $TestType" -ForegroundColor "Cyan"
    Write-Host "Exchange: $Exchange" -ForegroundColor "Cyan"
    Write-Host "Filter: $filter" -ForegroundColor "Cyan"
    Write-Host "Verbose: $VerboseOutput" -ForegroundColor "Cyan"
    
    # Change to test directory
    Push-Location $PSScriptRoot
    
    try {
        Write-Section "Building Test Project"
        dotnet build --configuration Release
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Build failed!" -ForegroundColor "Red"
            return
        }
        
        Write-Section "Running Tests"
        
        # Build dotnet test command
        $testArgs = @(
            "test"
            "--configuration", "Release"
            "--filter", $filter
            "--logger", "console;verbosity=normal"
        )
        
        if ($VerboseOutput) {
            $testArgs += "--verbosity", "diagnostic"
        }
        
        Write-Host "Executing: dotnet $($testArgs -join ' ')" -ForegroundColor "Yellow"
        
        # Run tests
        & dotnet $testArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "`nAll tests passed successfully!" -ForegroundColor "Green"
        } else {
            Write-Host "`nSome tests failed. Check the output above for details." -ForegroundColor "Red"
        }
        
    } finally {
        Pop-Location
    }
}

function Show-Help {
    Write-Header "Integration Test Helper"
    
    Write-Host "Available commands:" -ForegroundColor "Yellow"
    Write-Host "  Setup credentials:     .\Run-IntegrationTests.ps1 -SetupCredentials" -ForegroundColor "Cyan"
    Write-Host "  Run all tests:         .\Run-IntegrationTests.ps1 -TestType All" -ForegroundColor "Cyan"
    Write-Host "  Run BingX tests:       .\Run-IntegrationTests.ps1 -TestType BingX" -ForegroundColor "Cyan"
    Write-Host "  Run Bybit tests:       .\Run-IntegrationTests.ps1 -TestType Bybit" -ForegroundColor "Cyan"
    Write-Host "  Run position tests:    .\Run-IntegrationTests.ps1 -TestType Positions" -ForegroundColor "Cyan"
    Write-Host "  Run balance tests:     .\Run-IntegrationTests.ps1 -TestType Balance" -ForegroundColor "Cyan"
    Write-Host "  Run performance tests: .\Run-IntegrationTests.ps1 -TestType Performance" -ForegroundColor "Cyan"
    Write-Host "  Run with verbose:      .\Run-IntegrationTests.ps1 -TestType All -VerboseOutput" -ForegroundColor "Cyan"
    
    Write-Host "`nTest Categories:" -ForegroundColor "Yellow"
    Write-Host "  - Positions: Tests for getting open positions" -ForegroundColor "Cyan"
    Write-Host "  - Balance: Tests for account balance retrieval" -ForegroundColor "Cyan"
    Write-Host "  - TradeHistory: Tests for historical trade data" -ForegroundColor "Cyan"
    Write-Host "  - Performance: Tests for response times and rate limiting" -ForegroundColor "Cyan"
    Write-Host "  - ErrorHandling: Tests for error scenarios" -ForegroundColor "Cyan"
    Write-Host "  - CrossExchange: Tests comparing data across exchanges" -ForegroundColor "Cyan"
    
    Write-Host "`nNote: All integration tests require valid API credentials" -ForegroundColor "Yellow"
    Write-Host "Use -SetupCredentials to configure your API keys securely" -ForegroundColor "Yellow"
}

# Main execution
try {
    if ($SetupCredentials) {
        Setup-UserSecrets
    } elseif ($TestType -eq "Help" -or $args -contains "-Help" -or $args -contains "--help") {
        Show-Help
    } else {
        Run-IntegrationTests -TestType $TestType -Exchange $Exchange -VerboseOutput $VerboseOutput
    }
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor "Red"
    exit 1
}
