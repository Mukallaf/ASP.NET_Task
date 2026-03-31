# ASP.NET Task

A full-stack web application built with ASP.NET MVC and .NET Core Web API.

## Features
- User registration and login
- JWT token authentication
- BCrypt password hashing
- View and edit profile
- SQL Server database with Entity Framework Core

## Projects
- `UserApi` — .NET Core Web API
- `UserMvcApp` — ASP.NET MVC

## How to Run

### Requirements
- Visual Studio 2022
- .NET 8 SDK
- SQL Server LocalDB (comes with Visual Studio)

### Steps
1. Clone the repository
2. Open the solution in Visual Studio
3. Check the API port in `UserApi/Properties/launchSettings.json`
4. Update `BaseAddress` in `UserMvcApp/Program.cs` to match the API port
5. Open Package Manager Console, select `UserApi` and run: Update-Database
6. Right click the Solution > Properties > set both projects to Start
7. Press ctrl + shift + b to build
8. Press F5

### Default Admin Key (for Swagger)
To access admin endpoints in Swagger, use this header: X-Admin-Key: Admin_SecretKey
