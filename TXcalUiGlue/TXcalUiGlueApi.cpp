#include "pch.h"

#define TXCALUIGLUE_EXPORTS
#include "TXcalUiGlueApi.h"
#include "TXcalControllerBridge.h"
#include "TXcalUiHost.h"
#include <msclr/gcroot.h>

using namespace msclr;
using namespace TXcalUiGlue;

using namespace System;
using namespace System::Reflection;
using namespace System::IO;

// ---------------------------------------------------------------------------------
// AssemblyResolve: lets the CLR find TXcalUi.dll (and any other sibling managed
// assembly) relative to THIS DLL's own folder, instead of the default probing path
// (which anchors to the host process's own directory -- SDRUno.exe's install folder,
// not wherever TXcalUiGlue.dll actually lives). Neither this function nor
// TXcalUiGlue_Init() below reference anything from TXcalUi.dll, so both JIT
// and run using only mscorlib -- that's what makes it safe to call Init() before
// Create() ever needs TXcalUi.dll resolved. See TXcalUiGlueApi.h for why this
// two-step split (Init, then Create) matters.
// ---------------------------------------------------------------------------------
static Assembly^ OnAssemblyResolve(Object^ sender, ResolveEventArgs^ args)
{
	String^ shortName = args->Name->Split(',')[0];
	String^ selfDir = Path::GetDirectoryName(Assembly::GetExecutingAssembly()->Location);
	String^ candidate = Path::Combine(selfDir, shortName + ".dll");

	if (File::Exists(candidate))
		return Assembly::LoadFrom(candidate);

	return nullptr; // let the CLR report its normal error for anything else
}

static bool s_resolverRegistered = false;

static void EnsureAssemblyResolveRegistered()
{
	if (!s_resolverRegistered)
	{
		AppDomain::CurrentDomain->AssemblyResolve += gcnew ResolveEventHandler(&OnAssemblyResolve);
		s_resolverRegistered = true;
	}
}

// Plain native struct holding gcroot handles -- this is what lets a 100% native caller
// (the main plugin project, no /clr) hold a "handle" to managed objects via void*.
struct TXcalUiGlueHandle
{
	gcroot<TXcalControllerBridge^> bridge;
	gcroot<TXcalUiHost^> host;
};

extern "C"
{
	// Deliberately references NOTHING from TXcalUi.dll -- only AppDomain/Assembly
	// (mscorlib). That's what lets this JIT and run successfully BEFORE
	// TXcalUi.dll can be resolved, so the resolver is live in time for
	// TXcalUiGlue_Create's own JIT compilation (which does need TXcalUi.dll,
	// because TXcalControllerBridge implements ITXcalController from there).
	void __cdecl TXcalUiGlue_Init()
	{
		EnsureAssemblyResolveRegistered();
	}

	void* __cdecl TXcalUiGlue_Create(IUnoPluginController* controller)
	{
		EnsureAssemblyResolveRegistered(); // harmless no-op if Init() already ran
		TXcalUiGlueHandle* h = new TXcalUiGlueHandle();
		h->bridge = gcnew TXcalControllerBridge(controller);
		h->host = gcnew TXcalUiHost(h->bridge);
		return h;
	}

	void __cdecl TXcalUiGlue_Destroy(void* handle)
	{
		if (!handle) return;
		TXcalUiGlueHandle* h = static_cast<TXcalUiGlueHandle*>(handle);
		h->host->Shutdown();
		delete h;
	}

	void __cdecl TXcalUiGlue_Show(void* handle)
	{
		if (!handle) return;
		static_cast<TXcalUiGlueHandle*>(handle)->host->Show();
	}

	void __cdecl TXcalUiGlue_Close(void* handle)
	{
		if (!handle) return;
		static_cast<TXcalUiGlueHandle*>(handle)->host->Close();
	}

	void __cdecl TXcalUiGlue_NotifyEvent(void* handle, int eventType, unsigned short channel)
	{
		if (!handle) return;
		static_cast<TXcalUiGlueHandle*>(handle)->host->NotifyUnoEvent(eventType, channel);
	}
}
