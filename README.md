# wocodemo — Floorplan Annotator

A **WPF + MVVM + Canvas** application to visualize annotated floorplans.

## Features

- **Create a Project** – supply a name, upload a floorplan image and an annotations JSON file.  
- **Persist to SQLite** – every project (floorplan path, annotations, metadata) is stored in a local SQLite database.  
- **Canvas visualization** – the floorplan image is rendered on a scrollable WPF Canvas; each annotation is drawn as a rectangle, polygon, point, or label with a configurable color.  
- **Annotation table** – a lower panel lists all annotations for the selected project (label, type, coordinates, color swatch).  
- **Delete projects** – remove projects with a confirmation prompt.

## Architecture

```
src/FloorplanAnnotator/
├── Commands/         RelayCommand (ICommand)
├── Converters/       BoolToVisibilityConverter
├── Models/           Project, Annotation
├── Services/         AppDbContext (EF Core/SQLite), ProjectRepository, AnnotationParser
├── ViewModels/       BaseViewModel, MainViewModel, CreateProjectViewModel
└── Views/            MainWindow, CreateProjectView, AnnotationCanvas (UserControl)
```

### MVVM

| Layer | Responsibility |
|-------|----------------|
| **Model** | `Project`, `Annotation` – plain C# classes, persisted via EF Core |
| **ViewModel** | `MainViewModel` – project list, selection, commands; `CreateProjectViewModel` – dialog logic |
| **View** | `MainWindow.xaml` – layout, toolbar, project list, canvas; `CreateProjectView.xaml` – new project dialog; `AnnotationCanvas.xaml` – custom `UserControl` that draws the floorplan + annotations |

## Annotations JSON format

The annotations file is a JSON array.  Each item can have the following fields:

| Field | Type | Description |
|-------|------|-------------|
| `label` | string | Human-readable name |
| `type` | string | `rectangle` \| `polygon` \| `point` \| `label` |
| `coordinates` | string | Comma-separated numbers. Rectangle: `x1,y1,x2,y2`. Polygon: `x1,y1,x2,y2,...`. Point: `x,y`. Label: `x,y`. |
| `color` | string | CSS/WPF color string, e.g. `#FF0000` |

Example:
```json
[
  { "label": "Living Room", "type": "rectangle", "coordinates": "50,80,300,250", "color": "#2196F3" },
  { "label": "Front Door",  "type": "point",     "coordinates": "175,80",         "color": "#4CAF50" },
  { "label": "Terrace",     "type": "polygon",   "coordinates": "310,80,450,80,450,180,310,180", "color": "#FF9800" }
]
```

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) (Windows)
- Windows 10 / 11 (WPF is Windows-only)

## Build & Run

```bash
cd src/FloorplanAnnotator
dotnet run
```

The SQLite database is stored at `%LOCALAPPDATA%\FloorplanAnnotator\projects.db`.
