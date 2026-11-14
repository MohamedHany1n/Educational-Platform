# 12. System Deployment & Integration

## 12.1 Technology Stack

| Layer | Technology |
|-------|------------|
| Frontend | Razor Views, Bootstrap 5, jQuery Validation |
| Backend | ASP.NET Core MVC (C#), Cookie authentication |
| ORM | Entity Framework Core (Code-First, migrations) |
| Database | SQL Server |
| File storage | Server file system (receipts, thumbnails) |
| Video | External video links |
| Version control | Git and GitHub |

## 12.2 Component Diagram

```mermaid
flowchart LR
    subgraph Client["Client"]
        UI["Razor Views (.cshtml)"]
        RT["Router and Role Guards"]
        AX["API Client with Cookie Authentication"]
        UI --> RT --> AX
    end

    subgraph Server["ASP.NET Core MVC"]
        AC["Auth Component"]
        UC["Users Component"]
        CC["Courses Component"]
        PC["Payments and Enrollment Component"]
        EC["Exams Component"]
        RC["Reports Component"]
        DA["Data Access: EF Core"]
        FM["File Manager"]
        AC & UC & CC & PC & EC & RC --> DA
        PC --> FM
        CC --> FM
    end

    AX -- "REST over HTTPS" --> AC
    AX --> UC
    AX --> CC
    AX --> PC
    AX --> EC
    AX --> RC
    DA --> DB[("SQL Server")]
    FM --> FS[("Uploads folder")]
```

## 12.3 Deployment Diagram

```mermaid
flowchart TB
    subgraph CL["Client Device"]
        BR["Web Browser"]
    end

    subgraph HOST["Hosting Server (Windows or Linux)"]
        subgraph WEB["Web Server (IIS or Nginx)"]
            SPA["React static build"]
            API["ASP.NET Core MVC (Kestrel)"]
        end
        UP[("Uploads folder")]
    end

    subgraph DBS["Database Server"]
        SQL[("SQL Server: EducationalPlatformDB")]
    end

    VID["External video host"]

    BR -- "HTTPS 443" --> SPA
    BR -- "HTTPS REST + Cookie Authentication" --> API
    API -- "TCP 1433 (EF Core)" --> SQL
    API -- "File I/O" --> UP
    BR -. "video streaming" .-> VID
```

## 12.4 Integration Points
| Integration | Type | Notes |
|-------------|------|-------|
| React ↔ MVC Web Application | REST / JSON over HTTPS | Cookie Authentication in `Authorization: Bearer` header |
| MVC Web Application ↔ SQL Server | EF Core | Code-First migrations |
| MVC Web Application ↔ File system | File I/O | Receipts and thumbnails |
| Browser ↔ video host | Embedded player | No server-side integration |
| InstaPay / Vodafone Cash | **None (manual)** | Student pays outside the system and uploads the receipt |
