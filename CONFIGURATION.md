# Crypto Position Analysis - Configuration Guide

## 🔐 IMPORTANT: Never commit API secrets to Git!

This file explains how to securely configure your API credentials for different environments.

## Configuration Priority (highest to lowest):

1. **Environment Variables** (highest priority)
2. **User Secrets** (development)
3. **appsettings.{Environment}.json** (if exists)
4. **appsettings.json** (default values only)

## 🛡️ Secure Configuration Methods

### Method 1: User Secrets (Recommended for Development)

```bash
# Navigate to the tests directory
cd tests

# Initialize user secrets
dotnet user-secrets init

# Set BingX credentials
dotnet user-secrets set "BingXApi:ApiKey" "your-actual-bingx-api-key"
dotnet user-secrets set "BingXApi:SecretKey" "your-actual-bingx-secret-key"

# Set Bybit credentials
dotnet user-secrets set "BybitApi:ApiKey" "your-actual-bybit-api-key"
dotnet user-secrets set "BybitApi:SecretKey" "your-actual-bybit-secret-key"

# List all secrets (optional)
dotnet user-secrets list
```

### Method 2: Environment Variables

```bash
# Windows (PowerShell)
$env:BingXApi__ApiKey = "your-actual-bingx-api-key"
$env:BingXApi__SecretKey = "your-actual-bingx-secret-key"
$env:BybitApi__ApiKey = "your-actual-bybit-api-key"
$env:BybitApi__SecretKey = "your-actual-bybit-secret-key"

# Linux/Mac (bash)
export BingXApi__ApiKey="your-actual-bingx-api-key"
export BingXApi__SecretKey="your-actual-bingx-secret-key"
export BybitApi__ApiKey="your-actual-bybit-api-key"
export BybitApi__SecretKey="your-actual-bybit-secret-key"
```

### Method 3: Local Configuration File (Not Recommended)

Create `appsettings.local.json` (this file is ignored by Git):

```json
{
  "BingXApi": {
    "ApiKey": "your-actual-bingx-api-key",
    "SecretKey": "your-actual-bingx-secret-key"
  },
  "BybitApi": {
    "ApiKey": "your-actual-bybit-api-key",
    "SecretKey": "your-actual-bybit-secret-key"
  }
}
```

## 🎯 Quick Setup with Helper Script

```powershell
# Use the helper script to set up credentials
.\tests\Run-IntegrationTests.ps1 -SetupCredentials
```

## 📂 What Gets Committed to Git

### ✅ Safe to Commit:
- `appsettings.json` (with empty/placeholder values)
- `appsettings.Development.json` (without secrets)
- Configuration structure and non-sensitive settings

### ❌ Never Commit:
- API keys or secret keys
- `appsettings.local.json`
- `appsettings.Production.json` (with real credentials)
- `.env` files with credentials
- Any file with actual API credentials

## 🔍 How to Verify Configuration

```bash
# Check if credentials are configured (without revealing them)
dotnet user-secrets list --project tests

# Or run a simple test
dotnet test tests --filter "TestCategory=Integration&TestCategory=BingX" --logger console
```

## 🚀 Team Development Setup

### For New Team Members:

1. **Clone the repository**
2. **Set up credentials** using one of the methods above
3. **Run tests** to verify setup:
   ```bash
   cd tests
   .\Run-IntegrationTests.ps1 -TestType BingX -Verbose
   ```

### For CI/CD Pipelines:

Use environment variables in your CI/CD system:
- Azure DevOps: Pipeline Variables (marked as secret)
- GitHub Actions: Repository Secrets
- Jenkins: Credentials Plugin

## 🛠️ Development Workflow

```bash
# 1. Pull latest changes
git pull origin main

# 2. Set up your credentials (one-time)
dotnet user-secrets set "BingXApi:ApiKey" "your-key" --project tests

# 3. Run integration tests
.\tests\Run-IntegrationTests.ps1 -TestType Positions

# 4. Develop and test
# ... make changes ...

# 5. Run tests before committing
dotnet test tests --filter "TestCategory!=Integration"

# 6. Commit (credentials are automatically excluded)
git add .
git commit -m "Add new feature"
git push origin feature-branch
```

## 🔧 Troubleshooting

### Issue: "API credentials are not configured"

**Solution**: Verify credentials are set up correctly:

```bash
# Check user secrets
dotnet user-secrets list --project tests

# Check environment variables
echo $env:BingXApi__ApiKey  # PowerShell
echo $BingXApi__ApiKey      # bash

# Test configuration loading
dotnet run --project tests -- --test-config
```

### Issue: "Invalid API key" errors

**Solution**: 
1. Verify API keys are active in exchange account
2. Check API key permissions (read permissions required)
3. Ensure API key is not expired

### Issue: "Signature verification failed"

**Solution**:
1. Verify secret key is correct
2. Check system time synchronization
3. Ensure no extra spaces in credentials

## 📚 Additional Resources

- [.NET User Secrets Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [.NET Configuration Documentation](https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration)
- [BingX API Documentation](https://bingx-api.github.io/docs/)
- [Bybit API Documentation](https://bybit-exchange.github.io/docs/)

## 🔐 Security Best Practices

1. **Rotate API keys regularly**
2. **Use read-only permissions** when possible
3. **Limit API key IP access** if supported
4. **Monitor API usage** for suspicious activity
5. **Use separate test accounts** for development
6. **Never share API keys** in chat, email, or documentation
7. **Review and revoke unused API keys** periodically
