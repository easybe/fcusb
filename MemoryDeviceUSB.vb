Public Interface MemoryDeviceUSB
    ''' <summary>
    ''' Sends text messages to our underlying application
    ''' </summary>
    Event PrintConsole(ByVal message As String)
    ''' <summary>
    ''' Connects to our physical USB device
    ''' </summary>
    Function Connect() As Boolean
    ''' <summary>
    ''' Disconnects from our physical USB device
    ''' </summary>
    Function Disconnect() As Boolean
    ''' <summary>
    ''' Checks to see if we are connected to the physical USB device
    ''' </summary>
    Function IsConnected() As Boolean
    ''' <summary>
    ''' Turns on the LED on our USB device
    ''' </summary>
    Sub LEDOn()
    ''' <summary>
    ''' Turns off the LED on our USB device
    ''' </summary>
    Sub LedOff()
    ''' <summary>
    ''' Makes the LED blink on our USB device
    ''' </summary>
    Sub LEDBlink()
    ''' <summary>
    ''' Initiates the hardware to connect to the exteral memory device
    ''' </summary>
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
    ReadOnly Property SectorSize(ByVal sector As Integer) As Long
    ''' <summary>
    ''' Reads data from the external memory device
    ''' </summary>
    Function ReadData(ByVal flash_offset As Long, ByVal data_count As Long) As Byte()
    ''' <summary>
    ''' Writes data to an external memory device
    ''' </summary>
    Function WriteData(ByVal flash_offset As Long, ByVal data_to_write() As Byte) As Boolean
    ''' <summary>
    ''' Erases all the data on the external memory device
    ''' </summary>
    Function ChipErase() As Boolean
    Sub WaitUntilReady()
    Function Sector_Find(ByVal SectorIndex As Long) As Long
    Function Sector_Erase(ByVal Sector As Long) As Boolean
    Function Sectors_Count() As Long
    Function Sector_Write(ByVal Sector As Integer, ByVal data() As Byte) As Boolean

End Interface
