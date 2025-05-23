# Art Reference Viewer Project (.NET C# API)

This project consists of a .NET 8 API backend and an Angular (Standalone Components) frontend to make a timed art reference website to prevent "analysis paralysis" by forcing time limits on drawings. This helps artists to both decide what to draw for them and force a time limit as to not overwork a piece. Artists will be able to customize a time limit and refer back to images cycled for their session if they do need to go back to check their work.

## Prerequisites

1.  **.NET 8 SDK:** [Download and install](https://dotnet.microsoft.com/download/dotnet/8.0)
2.  **Node.js and npm:** [Download and install](https://nodejs.org/) (LTS version recommended, e.g., 18.x or 20.x)
3.  **Angular CLI:** Install globally if you haven't: `npm install -g @angular/cli` (v17+ recommended for best standalone support)
4.  **Code Editor:** Visual Studio Code, Visual Studio, or any preferred editor.

## Running the Application

You'll need two terminal windows/tabs - or as I run it, the API within Visual Studio and the Frontend in VSC

### Terminal 1: Run the .NET API

1.  Navigate to `ArtReferenceViewerProject/ArtReferenceAPI`.
2.  Run: `dotnet run`
    (Usually on `https://localhost:5001` and `http://localhost:5000`)

### Terminal 2: Run the Angular Frontend

1.  Navigate to `ArtReferenceViewerProject/art-reference-ui`.
2.  Run: `ng serve --open`
    (Usually on `http://localhost:4200`)
