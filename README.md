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
- Netzwerkfähiger Drucker (getestet mit HP OfficeJet MFP 3302fdwg / HP Color LaserJet Pro MFP 3302)

## Features

- Massendruck von PDF-, Word- (`.docx`/`.doc`) und Excel-Dateien (`.xlsx`/`.xls`) ohne das 10-Dateien-Limit von Windows Explorer
- Warteschlange mit Umsortierung (Nach oben/unten), Entfernen einzelner Dateien und Queue leeren
- Einzelne oder alle Dateien erneut drucken, auch nach einem Fehler
- Druckreihenfolge umkehren (📃 "Rückwärts"-Option startet mit dem letzten statt dem ersten Dokument der Liste)
- Druckerauswahl per Dropdown sowie native Windows-Druckereinstellungen (Duplex, Papierformat, Qualität etc.) über "⚙ Druckereinstellungen"
- Ohne explizit gewählte Einstellungen wird der Standard des Druckertreibers (inkl. Duplex) übernommen, statt ihn zu überschreiben
- Seitenanzahl-Anzeige für PDF-Dateien in der Warteschlange
- Dateityp-Symbol (das tatsächliche Windows-Icon der Anwendung, die mit dem Dateityp verknüpft ist) je Zeile
- Status pro Datei (Queued/Printing/Printed/Failed/Skipped) inkl. Fehlermeldung bei Problemen
- Fortschrittsanzeige und Statusleiste während des Drucks
- Protokolldatei unter `%LOCALAPPDATA%\PrintBot\printbot<Datum>.log` (täglich rotierend, via Serilog)

Architektur- und Implementierungsdetails: siehe [SPECIFICATION.md](SPECIFICATION.md). Hinweise für Claude Code / KI-Assistenten zum Projekt: siehe [CLAUDE.md](CLAUDE.md).

## Bekannte Einschränkungen

- Seitenanzahl wird nur für PDFs ermittelt (schnell, ohne Office zu öffnen); bei Word/Excel bleibt die Spalte leer, da eine echte Zählung ein COM-Interop-Öffnen der Datei erfordern würde
- App-Icon fehlt aktuell (`Assets/printbot.ico`); siehe [src/PrintBot/Assets/README.md](src/PrintBot/Assets/README.md) zum Erzeugen eines eigenen Icons
- Einstellungen (Drucker, Duplex etc.) werden nur pro Sitzung gehalten, nicht dauerhaft gespeichert
