# 1.3 Task Assignment & Roles

| Member | Role | Responsibilities |
|--------|------|------------------|
| Mohamed Hany | Team Lead | Project setup, identity and login (JWT, roles), admin features, code review, repository management |
| Mohamed Medhat | Developer | Teacher features: courses, lessons, videos |
| Omar Ghamry | Developer | Exams: questions, attempts, grading |
| Mohamed Hatem | Developer | Student features, enrollment, parent features, notifications |

## Module Ownership
| Module | Owner |
|--------|-------|
| Auth and Users (admin) | Mohamed Hany |
| Courses and Lessons | Mohamed Medhat |
| Exams and Grading | Omar Ghamry |
| Payments and Enrollment | Mohamed Hatem |
| Parent, Progress and Notifications | Mohamed Hatem |
| Reports | Mohamed Hany |

## Working Rules
- One feature branch per task, for example `feature/teacher-courses`.
- No direct pushes to `main` or `develop`; every change goes through a Pull Request and one review.
- Commit messages use a prefix: `feat`, `fix`, `docs`, `refactor`, `test`.
