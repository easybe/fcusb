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
    Function SectorSize(sector As Integer) As Integer
    ''' <summary>
    ''' Reads data from the external memory device
    ''' </summary>
    Function ReadData(flash_offset As Long, data_count As Integer) As Byte()
    ''' <summary>
    ''' Writes data to an external memory device
    ''' </summary>
    Function WriteData(flash_offset As Long, data_to_write() As Byte, Optional Params As WriteParameters = Nothing) As Boolean
    ''' <summary>
    ''' Erases all the data on the external memory device
    ''' </summary>
    Function EraseDevice() As Boolean
    Sub WaitUntilReady()
    Function SectorFind(sector_index As Integer) As Long
    Function SectorErase(sector_index As Integer) As Boolean
    Function SectorCount() As Integer
    Function SectorWrite(sector_index As Integer, data() As Byte, Optional Params As WriteParameters = Nothing) As Boolean

End Interface
