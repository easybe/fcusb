
Public Module Configuration
    Public Const FC_BUILD As Integer = 635
    Public Const PRO_PCB5_FW As Single = 1.15F 'This is the embedded firmware version for pro
    Public Const MACH1_PCB2_FW As Single = 2.27F 'Firmware version for Mach1
    Public Const XPORT_PCB2_FW As Single = 5.27F 'XPORT PCB 2.x
    Public Const CLASSIC_FW As Single = 4.55F 'Min revision allowed for classic (PCB 2.x)
    Public Const MACH1_FGPA_3V3 As UInt32 = &HAF330007UI
    Public Const MACH1_FGPA_1V8 As UInt32 = &HAF180007UI
    Public Const MACH1_SPI_3V3 As UInt32 = &HAF330101UI 'Passthrough for SPI
    Public Const MACH1_SPI_1V8 As UInt32 = &HAF180102UI 'Passthrough for SPI
End Module

