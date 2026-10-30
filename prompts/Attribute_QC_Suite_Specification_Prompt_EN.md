# Attribute QC Suite — Complete Architectural Specification & Developer Prompt (English)

> **Document Type:** System Architecture Specification & Developer Implementation Prompt
> **Target Framework:** ArcGIS Pro 3.x SDK (.NET 8.0-windows / C# 12 / WPF)
> **Status:** Final Approved Build Specification — Custom Build (6 Field Rules)
> **Date:** October 30, 2026

---

## 📌 1. Objective & Vision

Expand the **Geometry QC Analyzer** into an enterprise-grade, unified **Geometry & Attribute Quality Control Suite** for ArcGIS Pro:

1. **Geometry QC:** The existing 11 cadastral checks on polygon parcel layers.
2. **Attribute QC:** A specialized, ultra-fast attribute verification engine supporting any `FeatureLayer` (Local Feature Classes and **Web Feature Services**), enforcing 6 focused field rules, and cross-referencing against Geodatabase tables and external Microsoft Excel (`.xlsx` / `.xls`) spreadsheets without file locking.

---

## 🔒 2. Architectural Guardrails & Performance Mandates

1. **Zero Geometry Processing (The Golden Rule):**
   * Calling, reading, or processing any `Geometry` or `GeometryEngine` methods is **strictly prohibited** during the Attribute QC execution cycle.
   * Inspection is 100% constrained to tabular textual and numeric attributes.
2. **Strict Read-Only Guarantee:**
   * No `EditOperation`, no source layer updates, and zero schema locks or writes to reference tables or Excel files.
3. **Non-Locking Excel Concurrency (`FileShare.ReadWrite`):**
   * Excel files are opened with a shared read stream via **`ExcelDataReader`** so users can edit `.xlsx` spreadsheets in Microsoft Excel simultaneously without triggering I/O exceptions.
   * **.NET 8 Prerequisite:** Register the code pages encoding provider at startup:
     ```csharp
     System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
     ```
4. **Fault Isolation (Per-Rule & Per-Row `try/catch`):**
   * Corrupted values or individual row failures must never abort the entire validation run.
5. **Thread Safety & Responsiveness:**
   * All queries execute inside `QueuedTask.Run` and respect the `CancellationToken` for instant cancellation.

---

## ⚙️ 3. Execution Flow & Target Layer Selection

* **Dual Execution Switches:**
  - Independent checkboxes for `Run Geometry QC` and `Run Attribute QC`.
  - **Default State:** Geometry QC is enabled by default, unless the settings are changed by the user.
  - Single **Run QC** ribbon button initiates either or both engines based on configuration.
* **Target Layer Constraint:**
  - Attribute QC requires an explicit `Target Layer` selection (Local Feature Class or **Web Feature Service**).
  - If set to `"All Polygon Layers"` or if no layer is selected, Attribute QC displays:
    `"Please select a target layer from Contents."`
* **Definition Query Compliance:**
  - Attribute QC queries strictly respect any active `Definition Query` applied to the layer in the map.

---

## 🖥️ 4. Attribute Rules Editor (Dedicated WPF Window)

Opened via an **"Attribute Rules"** button on the Settings DockPane and Ribbon. Implemented as a dedicated, spacious WPF Window (Dialog Window) for seamless rule configuration and reference table mapping.

### Key Capabilities & Smart Schema Signatures:

1. **Field Inspection:** Lists layer fields with Name, Alias, and Data Type for easy rule assignment.
2. **Smart Schema Signatures:**
   - Rules are bound not only to layer names, but to **Field Schema Signatures** (ordered set of field names and types).
   - **Auto-Detection:** Selecting a new layer that shares the schema signature of a previously configured layer automatically pre-populates its rules.
3. **Rule Profiles:** Ability to save, export, and import rule configurations as JSON templates across projects.
4. **Schema Drift Resilience:** If a field is deleted or renamed, execution skips the missing field and logs a safe `field missing` warning instead of crashing.

---

## 📋 5. The 6 Target Field Rules

Inspection is tightly focused on the following 6 rules:

|   Rule ID   | Rule Name                              | Validation Logic & Scope                                                                       |
| :----------: | :------------------------------------- | :--------------------------------------------------------------------------------------------- |
| **R1** | **Not Null / Not Empty**         | Field cannot be null, empty, or whitespace-only.                                               |
| **R2** | **No Leading / Trailing Spaces** | Flag strings with leading or trailing whitespace.                                              |
| **R3** | **Unique Value**                 | Value must be unique across all records within the current definition query.                   |
| **R4** | **Constant / Uniform Value**     | All features in the layer must share the identical value.                                      |
| **R5** | **Not Placeholder Value**        | Blacklists generic placeholder values: `N/A`, `NULL`, `none`, `-`, `0`, `unknown`. |
| **R6** | **Cross-Field Equality**         | Enforces equality between two fields in the same record (Field A == Field B).                  |

---

## 📊 6. Multi-Source Reference Table Comparison

### 1) Raw Values for Feature Services:

* To ensure full compatibility with Enterprise Geodatabases and Web Feature Services, the engine reads the stored **Raw Code** directly from the layer and compares it against the external reference table without domain description transformation.

### 2) Field Comparisons & Relative Difference:

1. **Text & Dates:** Direct normalized string match (with whitespace trimming).
2. **Numbers, Areas & Lengths (Stored Attribute Values Only):**
   * **No Geometry Calculations:** The engine solely reads the stored numerical attribute value (e.g. `Shape_Area` or any attribute field) and compares it with the reference value in Excel.
   * **Percentage Tolerance (Default: `1.0%`):** Users configure an acceptable tolerance percentage.
   * **Relative Difference Formula:**
     $$
     \text{Difference (\%)} = \left( \frac{|V_{\text{Layer}} - V_{\text{Ref}}|}{|V_{\text{Ref}}|} \right) \times 100
     $$
   * **Zero Value Guard:** If $V_{\text{Ref}} = 0$, then $V_{\text{Layer}}$ must equal $0$; otherwise an error is logged.

---

## 🚀 7. High-Performance Architecture

1. **In-Memory Caching:**
   * Reference tables (Excel or GDB) are read **once** into `Dictionary<string, Dictionary<string, object>>` for instant $O(1)$ lookups.
2. **Optimized SubFields Query (The Golden Rule for Speed):**
   * QueryFilter explicitly requests required attribute fields only:
     ```csharp
     var queryFilter = new QueryFilter { SubFields = "OBJECTID, " + string.Join(", ", requiredFields) };
     ```
   * **Strict Geometry Exclusion:** `Shape` field is **never** included in this query, preventing massive network payloads when validating Web Feature Services.

---

## 🧭 8. Results Presentation & Error Layer Generation

1. **Results DockPane Tree:**
   * Errors organized by Rule and Field, displaying OID, Field Name, Layer Value, Reference Value, and Percentage Difference.
   * Existing **Zoom** button navigates directly to the affected feature.
   * Dedicated **Orphan Keys** section for records present in Excel but missing in the GIS layer.
2. **Isolated Error Layer Generation (`Attribute_Errors_Log`):**
   * **Post-Validation Fetch:** After the super-fast tabular scan completes, a secondary, isolated query retrieves the `Shape` solely for the failed OIDs.
   * Creates an `Attribute_Errors_Log` layer added to the Map Contents with distinctive symbology.
3. **Clean State Feedback:** Header displays `"No attribute errors found ✓"`.

---

## 📝 9. Developer Test Checklist

- [ ] **Performance Audit:** Verify zero calls to `Shape` or `GeometryEngine` during the Attribute QC evaluation loop.
- [ ] Test the 6 rules (R1 to R6) on text and numeric fields containing nulls, placeholders, and duplicates.
- [ ] Test R3 (Unique) across the full dataset respecting the active Definition Query.
- [ ] Test Web Feature Service layers to confirm low network overhead and high-speed execution.
- [ ] Verify the 1.0% relative difference formula for stored numbers against Excel references.
- [ ] Open the Excel spreadsheet in Microsoft Excel and run validation concurrently to verify non-locking `FileShare.ReadWrite`.
- [ ] Verify that `Attribute_Errors_Log` is generated at the end of the run containing only the failed feature shapes.
