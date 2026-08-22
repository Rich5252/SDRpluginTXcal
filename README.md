# New SDRUno plugin skeleton (WinForms UI, serial/pipe/UDP-ready)

A minimal, from-scratch SDRUno plugin: WinForms UI via the same C++/CLI bridge pattern
as the TX Link kit, no TX-Link-specific business logic, and a single placeholder
background-worker thread ready for the serial / named-pipe / UDP link you mentioned
adding later.

**Rename first.** Every file uses the placeholder name "TXcal" -- do a project-wide
find & replace of `TXcal` for your real plugin name before you start adding logic
(class names, namespaces, and filenames all follow that one token).

## Architecture (same shape as the TX Link bridge)

```
SDRunoPlugin_TXcal.dll   (native -- the DLL SDRUno actually loads)
        |  links against TXcalUiGlue.lib, calls 5 plain extern "C" functions
        v
TXcalUiGlue.dll           (C++/CLI, /clr) -- owns the STA UI thread, wraps
        |  references TXcalUi.dll          IUnoPluginController for the UI
        v
TXcalUi.dll                 (pure C#, WinForms) -- your actual UI/logic
```

Reference direction is one-way (`TXcalUiGlue -> TXcalUi`), so there's no circular
project reference: `TXcalUi` defines `ITXcalController` (an interface) and
`MainForm`; `TXcalUiGlue`'s `TXcalControllerBridge` implements that interface.

## What's different from the TX Link kit

- No annotator (`IUnoAnnotator`/`AnnoEntry`) -- add it back later only if this plugin
  needs to draw markers on the spectrum display.
- No Nana UI, no TX-specific UDP/RTTY/Tuner/DXspider servers.
- `SDRunoPlugin_TXcal` has exactly one placeholder background thread
  (`StartWorker`/`StopWorker`/`WorkerLoop`, guarded by a single `std::atomic<bool>`
  exit flag) instead of TX Link's five -- this is where your serial port, named pipe,
  or UDP socket code goes when you're ready. It's created but not started
  (`StartWorker()` is commented out in the constructor) until you've filled in
  `WorkerLoop()`. If you end up needing more than one link running concurrently, just
  duplicate the thread/flag pair per link -- follow TX Link's pattern in the .cpp you
  showed me (one `std::unique_ptr<std::thread>` + one `std::atomic<bool>` exit flag
  per link, all joined in the destructor).
- `dllmain.cpp` is trimmed of the `Console.h`/`MessageBox.h` debug-console dependency
  so the skeleton is self-contained; copy those two files over from TX Link (and
  restore the `RedirectIOToConsole()` call) if you want that debug output.

## Files in this kit

- `PluginProject/` -- the native plugin project.
  - `iunoplugin.h`, `unoevent.h`, `iunoplugincontroller.h` -- copied verbatim from your
    TX Link headers. **`iunoplugincontroller.h` needs five more SDK headers this kit
    doesn't include a copy of** (I haven't seen their contents): `iunostreamobserver.h`,
    `iunoaudioobserver.h`, `iunoaudioprocessor.h`, `iunostreamprocessor.h`,
    `iunompxobserver.h`, `iunoannotator.h`. Simplest: point this project's Additional
    Include Directories at the same shared SDK header folder TX Link already uses,
    rather than copying those five files -- one copy, no drift.
  - `dllmain.cpp` -- generic DLL entry point.
  - `SDRunoPlugin_TXcal.h` / `.cpp` -- the plugin class.
  - `SDRunoPlugin_TXcalProject.cpp` -- the `CreatePlugin`/`DestroyPlugin`/
    `GetPluginApiLevel` exports SDRUno calls.
  - `SDRunoPlugin_TXcalUi.h` -- native faÃ§ade over the WinForms UI (header-only).
- `UiGlue/` -- C++/CLI Class Library (`/clr`).
  - `TXcalUiGlueApi.h` / `.cpp` -- the plain `extern "C"` API the native project
    calls.
  - `TXcalControllerBridge.h` / `.cpp` -- wraps `IUnoPluginController&`, implements
    `ITXcalController`.
  - `TXcalUiHost.h` / `.cpp` -- owns the dedicated STA thread and
    `Application::Run`.
- `Ui/` -- pure C# WinForms Class Library.
  - `ITXcalController.cs`, `UnoEventType.cs`, `MainForm.cs` + `MainForm.Designer.cs`
    (same minimal demo form as the TX Link kit: frequency readout, status label, a
    "set frequency" box/button -- replace with your real UI).

## Setup in VS2026

1. **TXcalUi project**: Windows Forms Class Library, target **.NET Framework 4.8**.
   Add the four files from `Ui/`.
2. **TXcalUiGlue project**: CLR Class Library (.NET Framework), also **4.8**.
   - Project Properties > General > **Common Language Runtime Support: /clr**.
   - Preprocessor Definitions: add `TXCALUIGLUE_EXPORTS`.
   - Additional Include Directories: your SDK headers folder (see note above).
   - References > Add Reference > Projects > **TXcalUi**.
   - Add the 5 files from `UiGlue/`.
3. **SDRunoPlugin_TXcal project** (native, no `/clr`, no changed compiler settings):
   - Add all files from `PluginProject/`.
   - Additional Include Directories: the `UiGlue` folder (for `TXcalUiGlueApi.h`)
     and your shared SDK header folder.
   - Linker > Input > Additional Dependencies: `TXcalUiGlue.lib`. Linker > General >
     Additional Library Directories: TXcalUiGlue's output folder.
   - Project Dependencies: check `TXcalUiGlue`.
4. **Deployment**: SDRUno loads only the one plugin DLL by path, but
   `TXcalUiGlue.dll` and `TXcalUi.dll` must sit alongside it. Set all three
   projects' Output Directory to the same path, or add a post-build copy step.

Threading notes (UI-thread marshaling, `Show`/`Close`/`NotifyUnoEvent` being safe from
any thread) are identical to the TX Link kit's README -- nothing new here.

## When you're ready for serial/pipe/UDP

Fill in `WorkerLoop()` in `SDRunoPlugin_TXcal.cpp`, uncomment the `StartWorker()`
call in the constructor, and decide how results reach the UI: either add methods to
`ITXcalController`/`TXcalControllerBridge` (if the UI pulls data on demand), or
add a push-style notification alongside `NotifyUnoEvent` (if the worker thread needs to
shove data at the UI as it arrives -- same `BeginInvoke`-marshaled pattern
`TXcalUiHost::NotifyUnoEvent` already uses). Happy to sketch either once you know
which one the serial/pipe/UDP link needs.
