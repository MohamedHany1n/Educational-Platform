# Educational Platform

A web-based educational platform where teachers publish courses, lessons, videos and exams, students enroll and study, parents follow their children's progress, and admins manage the whole system.

Built for the DEPI graduation project (Full Stack .NET track).

## Roles

| Role | What they do |
|------|--------------|
| Admin | Manage teachers, students, courses and reports |
| Teacher | Create courses, lessons, videos and exams, and grade essay questions |
| Student | Enroll in courses, watch lessons, take exams, view grades |
| Parent | Follow children's progress and grades |

## Tech Stack

- Backend: ASP.NET Core Web API
- Database: SQL Server with Entity Framework Core
- Authentication: ASP.NET Core Identity and JWT
- Frontend: React

## Team

| Member | Responsibility |
|--------|----------------|
| Mohamed Hany (Lead) | Setup, identity and login, admin, code review |
| Mohamed Medhat | Teacher features: courses, lessons, videos |
| Omar Ghamry | Exams: questions, attempts, grading |
| Mohamed Hantem | Student, enrollment, parent, notifications |

## Repository Structure

- docs/ : project documentation (planning, requirements, design, testing)
- src/ : source code

## Branching Strategy

- main: stable version, used for submission only
- develop: integration branch, all features are merged here
- feature/name: one branch per task, for example feature/teacher-courses

No direct pushes to main or develop. Every change goes through a Pull Request and one review.

## Commit Messages

Use a clear prefix: feat, fix, docs, refactor, test.

Example: feat: add teacher courses endpoint

## Getting Started

Requirements: .NET SDK, SQL Server, Visual Studio or VS Code.

To run the API:

    cd src/EducationalPlatform.Api
    dotnet restore
    dotnet ef database update
    dotnet run

Set your connection string and JWT key with User Secrets, not in appsettings.json.

## Documentation

- [Planning](docs/1-planning)
- [Literature Review](docs/2-literature-review)
- [Requirements](docs/3-requirements)
- [Analysis and Design](docs/4-analysis-design)
- [Testing](docs/5-testing)
- [Final Reports](docs/6-final)
