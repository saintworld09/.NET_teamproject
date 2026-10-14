# EventHub – Event Management System

**A web-based event management application built with ASP.NET Core MVC and C#**

**Live Application:** https://eventhub-4kdr.onrender.com  
**GitHub Repository:** https://github.com/saintworld09/.NET_teamproject

## 1. Project Overview

EventHub is a web-based event management system designed to simplify the process of creating, discovering, organizing, and registering for events.

The application provides a central platform where event organizers can publish and manage events, while attendees can explore available events, view event details, and register to participate. It also provides dashboards and role-based access to help users manage their event-related activities.

EventHub was developed as a software development team project to apply practical concepts in C#, ASP.NET Core MVC, database management, authentication, authorization, CRUD operations, server-side validation, and responsive web development.

## 2. Project Objectives

The main objectives of EventHub are to:

- Provide a centralized platform for publishing and discovering events.
- Simplify event creation and event information management.
- Allow users to register for available events.
- Help organizers manage event attendees.
- Protect user accounts and restrict access to authorized features.
- Demonstrate practical .NET web development and relational database integration.

## 3. Technology Stack

| Technology | Purpose |
|---|---|
| C# | Application programming language |
| ASP.NET Core MVC | Web application framework and MVC architecture |
| .NET 8 | Application runtime and development platform |
| Entity Framework Core | Object-relational mapping and database operations |
| PostgreSQL | Relational database for the deployed application |
| Npgsql | PostgreSQL database provider for Entity Framework Core |
| HTML | Web page structure |
| CSS | Styling and responsive layout |
| JavaScript | Client-side interactions |
| Git and GitHub | Source control and collaboration |
| Render | Application hosting and deployment |
| Neon | Hosted PostgreSQL database service |

## 4. Core Features

### User Registration and Authentication
Users can create accounts, log in, and access features that require authentication.

### Role-Based Authorization
The application supports different user roles, including attendees, organizers, and administrators. Access to protected features is restricted according to the application's authorization rules.

### Event Creation and Management
Authorized organizers can create events and manage their event information, including titles, descriptions, dates, locations, capacity, and categories.

### Event Discovery
Users can browse available events and use the application's supported search and filtering functionality to find events of interest.

### Event Details
Users can view information about an event before deciding whether to register.

### Event Registration
Authenticated users can register for events. Registration records are stored in the database, and the application enforces applicable registration and capacity rules.

### Attendee Management
Organizers can view registrations for the events they manage.

### User and Organizer Dashboards
Dashboards provide relevant information about users' registrations, events, and organizer activities.

### Data Validation and Access Control
The application validates user input and restricts protected operations to authorized users.

## 5. User Roles

| Role | Main responsibilities |
|---|---|
| Attendee | Browse events, view event details, and register for events |
| Organizer | Create and manage events and view registered attendees |
| Administrator | Perform authorized administrative activities and oversee the application |

The features available to each role depend on the authorization rules implemented in the application.

## 6. Database Design

EventHub uses a relational database to maintain application data.

The main data entities include:

- **Users:** Account information, credentials stored as password hashes, and user roles.
- **Events:** Event details, dates, locations, capacity, categories, and organizer relationships.
- **Categories:** Event category information.
- **Registrations:** Records connecting users to events they have registered for.

Entity Framework Core is used to work with the database through application models and database contexts. The deployed application uses PostgreSQL hosted on Neon.

## 7. Application Architecture

EventHub follows the Model-View-Controller (MVC) architectural pattern provided by ASP.NET Core.

- **Models:** Represent application entities and data.
- **Views:** Present information and user interfaces using Razor, HTML, and CSS.
- **Controllers:** Handle incoming requests, coordinate application logic, and return appropriate responses.
- **Entity Framework Core:** Provides database access and persistence.
- **Authentication and Authorization:** Help protect user accounts and control access to application functionality.

This separation of responsibilities helps keep the application organized and maintainable.

## 8. Security and Error Handling

The application incorporates security and validation measures appropriate to its implemented features, including:

- User authentication.
- Role-based authorization.
- Password hashing through the application's authentication implementation.
- Server-side validation of important form inputs.
- Restrictions on event-management operations.
- Registration and event-capacity checks.
- User-facing validation and error feedback.

Database credentials and other environment-specific secrets should be supplied through environment variables rather than committed to the source repository.

## 9. Live Deployment

EventHub is deployed on Render, with its PostgreSQL database hosted on Neon.

**Access the application:** https://eventhub-4kdr.onrender.com

The deployed version allows users to interact with the application through a web browser without installing the application locally.

Deployment configuration and database connection settings are managed separately from the source code. Sensitive configuration values must not be published in this repository.

## 10. Getting Started Locally

### Prerequisites

Before running the application locally, ensure that you have:

- .NET 8 SDK installed.
- Git installed.
- Visual Studio Code or another compatible code editor.
- Access to a correctly configured PostgreSQL database, or the existing local SQLite setup if you are working with that version of the project.
- The required application configuration and database connection settings.

### Step 1: Clone the Repository

Open PowerShell and run:

```powershell
git clone https://github.com/saintworld09/.NET_teamproject.git
```

### Step 2: Navigate to the Application Directory

```powershell
cd .NET_teamproject\EventHub
```

### Step 3: Restore Dependencies

```powershell
dotnet restore
```

### Step 4: Configure the Database

Configure the connection string using the environment-variable name expected by the application:

`ConnectionStrings__DefaultConnection`

For the deployed PostgreSQL setup, use the connection string supplied by your PostgreSQL provider. Do not place real credentials in the README or commit them to GitHub.

If your local project uses a `.env` file, create or configure it locally according to the application's configuration code. Keep the file excluded from version control.

Ensure that the required database schema and migrations are available before running the application. Do not apply migrations to an existing database without confirming that they are appropriate for that database.

### Step 5: Run the Application

```powershell
dotnet run
```

Open the local URL printed in the terminal in your browser.

The exact URL and port may vary depending on your local launch configuration.

## 11. Testing

The application should be evaluated through both normal user journeys and invalid-input scenarios.

Recommended test areas include:

- Account registration, login, and logout.
- Event creation, editing, and deletion.
- Event browsing, searching, and filtering.
- Event registration and duplicate-registration prevention.
- Event-capacity enforcement.
- Dashboard and attendee-list functionality.
- Role-based access restrictions.
- Form validation and user-facing error messages.
- Database persistence after logging out and signing back in.

Record actual test outcomes as part of the project's completion evidence.

## 12. Project Management and Documentation

The project repository is used to maintain the application's source code and track development changes.

**GitHub Repository:** https://github.com/saintworld09/.NET_teamproject

**Project Board:** Add the URL of the team's actual project board here.

The project board should document planned features, development tasks, progress, and completed work. Additional documentation and screenshots can be maintained in the repository to support the final project presentation.

## 13. Current Scope and Future Improvements

The current application focuses on core event management, user authentication, event discovery, registration, and attendee management.

Possible future improvements include:

- Email notifications and event reminders.
- QR-code-based attendance tracking.
- Calendar integration.
- Advanced event analytics.
- Online payment integration.
- Expanded automated testing.
- Further accessibility improvements.

These are potential enhancements and should not be considered completed features unless they are implemented.

## 14. Conclusion

EventHub demonstrates the application of ASP.NET Core MVC, C#, Entity Framework Core, and relational database concepts in a practical web application. It provides event organizers and attendees with a centralized platform for managing event information and registrations.

The project also provides practical experience in application architecture, database integration, authentication, authorization, validation, version control, deployment, and software project documentation.