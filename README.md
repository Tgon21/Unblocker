# Unblocker

A tiny portable Windows utility that strips the "downloaded from the internet"
block (the `Zone.Identifier` alternate data stream / Mark-of-the-Web) from any
file or folder.

No installer, no dependencies beyond what ships with Windows. Single ~22 KB
`.exe`.


## Why

Files downloaded with a browser, extracted from a `.zip`, or copied off a
network share often carry an NTFS alternate data stream named `Zone.Identifier`.
Windows uses it to flag the file as untrusted, which causes:

- "This file came from another computer..." warnings
- Blocked DLLs, scripts, and help (`.chm`) files
- Office documents opening in Protected View
- PowerShell refusing to run downloaded scripts

`Unblock-File` in PowerShell does the same thing, but this gives you a GUI
that works on a whole folder tree and doesn't need a terminal open.

## Usage

1. Double-click `Unblocker.exe`.
2. Paste a path, drag-and-drop a file/folder onto the path box, or click
   **Browse File...** / **Browse Folder...**.
3. Click **Unblock**.

When given a folder, every file inside is unblocked recursively. The log at
the bottom reports how many files were unblocked, how many were already
clean, and any errors.

## How it works

Each blocked file has an NTFS alternate data stream accessible at
`C:\path\to\file.ext:Zone.Identifier`. Removing the block is a single Win32
`DeleteFile` call against that stream path. The app is a small C# WinForms
program that P/Invokes `kernel32!DeleteFileW`.

## Requirements

- Windows 7 or later
- .NET Framework 4.x (preinstalled on every supported version of Windows)
- An NTFS volume (Zone.Identifier streams don't exist on FAT32 / exFAT)

## Building from source

The project ships with three source files:

| File             | Purpose                                                   |
|------------------|-----------------------------------------------------------|
| `Unblocker.cs`   | The app itself (WinForms, ~150 lines of C#)               |
| `make-icon.ps1`  | Generates `Unblocker.ico` (multi-resolution PNG icon)     |
| `README.md`      | This file                                                 |

To rebuild from a clean checkout:

```powershell
# 1. Regenerate the icon
./make-icon.ps1

# 2. Compile with the .NET Framework C# compiler that ships with Windows
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
& $csc /nologo /target:winexe /optimize+ `
       /win32icon:Unblocker.ico `
       /out:Unblocker.exe `
       /reference:System.Windows.Forms.dll `
       /reference:System.Drawing.dll `
       Unblocker.cs
```

No Visual Studio, MSBuild, or NuGet required — the `csc.exe` that ships with
.NET Framework 4 is enough.

## Security note

This tool removes a security signal Windows uses to decide whether to trust
a file. Only unblock content you actually trust the source of.

## License

MIT
