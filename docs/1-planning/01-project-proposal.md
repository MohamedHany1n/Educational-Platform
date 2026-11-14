# 1.1 Project Proposal

## Project Name
**Educational Platform**

## Overview
A web-based educational platform where **teachers** publish courses (video lessons and exams), **students** enroll after paying manually through InstaPay or Vodafone Cash, **parents** follow their children's progress, and an **admin** manages the whole system.

Built for the DEPI graduation project (**Full Stack .NET** track).

## Problem
Teachers who want to teach online usually rely on scattered tools (chat groups, file sharing, payment screenshots in chats). This leads to unorganized material, manual and error-prone payment tracking, no central place for exams and grading, and no way for parents to follow progress.

## Objectives
1. Let teachers create courses, lessons and exams in one place.
2. Let students request enrollment with a payment receipt and access the course after admin approval.
3. Automate grading of multiple-choice questions and let teachers grade essay questions.
4. Let students view grades and parents follow their children's progress.
5. Give the admin control over users, courses, payment requests and reports.

## Scope
**In scope**
- Authentication and role-based authorization (Admin, Teacher, Student, Parent)
- Course, lesson and exam management
- Manual payment workflow (receipt upload + admin approval)
- Exam taking, automatic grading (MCQ) and manual grading (essay)
- Progress and grade viewing for students and parents
- Admin reports

**Out of scope**
- Online payment gateways
- Live streaming classes
- Native mobile apps
- Certificates and chat between users

## Proposed Technology
| Part | Technology |
|------|------------|
| Backend | ASP.NET Core Web API |
| Database | SQL Server with Entity Framework Core |
| Authentication | ASP.NET Core Identity and JWT |
| Frontend | React |
| Version control | Git and GitHub |

## Expected Deliverables
Source code, documentation on GitHub, a working web application, test cases and reports, a user manual, a final presentation and a project infographic.

> Full analysis and design details: [System Analysis & Design](../4-analysis-design/README.md)
