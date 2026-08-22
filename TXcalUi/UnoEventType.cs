namespace TXcalUi
{
    // Mirrors UnoEvent::UnoEventType from unoevent.h, so MainForm.OnUnoEvent can switch
    // on readable names instead of raw ints. Keep this in sync if SDRplay adds new event
    // types.
    public enum UnoEventType
    {
        UndefinedEvent = 0,
        DemodulatorChanged = 1,
        BandwidthChanged = 2,
        FrequencyChanged = 3,
        CenterFrequencyChanged = 4,
        SampleRateChanged = 5,
        StreamingStarted = 6,
        StreamingStopped = 7,
        SquelchEnableChanged = 8,
        SquelchThresholdChanged = 9,
        AgcThresholdChanged = 10,
        AgcModeChanged = 11,
        NoiseBlankerLevelChanged = 12,
        NoiseReductionLevelChanged = 13,
        CwPeakFilterThresholdChanged = 14,
        FmNoiseReductionEnabledChanged = 15,
        FmNoiseReductionThresholdChanged = 16,
        WfmDeemphasisModeChanged = 17,
        AudioVolumeChanged = 18,
        AudioMuteChanged = 19,
        IFGainChanged = 20,
        SavingWorkspace = 21,
        VRXCountChanged = 22,
        VRXStateChanged = 23,
        StepSizeChanged = 24,
        VFOChanged = 25,
        ClosingDown = 26,
        SP1MinFreqChanged = 27,
        SP1MaxFreqChanged = 28,
        BiasTEnableChanged = 29,
        SP1MinPowerChanged = 30,
        SP1MaxPowerChanged = 31,
        StartRequest = 32,
        StopRequest = 33,
        VFOSignalPresent = 34,
        VFOSignalNotPresent = 35
    }
}
