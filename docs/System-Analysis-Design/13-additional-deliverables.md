# 13. Additional Deliverables

## 13.1 API Documentation (main endpoints)

Base URL: `/api` — all endpoints except register/login require `Authorization: Bearer <token>`.

| Module | Method | Endpoint | Role | Description |
|--------|--------|----------|------|-------------|
| Auth | POST | `/auth/register` | Public | Register student or parent |
| Auth | POST | `/auth/login` | Public | Login, returns JWT |
| Users | GET | `/users?role=` | Admin | List users |
| Users | POST | `/users/teachers` | Admin | Create teacher account |
| Users | PUT | `/users/{id}/status` | Admin | Activate / deactivate |
| Users | POST | `/parents/link` | Parent | Link to student |
| Courses | GET | `/courses` | Any | List published courses |
| Courses | POST | `/courses` | Teacher | Create course |
| Courses | PUT | `/courses/{id}` | Teacher, Admin | Edit / publish / hide |
| Courses | DELETE | `/courses/{id}` | Teacher, Admin | Delete course |
| Lessons | GET | `/courses/{id}/lessons` | Enrolled Student, Teacher | List lessons |
| Lessons | POST | `/courses/{id}/lessons` | Teacher | Add lesson |
| Lessons | POST | `/lessons/{id}/complete` | Student | Mark lesson completed |
| Payments | POST | `/payment-requests` | Student | Submit request (multipart: courseId, method, receipt) |
| Payments | GET | `/payment-requests?status=` | Admin | List requests |
| Payments | GET | `/payment-requests/mine` | Student | My requests |
| Payments | PUT | `/payment-requests/{id}/approve` | Admin | Approve and create enrollment |
| Payments | PUT | `/payment-requests/{id}/reject` | Admin | Reject with reason |
| Exams | POST | `/courses/{id}/exams` | Teacher | Create exam with questions |
| Exams | POST | `/exams/{id}/start` | Student | Start attempt |
| Exams | POST | `/attempts/{id}/submit` | Student | Submit answers |
| Exams | GET | `/attempts?status=Submitted` | Teacher | Submissions waiting for grading |
| Exams | PUT | `/attempts/{id}/grade` | Teacher | Grade essay answers |
| Grades | GET | `/grades/mine` | Student | My grades |
| Grades | GET | `/children/{id}/progress` | Parent | Child progress and grades |
| Reports | GET | `/reports/summary` | Admin | Platform report |

**Example — submit payment request**
```http
POST /api/payment-requests
Content-Type: multipart/form-data

courseId=3
paymentMethod=InstaPay
receipt=<image file>
```
Response `201 Created`
```json
{ "id": 12, "courseId": 3, "amount": 500.00, "status": "Pending" }
```

**Standard errors:** `400` validation, `401` not authenticated, `403` wrong role, `404` not found, `409` already enrolled.

## 13.2 Testing & Validation Plan

| Level | What | Tool |
|-------|------|------|
| Unit tests | Services: approve payment creates enrollment, MCQ auto-grading, total score calculation | xUnit, Moq |
| Integration tests | API endpoints with test database | xUnit, WebApplicationFactory |
| UI tests | Main flows in the browser | Manual (and Cypress if time allows) |
| User acceptance | Each role tries its main flow | Checklist below |

**Sample test cases**

| ID | Scenario | Expected result |
|----|----------|-----------------|
| TC-01 | Login with correct credentials | Token returned, redirect by role |
| TC-02 | Login with wrong password | 401 and error message |
| TC-03 | Student submits request without receipt | Validation error |
| TC-04 | Student uploads a non-image file | Rejected |
| TC-05 | Admin approves request | Status Approved and enrollment exists |
| TC-06 | Admin rejects without reason | Validation error |
| TC-07 | Non-enrolled student opens lessons | 403 |
| TC-08 | Student requests a course twice after approval | 409 |
| TC-09 | Submit MCQ-only exam | Status Graded, correct score |
| TC-10 | Submit exam with essay | Status Submitted, teacher sees it |
| TC-11 | Teacher grades essays | TotalScore updated, status Graded |
| TC-12 | Parent opens a non-linked child | 403 |
| TC-13 | Teacher edits another teacher's course | 403 |

**UAT checklist:** admin approves a payment ✔ · teacher publishes a course ✔ · student watches a lesson and takes an exam ✔ · parent sees grades ✔

## 13.3 Deployment Strategy
- **Hosting:** ASP.NET Core API and React build on one host (for example Azure App Service or a Windows/Linux VPS); SQL Server on the same or a managed instance.
- **Environments:** Development (local) and Production.
- **Configuration:** connection string, JWT secret and upload path in environment variables / `appsettings.Production.json` (never committed).
- **Pipeline:** GitHub Actions — build, run tests, publish artifacts on every push to `main`.
- **Database:** EF Core migrations applied on deploy; daily backup.
- **Scaling:** start with one instance; if needed move uploads to cloud blob storage and add a second API instance behind a load balancer.
