# Print-Bot

Bulk-Print-Tool für Windows. Umgeht das 10-Dateien-Limit von Windows Explorer und druckt PDF, Word und Excel-Dateien mit konfigurierbaren Druckeinstellungen.

## Quick Start

```powershell
# Build
dotnet restore
dotnet build --configuration Release

# Run
dotnet run --project src/PrintBot

# Publish self-contained EXE
dotnet publish src/PrintBot/PrintBot.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./publish
```

## Anforderungen

- Windows 10/11
- .NET 8 SDK (zum Bauen) oder .NET 8 Desktop Runtime (zum Ausführen der nicht-self-contained Variante)
- Microsoft Office (für Word/Excel-Druck)
- Netzwerkfähiger Drucker (getestet mit HP OfficeJet MFP 3302fdwg)

## Features

Siehe [SPECIFICATION.md](SPECIFICATION.md)
