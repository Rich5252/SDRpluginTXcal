namespace TXcalUi
{
    // Implemented on the C++/CLI side by TXcalUiGlue::TXcalControllerBridge, which
    // wraps the native IUnoPluginController&. Add more members here as your UI (or the
    // future serial/pipe/UDP worker) needs more of the SDRuno controller API -- mirror
    // every addition with a matching pass-through method on TXcalControllerBridge.
    //
    // Keeping this interface (rather than a concrete class) in the pure-C# project is
    // what avoids a circular project reference between TXcalUi and TXcalUiGlue:
    // TXcalUiGlue references TXcalUi (for MainForm) and implements this interface;
    // TXcalUi never needs to reference TXcalUiGlue at all.
    public interface ITXcalController
    {
        double GetVfoFrequency(int channel);
        bool SetVfoFrequency(int channel, double frequencyHz);

        double GetCenterFrequency(int channel);
        bool SetCenterFrequency(int channel, double frequencyHz);

        DemodulatorType GetDemodulatorType(int channel);
        bool SetDemodulatorType(int channel, DemodulatorType type);

        int GetFilterBandwidth(int channel);
        bool SetFilterBandwidth(int channel, int bandwidthHz);

        bool IsStreamingEnabled(int channel);

        bool SetAudioVolume(int channel, int volume);
        int GetAudioVolume(int channel);
        bool SetAudioMute(int channel, bool mute);
        bool GetAudioMute(int channel);

        double GetSNR(int channel);
        double GetPower(int channel);

        string GetConfigurationKey(string key);
        bool SetConfigurationKey(string key, string value);
    }
}
