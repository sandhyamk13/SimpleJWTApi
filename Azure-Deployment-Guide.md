# Azure App Service Deployment Guide

## Prerequisites
- Azure CLI installed
- Azure subscription
- Resource group created

## Method 1: Azure CLI Deployment (Quick Start)

### 1. Login to Azure
```bash
az login
```

### 2. Create Resource Group (if not exists)
```bash
az group create --name rg-simplejwtapi --location "East US"
```

### 3. Create App Service Plan
```bash
az appservice plan create \
  --name plan-simplejwtapi \
  --resource-group rg-simplejwtapi \
  --sku B1 \
  --is-linux
```

### 4. Create Web App
```bash
az webapp create \
  --name simplejwtapi-$(date +%s) \
  --resource-group rg-simplejwtapi \
  --plan plan-simplejwtapi \
  --runtime "DOTNETCORE:8.0"
```

### 5. Configure App Settings (Replace placeholder values)
```bash
# JWT Settings
az webapp config appsettings set \
  --name YOUR_APP_NAME \
  --resource-group rg-simplejwtapi \
  --settings \
    JwtSettings__SecretKey="YourSuperSecretKeyThatIsAtLeast32CharactersLongForProduction!" \
    JwtSettings__Issuer="https://YOUR_APP_NAME.azurewebsites.net" \
    JwtSettings__Audience="SimpleJWTApiClients" \
    JwtSettings__ExpirationInMinutes="60" \
    ClientCredentials__ClientId="azure-prod-client" \
    ClientCredentials__ClientSecret="your-secure-prod-client-secret-123456"
```

### 6. Deploy from Local Git
```bash
# Enable local git deployment
az webapp deployment source config-local-git \
  --name YOUR_APP_NAME \
  --resource-group rg-simplejwtapi

# Get git URL
az webapp deployment list-publishing-credentials \
  --name YOUR_APP_NAME \
  --resource-group rg-simplejwtapi
```

### 7. Deploy Code
```bash
# From your project directory
git init
git add .
git commit -m "Initial Azure deployment"
git remote add azure https://YOUR_APP_NAME.scm.azurewebsites.net/YOUR_APP_NAME.git
git push azure main
```

## Method 2: Visual Studio Publish Profile

1. Right-click project → Publish
2. Select Azure → Azure App Service (Linux)
3. Create new or select existing App Service
4. Configure settings and publish

## Method 3: GitHub Actions (CI/CD)

See `.github/workflows/azure-deploy.yml` for automated deployment

## Environment Variables for Production

Set these in Azure App Service Configuration:

| Setting | Value | Description |
|---------|--------|-------------|
| `JwtSettings__SecretKey` | `Strong-Secret-Key-256-bits-minimum!` | JWT signing key |
| `JwtSettings__Issuer` | `https://yourapp.azurewebsites.net` | Token issuer |
| `JwtSettings__Audience` | `YourApiClients` | Token audience |
| `ClientCredentials__ClientId` | `prod-client-id` | Production client ID |
| `ClientCredentials__ClientSecret` | `secure-secret` | Production client secret |

## Post-Deployment Steps

1. Test endpoints at: `https://YOUR_APP_NAME.azurewebsites.net/swagger`
2. Update CORS settings if needed
3. Configure custom domain (optional)
4. Set up Application Insights for monitoring
5. Configure scaling rules

## Troubleshooting

- Check App Service logs: `az webapp log tail --name YOUR_APP_NAME --resource-group rg-simplejwtapi`
- Verify environment variables in Azure portal
- Ensure HTTPS is enforced for JWT tokens