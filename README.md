# Coding Time Tracker for Steam 🕒🎮

<p align="center">
<img src="/Resources/codePreview.png" alt="Logotype" width="10%">
</div>

**Please choose your preferred language (Пожалуйста, выберите предпочитаемый язык):**

- [Documentation in English](README.md)
- [Документация на Русском языке](README.ru.md)

**Coding Time Tracker for Steam** is a program that tracks the time spent programming in popular IDEs like *Visual Studio Code*, *JetBrains*, and others. It automatically records the time and displays it as activity for the game Kode Studio on your **Steam profile**. The program launches Kode Studio when you start your IDE and closes it when you finish, hiding the game window to avoid distractions. With this program, you can easily track the time spent programming and share it with your friends.

<img src="/Resources/Preview.gif" alt="Logotype" width="50%">

## ✨ Features

- **Automatic time tracking**: The program records your time spent working in various IDEs and automatically transfers it to **Kode Studio**.
- **Instant launch of Kode Studio**: When you launch any supported IDE, the program automatically starts **Kode Studio** via Steam, without the need for manual intervention.
- **Automatic closing of Kode Studio**: Once you close the IDE or code editor, **Kode Studio** automatically shuts down, and the program returns to standby mode.
- **Hiding the game window**: The **Kode Studio** game window is automatically hidden while you are working in the code editor, so it does not distract you from programming.
- **Easy access to data**: All the information about the time spent programming is displayed directly on your Steam profile.
- **Multilingual support**: The program supports multiple languages, including Russian, English, Spanish, German, and others.
- **Tray management**: Quick access to the project page on GitHub, the developer's page, or the ability to immediately exit the program through the system tray context menu.
- **Customizable autostart**: During installation, you can opt to enable autostart so you won’t forget to launch it when you start programming.

## ⚙️ How It Works

1. Upon launch, the program checks if **Kode Studio** is installed. If the game is installed, the program closes **Kode Studio** and goes into standby mode.
2. As soon as the user opens any supported IDE or code editor, the program automatically launches **Kode Studio** in the background.
3. The time spent programming is displayed on your Steam profile.
4. If the user closes the IDE or code editor, **Kode Studio** automatically shuts down, and the program returns to standby mode.

## 🛠️ Technical Details

- **Written in**: C#
- **Requirements**: .NET is **not required** as it is embedded within the application.
- **Autostart support**: The program starts automatically when the system boots.

## 🚀 Installation and Launch

1. Ensure that [**Kode Studio**](https://store.steampowered.com/app/779260/) is installed via Steam.
2. Download and launch **Coding Time Tracker for Steam**. [[Download link]](https://github.com/Chelovedus/Coding-Time-Tracker-For-Steam/releases/download/Release/CodingTimeTrackerForSteam_Installer.exe)
3. Open your IDE and start coding — the program will take care of the rest!

## 🧑‍💻 Building the Project Yourself

1. Download the project files from the [GitHub repository]([https://github.com/](https://github.com/Chelovedus/Coding-Time-Tracker-For-Steam/archive/refs/tags/Release.zip)).
2. Open your IDE (e.g., **Visual Studio** or **Rider**) and select the downloaded project folder.
3. Open the terminal or integrated console in your IDE.
4. Run the following command to build the project:

    ```bash
    dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true
    ```

5. After a successful build, the executable file will be located in the `bin/Release/netX.X/win-x64/publish/` folder.

## 📬 Contact the Developer

- **[Developer's GitHub page](https://github.com/Chelovedus)**
- **[Developer's Steam page](https://steamcommunity.com/id/superfrost/)**

---

Don't miss the chance to use **Coding Time Tracker for Steam** for easy and efficient time tracking while coding! 🔥
