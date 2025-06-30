# Contributing to Gulp 💧

Thank you for your interest in contributing to Gulp! We welcome contributions from everyone.

## 🚀 Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/carl-unthank/gulp.git
   cd gulp
   ```
3. **Set up the development environment** following the [README.md](README.md) instructions
4. **Create a new branch** for your feature or bug fix:
   ```bash
   git checkout -b feature/your-feature-name
   ```

## 📝 Development Guidelines

### Code Style
- Follow **Clean Architecture** principles
- Use **async/await** throughout the entire call chain
- Implement the **Result pattern** for error handling
- Write **meaningful commit messages**
- Add **XML documentation** for public APIs

### Architecture
- **Controllers**: Handle HTTP requests and responses only
- **Services**: Contain business logic and orchestration
- **Repositories**: Handle data access with IQueryable support
- **DTOs**: Use for data transfer between layers
- **Components**: Keep UI components focused and reusable

### Testing
- Write **unit tests** for all services and business logic
- Use **XUnit** for testing framework
- Mock dependencies using **Moq**
- Aim for **high test coverage**

## 🐛 Bug Reports

When filing a bug report, please include:
- **Clear description** of the issue
- **Steps to reproduce** the problem
- **Expected vs actual behavior**
- **Environment details** (OS, .NET version, browser)
- **Screenshots** if applicable

## ✨ Feature Requests

For new features:
- **Check existing issues** to avoid duplicates
- **Describe the use case** and why it's valuable
- **Provide mockups** or examples if helpful
- **Consider the impact** on existing functionality

## 🔄 Pull Request Process

1. **Update documentation** if needed
2. **Add or update tests** for your changes
3. **Ensure all tests pass**:
   ```bash
   dotnet test
   ```
4. **Build successfully**:
   ```bash
   dotnet build
   ```
5. **Follow the PR template** when creating your pull request
6. **Link related issues** in your PR description

### PR Requirements
- ✅ All tests pass
- ✅ No build warnings or errors
- ✅ Code follows project conventions
- ✅ Documentation updated if needed
- ✅ Meaningful commit messages

## 🏗️ Development Setup

### Prerequisites
- .NET 9 SDK
- SQL Server Express LocalDB
- Node.js (for Tailwind CSS)

### Quick Start
```bash
# Install dependencies
dotnet restore
cd Gulp.Client && npm install && cd ..

# Setup database
dotnet ef database update --project Gulp.Infrastructure --startup-project Gulp.Api

# Run the application
dotnet run --project Gulp.Api
```

## 📋 Code Review Process

1. **Automated checks** must pass (build, tests)
2. **Manual review** by maintainers
3. **Address feedback** promptly
4. **Squash commits** if requested
5. **Merge** once approved

## 🤝 Community Guidelines

- Be **respectful** and **inclusive**
- **Help others** learn and grow
- **Ask questions** if you're unsure
- **Share knowledge** and best practices

## 📞 Getting Help

- **GitHub Issues**: For bugs and feature requests
- **Discussions**: For questions and general discussion
- **Documentation**: Check the README and code comments

Thank you for contributing to Gulp! 🙏
