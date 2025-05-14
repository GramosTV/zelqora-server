# Healthcare API

A .NET Web API backend for a healthcare application that provides authentication, user management, appointment scheduling, messaging, and reminders.

## Features

- **Authentication**: JWT-based authentication with refresh token support
- **User Management**: Account creation, profile updates, and role-based access control
- **Appointments**: Scheduling, viewing, and managing healthcare appointments
- **Messaging**: Secure communication between patients and healthcare providers
- **Reminders**: Automated and manual reminders for upcoming appointments

## Tech Stack

- ASP.NET Core 9.0
- Entity Framework Core with SQLite
- JWT Authentication
- CORS support for Angular integration

## Getting Started

### Prerequisites

- .NET SDK 9.0 or later
- Visual Studio, VS Code, or another IDE with C# support

### Installation

1. Clone this repository
2. Navigate to the project directory
3. Run the application:

```bash
dotnet run
```

The API will be available at `http://localhost:5296` by default.

## API Endpoints

### Authentication

- `POST /api/auth/login` - Authenticate user
- `POST /api/auth/register` - Register new user
- `POST /api/auth/refresh-token` - Refresh access token

### Users

- `GET /api/users` - Get all users (Admin only)
- `GET /api/users/{id}` - Get user by ID
- `PUT /api/users/{id}` - Update user profile
- `DELETE /api/users/{id}` - Delete user (Admin only)

### Appointments

- `GET /api/appointments` - Get appointments for current user
- `GET /api/appointments/{id}` - Get appointment by ID
- `POST /api/appointments` - Create new appointment
- `PUT /api/appointments/{id}` - Update appointment
- `DELETE /api/appointments/{id}` - Delete appointment

### Messages

- `GET /api/messages` - Get messages for current user
- `GET /api/messages/{id}` - Get message by ID
- `POST /api/messages` - Send a new message
- `PUT /api/messages/{id}/read` - Mark message as read

### Reminders

- `GET /api/reminders/user/{id}` - Get reminders for user
- `POST /api/reminders` - Create new reminder
- `PUT /api/reminders/{id}/read` - Mark reminder as read
- `POST /api/reminders/mark-all-read` - Mark all reminders as read

## CORS Configuration

The API is configured to allow requests from:

- `http://localhost:4200` (Angular development server)
- `http://localhost:4201` (Alternative Angular port)

In development mode, the API also allows requests from any origin for testing purposes.

## Database

The application uses SQLite for data storage. The database is automatically created and seeded with sample data when the application runs for the first time.

## License

This project is licensed under the MIT License.
