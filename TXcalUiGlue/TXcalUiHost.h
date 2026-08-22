#pragma once

#include "TXcalControllerBridge.h"

// TXcalUi::MainForm becomes visible with no #include needed, via the same Project
// Reference to TXcalUi noted in TXcalControllerBridge.h.

namespace TXcalUiGlue
{
	// Owns a dedicated STA thread running Application::Run(MainForm) -- SDRUno's own
	// message loop is native, not WinForms, so the plugin has to bring its own.
	// All public methods are safe to call from any thread; they marshal onto the UI
	// thread via Control::Invoke/BeginInvoke as needed.
	public ref class TXcalUiHost
	{
	public:
		explicit TXcalUiHost(TXcalControllerBridge^ bridge);

		void Show();
		void Close();

		// Blocks briefly until the UI thread has shut down. Call from the plugin's
		// destructor (via SDRunoPlugin_TXcalUi::~SDRunoPlugin_TXcalUi).
		void Shutdown();

		void NotifyUnoEvent(int eventType, unsigned short channel);

	private:
		void ThreadMain();
		void DoShowForm();
		void DoCloseForm();

		TXcalControllerBridge^ m_bridge;
		System::Threading::Thread^ m_uiThread;
		System::Threading::ManualResetEvent^ m_formReady;
		TXcalUi::MainForm^ m_form;
	};
}
