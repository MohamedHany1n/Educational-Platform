# 11. UI/UX Design & Prototyping

## 11.1 Wireframes

### Login
```
+--------------------------------------------+
|              Educational Platform          |
|                                            |
|     Email    [______________________]      |
|     Password[______________________]       |
|                                            |
|              [      Login      ]           |
|        New student or parent? Register     |
+--------------------------------------------+
```

### Student — Course Page and Payment Request
```
+----------------------------------------------------------+
| Logo   Courses   My Courses   My Grades        [Profile] |
+----------------------------------------------------------+
|  +-----------+   Course Title                            |
|  | Thumbnail |   Teacher: ______      Price: 500 EGP     |
|  +-----------+   Description ...                         |
|                                                          |
|  Lessons (locked until enrollment)                       |
|   1. Introduction      2. Variables      3. Loops        |
|                                                          |
|  [ Enroll — Pay with InstaPay / Vodafone Cash ]          |
+----------------------------------------------------------+

+--------------- Payment Request (modal) ------------------+
|  Course: ____________   Amount: 500 EGP                  |
|  Payment method:  (o) InstaPay   ( ) Vodafone Cash       |
|  Receipt image:   [ Choose file ]                        |
|                     [ Submit Request ]                   |
+----------------------------------------------------------+
```

### Student — My Courses (lesson player)
```
+----------------------------------------------------------+
| Course: Programming Basics            Progress: 40%      |
+------------------------+---------------------------------+
| Lessons                |   +---------------------------+ |
| [x] 1. Introduction    |   |        Video Player       | |
| [ ] 2. Variables  <--  |   +---------------------------+ |
| [ ] 3. Loops           |   [ Mark as completed ]         |
| Exams                  |                                 |
|  > Midterm (60 min)    |                                 |
+------------------------+---------------------------------+
```

### Student — Exam
```
+----------------------------------------------------------+
| Midterm Exam                         Time left: 42:15    |
+----------------------------------------------------------+
| Q1 (2 marks)  What is a variable?                        |
|   ( ) A   ( ) B   (o) C   ( ) D                          |
| Q2 (5 marks)  Explain the difference between ...         |
|   [ essay answer text area ...........................] |
|                                         [ Submit Exam ]  |
+----------------------------------------------------------+
```

### Teacher — Dashboard
```
+----------------------------------------------------------+
| My Courses                                [ + New Course ]|
+----------------------------------------------------------+
| Title          | Students | Status    | Actions           |
| Programming 1  |   35     | Published | Lessons Exams Edit|
| Databases      |   12     | Draft     | Lessons Exams Edit|
+----------------------------------------------------------+
| Submissions waiting for grading: 7     [ Open ]          |
+----------------------------------------------------------+
```

### Teacher — Grade Essay
```
+----------------------------------------------------------+
| Student: Ahmed Ali     Exam: Midterm                     |
+----------------------------------------------------------+
| Q2 (5 marks): Explain the difference between ...          |
| Answer: ........................................         |
| Score: [ 4 ] / 5      Feedback: [____________________]   |
|                                       [ Save Grades ]    |
+----------------------------------------------------------+
```

### Admin — Payment Requests
```
+----------------------------------------------------------------------+
| Payment Requests      Filter: [Pending v]                            |
+----------------------------------------------------------------------+
| Student | Course        | Price | Method       | Receipt | Actions   |
| Omar    | Programming 1 | 500   | InstaPay     | [view]  | Approve / Reject |
| Sara    | Databases     | 400   | VodafoneCash | [view]  | Approve / Reject |
+----------------------------------------------------------------------+
```

### Admin — Reports
```
+----------------------------------------------------------+
| Students: 250   Teachers: 8   Courses: 20                |
| Pending requests: 5   Approved revenue: 48,000 EGP       |
| [ Enrollments per course — bar chart ]                   |
+----------------------------------------------------------+
```

### Parent — Child Progress
```
+----------------------------------------------------------+
| Child: [ Omar v ]                                        |
| Course            | Progress | Last exam | Score         |
| Programming 1     |   60%    | Midterm   | 18 / 20       |
| Databases         |   30%    | Quiz 1    | 7 / 10        |
+----------------------------------------------------------+
```

## 11.2 Navigation per Role
| Role | Main menu |
|------|-----------|
| Admin | Dashboard, Users, Courses, Payment Requests, Reports |
| Teacher | My Courses, Exams, Submissions, Results |
| Student | Courses, My Courses, My Requests, My Grades |
| Parent | My Children, Progress, Grades |

## 11.3 UI/UX Guidelines

**Design principles**
- Simple and consistent: same layout, buttons and tables on every page.
- One main action per screen (for example "Submit Request").
- Clear feedback: success / error messages, loading indicators, status badges.

**Color scheme**
| Use | Color |
|-----|-------|
| Primary | Blue `#2563EB` |
| Secondary | Indigo `#4F46E5` |
| Success (Approved, Graded) | Green `#16A34A` |
| Warning (Pending) | Amber `#F59E0B` |
| Danger (Rejected, Delete) | Red `#DC2626` |
| Background | `#F8FAFC`, text `#0F172A` |

**Typography:** Inter / Cairo (Arabic support), headings 24–32 px, body 16 px, line height 1.5.

**Accessibility**
- Text contrast ratio at least 4.5:1.
- All inputs have labels; forms are keyboard-navigable.
- Status is shown by text plus color, never color only.
- Responsive layout (mobile first), RTL support for Arabic content.
