// dllmain.cpp : Defines the entry point for the DLL application.
//
// Simplified from TX Link's dllmain.cpp -- dropped the dependency on its Console.h/
// MessageBox.h debug-console helpers so this skeleton is self-contained. If you want
// that std::cout-to-console redirect for debugging, copy Console.h/MessageBox.h over
// from TX Link and restore the RedirectIOToConsole() call under DLL_PROCESS_ATTACH.

#include <Windows.h>
#include "pch.h"

HMODULE _hModule;

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		_hModule = hModule;
		break;
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
		break;
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

HMODULE GetHModule()
{
	return _hModule;
}

