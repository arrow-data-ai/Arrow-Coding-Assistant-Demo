# Database Initialization

## Overview

The Chronocode application now includes automatic database initialization logic that ensures the database and all required schema are created properly on first launch.

## Features

### Automatic Database Creation
- **First Launch Detection**: The application detects if the database file exists and is accessible
- **Directory Creation**: Automatically creates the `Data` directory if it doesn't exist
- **Schema Migration**: Applies all pending EF Core migrations to ensure the database schema is up-to-date
- **Cross-Platform Support**: Works consistently across Windows, macOS, and Linux environments

### Logging and Monitoring
The initialization process includes comprehensive logging:
- Database file existence check
- Directory creation notifications
- Migration application status
- Success/failure notifications with detailed error information

## Technical Implementation

### Program.cs Integration
The database initialization is integrated into the application startup pipeline in `Program.cs`:

```csharp
// Ensure database is created and migrated on startup
await InitializeDatabaseAsync(app);
```

### Key Components

1. **InitializeDatabaseAsync Method**: Main initialization logic
   - Checks for database connectivity
   - Creates database directory if needed
   - Applies migrations as needed
   - Provides detailed logging throughout the process

2. **ExtractDatabasePath Method**: Utility function
   - Parses SQLite connection string to extract database file path
   - Handles cross-platform path separators
   - Provides fallback path if parsing fails

### Connection String Configuration
The application uses forward slashes in the connection string for better cross-platform compatibility:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DataSource=Data/app.db;Cache=Shared"
  }
}
```

## Deployment Considerations

### Containerized Environments
- The `Data` directory should be mounted as a persistent volume
- Database files are created with appropriate permissions
- Initialization happens before the web server starts accepting requests

### Development vs Production
- Development: Uses detailed logging to help with debugging
- Production: Same initialization logic ensures consistency across environments

### Project File Configuration
The project file is configured to only copy existing database files to the output directory:

```xml
<None Update="Data\app.db" CopyToOutputDirectory="PreserveNewest" ExcludeFromSingleFile="true" Condition="Exists('Data\app.db')" />
```

## Error Handling

The initialization process includes robust error handling:
- Database connectivity issues are logged and re-thrown
- File system permission errors are captured
- Migration failures are properly logged with stack traces

## Best Practices Alignment

This implementation follows the coding instructions and best practices:
- ✅ Uses EF Core migrations (never modifies schema manually)
- ✅ Protects against SQL injection through LINQ usage
- ✅ Enables logging for EF Core operations
- ✅ Uses asynchronous programming for database operations
- ✅ Works in containerized environments
- ✅ Provides comprehensive error handling and logging

## Startup Log Example

When starting the application for the first time, you'll see logs like:

```
info: Program[0]
      Database file exists: False at path: Data/app.db
info: Program[0]
      Database not accessible. Creating and migrating database...
info: Microsoft.EntityFrameworkCore.Migrations[20411]
      Acquiring an exclusive lock for migration application...
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '00000000000000_CreateIdentitySchema'.
...
info: Program[0]
      Database created and migrated successfully at: Data/app.db
```

## Migration Handling

For existing databases, the system will:
1. Check for pending migrations
2. Apply any new migrations automatically
3. Log the migration names being applied
4. Confirm successful completion

This ensures the database schema stays current with the application code without manual intervention.
