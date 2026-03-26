# Create Admin User Script for Chronocode
# PowerShell version for Windows

Write-Host "=== Chronocode Admin User Creator ===" -ForegroundColor Green
Write-Host

# Function to update appsettings.json
function Update-AdminConfig {
    param(
        [string]$Email,
        [string]$Password,
        [string]$FirstName,
        [string]$LastName
    )
    
    $appSettings = @{
        "ConnectionStrings" = @{
            "DefaultConnection" = "DataSource=Data\\app.db;Cache=Shared"
        }
        "Logging" = @{
            "LogLevel" = @{
                "Default" = "Information"
                "Microsoft.AspNetCore" = "Warning"
            }
        }
        "AllowedHosts" = "*"
        "DefaultAdmin" = @{
            "Email" = $Email
            "Password" = $Password
            "FirstName" = $FirstName
            "LastName" = $LastName
        }
    }
    
    $appSettings | ConvertTo-Json -Depth 3 | Set-Content -Path "appsettings.json"
    Write-Host "✅ Updated appsettings.json with new admin credentials" -ForegroundColor Green
}

# Get user input
$adminEmail = Read-Host "Enter admin email (default: admin@chronocode.local)"
if ([string]::IsNullOrWhiteSpace($adminEmail)) {
    $adminEmail = "admin@chronocode.local"
}

$adminPassword = Read-Host "Enter admin password (default: Admin123!)" -AsSecureString
if ($adminPassword.Length -eq 0) {
    $adminPassword = ConvertTo-SecureString "Admin123!" -AsPlainText -Force
}
$adminPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($adminPassword))

$firstName = Read-Host "Enter first name (default: System)"
if ([string]::IsNullOrWhiteSpace($firstName)) {
    $firstName = "System"
}

$lastName = Read-Host "Enter last name (default: Administrator)"
if ([string]::IsNullOrWhiteSpace($lastName)) {
    $lastName = "Administrator"
}

Write-Host
Write-Host "=== Creating Admin User ===" -ForegroundColor Yellow
Write-Host "Email: $adminEmail"
Write-Host "First Name: $firstName"
Write-Host "Last Name: $lastName"
Write-Host

# Update configuration
Update-AdminConfig -Email $adminEmail -Password $adminPasswordPlain -FirstName $firstName -LastName $lastName

Write-Host "✅ Configuration updated!" -ForegroundColor Green
Write-Host
Write-Host "Now run the application:" -ForegroundColor Cyan
Write-Host "  dotnet run" -ForegroundColor White
Write-Host
Write-Host "The admin user will be created automatically on startup." -ForegroundColor Yellow
Write-Host "After logging in, please change the password through the account management page." -ForegroundColor Yellow
