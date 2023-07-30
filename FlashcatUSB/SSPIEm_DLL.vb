Imports System.Runtime.InteropServices
Module SSPIEm_DLL
    Public Delegate Sub PROGRESS_CALLBACK(someparameter As Integer)

    Public Delegate Sub WRITE_CALLBACK(value As Byte) 'typedef void(*WRITE_CALLBACK)(unsigned char value);

    Public Delegate Function SPI_WRITE_CALLBACK(NumBytesToWrite As UInteger) As Integer

    Public Delegate Function SPI_READ_CALLBACK(NumBytesToRead As UInteger, ByRef NumBytesReturned As UInteger) As Integer

    Public Delegate Function SPI_WRITEREAD_CALLBACK(NumBytesToWrite As UInteger, ByRef NumBytesReturned As UInteger) As Integer

    <DllImport("SSPIEm_dll.dll")>
    Public Function HelloWorld(d As Integer) As Integer
    End Function

    <DllImport("SSPIEm_dll.dll")>
    Public Function SSPIEm_preset(ByVal algoFileName As String, ByVal dataFileName As String) As Integer
    End Function
    <DllImport("SSPIEm_dll.dll")>
    Public Function SSPIEm(algoID As UInteger) As Integer
    End Function

    <DllImport("SSPIEm_dll.dll")>
    Public Sub algo_process(ByRef pos As Integer, ByRef size As Integer)
    End Sub

    <DllImport("SSPIEm_dll.dll")>
    Public Sub SetCallback(ByVal callback As PROGRESS_CALLBACK)
    End Sub

    <DllImport("SSPIEm_dll.dll")>
    Public Sub callback_run()
    End Sub

    <DllImport("SSPIEm_dll.dll")>
    Public Sub SetCallback_write(callback1 As WRITE_CALLBACK, callback2 As WRITE_CALLBACK, callback3 As WRITE_CALLBACK)
    End Sub

    <DllImport("SSPIEm_dll.dll")>
    Public Sub SetCallback_spi(callback1 As SPI_WRITE_CALLBACK, callback2 As SPI_READ_CALLBACK, callback3 As SPI_WRITEREAD_CALLBACK)
    End Sub

    <DllImport("SSPIEm_dll.dll")>
    Public Sub SetBuffer(ByRef tx_buffer As Byte, ByRef rx_buffer As Byte)
    End Sub

    <DllImport("SSPIEm_dll.dll")>
    Public Sub call_back()
    End Sub

    Public Enum SSPIEm_Code As Integer
        Succeed = 2
        Process_Failed = 0
        Init_Algo_Failed = -1
        Init_Data_Failed = -2
        Version_not_support = -3
        Header_Checksum_Mismatch = -4
        Init_SPI_Port_Failed = -5
        Init_Failed = -6
        Algorithm_Error = -11
        Data_Error = -12
        Hardware_Error = -13
        Verification_Error = -20
    End Enum
End Module
