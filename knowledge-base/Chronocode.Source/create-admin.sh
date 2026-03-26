#!/bin/bash
# Create Admin User Script for Chronocode

echo "=== Chronocode Admin User Creator ==="
echo

# Function to update appsettings.json
update_admin_config() {
    local email="$1"
    local password="$2"
    local firstname="$3"
    local lastname="$4"
    
    # Create temporary appsettings with new admin config
    cat > temp_appsettings.json << EOF
{
  "ConnectionStrings": {
    "DefaultConnection": "DataSource=Data\\\\app.db;Cache=Shared"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "DefaultAdmin": {
    "Email": "$email",
    "Password": "$password",
    "FirstName": "$firstname",
    "LastName": "$lastname"
  }
}
EOF
    
    # Replace the original file
    mv temp_appsettings.json appsettings.json
    echo "✅ Updated appsettings.json with new admin credentials"
}

# Get user input
read -p "Enter admin email (default: admin@chronocode.local): " admin_email
admin_email=${admin_email:-admin@chronocode.local}

read -s -p "Enter admin password (default: Admin123!): " admin_password
echo
admin_password=${admin_password:-Admin123!}

read -p "Enter first name (default: System): " first_name
first_name=${first_name:-System}

read -p "Enter last name (default: Administrator): " last_name
last_name=${last_name:-Administrator}

echo
echo "=== Creating Admin User ==="
echo "Email: $admin_email"
echo "First Name: $first_name"
echo "Last Name: $last_name"
echo

# Update configuration
update_admin_config "$admin_email" "$admin_password" "$first_name" "$last_name"

echo "✅ Configuration updated!"
echo
echo "Now run the application:"
echo "  dotnet run"
echo
echo "The admin user will be created automatically on startup."
echo "After logging in, please change the password through the account management page."
