# 13. Additional Deliverables

## 13.1 Routes Documentation (main pages and actions)

The system is a server-rendered MVC website, so the "endpoints" are page routes. All routes except Register and Login require a logged-in user (Cookie Authentication). Routes of modules that are not built yet are the planned design and may change slightly.

| Module | Method | Route | Role | Description |
|--------|--------|-------|------|-------------|
| Account | GET, POST | `/Account/Register` | Public | Register as a student (waits for admin approval) |
| Account | GET, POST | `/Account/Login` | Public | Login and redirect to the role dashboard |
| Account | POST | `/Account/Logout` | Any | Logout |
| Admin | GET | `/Admin` | Admin | Dashboard |
| Admin | GET | `/Admin/PendingUsers` | Admin | List pending student registrations; approve with POST `/Admin/ApproveUser/{id}` or reject with POST `/Admin/RejectUser/{id}` |
| Admin | GET | `/Admin/Students` | Admin | List, search (name, email, phone) and filter students by status, with paging |
| Admin | GET | `/Admin/StudentDetails/{id}` | Admin | Student details |
| Admin | GET, POST | `/Admin/EditStudent/{id}` | Admin | Edit name, email, phone and parent phone |
| Admin | POST | `/Admin/ToggleStudentStatus/{id}` | Admin | Suspend or activate a student |
| Admin | POST | `/Admin/DeleteStudent/{id}` | Admin | Soft delete a student |
| Admin | POST | `/Admin/RestoreStudent/{id}` | Admin | Restore a deleted student (returns as suspended) |
| Admin | GET | `/Admin/Parents` | Admin | List parent accounts |
| Admin | GET, POST | `/Admin/CreateParent?studentId=` | Admin | Create a parent account and link it to a student |
| Admin | GET | `/Admin/ParentDetails/{id}` | Admin | Parent details and linked students |
| Admin | POST | `/Admin/LinkStudent` | Admin | Link a student to a parent |
| Admin | POST | `/Admin/UnlinkStudent` | Admin | Remove a link |
| Admin | POST | `/Admin/ToggleParentStatus/{id}` | Admin | Suspend or activate a parent |
| Admin | GET | `/Admin/Teachers` | Admin | List teachers |
| Admin | GET, POST | `/Admin/CreateTeacher` | Admin | Create a teacher account |
| Admin | GET | `/Admin/Payments` | Admin | Review payment requests; approve with POST `/Admin/ApprovePayment/{id}` (creates enrollment) or reject with POST `/Admin/RejectPayment/{id}` and a reason |
| Admin | GET | `/Admin/Reports` | Admin | Platform report |
| Courses | GET | `/Courses` | Any | List published courses |
| Courses | GET, POST | `/Courses/Create` | Teacher | Create course |
| Courses | GET, POST | `/Courses/Edit/{id}` | Teacher, Admin | Edit, publish or hide |
| Courses | POST | `/Courses/Delete/{id}` | Teacher, Admin | Delete course |
| Lessons | GET | `/Courses/{id}/Lessons` | Enrolled Student, Teacher | List lessons |
| Lessons | GET, POST | `/Lessons/Create?courseId=` | Teacher | Add lesson |
| Lessons | POST | `/Lessons/Complete/{id}` | Student | Mark lesson completed |
| Payments | GET, POST | `/Payments/Create?courseId=` | Student | Submit request with transaction number and receipt image |
| Payments | GET | `/Payments/Mine` | Student | My requests |
| Exams | GET, POST | `/Exams/Create?courseId=` | Teacher | Create exam with questions |
| Exams | POST | `/Exams/Start/{id}` | Student | Start attempt |
| Exams | POST | `/Exams/Submit/{attemptId}` | Student | Submit answers |
| Exams | GET | `/Teacher/Submissions` | Teacher | Submissions waiting for grading |
| Exams | POST | `/Teacher/Grade/{attemptId}` | Teacher | Grade essay answers |
| Grades | GET | `/Grades/Mine` | Student | My grades |
| Parent | GET | `/Parent/Child/{id}` | Parent | Child progress and grades |

**Example: submit a payment request**

The student fills a form on `/Payments/Create?courseId=3` and sends it as `multipart/form-data`:

```http
POST /Payments/Create
Content-Type: multipart/form-data

courseId=3
paymentMethod=InstaPay
transactionNumber=123456789
receipt=<image file>
```

On success the user is redirected to `/Payments/Mine`, where the new request appears with status `Pending`.

**Standard results:** validation errors are shown on the same page, a user who is not logged in is redirected to Login, a wrong role sees the Access Denied page, a missing item shows a not-found page, and a student who is already enrolled sees a message.

## 13.2 Testing & Validation Plan

| Level | What | Tool |
|-------|------|------|
| Unit tests | Services: approve payment creates enrollment, MCQ auto-grading, total score calculation | xUnit, Moq |
| Integration tests | Controller actions with test database | xUnit, WebApplicationFactory |
| UI tests | Main flows in the browser | Manual (and Cypress if time allows) |
| User acceptance | Each role tries its main flow | Checklist below |

**Sample test cases**

| ID | Scenario | Expected result |
|----|----------|-----------------|
| TC-01 | Login with correct credentials | Redirect to the role dashboard |
| TC-02 | Login with wrong password | Login page shows an error message |
| TC-03 | Student submits request without receipt | Validation error |
| TC-04 | Student uploads a non-image file | Rejected |
| TC-05 | Admin approves request | Status Approved and enrollment exists |
| TC-06 | Admin rejects without reason | Validation error |
| TC-07 | Non-enrolled student opens lessons | Access Denied page |
| TC-08 | Student requests a course twice after approval | Message: already enrolled |
| TC-09 | Submit MCQ-only exam | Status Graded, correct score |
| TC-10 | Submit exam with essay | Status Submitted, teacher sees it |
| TC-11 | Teacher grades essays | TotalScore updated, status Graded |
| TC-12 | Parent opens a non-linked child | Access Denied page |
| TC-13 | Teacher edits another teacher's course | Access Denied page |
| TC-14 | Student registers with a phone number already used | Validation error |
| TC-15 | Student registers with the same number for student and parent | Validation error |
| TC-16 | Student submits a transaction number already used with the same method | Request rejected with a message |
| TC-17 | Admin deletes a student, then the student tries to log in | Login refused |
| TC-18 | Admin restores a deleted student | Student appears as suspended |
| TC-19 | Admin creates a parent and links a student | Parent logs in and sees only the linked child |
| TC-20 | Admin approves a pending student | Student can log in and the approval time is saved |

**UAT checklist:** admin approves a payment ✔ · teacher publishes a course ✔ · student watches a lesson and takes an exam ✔ · parent sees grades ✔

## 13.3 Deployment Strategy
- **Hosting:** ASP.NET Core MVC website on one host (for example Azure App Service or a Windows/Linux VPS); SQL Server on the same or a managed instance.
- **Environments:** Development (local) and Production.
- **Configuration:** connection string and upload path in environment variables / `appsettings.Production.json` (never committed).
- **Pipeline:** GitHub Actions — build, run tests, publish artifacts on every push to `main`.
- **Database:** EF Core migrations applied on deploy; daily backup.
- **Scaling:** start with one instance; if needed move uploads to cloud blob storage and add a second web instance behind a load balancer.
