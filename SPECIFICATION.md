# Print-Bot — Specification

## Status: Draft · Last updated: 2026-09-24

---

## 1. Overview

**Print-Bot** is a Windows desktop application that overcomes the 10-file context-menu print limit in Windows Explorer. It provides bulk printing with queue management, printer configuration, and per-file status tracking. Designed specifically for Alex's accounting document workflow from a Terramaster NAS SMB share to an HP OfficeJet MFP 3302fdwg printer.

## 2. Software Stack

| Component | Choice | Rationale |
|-----------|--------|-----------|
| **Runtime** | .NET 8 (LTS) | Latest LTS, supported through Nov 2026; .NET 9 STS for migration path |
| **UI Framework** | WPF (Windows Presentation Foundation) | Native Windows UI, DataGrid/ListBox with drag-drop, MVVM-ready |
| **PDF Rendering** | PdfiumViewer (NuGet) | Open-source, fast, built-in "fit to page" scaling, no Ghostscript dependency |
| **Office Docs** | Microsoft.Office.Interop (Word + Excel) | Full fidelity printing with native page setup; requires Office installed on host |
| **Print Subsystem** | `System.Printing` (.NET) | Direct Windows Print Spooler API access; printer enumeration, job management |
| **Packaging** | Self-contained single-file EXE | No runtime installation needed; `dotnet publish -p:PublishSingleFile=true` |

**Why not alternatives:**
- *Electron* — Bloated, poor native printing, overkill for a single-purpose tool
- *Python/PyQt* — Distribution complex (PyInstaller), COM Interop fragile
- *PowerShell* — Cannot build a proper queue UI with drag-drop
- *.NET MAUI* — Not mature enough for desktop-only; WPF is battle-tested

## 3. Features

### 3.1 Supported File Types

| Format | Extensions | Print Backend |
|--------|-----------|---------------|
| PDF | `.pdf` | PdfiumViewer → `System.Printing.PrintQueue.AddJob()` |
| Word | `.docx`, `.doc` | Word Interop → `Document.PrintOut()` |
| Excel | `.xlsx`, `.xls` | Excel Interop → `Workbook.PrintOut()` |

File type detection by extension (case-insensitive). Unsupported files are silently skipped during directory import.

### 3.2 File & Directory Import

**Single files:** File → Add Files (Ctrl+O) opens standard Windows file dialog filtered to supported types.

**Directory import:** File → Add Folder (Ctrl+Shift+O) opens folder browser dialog.

- All supported files in the selected folder are added to the queue
- Checkbox: ☐ **Include subdirectories** — recursively scans child folders
- Duplicate entries (same full path) are rejected with a silent skip

### 3.3 Queue Order Editing

The queue is displayed as a numbered list with columns:

| # | File Name | Path | Status | Pages |
|---|-----------|------|--------|-------|

**Editing:**
- **Move Up / Move Down** buttons (or Alt+↑ / Alt+↓)
- **Drag & Drop** reordering within the list
- **Send to Top / Send to Bottom** via context menu

### 3.4 Queue Item Removal

- Select one or more items → **Remove** button (or Del key)
- **Clear Queue** button removes all items after confirmation dialog
- Removing an item does not delete the source file

### 3.5 Bulk Queueing

No artificial limit on queue size. Tested target: 500+ files. UI remains responsive via async loading and virtualized list.

### 3.6 Print Execution

**Bulk Print:** "Print All" button starts sequential printing of all queued items.

**Single Print:** Right-click → "Print Selected" or select items → "Print Selected" button.

**Safety:** A confirmation dialog appears before bulk print showing:
- Number of files to print
- Estimated total pages (if computable)
- Selected printer name

**Cancel:** "Cancel" button stops the queue after the current file finishes.

### 3.7 Status Tracking

Each queue item has one of these statuses:

| Status | Icon | Meaning |
|--------|------|---------|
| **Queued** | ⬜ | Ready, not yet printed |
| **Printing** | 🟡 | Currently being sent to printer |
| **Printed** | ✅ | Successfully spooled |
| **Failed** | ❌ | Error during print (with tooltip showing reason) |
| **Skipped** | ⏭️ | Manually excluded or cancelled |

Status persists only during the current session (no persistent state between app restarts).

### 3.8 Printer Selection & Settings

**Printer dropdown:** Enumerates all installed printers via `LocalPrintServer.GetPrintQueues()`. Defaults to system default printer.

**Settings (applied to all jobs in the queue):**

| Setting | Options | Default |
|---------|---------|---------|
| Page Scaling | Fit to Page / Actual Size / Shrink Oversized | **Fit to Page** |
| Orientation | Portrait / Landscape / Auto | Auto |
| Color Mode | Color / Grayscale | Color |
| Duplex | Off / Long Edge / Short Edge | Off |
| Copies | 1–99 | 1 |
| Paper Size | A4 / Letter / Legal / ... | A4 |

Settings are read from the selected printer's capabilities where possible. Unsupported settings are greyed out.

### 3.9 Additional Features (Nice-to-Have)

| Feature | Priority | Notes |
|---------|----------|-------|
| Progress bar (file X of Y) | High | Shown during bulk print |
| Log file (`%APPDATA%\PrintBot\printbot.log`) | Medium | Timestamped per-file results |
| Remember last printer & settings | Medium | Saved to `%APPDATA%\PrintBot\settings.json` |
| Minimize to tray during print | Low | Notifications on completion |
| Dark mode | Low | Follow Windows theme |

## 4. Architecture

### 4.1 Project Structure

```
print-bot/
├── PrintBot.sln
├── SPECIFICATION.md
├── README.md
├── src/
│   └── PrintBot/
│       ├── PrintBot.csproj
│       ├── App.xaml / App.xaml.cs
│       ├── Models/
│       │   ├── PrintJob.cs          # File info + status
│       │   ├── PrintSettings.cs     # Printer config
│       │   └── PrintResult.cs       # Outcome per job
│       ├── ViewModels/
│       │   ├── MainViewModel.cs     # Queue, commands, state
│       │   └── PrintJobViewModel.cs # Per-item VM wrapper
│       ├── Views/
│       │   ├── MainWindow.xaml      # Main UI
│       ��   └── SettingsDialog.xaml  # Printer settings
│       ├── Services/
│       │   ├── IPrintService.cs
│       │   ├── PrintService.cs      # Orchestrates print jobs
│       │   ├── PdfPrintService.cs   # Pdfium-based PDF printing
│       │   ├── OfficePrintService.cs # Word/Excel Interop
│       │   └── PrinterDiscoveryService.cs
│       └── Helpers/
│           └── FileTypeHelper.cs
```

### 4.2 Key Design Decisions

1. **Async printing via `Task.Run` + `IProgress<T>`** — Keeps UI responsive; each job runs on thread pool
2. **MVVM with CommunityToolkit.Mvvm** — Source-generated commands, observable properties
3. **`ObservableCollection<PrintJob>`** in MainViewModel — Automatic UI updates on add/remove/reorder
4. **Print one file at a time** — Sequential to avoid spooler overload; configurable delay between jobs (default 500ms)
5. **COM cleanup** — Each Office Interop instance is properly released in `finally` blocks to prevent zombie processes

### 4.3 Print Flow

```
User clicks "Print All"
  → MainViewModel.PrintAllCommand.Execute()
    → For each PrintJob with Status.Queued:
        → Set Status = Printing
        → Resolve IPrintService by file extension
        → printService.PrintAsync(job, settings, cancellationToken)
        → On success: Set Status = Printed
        → On failure: Set Status = Failed, log error
        → Report progress (IProgress<int>)
    → Show summary: "X printed, Y failed"
```

### 4.4 Error Handling

| Error | Behavior |
|-------|----------|
| File not found (NAS disconnected) | Mark as Failed, continue queue |
| Printer offline | Show dialog, pause queue, offer retry |
| COM exception (Office not installed) | Mark as Failed, show message |
| Pdfium load error | Mark as Failed, log details |
| Out of paper / paper jam | Detect via `PrintQueue.IsPaperJammed` etc., show alert |
| User cancellation | Finish current job, mark remaining as Skipped |

## 5. Build & Distribution

```powershell
# Build (from project root)
dotnet restore
dotnet build --configuration Release

# Publish self-contained single EXE (Win x64)
dotnet publish src/PrintBot/PrintBot.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=false `
    -o ./publish
```

Output: `./publish/PrintBot.exe` (~80–120 MB with self-contained .NET runtime, ~15 MB without)

## 6. MVP Scope

For the **first release**, implement:

- [x] PDF printing with fit-to-page (PdfiumViewer) — *core need*
- [ ] Word/Excel printing via Office Interop
- [x] Add files / Add folder (no subdirectory toggle yet)
- [x] Queue reorder (move up/down)
- [x] Remove from queue
- [x] Print All with cancel
- [x] Per-item status (queued/printed/failed)
- [x] Printer selection dropdown
- [x] Print settings panel (scaling, copies, duplex, color, paper size)
- [ ] Session log (optional for MVP)

**Phase 2:**
- [ ] Drag-drop reorder
- [ ] Include subdirectories toggle
- [ ] Single-file print from queue
- [ ] Persistent settings (`settings.json`)
- [ ] Progress bar + summary dialog
- [ ] Dark mode

## 7. Prerequisites (User Machine)

- Windows 10 21H2+ / Windows 11
- .NET 8 Desktop Runtime (included in self-contained EXE)
- Microsoft Office 2016+ (for Word/Excel printing)
- Default PDF viewer (Pdfium is bundled)
- Accessible printer via Windows Print Spooler

---

*End of specification*
