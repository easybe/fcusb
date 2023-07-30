Public Interface MemoryDeviceUSB
    ''' <summary>
    ''' Sends text messages to our underlying application
    ''' </summary>
    Event PrintConsole(message As String)
    ''' <summary>
    ''' Sends text messages to our underlying application
    ''' </summary>
    Event SetProgress(percent As Integer)
    ''' <summary>
    ''' Initiates the hardware to connect to the exteral memory device
    ''' </summary>
    Function DeviceInit() As Boolean
    ''' <summary>
    ''' Returns the device class
    ''' </summary>
    ReadOnly Property GetDevice As FlashMemory.Device
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
    Function SectorSize(sector As UInt32) As UInt32
    ''' <summary>
    ''' Reads data from the external memory device
    ''' </summary>
    Function ReadData(flash_offset As Long, data_count As Long) As Byte()
    ''' <summary>
    ''' Writes data to an external memory device
    ''' </summary>
    Function WriteData(flash_offset As Long, data_to_write() As Byte, Optional Params As WriteParameters = Nothing) As Boolean
    ''' <summary>
    ''' Erases all the data on the external memory device
    ''' </summary>
    Function EraseDevice() As Boolean
    Sub WaitUntilReady()
    Function SectorFind(SectorIndex As UInt32) As Long
    Function SectorErase(SectorIndex As UInt32) As Boolean
    Function SectorCount() As UInt32
    Function SectorWrite(SectorIndex As UInt32, data() As Byte, Optional Params As WriteParameters = Nothing) As Boolean

End Interface
