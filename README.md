# Coding Time Tracker for Steam 🕒🎮

<p align="center">
<img src="/Resources/codePreview.png" alt="Logotype" width="10%">
</p>

**Please choose your preferred language (Пожалуйста, выберите предпочитаемый язык):**

- [Прочитать весь этот текст на РУССКОМ языке](README.ru.md)
- [English text](README.md)

---

## 📌 About

**Coding Time Tracker for Steam** is a program that tracks the time spent programming in popular IDEs like **Visual Studio Code**, **JetBrains IDEs**, and others.

The program automatically records your coding time and displays it as activity for the game **Kode Studio** on your **Steam profile**.

When you start coding:

- the program launches **Kode Studio** via Steam
- Steam counts the playtime
- the game window stays hidden
- when coding stops — the game automatically closes

This allows you to **track programming time and share it with friends through Steam**.

<img src="/Resources/Preview.gif" alt="Preview" width="100%">

---

# ✨ Features

- **Automatic time tracking**  
  Tracks your programming time automatically.

- **Steam integration**  
  Displays coding time as playtime for **Kode Studio**.

- **Automatic launch**  
  Starts **Kode Studio** when a supported editor is detected.

- **Automatic shutdown**  
  Closes **Kode Studio** when the editor is closed.

- **Hidden game window**  
  Prevents the game window from distracting you.

- **IDE detection**  
  Supports many popular editors and IDEs.

- **Multilingual support**  
  Supports multiple languages including:
  - English
  - Russian
  - Spanish
  - German

- **Tray management (Windows)**  
  Quick access to:
  - GitHub page
  - Developer page
  - Exit option

- **Autostart support**

---

# ⚙️ How It Works

1. The program starts in the background.
2. It checks whether **Kode Studio** is installed.
3. When a supported code editor is launched:
   - the program automatically launches **Kode Studio** via Steam.
4. Steam counts the playtime.
5. When the editor is closed:
   - **Kode Studio** automatically closes.
6. The program returns to standby mode.

---

# 🚀 Installation and Launch (Windows)

1. Install **Kode Studio** via Steam:

https://store.steampowered.com/app/779260/

2. Download the latest release:

https://github.com/Chelovedus/Coding-Time-Tracker-For-Steam/releases

3. Run the installer:

CodingTimeTrackerForSteam_Installer.exe

4. Start coding — the tracker will handle the rest.

---

# 🐧 Linux Installation

The Linux version installs itself into the user environment and runs as a **systemd user service**.

## Requirements

- Steam for Linux
- **Kode Studio installed in Steam**
- Desktop environment with `notify-send`
- Linux distribution with **systemd**

Supported Steam installations:

~/.local/share/Steam  
~/.steam/steam  
~/.var/app/com.valvesoftware.Steam/.local/share/Steam  
~/snap/steam/current/.local/share/Steam  
~/Steam  

---

## First Launch

Build or download the Linux binary and run it once:

./CodingTimeTrackerForSteam

During the first launch the program automatically:

1. Installs itself to:

~/.local/bin/CodingTimeTrackerForSteam

2. Creates a **systemd user service**:

~/.config/systemd/user/codingtimetracker.service

3. Enables and starts the service.

After this step the program runs **automatically in the background**.

---

# 🔧 systemd Service Management

Check service status:

systemctl --user status codingtimetracker

Start service:

systemctl --user start codingtimetracker

Stop service:

systemctl --user stop codingtimetracker

Restart service:

systemctl --user restart codingtimetracker

Disable autostart:

systemctl --user disable codingtimetracker

---

# 🧠 Supported Code Editors

The program detects editors by process name.

Supported editors include:

Visual Studio Code  
IntelliJ IDEA  
Rider  
PyCharm  
CLion  
WebStorm  
PHPStorm  
Eclipse  
NetBeans  
Code::Blocks  
QtCreator  
KDevelop  
MonoDevelop  
Arduino IDE  
Sublime Text  
Atom  
Brackets  
Geany  
Kate  
Gedit  
Komodo  
jEdit  
Spyder  
Thonny  
RStudio  
Vim  
Neovim  
Emacs  
Mousepad  
Pluma  
Leafpad  
Micro  

---

# 🧩 Kode Studio Stub (Linux)

On Linux the tracker replaces the original **Kode Studio binary** with a lightweight stub.

Original file:

steamapps/common/Kode Studio/Linux/kodestudio

Backup created automatically:

kodestudio.original

The stub prevents the game from opening while still allowing **Steam to count playtime**.

---

# 🛠️ Building the Project

## Requirements

Install .NET SDK.

Example (Ubuntu):

sudo apt install dotnet-sdk-8.0

---

## Build Command

dotnet publish CodingTimeTrackerForSteam.csproj -c Release -r linux-x64 --self-contained true -o publish

The compiled binary will be located in:

publish/CodingTimeTrackerForSteam

Run it once to install the service:

./publish/CodingTimeTrackerForSteam

---

# 🧑‍💻 Technical Details

Language  
C#

Framework  
.NET (self-contained build)

Process detection  
Checks running processes every **1 second**

Steam launch method  
steam://rungameid/779260

---

# 📬 Contact the Developer

GitHub  
https://github.com/Chelovedus

Steam  
https://steamcommunity.com/id/superfrost/

---

⭐ If you like the project — consider starring the repository!