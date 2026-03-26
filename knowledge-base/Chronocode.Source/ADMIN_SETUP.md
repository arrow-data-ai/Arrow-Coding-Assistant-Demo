# Admin User Setup Guide

## Overview
The Chronocode application automatically creates a default admin user on first startup if no admin users exist in the system.

## Quick Start (Default Admin)

The application will automatically create an admin user with these default credentials:

- **Email**: `admin@chronocode.local`
- **Password**: `Admin123!`

Simply run the application and use these credentials to log in:

```bash
dotnet run
```

Then navigate to `http://localhost:5106` and log in with the default credentials.

## Custom Admin User Setup

### Method 1: Using Configuration File

Edit `appsettings.json` and modify the `DefaultAdmin` section:

```json
{
  "DefaultAdmin": {
    "Email": "your-admin@company.com",
    "Password": "YourSecurePassword123!",
    "FirstName": "Your",
    "LastName": "Name"
  }
}
```

### Method 2: Using Environment Variables

Set these environment variables before running the application:

```bash
export DefaultAdmin__Email="your-admin@company.com"
export DefaultAdmin__Password="YourSecurePassword123!"
export DefaultAdmin__FirstName="Your"
export DefaultAdmin__LastName="Name"
```

### Method 3: Using Setup Scripts

#### For macOS/Linux:
```bash
./create-admin.sh
```

#### For Windows:
```powershell
.\create-admin.ps1
```

## After First Login

1. **Change Password**: Navigate to Account → Manage Account and change the default password
2. **User Management**: Go to User Management (admin menu) to assign roles to other users
3. **Project Assignment**: Use the "Manage" button on projects to assign engineers

## Admin Capabilities

- **User Management**: Assign roles (Admin, Engineer, Manager) to users
- **Project Access**: View and manage all projects regardless of ownership
- **Engineer Assignment**: Assign engineers to specific projects
- **Full System Access**: Access to all features and data

## Security Notes

- **Change Default Password**: Always change the default password after first login
- **Strong Passwords**: Use strong, unique passwords for admin accounts
- **Role Assignment**: Only assign admin roles to trusted users
- **Regular Review**: Periodically review user roles and project assignments

## Troubleshooting

### Admin User Not Created
- Check application logs for error messages
- Verify database connectivity
- Ensure proper permissions on database filedo 

### Cannot Access Admin Features
- Verify user has "Admin" role assigned
- Check navigation menu for "User Management" link
- Try logging out and back in

### Password Requirements
Default password policy requires:
- At least 6 characters
- At least one uppercase letter
- At least one lowercase letter  
- At least one digit
- At least one special character

## Production Deployment

For production environments:

1. **Never use default credentials** - Always set custom admin credentials
2. **Use environment variables** for sensitive configuration
3. **Enable HTTPS** for all authentication
4. **Regular backups** of user and role data
5. **Monitor admin actions** through application logs
