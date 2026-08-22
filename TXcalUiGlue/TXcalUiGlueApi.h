// TXcalUiGlueApi.h
//
// Plain extern "C" API surface for TXcalUiGlue.dll -- this is what the native
// SDRunoPlugin_TXcal project calls (via SDRunoPlugin_TXcalUi.h's dynamic
// LoadLibrary/GetProcAddress, not a compile-time link) to drive the WinForms UI
// without itself knowing anything about C++/CLI or managed code.
//
// TXcalUiGlue_Init() MUST be called once, right after LoadLibrary succeeds and
// BEFORE TXcalUiGlue_Create() is ever called. It registers the AppDomain's
// AssemblyResolve handler so TXcalUi.dll (and any other sibling managed
// assembly) can be found relative to this DLL's own folder, regardless of where
// the host process (SDRUno.exe) itself is installed. Its own body deliberately
// touches nothing from TXcalUi.dll, so it JITs and runs using only mscorlib --
// unlike TXcalUiGlue_Create, whose JIT compilation needs TXcalUi.dll resolved
// before it can even start executing (because TXcalControllerBridge implements
// ITXcalController, defined over there). Registering the resolver from inside
// Create() itself is too late for exactly that reason -- see TXcalUiGlueApi.cpp.

#pragma once

#include "iunoplugincontroller.h"

#ifdef TXCALUIGLUE_EXPORTS
#define TXCALUIGLUE_API __declspec(dllexport)
#else
#define TXCALUIGLUE_API __declspec(dllimport)
#endif

extern "C"
{
	// Call first, before any other function below. Idempotent -- safe to call
	// more than once (only registers the resolver on the first call).
	TXCALUIGLUE_API void __cdecl TXcalUiGlue_Init();

	// Creates the bridge + UI host and spins up the WinForms UI thread. Returns an
	// opaque handle (really a TXcalUiGlueHandle*) to pass to every other call
	// below, or nullptr on failure.
	TXCALUIGLUE_API void* __cdecl TXcalUiGlue_Create(IUnoPluginController* controller);

	// Shuts down the UI thread and frees the handle. Do not use the handle again
	// after this call.
	TXCALUIGLUE_API void __cdecl TXcalUiGlue_Destroy(void* handle);

	TXCALUIGLUE_API void __cdecl TXcalUiGlue_Show(void* handle);
	TXCALUIGLUE_API void __cdecl TXcalUiGlue_Close(void* handle);

	TXCALUIGLUE_API void __cdecl TXcalUiGlue_NotifyEvent(void* handle, int eventType, unsigned short channel);
}
