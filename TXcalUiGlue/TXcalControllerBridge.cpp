#include "pch.h"

#include "TXcalControllerBridge.h"
#include <msclr/marshal_cppstd.h>

#include <mutex>
#include <system_error>
#include <chrono>
#include <thread>

using namespace System;
using namespace msclr::interop;

// ---------------------------------------------------------------------------------
// Two distinct hazards are handled here, discovered in this order:
//
// 1. Cross-thread / genuine contention: SDRUno's real IUnoPluginController can throw
//    std::system_error(EBUSY) if a call arrives while it's mid-update from another
//    caller. Fixed with a mutex + retry, same pattern as TX Link's SDRUnoHelper.cpp.
//    This alone fixed external SDRUno-driven FrequencyChanged events.
//
// 2. SAME-THREAD reentrancy: clicking our own Set button still crashed, because
//    SetVfoFrequency's call into SDRUno can synchronously (likely via a nested
//    Windows message pump SDRUno runs internally while it's busy) fire a
//    FrequencyChanged event BEFORE SetVfoFrequency has returned -- on the SAME
//    thread. That reentrant call lands back in GetVfoFrequency, which then tries to
//    lock the SAME mutex the outer SetVfoFrequency call already holds, on the SAME
//    thread. Re-locking a non-recursive std::mutex from the thread that already
//    holds it is undefined behaviour (typically a hard deadlock, not something a
//    retry loop can ever resolve -- the outer call can't finish and release the
//    lock until the nested call returns, and the nested call is stuck waiting on
//    that same lock).
//
//    Fixed with a thread_local reentrancy flag: while a Set call is in flight on a
//    given thread, a Get call on THAT SAME thread skips the native call entirely
//    and returns the last known cached value instead of attempting an unsafe
//    nested call. Cross-thread calls are unaffected (different thread => flag is
//    false there => goes through the normal mutex+retry path).
// ---------------------------------------------------------------------------------

#pragma managed(push, off)

static std::mutex g_controllerMutex;
static const int kMaxRetries = 5;

// thread_local, not a plain bool: only the thread currently inside a Set call
// should see this as true. A different thread calling Get concurrently should
// still go through the normal mutex+retry path, not be short-circuited.
static thread_local bool t_insideSetCall = false;

static double s_lastKnownVfoFrequency = 0.0;
static double s_lastKnownCenterFrequency = 0.0;

static double GetVfoFrequencySafe(IUnoPluginController* controller, channel_t channel)
{
	if (t_insideSetCall)
	{
		OutputDebugStringA("TXcalControllerBridge: GetVfoFrequency reentrant during SetVfoFrequency on this thread, returning cached value\n");
		return s_lastKnownVfoFrequency;
	}

	for (int attempt = 0; attempt < kMaxRetries; ++attempt)
	{
		try
		{
			std::lock_guard<std::mutex> lock(g_controllerMutex);
			double freq = controller->GetVfoFrequency(channel);
			s_lastKnownVfoFrequency = freq;
			return freq;
		}
		catch (const std::system_error& e)
		{
			if (e.code().value() == 16 /* EBUSY */ && attempt < kMaxRetries - 1)
			{
				OutputDebugStringA("TXcalControllerBridge: GetVfoFrequency EBUSY, retrying\n");
				std::this_thread::sleep_for(std::chrono::milliseconds(5));
				continue;
			}
			throw;
		}
	}
	throw std::runtime_error("GetVfoFrequencySafe: exhausted retries");
}

static bool SetVfoFrequencySafe(IUnoPluginController* controller, channel_t channel, double frequencyHz)
{
	for (int attempt = 0; attempt < kMaxRetries; ++attempt)
	{
		try
		{
			std::lock_guard<std::mutex> lock(g_controllerMutex);

			t_insideSetCall = true;
			bool result;
			try
			{
				result = controller->SetVfoFrequency(channel, frequencyHz);
			}
			catch (...)
			{
				t_insideSetCall = false;
				throw;
			}
			t_insideSetCall = false;

			if (result)
			{
				s_lastKnownVfoFrequency = frequencyHz;
			}
			return result;
		}
		catch (const std::system_error& e)
		{
			if (e.code().value() == 16 /* EBUSY */ && attempt < kMaxRetries - 1)
			{
				OutputDebugStringA("TXcalControllerBridge: SetVfoFrequency EBUSY, retrying\n");
				std::this_thread::sleep_for(std::chrono::milliseconds(5));
				continue;
			}
			throw;
		}
	}
	throw std::runtime_error("SetVfoFrequencySafe: exhausted retries");
}

static double GetCenterFrequencySafe(IUnoPluginController* controller, channel_t channel)
{
	if (t_insideSetCall)
	{
		OutputDebugStringA("TXcalControllerBridge: GetCenterFrequency reentrant during a Set call on this thread, returning cached value\n");
		return s_lastKnownCenterFrequency;
	}

	for (int attempt = 0; attempt < kMaxRetries; ++attempt)
	{
		try
		{
			std::lock_guard<std::mutex> lock(g_controllerMutex);
			double freq = controller->GetCenterFrequency(channel);
			s_lastKnownCenterFrequency = freq;
			return freq;
		}
		catch (const std::system_error& e)
		{
			if (e.code().value() == 16 /* EBUSY */ && attempt < kMaxRetries - 1)
			{
				OutputDebugStringA("TXcalControllerBridge: GetCenterFrequency EBUSY, retrying\n");
				std::this_thread::sleep_for(std::chrono::milliseconds(5));
				continue;
			}
			throw;
		}
	}
	throw std::runtime_error("GetCenterFrequencySafe: exhausted retries");
}

static bool SetCenterFrequencySafe(IUnoPluginController* controller, channel_t channel, double frequencyHz)
{
	for (int attempt = 0; attempt < kMaxRetries; ++attempt)
	{
		try
		{
			std::lock_guard<std::mutex> lock(g_controllerMutex);

			t_insideSetCall = true;
			bool result;
			try
			{
				result = controller->SetCenterFrequency(channel, frequencyHz);
			}
			catch (...)
			{
				t_insideSetCall = false;
				throw;
			}
			t_insideSetCall = false;

			if (result)
			{
				s_lastKnownCenterFrequency = frequencyHz;
			}
			return result;
		}
		catch (const std::system_error& e)
		{
			if (e.code().value() == 16 /* EBUSY */ && attempt < kMaxRetries - 1)
			{
				OutputDebugStringA("TXcalControllerBridge: SetCenterFrequency EBUSY, retrying\n");
				std::this_thread::sleep_for(std::chrono::milliseconds(5));
				continue;
			}
			throw;
		}
	}
	throw std::runtime_error("SetCenterFrequencySafe: exhausted retries");
}

#pragma managed(pop)

namespace TXcalUiGlue
{
	TXcalControllerBridge::TXcalControllerBridge(IUnoPluginController* controller)
		: m_controller(controller)
	{
	}

	double TXcalControllerBridge::GetVfoFrequency(int channel)
	{
		return GetVfoFrequencySafe(m_controller, static_cast<channel_t>(channel));
	}

	bool TXcalControllerBridge::SetVfoFrequency(int channel, double frequencyHz)
	{
		return SetVfoFrequencySafe(m_controller, static_cast<channel_t>(channel), frequencyHz);
	}

	double TXcalControllerBridge::GetCenterFrequency(int channel)
	{
		return GetCenterFrequencySafe(m_controller, static_cast<channel_t>(channel));
	}

	bool TXcalControllerBridge::SetCenterFrequency(int channel, double frequencyHz)
	{
		return SetCenterFrequencySafe(m_controller, static_cast<channel_t>(channel), frequencyHz);
	}


	TXcalUi::DemodulatorType TXcalControllerBridge::GetDemodulatorType(int channel)
	{
		return static_cast<TXcalUi::DemodulatorType>(
			static_cast<int>(m_controller->GetDemodulatorType(static_cast<channel_t>(channel))));
	}

	bool TXcalControllerBridge::SetDemodulatorType(int channel, TXcalUi::DemodulatorType type)
	{
		return m_controller->SetDemodulatorType(
			static_cast<channel_t>(channel),
			static_cast<IUnoPluginController::DemodulatorType>(static_cast<int>(type)));
	}

	int TXcalControllerBridge::GetFilterBandwidth(int channel)
	{
		return m_controller->GetFilterBandwidth(static_cast<channel_t>(channel));
	}

	bool TXcalControllerBridge::SetFilterBandwidth(int channel, int bandwidthHz)
	{
		return m_controller->SetFilterBandwidth(static_cast<channel_t>(channel), bandwidthHz);
	}

	bool TXcalControllerBridge::IsStreamingEnabled(int channel)
	{
		return m_controller->IsStreamingEnabled(static_cast<channel_t>(channel));
	}

	bool TXcalControllerBridge::SetAudioVolume(int channel, int volume)
	{
		return m_controller->SetAudioVolume(static_cast<channel_t>(channel), volume);
	}

	int TXcalControllerBridge::GetAudioVolume(int channel)
	{
		return m_controller->GetAudioVolume(static_cast<channel_t>(channel));
	}

	bool TXcalControllerBridge::SetAudioMute(int channel, bool mute)
	{
		return m_controller->SetAudioMute(static_cast<channel_t>(channel), mute);
	}

	bool TXcalControllerBridge::GetAudioMute(int channel)
	{
		return m_controller->GetAudioMute(static_cast<channel_t>(channel));
	}

	double TXcalControllerBridge::GetSNR(int channel)
	{
		return m_controller->GetSNR(static_cast<channel_t>(channel));
	}

	double TXcalControllerBridge::GetPower(int channel)
	{
		return m_controller->GetPower(static_cast<channel_t>(channel));
	}

	String^ TXcalControllerBridge::GetConfigurationKey(String^ key)
	{
		std::string nativeKey = marshal_as<std::string>(key);
		std::string value;
		if (m_controller->GetConfigurationKey(nativeKey, value))
		{
			return marshal_as<String^>(value);
		}
		return String::Empty;
	}

	bool TXcalControllerBridge::SetConfigurationKey(String^ key, String^ value)
	{
		return m_controller->SetConfigurationKey(marshal_as<std::string>(key), marshal_as<std::string>(value));
	}
}
