# WoCo

A **multi-project solution** for managing and visualizing annotated floorplans with revision history. 
The solution includes a **WPF desktop application** for interactive visualization and a **CLI tool** for batch importing and database management.

## Features

- **Multi-revision support** – Track changes to floorplans over time with automatic revision management
- **Create projects** – Import floorplan images with annotations and metadata
- **Persist to SQLite** – Projects, floorplan revisions, annotations, and revision history stored in a local database
- **Canvas visualization** – WPF application renders floorplans on a scrollable canvas with colored annotations (rectangles, polygons, points)
- **Coordinate transformation** – Automatic conversion from pixel coordinates to real-world coordinates using scale and offset metadata
- **CLI batch import** – Command-line tool for importing multiple projects and revisions
- **Database management** – Purge and inspect database contents via CLI

## Project Structure

The solution consists of three projects:

```
src/
├── WoCo.Core/              # Shared core library
│   ├── DataAccess/         # EF Core DbContext, repositories, configurations
│   ├── DependencyInjection/# Service registration extensions
│   ├── Models/             # Domain models (Project, Annotation, FloorplanRevision, AnnotationRevision)
│   ├── Services/           # Business logic (CreateProjectService, CreateRevisionService, AnnotationParser)
│   └── Types/              # Enums and value types (CoordinateSystemType, CoordinateOriginType)
│
├── WoCo.Wpf/               # WPF desktop application
│   ├── Controls/           # Custom WPF controls
│   ├── Converters/         # Value converters for data binding
│   ├── ViewModels/         # MVVM ViewModels
│   └── Views/              # XAML views and windows
│
└── WoCo.Cli/               # Command-line tool
    ├── Program.cs          # Main CLI entry point
    └── appsettings.json    # Configuration (data folder path, logging)
```

### Core Architecture

**WoCo.Core** provides the foundation:

| Component | Responsibility |
|-----------|----------------|
| **Models** | `Project`, `Annotation`, `FloorplanRevision`, `AnnotationRevision` – EF Core entities |
| **DataAccess** | `AppDbContext` (EF Core/SQLite), `ProjectRepository`, entity configurations |
| **Services** | `CreateProjectService` – project creation with initial floorplan<br>`CreateRevisionService` – add new floorplan revisions<br>`AnnotationParser` – parse JSON annotations |
| **Types** | Enums for coordinate systems (Pixels, RealWorld) and origins (TopLeft, BottomLeft) |

**WoCo.Wpf** implements MVVM pattern:

| Layer | Components |
|-------|------------|
| **View** | `MainWindow` – main application window with project list and canvas<br>`AnnotationCanvas` – custom control for rendering floorplans and annotations |
| **ViewModel** | ViewModels for managing UI state and commands |
| **Converters** | Data binding value converters |

**WoCo.Cli** provides command-line interface:
- Import projects from file system
- Create multiple revisions from numbered files
- Purge database contents
- Dump database for inspection

## CLI Usage

The CLI tool (`WoCo.Cli`) is used for batch operations and database management.

### Build & Run CLI

```bash
cd src/WoCo.Cli
dotnet run
```

The CLI accepts command-line arguments:

`purge`  

Deletes all data from the database.  

`load=<number>`  

Import data as new project with initial floorplan and annotations, and subsequent revisions.

This imports a project and all its revisions from the data folder. Files should be named:
- `<number>.0-floorplan.png` – Initial floorplan image
- `<number>.0-floorplan.json` – Initial floorplan metadata
- `<number>.0-annotations.json` – Initial annotations
- `<number>.1-floorplan.png` – Second revision floorplan (optional)
- `<number>.1-floorplan.json` – Second revision metadata (optional)
- And so on...

Combined arguments:

```bash
dotnet run purge load=sample
```

Purges the database then loads the project with prefix "sample".

### Configuration

Edit `appsettings.json` to configure the CLI:

```json
{
  "DataFolder": "TestData",
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

- **DataFolder** – Path to folder containing floorplan images and metadata files (relative or absolute)

### Floorplan Metadata Format

Each floorplan requires a JSON metadata file (`<prefix>.<revision>-floorplan.json`):

```json
{
  "scaleDenominator": 100.0,
  "offsetX": 0.0,
  "offsetY": 0.0
}
```

## WPF Application Usage

### Build & Run WPF App

```bash
cd src/WoCo.Wpf
dotnet run
```

### Features

- **Project List** – View all imported projects
- **Canvas Visualization** – Select a project to view floorplan with annotations
- **Revision Navigation** – Navigate between different revisions of a floorplan
- **Annotation Display** – Annotations rendered as colored shapes on the canvas
- **Coordinate Display** – Shows both pixel and real-world coordinates

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10)
- Windows 10 / 11 (WPF is Windows-only)

## Database Location

The SQLite database is stored at:
```
%LOCALAPPDATA%\WoCo\woco.db
```

Or as configured in the connection string in `WoCo.Core\DataAccess\AppDbContextDesignFactory.cs`.

## Development

### Add EF Core Migrations

```bash
cd src/WoCo.Core
dotnet ef migrations add <MigrationName> --startup-project ../WoCo.Cli/WoCo.Cli.csproj
```

### Update Database

The database is automatically migrated when running either the WPF app or CLI tool.

