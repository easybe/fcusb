Imports System.IO
Imports FlashcatUSB.MemoryInterface
Imports FlashcatUSB.USB

Public Class MemControl_v2

    Public Event WriteConsole(msg_out As String)
    Public Event SetStatus(status_text As String)
    Public Event GetEccLastResult(ByRef result As Object)
    Public Event GetSectorIndex(addr As Long, ByRef sector_int As Integer)
    Public Event GetSectorCount(ByRef count As Integer)
    Public Event GetSectorSize(sector_int As Integer, ByRef sector_size As Integer)
    Public Event WriteMemory(addr As Long, data() As Byte, verify_wr As Boolean, ByRef Success As Boolean)
    Public Event ReadStream(data_stream As Stream, f_params As ReadParameters)
    Public Event ReadMemory(base_addr As Long, ByRef data() As Byte)
    Public Event EraseMemory()
    Public Event WriteStream(data_stream As Stream, f_params As WriteParameters, ByRef Success As Boolean)
    Public Event SuccessfulWrite(mydev As FCUSB_DEVICE, x As MemControl_v2.XFER_Operation)
    Public Event GetSectorBaseAddress(sector_int As Integer, ByRef addr As Long)

    Public Property AllowFullErase As Boolean

    Sub New(i_face As MemoryDeviceInstance)

    End Sub

    Public Class XFER_Operation
        Public FileName As FileInfo  'Contains the shortname of the file being opened or written to
        Public DataStream As Stream
        Public Offset As Long
        Public Size As Long
        Public FileType As FileFilterIndex
    End Class

    Public Enum FileFilterIndex
        Binary
        IntelHex
        SRecord
    End Enum


    Friend Sub DisableControls(show_cancel As Boolean)
        Throw New NotImplementedException()
    End Sub

    Friend Sub EnableControls()
        Throw New NotImplementedException()
    End Sub

    Friend Sub RefreshView()
        Throw New NotImplementedException()
    End Sub

    Friend Sub AbortAnyOperation()
        Throw New NotImplementedException()
    End Sub


End Class
