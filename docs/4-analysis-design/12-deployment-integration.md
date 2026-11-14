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
        RT["Routing and Role Authorization"]
        UI --> RT
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

    RT -- "HTTPS page and form requests" --> AC
    RT --> UC
    RT --> CC
    RT --> PC
    RT --> EC
    RT --> RC
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
            API["ASP.NET Core MVC (Kestrel)"]
        end
        UP[("Uploads folder")]
    end

    subgraph DBS["Database Server"]
        SQL[("SQL Server: EducationalPlatformDB")]
    end

    VID["External video host"]

    BR -- "HTTPS 443 pages and forms + Cookie Authentication" --> API
    API -- "TCP 1433 (EF Core)" --> SQL
    API -- "File I/O" --> UP
    BR -. "video streaming" .-> VID
```

## 12.4 Integration Points
| Integration | Type | Notes |
|-------------|------|-------|
| Browser ↔ MVC Web Application | HTTPS pages and forms | Cookie Authentication (ASP.NET Core Identity) |
| MVC Web Application ↔ SQL Server | EF Core | Code-First migrations |
| MVC Web Application ↔ File system | File I/O | Receipts and thumbnails |
| Browser ↔ video host | Embedded player | No server-side integration |
| InstaPay / Vodafone Cash | **None (manual)** | Student pays outside the system and uploads the receipt |
