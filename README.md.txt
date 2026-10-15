
# RaceDay - RESTful API

## Project Overview
RaceDay is a RESTful API developed using ASP.NET Core Web API and C#. It supports the management of sporting events, categories, participant enrolments, routes and results.

## Technologies Used
- C#
- ASP.NET Core Web API
- .NET 8
- SQL Server
- Entity Framework Core
- Swagger / OpenAPI
- xUnit automated tests
- GitHub Actions

## Database
The project uses SQL Server and Entity Framework Core. Its main entities are:
- Users
- Events
- Categories
- EventCategories
- Enrolments
- Results
- Routes

## User Roles and Authentication
The API supports Organiser and Participant roles. Participant registration and login are provided through the authentication endpoints.

## Running the API
1. Install the .NET 8 SDK and SQL Server.
2. Configure the database connection string for your local SQL Server.
3. Open the solution in Visual Studio.
4. Restore NuGet packages and build the API.
5. Run the API and open Swagger to explore the endpoints.

## Automated Tests
The project includes a separate RaceDay.Api.Tests project for automated testing of the API.

The test suite was reported as passing all 15 tests during development.

To run the tests, open a terminal in the solution directory and run:

```bash
dotnet test
```

## Continuous Integration
The `.github/workflows/dotnet.yml` workflow is intended to build the solution and run automated tests using GitHub Actions.

## Project Structure
- `RACEDAY.API/` — API source code.
- `RaceDay.Api.Tests/` — automated test project.
- `.github/workflows/` — GitHub Actions workflow.
- `RACEDAY.API.slnx` — solution file.

## Academic Submission
This project was developed for the Programming 2B Part 2 RESTful API Development assessment.

## Demonstration Video
Add the link to your project demonstration video here after recording and uploading it.
