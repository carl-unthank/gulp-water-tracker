# 💧 Gulp - Water Intake Tracker

A modern, mobile-friendly web application for tracking daily water intake and hydration goals. Built with .NET 9, Blazor WebAssembly, and Tailwind CSS.

## ✨ Features

- **📱 Mobile-First Design** - Optimized for mobile devices with a native app-like experience
- **🎯 Daily Goal Setting** - Set and track personalized daily hydration goals
- **💧 Quick Water Logging** - Add water intake with preset amounts or custom values
- **📊 Progress Tracking** - Visual progress bars and completion percentages
- **📈 History & Analytics** - View weekly and monthly hydration history
- **🔐 User Authentication** - Secure JWT-based authentication with HTTP-only cookies
- **⚡ Real-time Updates** - Instant UI updates across all components
- **🎨 Modern UI** - Clean, responsive design with smooth animations

## 🛠️ Tech Stack

- **Frontend**: Blazor WebAssembly (.NET 9)
- **Backend**: ASP.NET Core Web API (.NET 9)
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: JWT with HTTP-only cookies
- **Styling**: Tailwind CSS
- **Architecture**: Clean Architecture with SOLID principles

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server Express LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) (or SQL Server)
- [Node.js](https://nodejs.org/) (for Tailwind CSS)

### 📥 Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/gulp.git
   cd gulp
   ```

2. **Install dependencies**
   ```bash
   # Restore .NET packages
   dotnet restore
   
   # Install Node.js dependencies for Tailwind CSS
   cd Gulp.Client
   npm install
   cd ..
   ```

3. **Set up the database**
   ```bash
   # Create and seed the database
   dotnet ef database update --project Gulp.Infrastructure --startup-project Gulp.Api
   ```

4. **Configure settings (optional)**
   
   The app is configured to work out-of-the-box with default settings. If needed, you can modify:
   - `Gulp.Api/appsettings.Development.json` - API configuration
   - Connection string (defaults to LocalDB)
   - JWT settings
   - CORS settings

### 🏃‍♂️ Running the Application

**Option 1: Run both API and Client together (Recommended)**
```bash
# From the root directory
dotnet run --project Gulp.Api
```
This will start both the API (backend) and serve the Blazor WebAssembly client.

**Option 2: Run separately**
```bash
# Terminal 1 - Start the API
dotnet run --project Gulp.Api

# Terminal 2 - Start the Client (if running separately)
dotnet run --project Gulp.Client
```

The application will be available at:
- **Main App**: https://localhost:7001
- **API**: https://localhost:7001/api
- **Swagger UI**: https://localhost:7001/swagger

### 🔑 Default Login

The application seeds a default user account:
- **Email**: `admin@gulp.com`
- **Password**: `Admin123!`

## 📁 Project Structure

```
Gulp/
├── Gulp.Api/              # ASP.NET Core Web API
├── Gulp.Client/           # Blazor WebAssembly Frontend
├── Gulp.Infrastructure/   # Data Access Layer (EF Core)
├── Gulp.Shared/          # Shared DTOs and Models
├── Gulp.UI/              # Reusable UI Components
└── Gulp.Tests/           # Unit Tests
```

## 🏗️ Architecture

The application follows **Clean Architecture** principles:

- **Domain Layer** (`Gulp.Shared`): Core business entities and DTOs
- **Application Layer** (`Gulp.Api`): API controllers and business logic
- **Infrastructure Layer** (`Gulp.Infrastructure`): Data access and external services
- **Presentation Layer** (`Gulp.Client`): Blazor WebAssembly UI

### Key Features:

- **State Management**: Centralized state management with reactive updates
- **Repository Pattern**: Generic repository with IQueryable support
- **Result Pattern**: Consistent error handling throughout the application
- **Component Architecture**: Reusable UI components with proper separation of concerns

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 🚀 Deployment

The application is configured for easy deployment to various platforms:

- **Azure App Service**: Ready for deployment with included configuration
- **Docker**: Dockerfile included for containerization
- **IIS**: Can be deployed to IIS with proper configuration

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [Blazor WebAssembly](https://blazor.net/)
- Styled with [Tailwind CSS](https://tailwindcss.com/)
- Icons from [Font Awesome](https://fontawesome.com/)
