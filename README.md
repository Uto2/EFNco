# EFNco — Efficient Facility Network Control

> A digital parking permit and access management system designed to replace outdated sticker-based verification processes in universities, residential communities, and office facilities.

---

## 📌 About the Project

**EFNco (Efficient Facility Network Control)** is a full-stack web-based facility management platform built with **ASP.NET Core 8 MVC**. It replaces traditional sticker-based parking systems with secure digital permits, QR-based gate verification, real-time analytics, and automated violation enforcement.

The system was built incrementally across **7 sprints**, covering the full lifecycle from permit application to gate scanning, violation management, and authorized person access control.

---

## 🚀 Features

### Sprint 1 — Authentication & User Management
- User registration and login with ASP.NET Core Identity
- Role-based access control (Admin, Guard, User)
- Account lockout after 5 failed attempts
- Profile management and password change
- Admin user management (create, edit, delete, reset password)
- Public landing page with feature overview
- Show/hide password toggle on all password fields

### Sprint 2 — Permit Application & Management
- Vehicle registration (plate, make, model, year, color, type)
- Online permit application with Driver's License and OR/CR file upload
- Permit types: Student, Faculty, Staff
- One vehicle, one permit per user at a time
- Admin permit review (Approve / Reject / Revoke with remarks)
- Application status tracking (Pending, Approved, Rejected, Revoked, Expired)
- Document preview inline (images show directly, PDFs open in new tab)

### Sprint 3 — QR / Digital Permit Generation
- QR code automatically generated on permit approval
- Unique QR token per permit (secure, non-guessable)
- Full-screen QR display page for gate scanning
- Expired permit visual overlay on QR page
- Downloadable QR code (PNG)
- Printable permit card with all details and QR code
- Browser print-friendly layout (`window.print()`)

### Sprint 4 — Gate Verification & Entry/Exit Logging
- Camera-based QR code scanning (jsQR library)
- Manual plate number entry fallback
- Real-time permit validation at gate
- Auto-detects entry vs exit (based on last log)
- Parking duration calculated and displayed on exit
- Entry/exit log with date and action filters
- Paginated log history
- Dashboard stats: Today's Entries, Currently Inside

### Sprint 5 — Admin Dashboard & Analytics
- Interactive Chart.js charts (line, bar, donut)
- Daily entry/exit line chart
- Peak hours bar chart
- Permit status donut chart
- Permit type breakdown
- User growth line chart
- Filterable by custom date range with quick presets (Today / 7 Days / 30 Days)
- PDF export via Rotativa.AspNetCore
- Recent gate activity and permit tables
- Approval rate percentage tracking

### Sprint 6 — Violation Notifications & Enforcement
- Violation types: Overstay, No Permit, Expired Permit, Unauthorized Vehicle, Wrong Parking Zone
- Preset fines per violation type
- Guard and Admin can log violations
- In-app notification bell with unread count badge
- Email notifications via SMTP (violation notice + appeal result)
- Violation appeal system — user submits reason, admin approves or rejects
- Admin violation management with status filters
- Fine tracking (Unpaid, Paid, Appealed, Dismissed)

### Sprint 7 — Authorized Person Access & Duration Tracking
- Add authorized persons to a permit (name, ID, relationship, photo)
- Authorized persons displayed on gate verify result screen
- Parking duration limit settings per permit type (Admin configurable)
- Grace period configuration
- Auto-violation on overtime (configurable toggle)
- Overtime alert email sent to permit holder
- Parking session history per permit
- Forgot Password and Email Confirmation flow

---

## 🛠️ Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 8 MVC |
| Language | C# |
| Authentication | ASP.NET Core Identity |
| Database | SQL Server (Entity Framework Core 8) |
| ORM | Entity Framework Core with Migrations |
| Frontend | Razor Views, HTML5, CSS3 (custom dark theme) |
| Charts | Chart.js 4.4.0 (CDN) |
| QR Generation | QRCoder 1.6.0 |
| QR Scanning | jsQR 1.4.0 (CDN) |
| PDF Export | Rotativa.AspNetCore 1.0.6 |
| Email | SMTP (System.Net.Mail) |
| Client Libraries | jQuery 3.7.1, Bootstrap 5.3.2, jQuery Validation |
| Library Manager | libman (Microsoft Library Manager) |
| IDE | Visual Studio 2022 |

---

## 📁 Project Structure

```
EFNco/
├── Controllers/
│   ├── AccountController.cs         # Login, Register, Profile, Change Password, Forgot/Reset Password
│   ├── AdminController.cs           # User management, Permit review, Violation management
│   ├── AdminSettingsController.cs   # Parking duration limit settings
│   ├── AuthorizedPersonController.cs # Authorized person CRUD
│   ├── DashboardController.cs       # Analytics dashboard + PDF export
│   ├── GateController.cs            # QR scan, Verify, Entry/Exit log, Duration history
│   ├── HomeController.cs            # Landing page + authenticated dashboard
│   ├── NotificationController.cs    # Mark notifications read/all read
│   ├── PermitController.cs          # Apply, My Permits, QR view, Print permit
│   └── ViolationController.cs       # My violations, Appeal, Log violation
├── Data/
│   ├── ApplicationDbContext.cs      # EF Core DbContext with all entity configurations
│   └── SeedData.cs                  # Seeds Admin & Guard roles + default accounts
├── Models/
│   ├── AccountViewModels.cs         # Login, Register, Profile, ChangePassword, ForgotPassword VMs
│   ├── AdminViewModels.cs           # UserList, EditUser, ReviewPermit VMs
│   ├── ApplicationUser.cs           # Extended Identity user
│   ├── DashboardViewModel.cs        # Analytics dashboard data
│   ├── EntryExitLog.cs              # Gate entry/exit log model
│   ├── GateViewModels.cs            # GateVerifyResultViewModel
│   ├── Permit.cs                    # Vehicle, ParkingPermit models + enums
│   ├── PermitViewModels.cs          # Apply, MyPermit, AdminPermitList VMs
│   ├── Sprint7Models.cs             # AuthorizedPerson, ParkingDurationSetting models
│   ├── Sprint7ViewModels.cs         # AuthorizedPerson, DurationSetting, ParkingSession VMs
│   ├── Violation.cs                 # Violation, ViolationAppeal, AppNotification models
│   └── ViolationViewModels.cs       # Violation list, details, appeal, log VMs
├── Services/
│   └── EmailService.cs              # SMTP email service (violation notice, appeal result, overtime alert)
├── Views/
│   ├── Account/                     # Login, Register, Profile, ChangePassword, ForgotPassword, ResetPassword
│   ├── Admin/                       # Users, Permits, Violations management views
│   ├── AdminSettings/               # Duration limits configuration
│   ├── AuthorizedPerson/            # Add, Edit, Index views
│   ├── Dashboard/                   # Analytics dashboard + PDF template
│   ├── Gate/                        # Scan, VerifyResult, Log, DurationHistory
│   ├── Home/                        # Landing page, Dashboard, Privacy
│   ├── Permit/                      # Apply, MyPermits, Details, QRView, PrintPermit
│   ├── Shared/                      # _Layout, _NotificationBell, Error, Auth layout
│   └── Violation/                   # MyViolations, Details, Appeal, Log
├── wwwroot/
│   ├── css/site.css                 # Custom dark navy + electric blue theme
│   ├── js/site.js                   # Show/hide password toggle, auto-dismiss alerts
│   └── lib/                         # jQuery, Bootstrap (restored by libman.json)
├── appsettings.json                 # Connection string + Email SMTP config
├── libman.json                      # Client-side library manager
└── Program.cs                       # App entry point, DI, Identity config, Rotativa setup
```

---

## ⚙️ Setup & Installation

### Prerequisites
- Visual Studio 2022 (v17.0+)
- .NET 8 SDK
- SQL Server or SQL Server Express
- wkhtmltopdf (for PDF export) — [Download here](https://wkhtmltopdf.org/downloads.html)

---

### Step 1 — Clone the Repository
```bash
git clone https://github.com/your-username/efnco.git
cd efnco
```

---

### Step 2 — Restore Client-Side Libraries
In Visual Studio, right-click `libman.json` → **Restore Client-Side Libraries**

Or via CLI:
```bash
dotnet tool install -g Microsoft.Web.LibraryManager.Cli
libman restore
```

---

### Step 3 — Configure the Database
Open `appsettings.json` and update the connection string:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER\\SQLEXPRESS; Database=EFNcoDB; Trusted_Connection=True; TrustServerCertificate=Yes;"
}
```

---

### Step 4 — Configure Email (Optional)
Add your Gmail SMTP credentials to `appsettings.json`:
```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "Username": "your-gmail@gmail.com",
  "Password": "your-app-password",
  "From": "noreply@efnco.com"
}
```
> To get a Gmail App Password: Google Account → Security → 2-Step Verification → App Passwords

---

### Step 5 — Configure PDF Export (Optional)
1. Download and install [wkhtmltopdf](https://wkhtmltopdf.org/downloads.html)
2. Create folder: `wwwroot/Rotativa/`
3. Copy `wkhtmltopdf.exe` into `wwwroot/Rotativa/`

---

### Step 6 — Run Migrations
```powershell
Add-Migration InitialMigration
Update-Database
```

Or via CLI:
```bash
dotnet ef database update
```

---

### Step 7 — Run the Application
Press **F5** in Visual Studio or:
```bash
dotnet run
```

App launches at `https://localhost:7001`

---

## 🔐 Default Accounts

| Role | Email | Password |
|------|-------|---------|
| Admin | admin@efnco.com | Admin@123 |
| Guard | guard@efnco.com | Guard@123 |

> These accounts are automatically seeded on first run via `SeedData.cs`.  
> **Remove or change these credentials before any production deployment.**

---

## 👤 Roles & Access

| Role | Access |
|------|--------|
| **Admin** | Full access — users, permits, violations, analytics, settings |
| **Guard** | Gate scanner, entry/exit log, log violations |
| **User** | Apply for permits, view QR, manage authorized persons, view violations |

---

## 📦 NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.0 | Identity with EF Core |
| Microsoft.AspNetCore.Identity.UI | 8.0.0 | Identity UI |
| Microsoft.EntityFrameworkCore | 8.0.0 | ORM |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | SQL Server provider |
| Microsoft.EntityFrameworkCore.Tools | 8.0.0 | Migrations CLI |
| QRCoder | 1.6.0 | QR code generation |
| Rotativa.AspNetCore | 1.0.6 | PDF export from Razor views |

---

## 🗄️ Database Migrations

| Migration | Description |
|-----------|-------------|
| `InitialMigration` | Users, Identity tables |
| `Permits` | Vehicles, ParkingPermits tables |
| `AddPermitFileUploads` | License photo and OR/CR file columns |
| `AddPermitQRToken` | QR token unique index on ParkingPermits |
| `Sprint3QRCode` | QR code image data column |
| `Sprint4EntryExitLog` | EntryExitLogs table |
| `Sprint6Violations` | Violations, ViolationAppeals, AppNotifications tables |
| `Sprint7AuthorizedPerson` | AuthorizedPersons, ParkingDurationSettings tables |

---

## 🗺️ Sprint Roadmap

| Sprint | Feature | Status |
|--------|---------|--------|
| 1 | Login, Register, Role-based Auth, Admin User Management | ✅ Complete |
| 2 | Permit Application & Management | ✅ Complete |
| 3 | QR / Digital Permit Generation | ✅ Complete |
| 4 | Gate Verification & Entry/Exit Logging | ✅ Complete |
| 5 | Admin Dashboard & Analytics | ✅ Complete |
| 6 | Violation Notifications & Enforcement | ✅ Complete |
| 7 | Authorized Person Access & Duration Tracking | ✅ Complete |

---

## 💡 Developer Notes

- All CSS `@keyframes` and `@media` rules are in `wwwroot/css/site.css` — never inside `.cshtml` files (Razor treats `@` as C# syntax). Use `@@keyframes` / `@@media` only if inline CSS in `.cshtml` is absolutely necessary.
- `wwwroot/lib/` is intentionally empty in source control — run `libman restore` to populate it.
- Email sending is non-critical — failures are silently caught and do not affect violation logging.
- The notification bell loads from `ViewBag.CurrentUserId` which is set globally via `BaseController.OnActionExecuting()`.
- QR codes encode a full verify URL with a unique token — the token is looked up server-side on scan for security.
- PDF export requires `wkhtmltopdf.exe` in `wwwroot/Rotativa/` — the app works without it but Export PDF will fail.

---

## 📄 License

This project was built as an academic project. All rights reserved.

---

*EFNco — Efficient Facility Network Control*  
*Built with ASP.NET Core 8 MVC*
