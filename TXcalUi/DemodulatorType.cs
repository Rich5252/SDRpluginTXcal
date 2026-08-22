namespace TXcalUi
{
    // Mirrors IUnoPluginController::DemodulatorType from iunoplugincontroller.h.
    public enum DemodulatorType
    {
        DemodulatorNone = 0, DemodulatorAM = 1, DemodulatorSAM = 2, DemodulatorNFM = 3,
        DemodulatorMFM = 4, DemodulatorWFM = 5, DemodulatorSWFM = 6, DemodulatorDSB = 7,
        DemodulatorLSB = 8, DemodulatorUSB = 9, DemodulatorCW = 10, DemodulatorDigital = 11,
        DemodulatorDAB = 12, DemodulatorIQOUT = 13, DemodulatorADSB2 = 14, DemodulatorADSB8 = 15
    }
}