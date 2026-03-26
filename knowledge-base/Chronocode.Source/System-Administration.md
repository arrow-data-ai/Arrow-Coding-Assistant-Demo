# System Administration Guide

This guide provides comprehensive information for system administrators managing Chronocode installations, user accounts, and system configuration.

## 🔧 System Overview

Chronocode is a Blazor Server application built on ASP.NET Core with Entity Framework and SQLite database. It provides professional time tracking with role-based access control and project management capabilities.

### Technology Stack
- **Framework**: ASP.NET Core 9.0 (Blazor Server)
- **Database**: SQLite with Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **UI Framework**: Bootstrap 5
- **JavaScript Libraries**: Font Awesome, Bootstrap JS

### System Architecture
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Web Browser   │    │  Blazor Server  │    │   SQLite DB     │
│                 │◄──►│                 │◄──►│                 │
│ • User Interface│    │ • Business Logic│    │ • Data Storage  │
│ • Client Logic  │    │ • Authentication│    │ • User Data     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## Initial Setup and Installation

### Prerequisites
- **Operating System**: Windows, Linux, or macOS
- **.NET Runtime**: .NET 9.0 or later
- **Database**: SQLite (included)
- **Web Server**: IIS, Nginx, or Kestrel
- **HTTPS Certificate**: For production deployments

### Installation Steps

#### 1. Download and Extract
```bash
# Extract application files to desired directory
cd /opt/chronocode  # Linux example
# or C:\inetpub\chronocode  # Windows example
```

#### 2. Database Setup
```bash
# Run database migrations
dotnet ef database update

# Or use the included migration scripts
./setup-database.sh  # Linux
setup-database.bat   # Windows
```

#### 3. Configuration
Edit `appsettings.json` for your environment:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=Data/app.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

#### 4. Create Initial Admin User
```bash
# Use the provided script
./create-admin.sh admin@company.com "SecurePassword123!"

# Or PowerShell on Windows
.\create-admin.ps1 -Email "admin@company.com" -Password "SecurePassword123!"
```

### First Launch Verification
1. **Start Application**: Run the application
2. **Access URL**: Navigate to configured URL
3. **Admin Login**: Log in with created admin account
4. **System Check**: Verify all features work correctly

## User Account Management

### Admin Account Creation
The system automatically creates an admin account on first startup. You can also create additional admin accounts:

#### Using Built-in Tools
1. **Register New User**: Have user register normally
2. **Assign Admin Role**: Use User Management interface
3. **Verify Access**: Confirm admin capabilities work

#### Using Command Line Scripts
```bash
# Create admin user (Linux/macOS)
./create-admin.sh newadmin@company.com "Password123!"

# Windows PowerShell
.\create-admin.ps1 -Email "newadmin@company.com" -Password "Password123!"
```

### Role Management Strategy

#### Default Roles
- **Admin**: Full system access and user management
- **Manager**: Project management and team oversight
- **Engineer**: Time tracking on assigned projects
- **User**: Basic access to owned projects

#### Role Assignment Guidelines
```
New Employee Process:
1. User registers account
2. Admin assigns Engineer role
3. Project manager assigns to projects
4. User begins time tracking

Management Assignment:
1. Promote existing Engineer to Manager
2. Assign to managed projects
3. Provide manager training
4. Grant appropriate project access
```

### Bulk User Operations

#### CSV Import (Custom Implementation)
While not built-in, you can implement bulk user import:
```csharp
// Example bulk user creation
foreach (var userRecord in csvData)
{
    var user = new ApplicationUser 
    { 
        Email = userRecord.Email,
        UserName = userRecord.Email,
        FullName = userRecord.FullName
    };
    
    await userManager.CreateAsync(user, defaultPassword);
    await userManager.AddToRoleAsync(user, userRecord.Role);
}
```

## Database Administration

### Database Location
- **Development**: `Data/app.db`
- **Production**: Configure in `appsettings.json`

### Backup Procedures

#### Regular Backups
```bash
#!/bin/bash
# Daily backup script
DATE=$(date +%Y%m%d_%H%M%S)
cp Data/app.db "backups/app_${DATE}.db"

# Keep only last 30 days
find backups/ -name "app_*.db" -mtime +30 -delete
```

#### Before Major Changes
```bash
# Backup before updates
cp Data/app.db Data/app_pre_update_$(date +%Y%m%d).db
```

### Database Maintenance

#### Regular Maintenance Tasks
```sql
-- Vacuum database (SQLite maintenance)
VACUUM;

-- Analyze for query optimization
ANALYZE;

-- Check database integrity
PRAGMA integrity_check;
```

#### Performance Monitoring
```sql
-- Check database size
SELECT page_count * page_size as size FROM pragma_page_count(), pragma_page_size();

-- Monitor table sizes
SELECT name, SUM("pgsize") as size 
FROM "dbstat" 
GROUP BY name 
ORDER BY size DESC;
```

### Migration Management

#### Applying Updates
```bash
# Check pending migrations
dotnet ef migrations list

# Apply migrations
dotnet ef database update

# Rollback if needed (specify target migration)
dotnet ef database update PreviousMigrationName
```

#### Custom Migrations
```bash
# Create new migration
dotnet ef migrations add DescriptiveName

# Review generated migration before applying
# Edit migration file if needed
# Apply migration
dotnet ef database update
```

## Security Configuration

### Authentication Settings

#### Password Policy
Configure in `Program.cs`:
```csharp
builder.Services.AddIdentityCore<ApplicationUser>(options => 
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredUniqueChars = 4;
    
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false; // Set true for email confirmation
})
```

#### Session Configuration
```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only
});
```

### HTTPS Configuration

#### Development
```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://localhost:5001"
      }
    }
  }
}
```

#### Production
```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:443",
        "Certificate": {
          "Path": "/path/to/certificate.pfx",
          "Password": "certificate_password"
        }
      }
    }
  }
}
```

### Security Headers
Add security headers in `Program.cs`:
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    
    await next();
});
```

## System Monitoring

### Application Logging

#### Log Configuration
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    },
    "File": {
      "Path": "logs/chronocode-.log",
      "MinLevel": "Information"
    }
  }
}
```

#### Log Analysis
```bash
# Monitor application logs
tail -f logs/chronocode-$(date +%Y%m%d).log

# Search for errors
grep -i "error\|exception" logs/chronocode-*.log

# Monitor login attempts
grep -i "login\|authentication" logs/chronocode-*.log
```

### Performance Monitoring

#### Key Metrics to Monitor
- **Response Times**: Page load and API response times
- **Memory Usage**: Application memory consumption
- **Database Performance**: Query execution times
- **User Activity**: Login frequency and feature usage
- **Error Rates**: Application errors and exceptions

#### Health Checks
```csharp
// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddCheck("disk_space", () => 
    {
        var drive = new DriveInfo("C:");
        return drive.AvailableFreeSpace > 1_000_000_000 
            ? HealthCheckResult.Healthy() 
            : HealthCheckResult.Unhealthy("Low disk space");
    });

// Configure health check endpoint
app.MapHealthChecks("/health");
```

### System Alerts

#### Disk Space Monitoring
```bash
#!/bin/bash
# Check disk space and alert if low
THRESHOLD=90
USAGE=$(df / | tail -1 | awk '{print $5}' | sed 's/%//')

if [ $USAGE -gt $THRESHOLD ]; then
    echo "Disk usage is ${USAGE}% - exceeds threshold of ${THRESHOLD}%"
    # Send alert email or notification
fi
```

#### Database Size Monitoring
```sql
-- Monitor database growth
SELECT 
    datetime('now') as check_time,
    (page_count * page_size) / 1024 / 1024 as size_mb
FROM pragma_page_count(), pragma_page_size();
```

## Backup and Recovery

### Backup Strategy

#### Automated Daily Backups
```bash
#!/bin/bash
# /etc/cron.daily/chronocode-backup
BACKUP_DIR="/backups/chronocode"
DATE=$(date +%Y%m%d_%H%M%S)

# Create backup directory
mkdir -p "$BACKUP_DIR"

# Backup database
cp /opt/chronocode/Data/app.db "$BACKUP_DIR/app_${DATE}.db"

# Backup configuration
cp /opt/chronocode/appsettings.json "$BACKUP_DIR/appsettings_${DATE}.json"

# Compress old backups
find "$BACKUP_DIR" -name "*.db" -mtime +7 -exec gzip {} \;

# Delete old backups
find "$BACKUP_DIR" -name "*.gz" -mtime +90 -delete
```

#### Weekly Full Backups
```bash
#!/bin/bash
# Weekly full backup including uploaded files
tar -czf "/backups/chronocode_full_$(date +%Y%m%d).tar.gz" \
    /opt/chronocode/Data/ \
    /opt/chronocode/wwwroot/uploads/ \
    /opt/chronocode/appsettings.json
```

### Recovery Procedures

#### Database Recovery
```bash
# Stop application
systemctl stop chronocode

# Restore database from backup
cp /backups/chronocode/app_20240101_120000.db /opt/chronocode/Data/app.db

# Restart application
systemctl start chronocode

# Verify functionality
curl -f https://your-chronocode-url/health
```

#### Disaster Recovery
```bash
# Full system restoration
tar -xzf /backups/chronocode_full_20240101.tar.gz -C /

# Update file permissions
chown -R chronocode:chronocode /opt/chronocode

# Start services
systemctl start chronocode
```

## System Updates and Maintenance

### Application Updates

#### Update Process
1. **Backup Current System**: Full backup before updates
2. **Download New Version**: Get latest application files
3. **Stop Application**: Gracefully shutdown current version
4. **Apply Database Migrations**: Update database schema
5. **Update Application Files**: Replace with new version
6. **Test Functionality**: Verify all features work
7. **Monitor**: Watch for issues post-update

#### Rolling Back Updates
```bash
# Stop current version
systemctl stop chronocode

# Restore previous version
cp -r /opt/chronocode_backup/* /opt/chronocode/

# Rollback database if needed
cp /backups/chronocode/app_pre_update.db /opt/chronocode/Data/app.db

# Start previous version
systemctl start chronocode
```

### Maintenance Windows

#### Regular Maintenance Tasks
- **Weekly**: Database maintenance, log cleanup
- **Monthly**: Security updates, user account review
- **Quarterly**: Full system backup verification, performance review
- **Annually**: Security audit, disaster recovery testing

#### Planned Downtime
```bash
# Maintenance mode page
echo "System maintenance in progress. Please try again in 30 minutes." > /var/www/maintenance.html

# Redirect traffic to maintenance page
# (Configure in web server)

# Perform maintenance
# ...

# Remove maintenance mode
rm /var/www/maintenance.html
```

## Troubleshooting

### Common Issues

#### Application Won't Start
**Check**:
- .NET runtime installation
- Database connectivity
- File permissions
- Port availability
- Certificate configuration

**Logs**:
```bash
# Check system logs
journalctl -u chronocode -f

# Check application logs
tail -f /opt/chronocode/logs/chronocode-$(date +%Y%m%d).log
```

#### Database Issues
**Symptoms**: Slow performance, corruption errors
**Solutions**:
```sql
-- Check database integrity
PRAGMA integrity_check;

-- Rebuild database
VACUUM;

-- Update statistics
ANALYZE;
```

#### Memory Issues
**Symptoms**: High memory usage, out of memory errors
**Solutions**:
```bash
# Monitor memory usage
top -p $(pgrep -f chronocode)

# Check for memory leaks
dotnet-dump collect -p $(pgrep -f chronocode)

# Restart application if needed
systemctl restart chronocode
```

### Performance Optimization

#### Database Optimization
```sql
-- Add indexes for common queries
CREATE INDEX IF NOT EXISTS idx_activities_user_date 
ON Activities(UserId, ActivityDate);

CREATE INDEX IF NOT EXISTS idx_activities_project 
ON Activities(TaskId);
```

#### Application Optimization
```csharp
// Enable response compression
builder.Services.AddResponseCompression();

// Configure caching
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();
```

## Integration and APIs

### External System Integration

#### LDAP/Active Directory
```csharp
// Configure LDAP authentication
builder.Services.AddAuthentication()
    .AddLdap(options =>
    {
        options.Server = "ldap://your-domain-controller";
        options.Domain = "your-domain.com";
        // Additional LDAP configuration
    });
```

#### Email Configuration
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.company.com",
    "SmtpPort": 587,
    "Username": "chronocode@company.com",
    "Password": "smtp_password",
    "EnableSsl": true
  }
}
```

### Data Export APIs

#### Custom Export Endpoints
```csharp
[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    [HttpGet("timesheet/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportUserTimesheet(string userId, DateTime start, DateTime end)
    {
        // Implementation for exporting user timesheet data
        // Return CSV, JSON, or other required format
    }
}
```

## Version Management and Changelog

### Application Versioning

Chronocode uses Semantic Versioning (SemVer) for version numbering:
- **Format**: MAJOR.MINOR.PATCH (e.g., 1.2.0)
- **MAJOR**: Breaking changes or major new features
- **MINOR**: New features, backward compatible
- **PATCH**: Bug fixes and minor improvements

### Version Information Display

#### Accessing Version Info
- **Home Page**: Version displayed in footer or about section
- **Admin Panel**: System information section
- **Help Menu**: Version and changelog links

#### Version Service
The `VersionService` automatically:
- Reads version from CHANGELOG.md
- Parses semantic version numbers
- Categorizes versions by type (Major, Minor, Patch)
- Provides version history

### Changelog Management

#### CHANGELOG.md Format
Located in `Documentation/CHANGELOG.md`:
```markdown
## [1.2.0] - 2026-02-07

### Added
- New features and capabilities

### Changed
- Modifications to existing features

### Fixed
- Bug fixes and corrections

### Security
- Security updates and patches
```

#### Changelog Service
The `ChangelogService` automatically:
- Parses markdown changelog
- Extracts version information
- Groups changes by category
- Provides recent version history
- Displays detailed release notes

#### Updating Changelog
When releasing new versions:
1. **Add New Version Section** at top of CHANGELOG.md
2. **Use Proper Format**: Follow Keep a Changelog standard
3. **Include Date**: Format as YYYY-MM-DD
4. **Categorize Changes**: Use Add/Change/Fix/Security sections
5. **Link Versions**: Include comparison URLs at bottom
6. **Update Version Info**: Ensure version matches in all locations

#### Example Update:
```markdown
## [1.3.0] - 2026-03-15

### Added
- New reporting dashboard with visualizations
- Export to Excel functionality

### Changed
- Improved charge code selection performance
- Updated user interface styling

### Fixed
- Issue #45: Resolved timezone handling in reports
- Corrected activity duplication date handling

### Security
- Updated authentication token expiration
- Enhanced input validation
```

### Version Display Features

#### Version Modal
- **Trigger**: Click version number in footer
- **Displays**:
  - Current version number
  - Release date
  - Recent changelog entries
  - Link to full changelog

#### Recent Versions List
- Shows last 5-10 versions
- Version number and release date
- High-level summary of changes
- Quick access to details

### Managing Application Files

#### Work Authorization Artifacts Storage

**Default Location**: `wwwroot/uploads/artifacts/`

**File Management**:
- Files stored with GUID-based names
- Original filenames tracked in database
- Metadata includes: uploader, upload date, description
- Maximum file size: 10MB

**Storage Monitoring**:
```bash
# Check storage usage
du -sh wwwroot/uploads/artifacts/

# Count stored files
find wwwroot/uploads/artifacts/ -type f | wc -l

# Find large files
find wwwroot/uploads/artifacts/ -type f -size +5M -exec ls -lh {} \;
```

**Storage Cleanup**:
- Delete artifacts through application UI (automatic cleanup)
- Orphaned files (database record deleted): Require manual cleanup
- Implement retention policies as needed

**Backup Considerations**:
- Include `wwwroot/uploads/` in backup strategy
- Database backup includes artifact metadata
- File backup includes actual artifact files
- Test restore procedures regularly

#### File Security
- **Access Control**: Project-based permissions
- **MIME Validation**: Server-side file type checking
- **Extension Verification**: Double-check file extensions
- **Path Traversal Prevention**: Sanitized filenames
- **Size Limits**: Enforced server-side (10MB)

---

*For advanced system administration topics or specific deployment scenarios, contact the development team or refer to the deployment documentation.*
