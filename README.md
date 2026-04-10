# EFNco — Efficient Facility Network Control
### Sprint 1: Authentication & User Management

> A digital parking permit and access management system designed to replace outdated sticker-based verification processes in universities, residential communities, and office facilities.

---

## About the Project

**EFNco (Efficient Facility Network Control)** is a web-based facility management platform built with ASP.NET Core MVC. It streamlines the entire parking workflow — from permit application to gate verification — using secure digital permits instead of physical stickers.

This repository covers **Sprint 1**, which focuses on the authentication system, user registration, role-based access control, and admin user management.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 MVC |
| Language | C# |
| Authentication | ASP.NET Core Identity |
| Database | SQL Server (Entity Framework Core 8) |
| Frontend | Razor Views, HTML5, CSS3 |
| Client Libraries | jQuery 3.7.1, Bootstrap 5.3.2, jQuery Validation |
| Library Manager | libman (Microsoft Library Manager) |
| IDE | Visual Studio 2022 |

---

## Sprint 1 — Features Implemented

### 1. Authentication
- User login with email and password
- Remember Me (persistent cookie)
- Account lockout after 5 failed attempts (10 minute lockout)
- Secure logout / sign out

### 2. Registration
- New user registration (First Name, Last Name, Email, Department)
- Password validation — minimum 6 characters, requires uppercase, lowercase, and digit
- Duplicate email prevention
- Auto sign-in after successful registration

### 3. User Profile
- View and edit personal profile (name, department, phone number)
- Change password with current password verification

### 4. Role-Based Access Control (RBAC)
- Admin role seeded automatically on first run
- Default admin account seeded on startup
- Route protection — unauthenticated users redirected to landing page
- Access denied page for unauthorized access attempts
- Role-based sidebar navigation (Admin sees extra menu items)

### 5. Admin — User Management
- View all registered users in a table
- View detailed user information
- Edit user (name, department, role assignment, active/inactive status)
- Delete user (with self-delete protection)
- Reset any user's password

### 6. Public Landing Page
- EFNco introduction and feature overview
- Sign In and Register call-to-action buttons
- How It Works section (4-step process)
- Responsive design for mobile and desktop

---

## Setup & Installation

### Prerequisites
- Visual Studio 2022 (v17.0 or higher)
- .NET 8 SDK
- SQL Server or SQL Server Express

---

### Step 1 — Clone / Open the Project
Open `EFNco.sln` in Visual Studio 2022.

---

### Step 2 — Restore Client-Side Libraries
EFNco uses **libman (Library Manager)** for frontend dependencies.

**In Visual Studio:**
> Right-click `libman.json` in Solution Explorer → **Restore Client-Side Libraries**

This downloads the following into `wwwroot/lib/`:
- jQuery 3.7.1
- jQuery Validation 1.19.5
- jQuery Validation Unobtrusive 4.0.0
- Bootstrap 5.3.2

**Or via CLI:**
```bash
dotnet tool install -g Microsoft.Web.LibraryManager.Cli
libman restore
```

---

### Step 3 — Configure the Database
Open `appsettings.json` and update the connection string with your SQL Server instance:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER\\SQLEXPRESS; Database=EFNcoDB; Trusted_Connection=True; TrustServerCertificate=Yes;"
}
```
Replace `YOUR_SERVER` with your machine name (e.g. `DESKTOP-ABC123` or `localhost`).

---

### Step 4 — Run Database Migrations
Open **Package Manager Console** (Tools → NuGet Package Manager → Package Manager Console):
```powershell
Add-Migration InitialMigration
Update-Database
```

Or via .NET CLI:
```bash
dotnet ef migrations add InitialMigration
dotnet ef database update
```

---

### Step 5 — Run the Application
Press **F5** in Visual Studio or run:
```bash
dotnet run
```
The app will launch at `https://localhost:7001`

---

## Default Admin Credentials

| Field    | Value            |
|----------|------------------|
| Email    | admin@efnco.com  |
| Password | Admin@123        |

> The admin account and Admin role are automatically seeded on first run via `Data/SeedData.cs`. Remove or secure this before any production deployment.

---

## Project Structure

```
EFNco/
├── Controllers/
│   ├── AccountController.cs       # Login, Register, Logout, Profile, Change Password
│   ├── AdminController.cs         # User management (Admin role only)
│   └── HomeController.cs          # Landing page + authenticated dashboard
├── Data/
│   ├── ApplicationDbContext.cs    # EF Core DbContext (extends IdentityDbContext)
│   └── SeedData.cs                # Seeds Admin role and default admin account
├── Models/
│   ├── ApplicationUser.cs         # Extended Identity user (FirstName, LastName, Department, IsActive)
│   ├── AccountViewModels.cs       # LoginVM, RegisterVM, ProfileVM, ChangePasswordVM
│   └── AdminViewModels.cs         # UserListVM, EditUserVM
├── Views/
│   ├── Account/
│   │   ├── Login.cshtml           # Sign in page
│   │   ├── Register.cshtml        # Registration page
│   │   ├── Profile.cshtml         # Edit profile
│   │   ├── ChangePassword.cshtml  # Change password
│   │   └── AccessDenied.cshtml    # 403 page
│   ├── Admin/
│   │   ├── Users.cshtml           # All users table
│   │   ├── UserDetails.cshtml     # Single user view
│   │   ├── EditUser.cshtml        # Edit user form
│   │   ├── DeleteUser.cshtml      # Delete confirmation
│   │   └── ResetUserPassword.cshtml # Admin password reset
│   ├── Home/
│   │   ├── Landing.cshtml         # Public landing page
│   │   ├── Index.cshtml           # Authenticated dashboard
│   │   └── Privacy.cshtml         # Privacy policy
│   └── Shared/
│       ├── _Layout.cshtml         # Main layout with sidebar navigation
│       ├── Error.cshtml           # Error page
│       └── _ValidationScriptsPartial.cshtml
├── wwwroot/
│   ├── css/site.css               # EFNco custom dark theme
│   ├── js/site.js                 # Utility scripts (auto-dismiss alerts)
│   └── lib/                       # Restored by libman.json (jQuery, Bootstrap)
├── Properties/
│   └── launchSettings.json        # Dev server config
├── appsettings.json               # App configuration & connection string
├── appsettings.Development.json   # Development overrides
├── libman.json                    # Client-side library manager config
├── Program.cs                     # App entry point, DI setup, Identity config
└── EFNco.csproj                   # Project file & NuGet dependencies
```

---

## Roles & Access (Sprint 1)

| Role | Access Level |
|------|-------------|
| Admin | Full access — dashboard, user management, profile |
| (No Role) | Limited — dashboard and own profile only |

> Additional roles (Security Guard, Vehicle Owner, Faculty/Staff) will be introduced in Sprint 2.

---

## Project Sprint Roadmap

| Sprint | Feature | Status |
|--------|---------|--------|
| 1 | Login, Register, Role-based Auth, Admin User Management | ✅ Complete |
| 2 | Permit Application & Management | 🔜 Next |
| 3 | QR / Digital Permit Generation | Upcoming |
| 4 | Gate Verification & Entry/Exit Logging | Upcoming |
| 5 | Admin Dashboard & Analytics | Upcoming |
| 6 | Violation Notifications & Enforcement | Upcoming |
| 7 | Authorized Person Access & Duration Tracking | Upcoming |

---

## NuGet Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.0 | Identity with EF Core |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | SQL Server provider |
| Microsoft.EntityFrameworkCore.Tools | 8.0.0 | Migrations CLI |
| Microsoft.AspNetCore.Identity.UI | 8.0.0 | Identity UI scaffolding |

---

##  Developer Notes

- All CSS `@keyframes` and `@media` rules are placed in `wwwroot/css/site.css` — **never inside `.cshtml` files** as Razor interprets `@` as C# syntax. If you must write CSS in a `.cshtml` file, use `@@keyframes` and `@@media` instead.
- The `wwwroot/lib/` folder is intentionally empty in source control — run libman restore to populate it.
- Password hashing is handled automatically by ASP.NET Core Identity — passwords are never stored in plain text.
- The default admin credentials in `SeedData.cs` and the hint on the Login page **must be removed before production deployment**.

---

*EFNco — Efficient Facility Network Control | Sprint 1 Documentation*
