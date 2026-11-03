# 12. System Deployment & Integration

## 12.1 Technology Stack

| Layer | Technology |
|-------|------------|
| Frontend | React, React Router, Axios |
| Backend | ASP.NET Core Web API (C#), JWT authentication |
| ORM | Entity Framework Core (Code-First, migrations) |
| Database | SQL Server |
| File storage | Server file system (receipts, thumbnails) |
| Video | External video links |
| Version control | Git and GitHub |

## 12.2 Component Diagram

```mermaid
flowchart LR
    subgraph Client["Client"]
        UI["React UI Components"]
        RT["Router and Role Guards"]
        AX["API Client with JWT"]
        UI --> RT --> AX
    end

    subgraph Server["ASP.NET Core Web API"]
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
            API["ASP.NET Core Web API (Kestrel)"]
        end
        UP[("Uploads folder")]
    end

    subgraph DBS["Database Server"]
        SQL[("SQL Server: EducationalPlatformDB")]
    end

    VID["External video host"]

    BR -- "HTTPS 443" --> SPA
    BR -- "HTTPS REST + JWT" --> API
    API -- "TCP 1433 (EF Core)" --> SQL
    API -- "File I/O" --> UP
    BR -. "video streaming" .-> VID
```

## 12.4 Integration Points
| Integration | Type | Notes |
|-------------|------|-------|
| React ↔ Web API | REST / JSON over HTTPS | JWT in `Authorization: Bearer` header |
| Web API ↔ SQL Server | EF Core | Code-First migrations |
| Web API ↔ File system | File I/O | Receipts and thumbnails |
| Browser ↔ video host | Embedded player | No server-side integration |
| InstaPay / Vodafone Cash | **None (manual)** | Student pays outside the system and uploads the receipt |
