#pragma once

// C++/CLI managed class -- compile this file (and the rest of the TXcalUiGlue
// project) with Common Language Runtime Support (/clr).

#include "iunoplugincontroller.h"

// TXcalUi::ITXcalController becomes visible with no #include needed, once you
// add a Project Reference to TXcalUi from this project (Solution Explorer ->
// TXcalUiGlue -> References -> Add Reference -> Projects -> TXcalUi).

namespace TXcalUiGlue
{
	// Implements TXcalUi.ITXcalController (the C# interface) by forwarding every
	// call to the native IUnoPluginController&. Extend this + ITXcalController
	// together as your UI (or, later, WorkerLoop) needs more of the controller API --
	// every method on iunoplugincontroller.h follows the same pass-through pattern.
	public ref class TXcalControllerBridge : public TXcalUi::ITXcalController
	{
	internal:
		// 'internal:' is deliberate: a plain C# project referencing this assembly
		// can't spell IUnoPluginController*, so it can never call this constructor --
		// only native/C++/CLI code inside TXcalUiGlue can.
		explicit TXcalControllerBridge(IUnoPluginController* controller);

	public:
		virtual double GetVfoFrequency(int channel);
		virtual bool SetVfoFrequency(int channel, double frequencyHz);

		virtual double GetCenterFrequency(int channel);
		virtual bool SetCenterFrequency(int channel, double frequencyHz);

		virtual int GetFilterBandwidth(int channel);
		virtual bool SetFilterBandwidth(int channel, int bandwidthHz);

		virtual bool IsStreamingEnabled(int channel);

		virtual bool SetAudioVolume(int channel, int volume);
		virtual int GetAudioVolume(int channel);
		virtual bool SetAudioMute(int channel, bool mute);
		virtual bool GetAudioMute(int channel);

		virtual double GetSNR(int channel);
		virtual double GetPower(int channel);

		virtual System::String^ GetConfigurationKey(System::String^ key);
		virtual bool SetConfigurationKey(System::String^ key, System::String^ value);

	private:
		// A native pointer held by a managed class is fine here: SDRUno guarantees the
		// IUnoPluginController outlives the plugin instance, and this bridge's lifetime
		// is scoped to the plugin instance (owned via SDRunoPlugin_TXcalUi).
		IUnoPluginController* m_controller;
	};
}
