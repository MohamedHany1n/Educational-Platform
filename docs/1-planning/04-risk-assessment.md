# 1.4 Risk Assessment & Mitigation Plan

Likelihood and impact: **L** = Low, **M** = Medium, **H** = High.

| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| R1 | Short implementation time before the 30 Nov deadline | H | H | Build the core flow first (login, courses, payment approval, exams); postpone extras; weekly progress check |
| R2 | Merge conflicts between team members | M | M | Feature branches, small Pull Requests, merge `develop` into the branch often |
| R3 | Unequal progress between members | M | H | Clear module ownership, review of progress every week, lead helps blocked tasks |
| R4 | Security issues (broken access control, weak validation) | M | H | Role checks (Authorize attribute) on every controller action, server-side validation, ownership checks, code review |
| R5 | Malicious or oversized receipt uploads | M | M | Accept only jpg/png up to 5 MB, rename files on the server, restrict who can view them |
| R6 | Secrets (connection string) committed to GitHub | M | H | Use `.gitignore`, environment variables and `appsettings.Production.json` that is never committed |
| R7 | Database changes breaking other members' work | M | M | EF Core migrations reviewed in Pull Requests; one member merges migrations at a time |
| R8 | Views and controllers not matching (wrong field names, missing validation) | M | M | Use view models, test each page in the browser before opening a Pull Request |
| R9 | Loss of work or environment problems | L | H | All work pushed to GitHub regularly; README with setup steps |
| R10 | Low test coverage because of time pressure | M | M | Write tests for the most important services first (payment approval, MCQ grading) |
