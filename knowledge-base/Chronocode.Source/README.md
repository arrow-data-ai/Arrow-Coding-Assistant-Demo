# Chronocode

**Professional Time Tracking for Engineering Teams and Project-Based Organizations**

---

## 📋 What is Chronocode?

Chronocode is a comprehensive time tracking application built for engineering teams and project-based organizations that need precise, auditable time records across multiple projects and billing codes. It provides a structured approach to managing project time with flexible charge code allocation and powerful reporting capabilities.

### Line Card Summary

> **Chronocode** is a Blazor Server web application that enables teams to track time against projects, tasks, and charge codes. It features automated time distribution across multiple billing codes, comprehensive timecard reporting, and an intuitive interface designed for collaborative project time allocation management.

---

## 🎯 Key Features

### Core Capabilities
- **Project-Based Organization**: Structure work hierarchically with Projects → Tasks → Activities
- **Flexible Time Allocation**: Distribute hours across multiple charge codes automatically
- **Active/Inactive Charge Code Management**: Time-bound validity for charge codes
- **Comprehensive Reporting**: Activity reports, task reports, and charge code analysis
- **Built-in Timer**: Start/stop functionality for real-time activity tracking
- **Team Collaboration**: Assign engineers and managers to projects

### Advanced Features
- Activity duplication for faster data entry
- Quick task creation from activity dialog
- CSV export and print functionality for reports
- Confirmation dialogs to prevent accidental deletions
- Integrated help system with comprehensive documentation
- Audit trail for all activities

---

## 🏗️ High-Level Architecture

### Technology Stack

**Frontend:**
- **Blazor Server** (.NET 9.0) - Interactive server-side UI framework
- **Bootstrap** - Responsive UI styling and components
- **Razor Components** - Server-rendered interactive components

**Backend:**
- **ASP.NET Core 9.0** - Web application framework
- **Entity Framework Core** - ORM for data access
- **SQLite** - Embedded database for data persistence
- **ASP.NET Core Identity** - Authentication and user management

**Deployment:**
- **Docker** - Containerized deployment
- **Linux** - Target deployment platform
- **HTTPS** - Secure communication

### Application Layers

```
┌─────────────────────────────────────────────────┐
│           Blazor Components Layer                │
│  (Pages, Shared Components, User Interface)      │
└──────────────────┬──────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────┐
│              Services Layer                      │
│  ProjectService, TaskService, ActivityService,   │
│  ChargeCodeService, ReportService, etc.          │
└──────────────────┬──────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────┐
│          Data Access Layer (EF Core)             │
│  ApplicationDbContext, Repositories              │
└──────────────────┬──────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────┐
│              SQLite Database                     │
│  (Users, Projects, Tasks, Activities, etc.)      │
└─────────────────────────────────────────────────┘
```

### Data Model Overview

The application uses a relational data model with the following core entities:

- **Project**: Container for tasks and charge codes; owned by a user
- **ProjectTask**: Specific work items within a project
- **Activity**: Time entries against tasks with duration and date
- **ChargeCode**: Billing codes with validity date ranges
- **ActivityChargeCode**: Many-to-many relationship for time allocation
- **User (ApplicationUser)**: Identity-based user with role assignments
- **ProjectEngineer/ProjectManager**: Role assignments at project level

### Key Relationships

```
Project 1──────* Task
   │              │
   │              │
   └──* ChargeCode│
                  │
               Activity ──* ActivityChargeCode ──* ChargeCode
                  │
                  └─── User
```

---

## ⏱️ Time Tracking Approach

### Conceptual Model

Chronocode uses a **hierarchical project-based time tracking model** where:

1. **Projects** define the scope of work and contain both tasks and charge codes
2. **Tasks** represent discrete work items or deliverables within a project
3. **Activities** are actual time entries that:
   - Reference a specific task
   - Have a duration in hours (decimal format: 2.5 = 2 hours 30 minutes)
   - Are logged on a specific date
   - Can be allocated across one or more charge codes

### Charge Code Allocation

A unique feature of Chronocode is its flexible charge code allocation system:

- **Single Charge Code**: All activity time goes to one billing code
- **Multiple Charge Codes**: Time is automatically distributed evenly across selected codes
- **Example**: A 4-hour activity with 2 charge codes = 2 hours per code

This **even distribution approach** (also known as "peanut butter spread" - the default behavior) simplifies time entry while maintaining accurate billing records across multiple charge codes.

### Charge Code Validity

Charge codes have time-bound validity:
- **ValidFromDate**: When the charge code becomes active
- **ValidToDate**: When the charge code expires
- **IsActive**: Administrative control for enabling/disabling codes

Users can only allocate time to charge codes that are:
1. Active (IsActive = true)
2. Valid for the activity date (between ValidFromDate and ValidToDate)

### Role-Based Access (Upcoming Feature)

The system is planned to implement three role levels:

1. **Admin**: Full system access, user management, all projects
2. **Manager**: Project management, view team activities, reporting
3. **Engineer**: Time entry, view assigned projects, personal reporting

> **Note**: Full role-based access control is currently under development. The data model includes support for ProjectEngineer/ProjectManager assignments and ASP.NET Core Identity roles, but enforcement of these permissions is not yet fully implemented.

### Workflow

```
1. Administrator creates Projects and Charge Codes
                ↓
2. Manager/Admin assigns Engineers to Projects
                ↓
3. Manager/Admin creates Tasks within Projects
                ↓
4. Engineers log Activities against Tasks
                ↓
5. System allocates time across selected Charge Codes
                ↓
6. Reports generated for timecards and billing
```

---

## 🚀 Getting Started

### Prerequisites

- .NET 9.0 SDK
- SQLite (included with .NET)
- Modern web browser (Chrome, Edge, Firefox)
- Docker (optional, for containerized deployment)

### Running Locally

```bash
# Clone the repository
git clone <repository-url>
cd chronocode

# Restore dependencies
dotnet restore

# Run the application
cd Chronocode.Source
dotnet run
```

The application will be available at `https://localhost:5001`

### Initial Setup

1. **First Run**: Database is automatically created on startup
2. **Default Admin**: Created automatically (check configuration for credentials)
3. **Create Projects**: Add your first project as Admin
4. **Assign Users**: Add engineers and managers to projects
5. **Start Tracking**: Engineers can begin logging activities

---

## 📁 Project Structure

```
chronocode/
├── Chronocode.Source/          # Main application
│   ├── Components/             # Blazor components and pages
│   │   ├── Account/           # Authentication components
│   │   ├── Layout/            # Layout components
│   │   └── Pages/             # Application pages
│   ├── Controllers/           # API controllers
│   ├── Data/                  # Data access layer
│   │   ├── Models/           # Entity models
│   │   └── Migrations/       # EF Core migrations
│   ├── Services/             # Business logic services
│   ├── Documentation/        # User documentation (Markdown)
│   ├── wwwroot/             # Static assets (CSS, JS, images)
│   └── Program.cs           # Application entry point
├── Chronocode.Tests/        # Unit and integration tests
├── tests/                   # Additional test files
└── chronocode.sln          # Visual Studio solution file
```

---

## 📚 Documentation

Comprehensive user documentation is available in the `Chronocode.Source/Documentation/` directory:

- **[Getting Started Guide](Chronocode.Source/Documentation/Getting-Started.md)** - First-time setup
- **[Project Management](Chronocode.Source/Documentation/Project-Management.md)** - Managing projects
- **[Task Management](Chronocode.Source/Documentation/Task-Management.md)** - Working with tasks
- **[Activity Tracking](Chronocode.Source/Documentation/Activity-Tracking.md)** - Logging time
- **[Charge Code Management](Chronocode.Source/Documentation/Charge-Code-Management.md)** - Billing codes
- **[Reporting](Chronocode.Source/Documentation/Reporting.md)** - Generating reports
- **[User Management](Chronocode.Source/Documentation/User-Management.md)** - Admin user operations
- **[System Administration](Chronocode.Source/Documentation/System-Administration.md)** - Admin tasks
- **[Best Practices](Chronocode.Source/Documentation/Best-Practices.md)** - Tips and recommendations
- **[FAQ](Chronocode.Source/Documentation/FAQ.md)** - Common questions
- **[Troubleshooting](Chronocode.Source/Documentation/Troubleshooting.md)** - Problem resolution

---

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

Test coverage reports are available in `COVERAGE-STATUS.md` and `SONARQUBE-COVERAGE.md`.

---

## 🔒 Security Features

- **ASP.NET Core Identity**: Secure user authentication and password management
- **Anti-Forgery Tokens**: CSRF protection on all forms
- **Parameterized Queries**: SQL injection prevention via EF Core
- **HTTPS**: Encrypted communication in production
- **Audit Trail**: Activity tracking for compliance

### Upcoming Security Features
- **Role-Based Authorization**: Three-tier access control system (Admin, Manager, Engineer)
- **Data Isolation**: Project-level access restrictions based on user assignments

---

## 📈 Version History

Current version: **1.1.0**

See [CHANGELOG](Chronocode.Source/Documentation/CHANGELOG.md) for detailed version history and release notes.

---

## 🤝 Contributing

This is a private project. For questions or issues, contact the repository owner.

---

## 📝 License

Copyright © 2025. All rights reserved.

---

## 🛠️ Development Tools

- **IDE**: Visual Studio 2022, Visual Studio Code, or JetBrains Rider
- **Database Viewer**: DB Browser for SQLite
- **Version Control**: Git
- **Testing**: xUnit, Moq, Coverlet
- **Code Quality**: SonarQube integration available

---

## 📞 Support

- **In-App Help**: Click the `?` icon in the navigation bar
- **Documentation**: Browse the Documentation folder
- **Administrator**: Contact your system administrator for access issues

---

*Built with ❤️ using .NET 9.0 and Blazor Server*
