Public Interface MemoryDeviceUSB
    ''' <summary>
    ''' Sends text messages to our underlying application
    ''' </summary>
    Event PrintConsole(ByVal message As String)
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
    ReadOnly Property DeviceSize As UInt32
    ''' <summary>
    ''' Returns the size (in bytes) of the sector/block
    ''' </summary>
    ReadOnly Property SectorSize(ByVal sector As UInt32, ByVal area As FlashMemory.FlashArea) As UInt32
    ''' <summary>
    ''' Reads data from the external memory device
    ''' </summary>
    Function ReadData(ByVal flash_offset As UInt32, ByVal data_count As UInt32, ByVal area As FlashMemory.FlashArea) As Byte()
    ''' <summary>
    ''' Writes data to an external memory device
    ''' </summary>
    Function WriteData(ByVal flash_offset As UInt32, ByVal data_to_write() As Byte, ByVal area As FlashMemory.FlashArea) As Boolean
    ''' <summary>
    ''' Erases all the data on the external memory device
    ''' </summary>
    Function ChipErase() As Boolean
    Sub WaitUntilReady()
    Function Sector_Find(ByVal SectorIndex As UInt32, ByVal area As FlashMemory.FlashArea) As UInt32
    Function Sector_Erase(ByVal SectorIndex As UInt32, ByVal area As FlashMemory.FlashArea) As Boolean
    Function Sectors_Count() As UInt32
    Function Sector_Write(ByVal SectorIndex As UInt32, ByVal data() As Byte, ByVal area As FlashMemory.FlashArea) As Boolean

End Interface
