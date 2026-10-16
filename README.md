https://youtu.be/U7KOyzPCp4w?si=7mc3ELnC_iHADhea

# RaceDay Part 2 – Race Event Management API

## 1. Project Overview

RaceDay is an ASP.NET Core Web API developed using C# and .NET 8. The system supports the management of race events, categories, participant enrolments, race results, and route information.

Part 2 builds on the database design and endpoint plan established in Part 1. The API connects to SQL Server using Entity Framework Core and includes authentication, role-based authorization, automated testing, and continuous integration using GitHub Actions.

## 2. Project Objectives

The objectives of RaceDay Part 2 are to:

- Develop an ASP.NET Core Web API using C#.
- Use SQL Server and Entity Framework Core to access the RaceDay database.
- Preserve the database structure designed in Part 1.
- Implement authentication and authorization for Organisers and Participants.
- Provide API endpoints for race-related resources.
- Create and run automated tests.
- Use GitHub Actions to build and test the application automatically.

## 3. Technologies Used

- C#
- ASP.NET Core Web API
- .NET 8
- SQL Server Express
- Entity Framework Core
- Swagger / OpenAPI
- xUnit
- Visual Studio
- Git and GitHub
- GitHub Actions

## 4. Database Design

The RaceDay database contains seven main entities:

1. Users
2. Events
3. Categories
4. EventCategories
5. Enrolments
6. Results
7. Routes

These entities support user accounts, race event management, event categories, participant enrolments, race results, and route information.

The database design follows the schema developed in Part 1.

## 5. Authentication and User Roles

The API supports two user roles:

- **Organiser:** Can access the event-management functionality permitted by the application's authorization rules.
- **Participant:** Can register an account, log in, and access permitted participant functionality.

The application uses password hashing and JSON Web Token (JWT) authentication. Authorization rules are used to restrict access to protected endpoints.

## 6. Project Structure

The repository includes the following main components:

- `RACEDAY.API/` – The main ASP.NET Core Web API project.
- `RaceDay.Api.Tests/` – The automated test project.
- `.github/workflows/` – GitHub Actions workflow configuration.
- `README.md` – Project overview and instructions.
- `docs/` – Project documentation and assessment evidence, where included.

## 7. Requirements

To run the project locally, the following software is required:

- Visual Studio with ASP.NET and web development support.
- .NET 8 SDK.
- SQL Server Express.
- SQL Server Management Studio (SSMS), if required to inspect or manage the database.
- Git, if cloning the repository.

## 8. Database Configuration

The API uses the `RaceDay` database on SQL Server.

The SQL Server instance used during development was:

`localhost\SQLEXPRESS`

The API connection string is configured using the `RaceDayConnection` setting.

Before running the project, ensure that SQL Server is running and that the connection string matches the local database configuration.

Do not commit database passwords or other private credentials to the repository.

## 9. Running the Application

### Step 1: Open the Project

Clone or download the repository:

https://github.com/ST10485418/RaceDayPart2

Open the solution or the API project in Visual Studio.

### Step 2: Configure Application Settings

Check that the database connection string and required JWT settings are configured correctly.

Keep production signing keys private and do not publish sensitive credentials.

### Step 3: Restore Dependencies

Open a terminal in the repository root and run:

```bash
dotnet restore RACEDAY.API/RACEDAY.API.csproj
```

### Step 4: Build the API

Run:

```bash
dotnet build RACEDAY.API/RACEDAY.API.csproj
```

### Step 5: Run the API

Run:

```bash
dotnet run --project RACEDAY.API/RACEDAY.API.csproj
```

Alternatively, select the API project as the startup project in Visual Studio and click Run.

### Step 6: Test the API with Swagger

Open the Swagger URL displayed when the application starts.

Swagger provides an interactive interface for viewing and testing the available API endpoints.

## 10. Automated Testing

RaceDay includes an automated test project named `RaceDay.Api.Tests`.

To run the tests from the repository root, execute:

```bash
dotnet test RaceDay.Api.Tests/RaceDay.Api.Tests.csproj
```

The tests check implemented application functionality. Test results depend on the current code and configuration.

## 11. Continuous Integration with GitHub Actions

GitHub Actions is used to automate the project's build and testing process.

The workflow restores dependencies, builds the API and test projects, and runs the automated tests.

The workflow history is available here:

https://github.com/ST10485418/RaceDayPart2/actions

A successful workflow run provides evidence that the configured build and test steps have completed successfully.

## 12. Documentation and Assessment Evidence

The project builds on the database design and endpoint plan prepared in Part 1.

Part 2 evidence may include:

- Screenshots of the API running in Swagger.
- Screenshots or output showing successful automated tests.
- A screenshot of a successful GitHub Actions run.
- Relevant project documentation and database design files, where required by the assessment rubric.

Ensure that any evidence required by the lecturer is saved in the appropriate repository folder.

## 13. Conclusion

RaceDay Part 2 implements an ASP.NET Core Web API for a race event management system. It builds on the Part 1 design and uses C#, SQL Server, Entity Framework Core, authentication, automated testing, and GitHub Actions.

The project demonstrates API development, database integration, application testing, and continuous integration.

## 14. GitHub Repository

Repository:

https://github.com/ST10485418/RaceDayPart2

GitHub Actions:

https://github.com/ST10485418/RaceDayPart2/actions

