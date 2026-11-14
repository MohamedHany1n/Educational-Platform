# 3.4 Non-Functional Requirements

> Same requirement IDs as [System Analysis & Design - Requirements](../4-analysis-design/03-requirements.md).


| Category | ID | Requirement |
|----------|----|-------------|
| Performance | NFR-01 | Normal pages respond in under 3 seconds with up to 200 concurrent users. |
| Performance | NFR-02 | Lists (courses, requests, users) are paginated. |
| Security | NFR-03 | Passwords are hashed (ASP.NET Core Identity / PBKDF2), never stored in plain text. |
| Security | NFR-04 | API uses JWT authentication and role-based authorization. |
| Security | NFR-05 | Uploaded receipts accept only image types (jpg, png) up to 5 MB and are renamed on the server; only the Admin and the owner can view them. |
| Security | NFR-06 | All input is validated on the server; EF Core parameterized queries prevent SQL injection. |
| Security | NFR-07 | Communication uses HTTPS. |
| Usability | NFR-08 | Responsive UI that works on mobile and desktop. |
| Usability | NFR-09 | Clear status messages and error messages for every action. |
| Reliability | NFR-10 | Data is persisted in SQL Server with foreign keys; daily database backup. |
| Reliability | NFR-11 | Exam answers are saved on submission so no answer is lost. |
| Maintainability | NFR-12 | Layered architecture, consistent naming, code in Git with feature branches. |
| Compatibility | NFR-13 | Works on the latest versions of Chrome, Edge, Firefox and Safari. |

## Constraints & Assumptions
- Payment is **manual** (InstaPay / Vodafone Cash) and verified by the admin; no payment gateway.
- Videos are stored as external links (for example unlisted YouTube); the platform does not host video files.
- One enrollment per student per course.
