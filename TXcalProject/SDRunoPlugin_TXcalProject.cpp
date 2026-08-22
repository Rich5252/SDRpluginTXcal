#include "pch.h"

#include <iunoplugin.h>

#include "SDRunoPlugin_TXcal.h"

extern "C"
{
	// DLL entry points called from SDRUno

	UNOPLUGINAPI IUnoPlugin* UNOPLUGINCALL CreatePlugin(IUnoPluginController& controller)
	{
		return new SDRunoPlugin_TXcal(controller);
	}

	UNOPLUGINAPI void UNOPLUGINCALL DestroyPlugin(IUnoPlugin* pPlugin)
	{
		delete pPlugin;
	}

	UNOPLUGINAPI unsigned int UNOPLUGINCALL GetPluginApiLevel()
	{
		return UNOPLUGINAPIVERSION;
	}
}
