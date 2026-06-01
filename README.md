# IP Range Scanner — .NET 4.8 WinForms

A dark-themed Windows Forms application that pings a range of IP addresses and 
displays their online/offline status in real time.

## Features

| Feature | Details |
|---|---|
| IP Range Input | Enter any valid start and end IP (e.g. 192.168.1.1 – 192.168.1.254) |
| Parallel Pinging | Up to 64 concurrent pings for fast scanning |
| Live Results Grid | Color-coded ONLINE (green) / OFFLINE (red) with response time and TTL |
| Hostname Resolution | Reverse DNS lookup for each online host |
| Manual Scan | Click **▶ Scan** at any time |
| Auto-Refresh | Enable the checkbox and set an interval (5–300 seconds) |
| Stop Anytime | Click **■ Stop** to cancel an in-progress scan |
| Stats Bar | Running counts of Online / Offline / Total |

---

## Requirements

- Windows 10 / 11  
- .NET Framework 4.8 (pre-installed on Windows 10 v1903+ and Windows 11)  
- Visual Studio 2019 or later (Community edition is free)

---

## Getting Started

### 1 — Open in Visual Studio

1. Double-click **IPScanner.sln** — Visual Studio will open.
2. Press **F5** (or **Ctrl+F5**) to build and run.

### 2 — Build from Command Line (MSBuild)

```bat
msbuild IPScanner\IPScanner.csproj /p:Configuration=Release
```

The compiled executable will be at:
```
IPScanner\bin\Release\IPScanner.exe
```

---

## Usage

1. **Enter IP Range** — type the start and end IP in the top fields.
2. Click **▶ Scan** — the grid populates instantly and results fill in as pings complete.
3. (Optional) Tick **Auto-Refresh** and set an interval in seconds to keep scanning repeatedly.
4. Click **■ Stop** to cancel at any time.
5. Click **✕ Clear** to reset the results grid.

---

## Project Structure

```
IPScanner.sln          ← Visual Studio solution
IPScanner/
  ├── IPScanner.csproj ← .NET 4.8 WinForms project file
  ├── Program.cs       ← Entry point
  └── Form1.cs         ← All UI + ping logic (self-contained, no designer file)
```

---

## Notes

- ICMP (ping) may be blocked by Windows Firewall or router ACLs.  
  Run the app as Administrator if hosts that should be online appear offline.
- Hostname resolution adds a small delay per online host; it runs inline but 
  times out gracefully if DNS is unavailable.
- The grid preserves previous results between scans so you can track state changes.
