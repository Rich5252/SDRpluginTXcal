#include "pch.h"

#include "SDRunoPlugin_TXcal.h"
#include "SDRunoPlugin_TXcalUi.h"

#include <unoevent.h>
#include <chrono>

SDRunoPlugin_TXcal::SDRunoPlugin_TXcal(IUnoPluginController& controller)
	: IUnoPlugin(controller)
{
	// The Ui constructor spins up the WinForms host thread and blocks briefly until
	// the form exists (see UiGlue/TXcalUiHost.cpp) -- keep this constructor itself
	// lightweight otherwise, same rule TX Link's m_Ui construction follows.
	m_Ui = std::make_unique<SDRunoPlugin_TXcalUi>(controller);
	m_Ui->Show();

	// Uncomment once WorkerLoop() below has real serial/pipe/UDP logic in it:
	// StartWorker();
}

SDRunoPlugin_TXcal::~SDRunoPlugin_TXcal()
{
	StopWorker();

	if (m_Ui)
	{
		m_Ui->Close();
		m_Ui.reset();
	}
}

void SDRunoPlugin_TXcal::HandleEvent(const UnoEvent& ev)
{
	if (m_Ui)
	{
		m_Ui->NotifyUnoEvent(static_cast<int>(ev.GetType()), ev.GetChannel());
	}

	if (ev.GetType() == UnoEvent::ClosingDown)
	{
		// SDRUno is tearing the plugin down; DestroyPlugin -> ~SDRunoPlugin_TXcal
		// follows shortly and StopWorker()/m_Ui->Close() run there. Nothing else
		// needed here unless you want to react early.
	}
}

void SDRunoPlugin_TXcal::StartWorker()
{
	if (m_workerThread) return;

	m_workerExitRequest = false;
	m_workerThread = std::make_unique<std::thread>(&SDRunoPlugin_TXcal::WorkerLoop, this);
}

void SDRunoPlugin_TXcal::StopWorker()
{
	if (!m_workerThread) return;

	m_workerExitRequest = true;
	if (m_workerThread->joinable())
	{
		m_workerThread->join();
	}
	m_workerThread.reset();
}

void SDRunoPlugin_TXcal::WorkerLoop()
{
	// TODO: open your serial port / named pipe / UDP socket here, then loop until
	// m_workerExitRequest is set, e.g.:
	//
	// while (!m_workerExitRequest)
	// {
	//     // read/write your link here, then push anything the UI should show back
	//     // through m_Ui (add a method alongside NotifyUnoEvent the same way, or
	//     // extend ITXcalController/TXcalControllerBridge if the UI needs to
	//     // send data the other way).
	//     std::this_thread::sleep_for(std::chrono::milliseconds(50));
	// }
}
