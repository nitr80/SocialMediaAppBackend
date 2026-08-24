# SocialMediaAppBackend

A RESTful backend for a social media application, built with **ASP.NET Core** and **Entity Framework Core**. The API provides user authentication and endpoints for managing users and posts.

## Features

- User registration and login
- JWT-based authentication and authorization
- Access and refresh token handling
- Protected API endpoints
- Post creation and retrieval
- Like and unlike functionality
- SQLite database persistence
- Entity Framework Core migrations

## Tech Stack

- **C#**
- **ASP.NET Core**
- **Entity Framework Core**
- **SQLite**
- **JWT**
- **REST API**

## Project Structure

```text
Backend/
├── Controllers/       # API controllers
├── Data/              # Database context and configuration
├── Models/            # Database entities
├── DTOs/              # Data transfer objects
├── Services/          # Application and authentication logic
├── Migrations/        # EF Core database migrations
└── Program.cs         # Application configuration and startup
```

## Authentication

The API uses **JSON Web Tokens (JWT)** for authentication.

After a successful login, the server provides an access token and a refresh token. Protected endpoints require a valid access token.

Refresh tokens are used to obtain a new access token when the current access token expires.

## Database

The application uses **SQLite** with **Entity Framework Core** for data persistence.

Database changes are managed using EF Core migrations.

To apply the existing migrations:

```bash
dotnet ef database update
```

To create a new migration after changing the data model:

```bash
dotnet ef migrations add <MigrationName>
```

## Running the API

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download)
- Entity Framework Core CLI

Clone the repository and navigate to the project directory:

```bash
git clone <repository-url>
cd <repository-directory>
```

Restore dependencies:

```bash
dotnet restore
```

Apply the database migrations:

```bash
dotnet ef database update
```

Start the development server:

```bash
dotnet run
```

The API will be available on the development URL displayed in the terminal.

## Configuration

Application configuration can be provided through `appsettings.json`, environment variables, or the ASP.NET Core development configuration.

For local development, configure the required JWT and database settings before starting the application.

## Related Project

This backend is used by the corresponding React frontend:

**Social Media Frontend:** `https://github.com/nitr80/SocialMediaAppFrontend`

## Purpose

This project was developed as a personal project to gain practical experience with **backend development, REST APIs, authentication, database management, and Entity Framework Core**.