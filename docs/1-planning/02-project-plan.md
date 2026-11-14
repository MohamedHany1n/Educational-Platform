# 1.2 Project Plan

## Timeline (Gantt Chart)

```mermaid
gantt
    title Educational Platform - Project Timeline
    dateFormat YYYY-MM-DD
    axisFormat %d %b

    section Documentation
    Planning, Literature Review, Requirements :doc1, 2026-10-01, 2026-10-16
    System Analysis & Design                  :doc2, 2026-10-17, 2026-11-06

    section Implementation
    Foundation (DB, Identity, roles, cookies) :dev1, 2026-11-09, 2026-11-15
    Feature modules (courses, payments, exams, parent) :dev2, 2026-11-14, 2026-11-26
    Views and UI for each module          :dev3, 2026-11-16, 2026-11-28
    Integration and bug fixing                :dev4, 2026-11-26, 2026-11-30

    section Testing and Final
    Testing and bug reports                   :test1, 2026-11-25, 2026-12-01
    User manual, presentation, infographic    :fin1, 2026-12-01, 2026-12-04
```

## Milestones and Deliverables
| # | Milestone | Deliverables | Due date |
|---|-----------|--------------|----------|
| M1 | Planning, Literature Review, Requirements | Proposal, plan, roles, risks, KPIs, related systems, requirements | 16 Oct 2026 |
| M2 | System Analysis & Design | Use cases, architecture, ERD, DFD, sequence/activity/state/class diagrams, wireframes, deployment | 6 Nov 2026 |
| M3 | Implementation | Working MVC website, README with run instructions | 30 Nov 2026 |
| M4 | Testing and Final | Test plan and results, bug reports, user manual, technical documentation, presentation, infographic | 4 Dec 2026 |

> Milestone due dates follow the DEPI Project Documentation Guidelines.

## Resource Allocation
| Resource | Details |
|----------|---------|
| Team | 4 members (see [Task Assignment & Roles](03-task-assignment-roles.md)) |
| Tools | Visual Studio / VS Code, SQL Server, Git and GitHub |
| Workflow | `main` (submission), `develop` (integration), `feature/*` (one branch per task) with Pull Request and one review |
| Communication | Team chat group and GitHub Pull Requests |
