# D&D Companion (DDCompanion)

A cross-platform app for creating and managing Dungeons & Dragons characters: character sheet, stats, skills, inventory and combat parameters — everything is stored locally on the device in a custom `.ddc` format.

Built with **.NET MAUI Blazor Hybrid** — the UI is written in Blazor (Razor components) and rendered inside a native MAUI window via `BlazorWebView`, so the same code and markup run on Android, Windows, iOS and macOS (Mac Catalyst).

## Features

- Character creation: name, species, class, worldview, backstory, portrait.
- Ability scores (STR/DEX/CON/INT/WIS/CHA) with automatically calculated modifiers.
- Combat parameters: health, armor class, protection, weapon and armor.
- Skill list and inventory with descriptions and quantities.
- Import/export a character as a `.ddc` file (standard system share sheet / file picker).
- All characters are stored locally, in the app's private folder — no accounts, no cloud.
- Settings page describing the `.ddc` format, the storage path and the author's contacts.

## Stack and dependencies

| Component | Purpose |
|---|---|
| **.NET 10**, `net10.0-android` / `-ios` / `-maccatalyst` / `-windows` | target platforms (single project, multi-targeted) |
| **.NET MAUI** (`Microsoft.Maui.Controls`) | native app shell, access to platform APIs (file system, sharing, file picker, browser) |
| **Microsoft.AspNetCore.Components.WebView.Maui** | hosts Blazor components inside MAUI (`BlazorWebView`) |
| **Microsoft.Extensions.Logging.Debug** | logging in Debug builds |
| `System.Text.Json` (built into .NET) | (de)serializing characters to/from `.ddc` files |
| Bootstrap (in `wwwroot/lib`) | base CSS reset; the custom dark theme lives in `wwwroot/css/ddcompanion-components.css` |
| Custom fonts `Prophecy`, `Determination` (`wwwroot/fonts`) | decorative display fonts |

No third-party NuGet packages are used for business logic — everything is hand-written on top of MAUI/Blazor.

## How it works

### Architecture

```
DDCompanion/
├── App.xaml(.cs)              # MAUI application entry point
├── MainPage.xaml(.cs)         # host page containing the BlazorWebView
├── MauiProgram.cs             # DI container: service registration
├── Components/
│   ├── Routes.razor           # Blazor router
│   ├── Layout/
│   │   ├── MainLayout.razor       # shared shell: header + content
│   │   └── NavMenu.razor          # header (back/settings/create/save, etc.)
│   └── Pages/                 # application pages (see below)
├── Models/                    # character and .ddc file models
├── Services/
│   ├── CharacterStorageService.cs   # reads/writes/imports/deletes .ddc files
│   ├── ImageCompressionService.cs   # compresses the character photo into base64 JPEG
│   ├── PendingFileImport.cs         # signals external file opens to the Blazor UI
│   └── ExternalImportService.cs     # imports external .ddc/.ddcs files and routes them
├── Platforms/                 # platform-specific code (Android/iOS/Windows/MacCatalyst)
└── wwwroot/                   # static assets: CSS, fonts, JS
```

### Navigation (pages and routes)

| Route | Page | Purpose |
|---|---|---|
| `/` | `Home.razor` | list of saved characters |
| `/characters/create` | `CharacterCreate.razor` | create a new character |
| `/characters/{FileName}` | `CharacterView.razor` | view a character sheet |
| `/characters/edit/{FileName}` | `CharacterEdit.razor` | edit a character |
| `/characters/import` | `ImportCharacter.razor` | import a `.ddc` file from the device |
| `/settings` | `Settings.razor` | settings: file storage, `.ddc` format, contacts |
| `/not-found` | `NotFound.razor` | 404 |

`NavMenu.razor` is the shared header used on every page: it detects the current route and swaps in the right buttons (back, settings gear, "+" create/import, character menu, save).

### Data storage

Characters are stored as plain files on disk — there is no database.

- `CharacterStorageService` (registered as a Singleton in `MauiProgram.cs`) creates a `Characters` folder on startup. On **Windows** it is `Documents\DDCompanion\Characters` (visible in Explorer; characters from the old hidden AppData location are moved there automatically). On other platforms it is `Characters` inside `FileSystem.AppDataDirectory` (the app's private sandbox).
- Each character is a separate `<name>.ddc` file in that folder.
- The service is responsible for:
  - `LoadAllAsync()` / `LoadAsync(path)` — loading all characters / a single one;
  - `SaveAsync(character)` — saving a new character (throws if the name is already taken);
  - `SaveToPathAsync(character, path)` — overwriting an existing file when editing;
  - `ImportAsync(stream)` — importing a third-party `.ddc` file;
  - `DeleteAsync(fileName)` — deleting a character;
  - automatic migration of old files (`Version < 2`) — recalculating ability modifiers on first load.
- Opening a `.ddc` character or `.ddcs` settings file from another app on Android is received by platform code, detected from the JSON `format`, imported, and then routed to the character page or settings page. `MainLayout` handles both cold and warm launches.

### The `.ddc` file format

`.ddc` is a plain JSON file (see `DdcCharacterFile.cs`):

```json
{
  "Format": "DDC",
  "Version": 2,
  "Character": { /* ...the whole character... */ }
}
```

- **Format** (`string`) — a format marker, always `"DDC"`; the app uses it to tell its own files apart from other JSON files.
- **Version** (`number`) — the schema version, used to migrate older characters on load.
- **Character** (`object`) — the character itself: name, species, class, worldview, backstory, portrait (`ImageBase64`), ability scores (`CharacterStats`), combat parameters (`CharacterCombat`), skill list (`CharacterSkill[]`) and inventory (`CharacterItem[]`).

The format is open and human-readable: the file can be opened in any text editor, edited by hand, committed to git, or sent to a friend — the app will simply read it back on any device.

### Character models (`Models/`)

- `Character` — the root model (name, species, class, worldview, backstory, portrait, stats, combat, skills, inventory).
- `CharacterStats` — the six D&D ability scores plus their modifiers; `RecalculateModifiers()` computes each modifier as `(value - 10) / 2`.
- `CharacterCombat` — health (current/max), armor class, protection, weapon, armor.
- `CharacterSkill`, `CharacterItem`, `CharacterAbility` — a skill and an ability (name + description), and an inventory item (name + description + quantity).

### UI / styling

- There's no CSS-variable-based theming — colors are hardcoded (`#101010`/`#171717` backgrounds, `#292929` borders, `#c62828`/`#ef5350` accent, white text).
- Styles for each page live in two places:
  - scoped CSS next to the page (`Page.razor.css`, Blazor's standard CSS isolation mechanism);
  - critical styles duplicated in `wwwroot/css/ddcompanion-components.css` — a global fallback file so the UI doesn't fall apart if the scoped stylesheet isn't picked up inside `BlazorWebView`.
- Icons are inline SVGs (`viewBox 0 0 640 640`, `currentColor` fill), no icon libraries.

## Build and run

Requires a .NET SDK with the MAUI workload installed:

```bash
dotnet workload install maui
dotnet build -f net10.0-android      # Android
dotnet build -f net10.0-windows10.0.19041.0   # Windows
```

iOS/Mac Catalyst can only be built on macOS (a MAUI/Xcode limitation).
