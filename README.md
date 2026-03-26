# WoCo - Floorplan Annotations Application

> **ARCHIVED REPOSITORY**  
> This repository has been archived. The assignment was presented to the team and achieved very positive feedback.  
> The code is preserved for reference purposes and is no longer actively maintained.

---

## Introduction

This project is a **proof-of-concept application** developed as part of a technical assignment.

The goal is to demonstrate how annotations on a floorplan can be preserved and corrected when the underlying floorplan changes.

The solution focuses on:
- visualizing floorplans and annotations
- supporting multiple revisions of a floorplan
- automatically transforming annotation coordinates between revisions
- allowing manual adjustments of annotations where needed

---

## Overview

This PoC introduces a **revision-based model**:

- Each floorplan update creates a new **Floorplan Revision**
- Each annotation gets a corresponding **Annotation Revision**
- New revisions are derived from the **latest known revision**
- Coordinates are automatically transformed using **scale and offset**
- Users can manually adjust annotations where needed

---

## Getting Started

### Prerequisites

- **.NET 10.0 SDK** or later
- **Visual Studio 2026** or later (for WPF GUI) or any .NET-compatible IDE
- **SQLite** (included via Entity Framework Core)

### Building the Solution

1. Clone the repository:
   ```bash
   git clone https://github.com/sadnielsen/wocodemo
   cd wocodemo
   ```

2. Restore dependencies and build:
   ```bash
   cd src
   dotnet restore
   dotnet build
   ```

3. Run database migrations (automatic on first run):
   ```bash
   dotnet ef database update --project WoCo.Core
   ```

### Project Structure

```
WoCo/
├── WoCo.Core/         # Core business logic, data access, and services
├── WoCo.Cli/          # Command-line interface for data import and management
├── WoCo.Wpf/          # Windows Presentation Foundation GUI application
└── WoCo.Tests/        # Unit tests using NUnit
```

---

## CLI Usage Guide

The **WoCo CLI** (`WoCo.Cli`) is a command-line tool for importing floorplan data, managing the database, and debugging projects.

### Running the CLI

Navigate to the CLI directory and run:

```bash
cd WoCo.Cli
dotnet build
dotnet run
```

### CLI Commands

#### **Database Dump (Default)**

Running the CLI without arguments performs a database dump showing all projects, revisions, and annotations:

```bash
dotnet run
```

---

#### **Purge Database**

Removes all data from the database (Projects, FloorplanRevisions, Annotations, AnnotationRevisions).

```bash
dotnet run -- purge
```

**Example:**
```bash
cd WoCo.Cli
dotnet run -- purge
```

**Output:**
```
Database purged successfully.
=== DATABASE DUMP ===
=== END ===
```

---

#### **Import Sample Data**

Imports a project with floorplan(s) and annotations from files in the data directory.

```bash
dotnet run load=<prefix>
```

**Expected File Structure:**

For initial project creation, the CLI expects these files in the `TestData` directory (configurable via `appsettings.json`):

```
TestData/
├── <prefix>.0-floorplan.png         # Initial floorplan image
├── <prefix>.0-floorplan.json        # Initial floorplan metadata
├── <prefix>.0-annotations.json      # Initial annotations data
├── <prefix>.1-floorplan.png         # Optional: Revision 1 floorplan
├── <prefix>.1-floorplan.json        # Optional: Revision 1 metadata
├── <prefix>.2-floorplan.png         # Optional: Revision 2 floorplan
├── <prefix>.2-floorplan.json        # Optional: Revision 2 metadata
└── ...
```

**Floorplan Metadata Format** (`<prefix>.<revision>-floorplan.json`):

```json
{
  "scaleDenominator": 100.0,
  "offsetX": 0.0,
  "offsetY": 0.0
}
```

**Annotations Format** (`<prefix>.0-annotations.json`):

The annotations file should contain annotation data in JSON format. See the `CreateProjectService` for the expected schema.

**Example:**
```bash
dotnet run -- load=building1
```

**Output:**
```
Imported project: Sample Project 20260319123045 (a1b2c3d4-...)
Creating revision 1 from building1.1-floorplan.png
Created revision 1 for project Sample Project 20260319123045
Successfully loaded 1 additional revision(s).
=== DATABASE DUMP ===
Project: Sample Project 20260319123045 (a1b2c3d4-...)
  FloorPlanRevision v1
    Size: 1920 x 1080
    ScaleDenominator: 100
    FileName: building1.0-floorplan.png
    FileType: png
    FileBytes: 245832
  FloorPlanRevision v2
    Size: 1920 x 1080
    ScaleDenominator: 50
    FileName: building1.1-floorplan.png
    FileType: png
    FileBytes: 248921
  Annotation: e5f6g7h8-...
    Rev 1 | Point | (150.5,200.75) | Deleted: False | CreatedAt: 2026-03-19 12:30:45
    Rev 2 | Point | (160.5,210.75) | Deleted: False | CreatedAt: 2026-03-19 12:30:47
=== END ===
```

---

#### 3. **Purge and Load**

Combines both commands to clear the database and load fresh data:

```bash
dotnet run purge load=<prefix>
```

**Example:**
```bash
dotnet run purge load=sample
```

---

### Configuration

Edit `WoCo.Cli/appsettings.json` to configure:

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

**Settings:**
- **`DataFolder`**: Path to the directory containing floorplan images and annotation files (relative or absolute)

---

## WPF Application

Launch the WPF GUI application:

```bash
cd WoCo.Wpf
dotnet run
```

The WPF application provides a graphical interface for:
- Creating and managing projects
- Viewing and editing floorplan revisions
- Managing annotations across revisions
- Visualizing coordinate transformations

---

## Running Tests

Run all tests:

```bash
cd WoCo.Tests
dotnet test
```

---

## Architecture

### Core Concepts

1. **Projects**: Top-level containers for floorplans and annotations
2. **FloorplanRevisions**: Versioned floorplan images with coordinate system metadata
3. **Annotations**: Geometric shapes (points, rectangles, polygons) placed on floorplans
4. **AnnotationRevisions**: Versioned annotation data that automatically transforms across floorplan changes

### Key Services

- **`CreateProjectService`**: Creates new projects with initial floorplan and annotations
- **`CreateRevisionService`**: Creates new floorplan revisions and auto-transforms existing annotations
- **`ProjectRepository`**: Data access layer for persisting and retrieving projects

### Coordinate Transformation

When creating a new floorplan revision, annotations are automatically transformed based on:
- **Scale changes**: Adjusts coordinates when the scale denominator changes
- **Offsets**: Applies X/Y offsets for realignment
- **Preservation**: Maintains deleted annotation states across revisions

---

## Database

WoCo uses **SQLite** with Entity Framework Core for data persistence.

**Default location**: `%LOCALAPPDATA%\WoCo\app.db`

To view or modify the database:
- Use the CLI's database dump feature
- Use SQLite tools like [DB Browser for SQLite](https://sqlitebrowser.org/)
- Query via Entity Framework Core in code

---

