# Lab Sample Tracker

**Author:** Luis Silva  
**Date:** September 22, 2026

A small proof of concept for a lab bench list. The WPF desktop app shows samples in a grid and edits them through a panel. The .NET 10 Web API stores those samples in memory and loads the first five rows from a CSV file. A test project checks the CRUD service.

## What this was meant to refresh

This exercise was a way to retest a simple end-to-end flow:

- A WPF screen with a grid, an add/edit panel, and checkbox selection
- A Web API split into a controller, a service, and a repository, wired with dependency injection
- An in-memory list seeded once from `LabSampleTracker.WebApi/Data/samples.csv`
- `async` / `await` from a WPF button through `HttpClient` to the API
- `Task.Run` on the API when creating a large in-memory batch

## Projects

| Project | Role |
|---|---|
| `LabSampleTracker.Desktop` | WPF client. Calls the REST API. |
| `LabSampleTracker.WebApi` | .NET 10 Web API. Holds the runtime sample list. |
| `LabSampleTracker.Tests` | xUnit tests for the sample service. |

## Run it

Start the API first, then the desktop app. The window loads samples as soon as it opens.

```bash
dotnet run --project LabSampleTracker.WebApi
dotnet run --project LabSampleTracker.Desktop
```

The API listens at `http://localhost:5287`.

| Action | Route |
|---|---|
| List samples | `GET /api/samples` |
| Add | `POST /api/samples` |
| Update | `PUT /api/samples/{id}` |
| Delete selected ids | `DELETE /api/samples` with `{ "ids": [1001, 1002] }` |
| Create 1,000,000 in-memory rows | `POST /api/samples/generate` with `{ "count": 1000000 }` |

The CSV is read the first time the repository is used. Later adds, updates, and deletes change the in-memory list only. Restart the API to load the five CSV rows again.

## Desktop behavior

- **Add** opens the panel. **Save** creates a sample and appends it to the grid.
- Check one or more rows to enable **Delete** and **Modify**.
- **Delete** asks for confirmation, sends the selected ids, and removes those rows.
- **Modify** opens the last checked row. The id stays locked, and **Save** updates that row.
- **Generate 1,000,000** builds the rows in API memory. **Refresh** stays disabled while that runs, then for 10 more seconds. When **Refresh** turns on, the background work is finished.
- **Refresh** calls the same list endpoint as the initial load and replaces the grid with whatever is in the repository.

Loading the million rows into the grid can take a while. Restart the API to go back to the five CSV rows.

## Retest

```bash
dotnet test LabSampleTracker.slnx
```

The service tests cover the CSV load, add, update, delete, and a small generate batch.

Manual pass:

1. Open the app and confirm the five CSV samples (ids 1001–1005).
2. Add a sample and confirm it appears at the end of the grid.
3. Check two rows, choose **Modify**, and confirm the last checked id is locked.
4. Save the edit and confirm that row changes in place.
5. Check rows, choose **Delete**, confirm the prompt, and confirm those rows disappear.
6. Choose **Generate 1,000,000** and confirm **Refresh** stays disabled. After the wait, **Refresh** turns on. Click it and confirm the grid reloads from the API.
