using HealthcareApi.Models;

namespace HealthcareApi.Data;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        // Check if database is already seeded
        if (context.Users.Any())
        {
            return; // DB has been seeded
        }

        // Create default users
        var users = new User[]
        {
            new()
            {
                Email = "admin@example.com",
                FirstName = "Admin",
                LastName = "User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Email = "doctor@example.com",
                FirstName = "John",
                LastName = "Doe",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                Role = UserRole.Doctor,
                Specialization = "Cardiology",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Email = "patient@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                Role = UserRole.Patient,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Users.AddRange(users);
        context.SaveChanges();

        // Create some sample appointments
        var startTime = DateTime.UtcNow.Date.AddDays(1).AddHours(10); // Tomorrow at 10 AM

        var appointment = new Appointment
        {
            Title = "Initial Consultation",
            PatientId = users[2].Id, // Jane
            DoctorId = users[1].Id,  // Dr. Doe
            StartTime = startTime,
            EndTime = startTime.AddHours(1),
            Status = AppointmentStatus.Confirmed,
            Notes = "First consultation for new patient",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Appointments.Add(appointment);
        context.SaveChanges();

        // Create reminder for appointment
        var reminder = new Reminder
        {
            UserId = users[2].Id, // Jane
            AppointmentId = appointment.Id,
            Title = "Appointment Reminder",
            Message = "You have an appointment with Dr. Doe tomorrow at 10 AM",
            ReminderDate = startTime.AddHours(-24), // 24 hours before
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Reminders.Add(reminder);

        // Create sample messages
        var messages = new Message[]
        {
            new()
            {
                SenderId = users[1].Id, // Dr. Doe
                ReceiverId = users[2].Id, // Jane
                Content = "Hello Jane, please bring your previous test results to your next appointment.",
                Encrypted = false,
                Read = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                SenderId = users[2].Id, // Jane
                ReceiverId = users[1].Id, // Dr. Doe
                Content = "I will bring them, thank you for the reminder Dr. Doe.",
                Encrypted = false,
                Read = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1).AddHours(1)
            }
        };

        context.Messages.AddRange(messages);
        context.SaveChanges();
    }
}
