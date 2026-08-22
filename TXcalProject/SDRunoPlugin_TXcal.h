#pragma once

// TODO: rename "TXcal" throughout this skeleton (files, class names, namespaces) to
// your plugin's real name -- a project-wide find & replace of "TXcal" is enough,
// the shape stays the same.

#include <atomic>
#include <memory>
#include <thread>

#include <iunoplugin.h>
#include <iunoplugincontroller.h>

class SDRunoPlugin_TXcalUi;

// Minimal plugin skeleton:
//  - WinForms UI via the C++/CLI bridge in ../UiGlue and ../Ui (same pattern as the
//    TX Link WinForms bridge kit, just renamed).
//  - No annotator, no TX-Link-specific servers.
//  - StartWorker/StopWorker/WorkerLoop are stubbed in as the extension point for the
//    serial / named-pipe / UDP link you mentioned adding later -- same start-in-
//    constructor, stop-in-destructor, atomic-exit-flag pattern TX Link uses for its
//    own worker threads, just with only one thread instead of five.
class SDRunoPlugin_TXcal : public IUnoPlugin
{
public:
	SDRunoPlugin_TXcal(IUnoPluginController& controller);
	virtual ~SDRunoPlugin_TXcal();

	virtual const char* GetPluginName() const override { return "TXcal"; }

	virtual void HandleEvent(const UnoEvent& ev) override;

private:
	void StartWorker();   // placeholder -- wire up serial/pipe/UDP here when ready
	void StopWorker();
	void WorkerLoop();    // runs on m_workerThread until m_workerExitRequest is set

	std::unique_ptr<SDRunoPlugin_TXcalUi> m_Ui;

	std::unique_ptr<std::thread> m_workerThread;
	std::atomic<bool> m_workerExitRequest{ false };
};
