# SocialMediaAppBackend

A RESTful backend for a social media application, built with **ASP.NET Core** and **Entity Framework Core**. The API provides user authentication and endpoints for managing users and posts.

## Features

- User registration and login
- JWT-based authentication and authorization
- Access and refresh token handling
- Protected API endpoints
- Create, view and delete posts
- A feed of your posts and posts from users you follow
- Like and unlike posts
- Comments and comment likes
- Follow and unfollow users
- Search users by username
- Editable bio and profile picture upload
- Rate limiting on authentication endpoints
- PostgreSQL database persistence
- Entity Framework Core migrations
- Health check endpoint

## Tech Stack

- **C#**
- **ASP.NET Core**
- **Entity Framework Core**
- **PostgreSQL**
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

## API Overview

| Area | Endpoints |
| --- | --- |
| Authentication | `POST /Auth/Register`, `/Auth/Login`, `/Auth/Refresh`, `/Auth/Logout` |
| Users | `GET /Users/{id}`, `GET /Users?search=`, `PATCH /Users/me/Bio`, `PATCH /Users/me/ProfilePicture` |
| Posts | `GET /Posts`, `GET /Posts/{id}`, `POST /Posts`, `DELETE /Posts/{id}` |
| Post likes | `GET/POST/DELETE /Posts/{postId}/Likes`, `GET /Posts/me/Likes` |
| Comments | `GET/POST /Posts/{postId}/Comments` |
| Comment likes | `GET/POST/DELETE /Comments/{commentId}/Likes`, `GET /Comments/me/{postId}/Likes` |
| Follows | `POST/DELETE /Users/{userId}/Follows`, `GET /Users/{userId}/Follows/status` |
| Health | `GET /health` |

All endpoints except registration, login, refresh and health require a valid access token.

## Database

The application uses **PostgreSQL** with **Entity Framework Core** for data persistence.

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

Application configuration can be provided through `appsettings.json`.

For local development, create a `.env` file in the project root with a JWT signing key and a PostgreSQL connection string:

```bash
JwtOptions__Key=<your-secret-key>
ConnectionStrings__Default=Host=localhost;Port=5432;Database=socialmediaapp;Username=postgres;Password=postgres
```

Migrations are applied automatically when the application starts. The app targets **PostgreSQL 18**.

## Tests

Run the test suite with:

```bash
dotnet test
```

## Related Project

This backend is used by the corresponding React frontend:

**Social Media Frontend:** `https://github.com/nitr80/SocialMediaAppFrontend`

## Purpose

This project was developed as a personal project to gain practical experience with **backend development, REST APIs, authentication, database management, and Entity Framework Core**.