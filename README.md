
# Employee Management System

A full-stack Employee Management System developed using ASP.NET Core Web API (.NET 10), React, Entity Framework Core, SQL Server, and Docker.

The application provides a web interface for managing employee records through a RESTful API.

## Technologies Used

- **Backend:** C#, ASP.NET Core Web API (.NET 10)
- **Frontend:** React, Vite, JavaScript, CSS
- **Database:** Microsoft SQL Server
- **ORM:** Entity Framework Core 10
- **Containerization:** Docker, Docker Compose, Nginx
- **Testing:** xUnit, EF Core InMemory
- **Version Control:** Git and GitHub

## Features

- View all employees
- Retrieve an employee by ID through the API
- Add new employees
- Edit employee information
- Delete employees
- Validate required fields and email addresses
- Reject duplicate employee email addresses
- Store employee data in SQL Server
- Run the application using Docker Compose

## Employee Model

| Property | Type | Description |
|---|---|---|
| Id | int | Unique employee identifier |
| FullName | string | Employee's full name |
| Email | string | Unique email address |
| Department | string | Employee department |
| JobTitle | string | Employee job title |
| IsActive | bool | Active employment status |

## REST API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/employees` | Retrieve all employees |
| GET | `/api/employees/{id}` | Retrieve employee by ID |
| POST | `/api/employees` | Create a new employee |
| PUT | `/api/employees/{id}` | Update an employee |
| DELETE | `/api/employees/{id}` | Delete an employee |

### HTTP Status Codes

- `200 OK`: Successful retrieval
- `201 Created`: Employee successfully created
- `204 No Content`: Successful update or deletion
- `400 Bad Request`: Invalid request data
- `404 Not Found`: Employee does not exist
- `409 Conflict`: Duplicate email address

## Running the Application with Docker

### Prerequisites

- Docker Desktop
- Docker Compose
- Linux containers enabled in Docker Desktop

### Setup

1. Clone the repository.
2. Open a terminal in the project root.
3. Create a `.env` file in the project root.
4. Add the following configuration, replacing the example password with a strong password:

   ```dotenv
   MSSQL_SA_PASSWORD=ReplaceWithYourOwnStrongPassword123!
   ```

5. Build and start the containers:

   ```bash
   docker compose up -d --build
   ```

6. Open the application:

   **Frontend:** http://localhost:8081

   **Backend API:** http://localhost:5001/api/employees

Docker Compose starts three services: the React frontend, ASP.NET Core API, and SQL Server database.

Entity Framework Core migrations are applied when the API starts.

### Stop the Application

```bash
docker compose down
```

This stops the containers while preserving the SQL Server database volume.

Do not use `docker compose down -v` unless you intend to delete the Docker database volume.

## Running Automated Tests

### Prerequisites

- .NET 10 SDK

Run the tests from the repository root:

```bash
dotnet test tests/EmployeeManagement.Api.Tests/EmployeeManagement.Api.Tests.csproj
```

The test suite covers:

- Retrieving employees
- Retrieving employees by ID
- Handling nonexistent employees
- Creating employees
- Rejecting duplicate email addresses
- Updating employees
- Deleting employees

Tests use EF Core InMemory databases and do not modify the SQL Server database.

## Project Structure

```text
employee-management-system-dotnet/
├── backend/
│   └── EmployeeManagement.Api/
│       ├── Controllers/
│       ├── Data/
│       ├── Migrations/
│       ├── Models/
│       ├── Dockerfile
│       └── Program.cs
├── frontend/
│   ├── src/
│   ├── Dockerfile
│   └── nginx.conf
├── tests/
│   └── EmployeeManagement.Api.Tests/
├── compose.yaml
└── README.md
```

## Database

The application uses SQL Server with Entity Framework Core.

Migrations create the Employees table and configure a unique index on employee email addresses.

Docker uses a named volume to persist database data. The Docker SQL Server database is separate from the local SQL Server Express development database.

## Project Purpose

This project was developed as part of a .NET training internship assignment to demonstrate full-stack application development, REST APIs, relational database integration, Docker containerization, and automated testing.
