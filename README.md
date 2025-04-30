# Simple Build Helper
One-click **Unity Editor** window for automated builds, zipping & build history

![Unity](https://img.shields.io/badge/Unity-2020.3%2B-black?logo=unity)
![Editor-only](https://img.shields.io/badge/Editor--only-Yes-blue)
![License](https://img.shields.io/badge/License-MIT-green)

---

## ✨ Features
| ✔ | What it does |
|---|--------------|
| **Build Now** | Builds the current *Build Settings* scenes for the active platform with one click. |
| **Smart name** | Auto-name: `Build_<Project>_<dd.MM.yyyy>_<N>` (counter increases automatically). |
| **Dedicated folder** | Every build is stored in its own container folder:<br>`Builds/Windows64/Build_MyGame_30.04.2025_6/…` |
| **ZIP archive** | Optional “Create ZIP archive” adds `<BuildName>.zip` next to the build folder. |
| **Logs** | Optional:<br>• `Build.txt` — BuildReport summary<br>• `UnityLog.txt` — last 800 lines of *Editor.log* |
| **History tab** | Stores every build (date, size, time) with an **Open** button to reveal the folder. |
| **No runtime footprint** | Code lives in `Assets/SimpleBuildHelper/Editor` + `.asmdef` → not included in player builds. |

---

## 🛠️ Installation
1. Copy **`Assets/SimpleBuildHelper`** into your project.  
   No external packages required.
2. Unity compiles it into `SimpleBuildHelper.dll` (**Editor-only**).

---
## Quick Start
1. **Install Simple Build Helper**  
   Window → Package Manager → **Add package from Git URL…**
  ```jsonc
  https://github.com/Je1rei/SimpleBuilderHelper.git
  ```
2. Open the window:  
   **`Tools → Build → Simple Helper`**
3. (Optional) open **Advanced Options** to tweak output folder, ZIP, logs, etc.
4. Press **Build Now**.  
   When finished the resulting folder (and ZIP) will open in the OS file explorer.
---

## ⚙️ Options

| Setting | Description |
|---------|-------------|
| **Use Custom Output Folder** | Builds are stored outside the default `Builds/` root. |
| **Create ZIP archive** | Compresses `<BuildName>` folder into `<BuildName>.zip`. |
| **Generate Logs** | Expands to:<br>• *Unity Log* — tail of Editor.log<br>• *Build Log* — BuildReport summary |
| **Heavy files log** | <span title="In Next Update">Disabled (coming soon)</span> |
| **History → Advanced** | Export / Clear build history. **Delete** button is disabled (next update). |

---
## 🧩 Integration details

| Topic | Explanation |
|-------|-------------|
| **Assembly Definition** | `SimpleBuildHelper.asmdef` (Editor platform only) keeps the code in its own DLL. |
| **No runtime code** | Entire source is inside an *Editor* folder; never enters a player build. |
| **Dependencies** | Only `UnityEditor` and standard `System.IO.Compression`. |
| **Zero init-overhead** | No `[InitializeOnLoad]` / `[DidReloadScripts]`; window logic runs only when opened. |

---

## 🛣️ Roadmap
* Enable **Delete** button in History
* Heavy-asset log based on `BuildReport.GetFiles()`
* Progress bar & coloured live status while building

---

## 🤝 Contributing
Bug reports and pull requests are welcome!  
For major changes, please open an issue first to discuss what you would like to change.

---
