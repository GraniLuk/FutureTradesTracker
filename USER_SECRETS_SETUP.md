# User Secrets Configuration Sample

This file shows how to configure user secrets for secure API credential storage.

## Setup User Secrets

1. Initialize user secrets (already done):
```bash
dotnet user-secrets init
```

2. Set your API credentials:

### For BingX:
```bash
dotnet user-secrets set "BingXApi:ApiKey" "your-actual-bingx-api-key"
dotnet user-secrets set "BingXApi:SecretKey" "your-actual-bingx-secret-key"
```

### For Bybit:
```bash
dotnet user-secrets set "BybitApi:ApiKey" "your-actual-bybit-api-key"
dotnet user-secrets set "BybitApi:SecretKey" "your-actual-bybit-secret-key"
```

## Alternative: Environment Variables

You can also set environment variables:

### Windows (PowerShell):
```powershell
$env:BingXApi__ApiKey = "your-actual-bingx-api-key"
$env:BingXApi__SecretKey = "your-actual-bingx-secret-key"
$env:BybitApi__ApiKey = "your-actual-bybit-api-key"
$env:BybitApi__SecretKey = "your-actual-bybit-secret-key"
```

### Linux/macOS:
```bash
export BingXApi__ApiKey="your-actual-bingx-api-key"
export BingXApi__SecretKey="your-actual-bingx-secret-key"
export BybitApi__ApiKey="your-actual-bybit-api-key"
export BybitApi__SecretKey="your-actual-bybit-secret-key"
```

## Security Notes

- Never commit real API credentials to source control
- User secrets are stored locally and are not included in the repository
- Environment variables are good for production deployments
- The application will warn you if credentials are not configured properly
