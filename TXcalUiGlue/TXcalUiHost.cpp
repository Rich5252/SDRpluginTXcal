#include "pch.h"

#include "TXcalUiHost.h"

using namespace System;
using namespace System::Threading;

#using "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.dll"
#using "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Windows.Forms.dll"
#using "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Drawing.dll"

using namespace System;
using namespace System::Threading;
using namespace System::Windows::Forms;

namespace TXcalUiGlue
{
	TXcalUiHost::TXcalUiHost(TXcalControllerBridge^ bridge)
		: m_bridge(bridge)
	{
		m_formReady = gcnew ManualResetEvent(false);

		m_uiThread = gcnew Thread(gcnew ThreadStart(this, &TXcalUiHost::ThreadMain));
		m_uiThread->SetApartmentState(ApartmentState::STA);
		m_uiThread->IsBackground = true;
		m_uiThread->Name = "TXcalUi";
		m_uiThread->Start();

		// Wait for MainForm to exist before returning, so an immediate Show()/
		// NotifyUnoEvent() call from the constructor's caller can't race construction.
		m_formReady->WaitOne();
	}

	void TXcalUiHost::ThreadMain()
	{
		Application::EnableVisualStyles();
		Application::SetCompatibleTextRenderingDefault(false);

		m_form = gcnew TXcalUi::MainForm(m_bridge);
		// Force handle creation now so InvokeRequired/Invoke work correctly for any
		// caller that races the initial Show().
		auto forceHandle = m_form->Handle;

		m_formReady->Set();

		Application::Run(m_form);
	}

	void TXcalUiHost::Show()
	{
		if (m_form == nullptr || m_form->IsDisposed) return;

		if (m_form->InvokeRequired)
			m_form->BeginInvoke(gcnew MethodInvoker(this, &TXcalUiHost::DoShowForm));
		else
			m_form->Show();
	}

	void TXcalUiHost::Close()
	{
		if (m_form == nullptr || m_form->IsDisposed) return;

		if (m_form->InvokeRequired)
			m_form->BeginInvoke(gcnew MethodInvoker(this, &TXcalUiHost::DoCloseForm));
		else
			m_form->Close();
	}

	void TXcalUiHost::DoShowForm()
	{
		if (m_form != nullptr && !m_form->IsDisposed)
			m_form->Show();
	}

	void TXcalUiHost::DoCloseForm()
	{
		if (m_form != nullptr && !m_form->IsDisposed)
			m_form->Close();
	}

	void TXcalUiHost::Shutdown()
	{
		if (m_form != nullptr && !m_form->IsDisposed)
		{
			m_form->BeginInvoke(gcnew MethodInvoker(m_form, &Form::Close));
		}

		if (m_uiThread != nullptr && m_uiThread->IsAlive)
		{
			m_uiThread->Join(2000);
		}
	}

	void TXcalUiHost::NotifyUnoEvent(int eventType, unsigned short channel)
	{
		if (m_form == nullptr || m_form->IsDisposed) return;

		array<Object^>^ args = gcnew array<Object^>{ eventType, static_cast<int>(channel) };

		// Always defer via BeginInvoke, even if we're already on the UI thread --
		// SDRUno can fire this event synchronously/reentrantly from inside
		// SetVfoFrequency (same thread, same call stack, not yet returned). Calling
		// straight through here would call back into GetVfoFrequency on the same
		// controller before SetVfoFrequency has finished, which is exactly the
		// reentrancy issue TX Link hit and fixed with a lock. Deferring via
		// BeginInvoke instead ensures this only runs after the current call chain
		// has fully unwound back to the message loop.
		m_form->BeginInvoke(gcnew Action<int, int>(m_form, &TXcalUi::MainForm::OnUnoEvent), args);
	}
}
