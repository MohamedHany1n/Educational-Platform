# 1. Problem Statement & Objectives

## 1.1 Problem Statement
Teachers who want to teach online have no simple, organized place to publish video lessons and exams, collect course fees, and follow student results. Today this is usually done through scattered tools (chat groups, file sharing, manual payment screenshots in chats), which causes:

- Lost or unorganized learning material.
- Manual, error-prone tracking of who paid and who is enrolled.
- No central place to take exams and grade them, especially essay questions.
- Parents have no clear way to follow their children's progress.

## 1.2 Proposed Solution
A web platform with four roles (Admin, Teacher, Student, Parent) that centralizes course content, enrollment with manual payment approval, exams with automatic and manual grading, and progress tracking.

## 1.3 Objectives
1. Let teachers create courses, lessons (videos) and exams in one place.
2. Let students browse courses, request enrollment by paying through InstaPay / Vodafone Cash, and access the content after admin approval.
3. Automate grading of multiple-choice questions and let teachers grade essay questions.
4. Let students view their grades and parents follow their children's progress.
5. Give the admin control over users, courses, payment requests and reports.

## 1.4 Scope

**In scope**
- Authentication and role-based authorization (Admin, Teacher, Student, Parent).
- Course, lesson and exam management.
- Manual payment workflow (receipt image upload + admin approval).
- Exam taking, auto-grading (MCQ), manual grading (essay).
- Progress and grade viewing for students and parents.
- Admin reports.

**Out of scope (for this phase)**
- Online payment gateways (card / wallet APIs).
- Live streaming classes.
- Mobile native apps.
- Certificates and chat between users.

## 1.5 Target Users
| User | Need |
|------|------|
| Admin | Control the platform and verify payments |
| Teacher | Publish content and evaluate students |
| Student | Learn and get evaluated |
| Parent | Follow the child's performance |

## 1.6 Success Criteria (KPIs)
- Payment request reviewed by admin in a clear, trackable status (Pending / Approved / Rejected).
- Student gets access to course content immediately after approval.
- MCQ exam result available instantly after submission.
- Page response time under 3 seconds for normal pages.
