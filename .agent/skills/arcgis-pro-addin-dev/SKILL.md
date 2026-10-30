---
name: arcgis-pro-addin-dev
description: >-
  ArcGIS Pro Add-In development pattern guide for .NET 8 / C#, DAML, WPF MVVM, and the ArcGIS Pro SDK.
  Use when creating or updating ArcGIS Pro Add-Ins, configuring Config.daml, managing QueuedTask.Run
  thread safety, persisting project-level settings in .aprx files, drawing temporary CIM map graphics,
  implementing feature selection guards, and packaging .esriAddinX with assembly cache purging.
---

# ArcGIS Pro Add-In Development Patterns (.NET 8 & ArcGIS Pro SDK)

This skill provides battle-tested design patterns, threading contracts, DAML declarations, and build workflows for creating high-performance ArcGIS Pro Add-Ins.

---

## 1. Declarative DAML & Button Rules

### Declarative Rules in `Config.daml`
1. **Module Auto-Load:** Set `<insertModule autoLoad="false">` to avoid unnecessary initialization overhead on ArcGIS Pro startup unless standing background listeners are required.
2. **Ribbon Tab & Group Placement:** Use `appearsOnAddInTab="false"` to define dedicated ribbon tabs rather than crowding the generic "Add-Ins" tab.
3. **Button Registration:**
   ```xml
   <button id="MyAddIn_RunBtn" caption="Run QC" className="MyAddIn.UI.RunButton" 
           loadOnClick="true" smallImage="Images\Run16.png" largeImage="Images\Run32.png">
     <tooltip heading="Run QC">Executes validation on selected features.<disabledText /></tooltip>
   </button>
   ```

### Button Constructor Rule
Every button class inheriting from `ArcGIS.Desktop.Framework.Contracts.Button` must explicitly set `Enabled = true;` in its parameterless constructor:
```csharp
public class RunQCButton : ArcGIS.Desktop.Framework.Contracts.Button
{
    public RunQCButton()
    {
        Enabled = true; // Essential to prevent ribbon button graying out
    }

    protected override void OnClick()
    {
        ResultsDockPaneViewModel.Show();
    }
}
```

---

## 2. Thread Safety Model & `QueuedTask.Run`

The ArcGIS Pro SDK strictly isolates the UI thread from the internal geodatabase/geometry core thread:
* **Esri Internal Thread:** All geometry operations (`GeometryEngine`), geodatabase cursors (`SearchCursor`, `InsertCursor`), and map view layer interactions (`MapView.Active`) MUST execute inside:
  ```csharp
  await QueuedTask.Run(() =>
  {
      // Safe to interact with ArcGIS Pro geometry & geodatabase
      var simplified = GeometryEngine.Instance.Simplify(geometry);
  });
  ```
* **UI Dispatcher:** Any updates to ObservableCollections, ViewModel properties, or WPF controls bound to the view must occur on the UI Dispatcher:
  ```csharp
  System.Windows.Application.Current.Dispatcher.Invoke(() =>
  {
      IssuesCollection.Add(issue);
  });
  ```

---

## 3. High-Performance Viewport Display Cache Acquisition

When validating features, avoid disk SearchCursors if the user is inspecting an active map. Reading from the MapView display cache offers orders of magnitude faster execution:
```csharp
await QueuedTask.Run(() =>
{
    var mapView = MapView.Active;
    if (mapView == null) return;

    // Fast selection pre-check (Feature Cap Guard)
    int totalSelected = layer.GetSelection().GetCount();
    if (totalSelected > maxFeaturesCap)
    {
        result.CapExceeded = true;
        return;
    }

    // Direct cache retrieval
    using (var displayCursor = mapView.GetFeatures(layer.GetSelection()))
    {
        // Extract in-memory polygons
    }
});
```

---

## 4. Per-Project State Persistence via `Module` Settings

To remember active layers, selections, or project-specific configurations inside the `.aprx` project file without polluting global machine settings:
```csharp
public class GeometryQCModule : Module
{
    public string TargetLayerUri { get; set; }
    public string TargetLayerName { get; set; }

    protected override Task OnWriteSettingsAsync(ModuleSettingsWriter settings)
    {
        settings.Add("TargetLayerUri", TargetLayerUri ?? string.Empty);
        settings.Add("TargetLayerName", TargetLayerName ?? string.Empty);
        return Task.CompletedTask;
    }

    protected override Task OnReadSettingsAsync(ModuleSettingsReader settings)
    {
        TargetLayerUri = settings.Get("TargetLayerUri") as string;
        TargetLayerName = settings.Get("TargetLayerName") as string;
        return Task.CompletedTask;
    }
}
```

---

## 5. Temporary CIM Map Graphic Overlays

Draw high-visibility, temporary visual markers on the active map view without altering geodatabase data:
1. Create CIM point or polygon symbols with distinct colors:
   - Error: Semi-transparent Red (`#CCFF0000`).
   - Warning: Amber / Coral (`#FFFF7F50`).
2. Add graphics to the map view overlay:
   ```csharp
   IDisposable graphicDisposable = MapView.Active.AddOverlay(geometry, symbol.MakeSymbolReference());
   ```
3. Maintain a collection of `IDisposable` graphic tokens and dispose them when results are cleared or a new run starts.

---

## 6. Packaging & Assembly Cache Purging

ArcGIS Pro caches add-in assemblies in `%LOCALAPPDATA%\ESRI\ArcGISPro\AssemblyCache\{AddIn-GUID}`. To prevent stale assembly locks during iterative development:
```powershell
$guid = "{8a7f921d-44a3-4b92-95f2-953e5e6080dc}"
$cachePath = "$env:LOCALAPPDATA\ESRI\ArcGISPro\AssemblyCache\$guid"
if (Test-Path $cachePath) {
    Remove-Item -Path $cachePath -Recurse -Force
}
dotnet build -c Release
```
Always alert users that ArcGIS Pro only reads add-in binaries on startup (no hot-reloading).
