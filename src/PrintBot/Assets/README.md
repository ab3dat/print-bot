# Assets

Place a real multi-resolution `printbot.ico` here (16×16, 32×32, 48×48, 256×256 recommended).

`printbot.ico` cannot be generated as a valid binary file through this remote editing
environment (only plain text can be authored here). To add it locally on Windows:

```powershell
# Quick placeholder icon using System.Drawing (Windows PowerShell 5.1):
Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap 256,256
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(255,33,150,243))
$font = New-Object System.Drawing.Font("Segoe UI",120,[System.Drawing.FontStyle]::Bold)
$g.DrawString("P", $font, [System.Drawing.Brushes]::White, 60, 40)
$icon = [System.Drawing.Icon]::FromHandle($bmp.GetHicon())
$fs = [System.IO.File]::Create("$PSScriptRoot\printbot.ico")
$icon.Save($fs)
$fs.Close()
```

Once `Assets\printbot.ico` exists, add the ApplicationIcon PropertyGroup back in
`PrintBot.csproj` (currently commented out).
