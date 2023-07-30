Public Interface MemoryDeviceUSB
    ''' <summary>
    ''' Sends text messages to our underlying application
    ''' </summary>
    Event PrintConsole(ByVal message As String)
    ''' <summary>
    ''' Sends text messages to our underlying application
    ''' </summary>
    Event SetProgress(ByVal percent As Integer)
    ''' <summary>
    ''' Initiates the hardware to connect to the exteral memory device
    ''' </summary>
    ''' 
    Function DeviceInit() As Boolean
    ''' <summary>
    ''' Returns the name of the memory device (if initiated)
    ''' </summary>
    ReadOnly Property DeviceName() As String
    ''' <summary>
    ''' Returns the size of the device
    ''' </summary>
    ReadOnly Property DeviceSize As Long
    ''' <summary>
    ''' Returns the size (in bytes) of the sector/block
    ''' </summary>
    ReadOnly Property SectorSize(ByVal sector As UInt32, Optional ByVal area As FlashMemory.FlashArea = FlashMemory.FlashArea.Main) As UInt32
    ''' <summary>
    ''' Reads data from the external memory device
    ''' </summary>
    Function ReadData(ByVal flash_offset As Long, ByVal data_count As UInt32, Optional ByVal area As FlashMemory.FlashArea = FlashMemory.FlashArea.Main) As Byte()
    ''' <summary>
    ''' Writes data to an external memory device
    ''' </summary>
    Function WriteData(ByVal flash_offset As Long, ByVal data_to_write() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean
    ''' <summary>
    ''' Erases all the data on the external memory device
    ''' </summary>
    Function EraseDevice() As Boolean
    Sub WaitUntilReady()
    Function SectorFind(ByVal SectorIndex As UInt32, Optional ByVal area As FlashMemory.FlashArea = FlashMemory.FlashArea.Main) As Long
    Function Sector_Erase(ByVal SectorIndex As UInt32, Optional ByVal area As FlashMemory.FlashArea = FlashMemory.FlashArea.Main) As Boolean
    Function Sector_Count() As UInt32
    Function Sector_Write(ByVal SectorIndex As UInt32, ByVal data() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean

End Interface
