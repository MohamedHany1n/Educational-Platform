# 3.3 Functional Requirements

> Same requirement IDs as [System Analysis & Design - Requirements](../4-analysis-design/03-requirements.md).


### Authentication & Users
| ID | Requirement |
|----|-------------|
| FR-01 | The system shall allow Students and Parents to register with name, email, phone and password. |
| FR-02 | The system shall allow all users to log in and log out. |
| FR-03 | The system shall restrict each page and API endpoint by role (Admin, Teacher, Student, Parent). |
| FR-04 | The admin shall create, edit, activate/deactivate and delete teacher and student accounts. |
| FR-05 | A parent shall be able to link to one or more student accounts. |

### Courses & Lessons
| ID | Requirement |
|----|-------------|
| FR-06 | A teacher shall create, edit and delete their own courses (title, description, price, thumbnail). |
| FR-07 | A teacher shall publish / unpublish a course. |
| FR-08 | A teacher shall add, edit, delete and reorder lessons with a video link. |
| FR-09 | The admin shall view, hide or delete any course. |
| FR-10 | Students shall browse and search published courses. |

### Payment & Enrollment
| ID | Requirement |
|----|-------------|
| FR-11 | A student shall submit an enrollment request for a course by choosing a payment method (InstaPay or Vodafone Cash) and uploading a receipt image. |
| FR-12 | The system shall store the course price at the time of the request. |
| FR-13 | The admin shall view pending requests with student, course, price, method and receipt image. |
| FR-14 | The admin shall approve or reject a request (a reason is required for rejection). |
| FR-15 | On approval, the system shall create an enrollment and give the student access to the course. |
| FR-16 | A student shall see the status of their requests (Pending / Approved / Rejected). |

### Learning
| ID | Requirement |
|----|-------------|
| FR-17 | An enrolled student shall watch the course lessons. |
| FR-18 | The system shall record which lessons each student has completed. |

### Exams & Grades
| ID | Requirement |
|----|-------------|
| FR-19 | A teacher shall create exams with MCQ and essay questions and per-question marks. |
| FR-20 | An enrolled student shall take a published exam within the time limit. |
| FR-21 | The system shall auto-grade MCQ answers on submission. |
| FR-22 | A teacher shall grade essay answers and add feedback. |
| FR-23 | The system shall calculate the total score once all questions are graded. |
| FR-24 | A student shall view their exam grades and feedback. |
| FR-25 | A teacher shall view results of students in their courses. |

### Parent & Reports
| ID | Requirement |
|----|-------------|
| FR-26 | A parent shall view progress (lesson completion) and grades of linked children only. |
| FR-27 | The admin shall view reports: users count, enrollments per course, requests by status, revenue from approved requests. |

