# 7. Sequence Diagrams

## 7.1 Login

```mermaid
sequenceDiagram
    actor U as User
    participant FE as Browser (Razor View)
    participant API as AccountController
    participant SV as AuthService
    participant DB as SQL Server

    U->>FE: Enter email and password
    FE->>API: POST /Account/Login
    API->>SV: Login(model)
    SV->>DB: Find user by email
    DB-->>SV: User record
    SV->>SV: Verify password hash and IsActive
    alt valid
        SV-->>API: authenticated session cookie and role
        API-->>FE: Redirect to role dashboard
        FE-->>U: Show dashboard page
    else invalid
        SV-->>API: Invalid credentials
        API-->>FE: Login page with error
        FE-->>U: Show error message
    end
```

## 7.2 Payment Request and Enrollment Approval

```mermaid
sequenceDiagram
    actor S as Student
    actor A as Admin
    participant FE as Browser (Razor View)
    participant API as PaymentRequestsController
    participant SV as PaymentService
    participant FS as File Storage
    participant DB as SQL Server

    S->>FE: Pay via InstaPay or Vodafone Cash (outside system)
    S->>FE: Choose method and upload receipt
    FE->>API: POST /Payments/Create (courseId, method, receipt image)
    API->>SV: CreateRequest(model)
    SV->>DB: Check not already enrolled
    SV->>DB: Get course price
    SV->>FS: Save receipt image
    SV->>DB: Insert PaymentRequest (Status = Pending)
    API-->>FE: Redirect to My Requests
    FE-->>S: Request submitted, waiting for approval

    A->>FE: Open pending requests
    FE->>API: GET /Admin/Payments
    API-->>FE: Page with requests and receipts
    A->>FE: Approve request
    FE->>API: POST /Admin/ApprovePayment/id
    API->>SV: Approve(id, adminId)
    SV->>DB: Update Status = Approved, ReviewedAt
    SV->>DB: Insert Enrollment
    API-->>FE: Redirect to Payments page
    FE-->>A: Request approved
    S->>FE: Open My Courses
    FE-->>S: Course is now accessible
```

## 7.3 Take Exam and Grade Essay Questions

```mermaid
sequenceDiagram
    actor S as Student
    actor T as Teacher
    participant FE as Browser (Razor View)
    participant API as ExamsController
    participant SV as ExamService
    participant DB as SQL Server

    S->>FE: Start exam
    FE->>API: POST /Exams/Start/id
    API->>SV: StartAttempt(examId, studentId)
    SV->>DB: Verify enrollment and exam published
    SV->>DB: Insert ExamAttempt (InProgress)
    API-->>FE: Exam page with questions and timer
    S->>FE: Answer questions and submit
    FE->>API: POST /Exams/Submit/id (answers)
    API->>SV: Submit(attemptId, answers)
    SV->>DB: Save AttemptAnswers
    SV->>SV: Auto-grade MCQ answers
    alt exam has essay questions
        SV->>DB: Status = Submitted
        API-->>FE: Submitted, waiting for teacher
    else only MCQ
        SV->>DB: Status = Graded, TotalScore
        API-->>FE: Result with score
    end

    T->>FE: Open ungraded submissions
    FE->>API: GET /Teacher/Submissions
    T->>FE: Enter score and feedback per essay
    FE->>API: POST /Teacher/Grade/id
    API->>SV: GradeEssays(attemptId, scores)
    SV->>DB: Update AttemptAnswers scores
    SV->>DB: TotalScore = sum, Status = Graded
    API-->>FE: Redirect to Submissions page
    S->>FE: View grades
    FE-->>S: Score and teacher feedback
```
