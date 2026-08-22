# Retargeting TXcalUiGlue to /clr:netcore (net10.0-windows)

Since TXcalUi is on net10.0-windows (not .NET Framework 4.8), TXcalUiGlue needs
to target modern .NET too via C++/CLI's `/clr:netcore` mode -- classic `/clr` can only
reference .NET Framework assemblies. This is fine for local-PC-only use; the tradeoff
(vs. net48) is that anyone else running this plugin needs the matching .NET Desktop
Runtime installed, since it doesn't ship with Windows the way .NET Framework does.

There's no "C++/CLI netcore Class Library" item in the New Project dialog -- create the
project as a normal CLR Class Library, then hand-edit its `.vcxproj`:

1. Replace `<CLRSupport>true</CLRSupport>` with `<CLRSupport>NetCore</CLRSupport>`
   (this is what actually selects `/clr:netcore` over `/clr`).
2. Replace `<TargetFrameworkVersion>...</TargetFrameworkVersion>` with
   `<TargetFramework>net10.0-windows</TargetFramework>` -- use `-windows`, matching
   TXcalUi, since WinForms needs the Windows-specific TFM.
3. Add a Windows Desktop framework reference so `System::Windows::Forms` types resolve
   in `TXcalUiHost.cpp`:
   ```xml
   <ItemGroup>
     <FrameworkReference Include="Microsoft.WindowsDesktop.App.WindowsForms" />
   </ItemGroup>
   ```
4. Keep the existing Project Reference to TXcalUi -- that part doesn't change.
5. Everything in `TXcalControllerBridge.*`, `TXcalUiHost.*`, and
   `TXcalUiGlueApi.*` from this kit is unchanged -- the C++/CLI *source* is the same
   either way, only the project-file plumbing above differs.

## Deployment detail specific to netcore mode

A `/clr:netcore` build produces `TXcalUiGlue.dll` plus a `TXcalUiGlue.runtimeconfig.json`
alongside it -- that json is how the DLL locates and bootstraps the CoreCLR runtime the
first time native code calls into it. It has to sit in the same folder as
`TXcalUiGlue.dll` at plugin load time, same as `TXcalUi.dll` already does. Check
your output folder after building to confirm it's actually there.

## Worth doing before wiring this into the full plugin

Classic `/clr` mixed-mode DLLs have been loadable via plain `LoadLibrary` from any
native process since .NET Framework 1.0 -- Windows has baked-in support for it. The
`/clr:netcore` equivalent is newer and more version/runtime-sensitive, and since SDRUno
is a plain native EXE with no idea it's hosting a .NET runtime, it's worth confirming
this actually works stand-alone before debugging it inside the real plugin:

Write a 10-line throwaway native `.exe` (or even just a debugger-launched test) that
does nothing but `LoadLibrary(L"TXcalUiGlue.dll")`, `GetProcAddress` for
`TXcalUiGlue_Create`, and calls it with a dummy/null-checked argument -- if a
WinForms window pops up from that tiny native test harness, the netcore mixed-mode
loading works and any further issues are in the real plugin's wiring, not the
CoreCLR-hosting layer itself.
