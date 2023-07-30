'Implements a standard JTAG state-machine for a Test-Access-Port
'This module was developed by EmbeddedComputers.net, ALL-RIGHTS-RESERVED

Public Class JTAG_STATE_CONTROLLER
    Public Event ShiftBits(ByVal BitCount As UInt32, ByVal tdi_bits() As Byte, ByVal tms_bits() As Byte, ByRef tdo_bits() As Byte)
    Public Property STATE As MachineState 'Is the current state of the JTAG machine

    Public Enum MachineState As Byte
        TestLogicReset = 0
        RunTestIdle = 1
        Select_DR = 2
        Capture_DR = 3
        Shift_DR = 4
        Exit1_DR = 5
        Pause_DR = 6
        Exit2_DR = 7
        Update_DR = 8
        Select_IR = 9
        Capture_IR = 10
        Shift_IR = 11
        Exit1_IR = 12
        Pause_IR = 13
        Exit2_IR = 14
        Update_IR = 15
    End Enum

    Sub New()

    End Sub

    'Shift out bits on tms to move the state machine to our desired state (this code was auto-generated for performance)
    Public Sub GotoState(ByVal to_state As MachineState)
        If Me.STATE = to_state Then Exit Sub
        Dim tms_bits As UInt64 = 0
        Dim tms_count As Integer = 0
        Select Case Me.STATE
            Case MachineState.TestLogicReset
                Select Case to_state
                    Case MachineState.RunTestIdle
                        tms_bits = 0 : tms_count = 1 '0
                    Case MachineState.Select_DR
                        tms_bits = 2 : tms_count = 2 '10
                    Case MachineState.Capture_DR
                        tms_bits = 2 : tms_count = 3 '010
                    Case MachineState.Shift_DR
                        tms_bits = 2 : tms_count = 4 '0010
                    Case MachineState.Exit1_DR
                        tms_bits = 10 : tms_count = 4 '1010
                    Case MachineState.Pause_DR
                        tms_bits = 10 : tms_count = 5 '01010
                    Case MachineState.Exit2_DR
                        tms_bits = 42 : tms_count = 6 '101010
                    Case MachineState.Update_DR
                        tms_bits = 26 : tms_count = 5 '11010
                    Case MachineState.Select_IR
                        tms_bits = 6 : tms_count = 3 '110
                    Case MachineState.Capture_IR
                        tms_bits = 6 : tms_count = 4 '0110
                    Case MachineState.Shift_IR
                        tms_bits = 6 : tms_count = 5 '00110
                    Case MachineState.Exit1_IR
                        tms_bits = 22 : tms_count = 5 '10110
                    Case MachineState.Pause_IR
                        tms_bits = 22 : tms_count = 6 '010110
                    Case MachineState.Exit2_IR
                        tms_bits = 86 : tms_count = 7 '1010110
                    Case MachineState.Update_IR
                        tms_bits = 54 : tms_count = 6 '110110
                End Select
            Case MachineState.RunTestIdle
                Select Case to_state
                    Case MachineState.TestLogicReset
                        tms_bits = 7 : tms_count = 3 '111
                    Case MachineState.Select_DR
                        tms_bits = 1 : tms_count = 1 '1
                    Case MachineState.Capture_DR
                        tms_bits = 1 : tms_count = 2 '01
                    Case MachineState.Shift_DR
                        tms_bits = 1 : tms_count = 3 '001
                    Case MachineState.Exit1_DR
                        tms_bits = 5 : tms_count = 3 '101
                    Case MachineState.Pause_DR
                        tms_bits = 5 : tms_count = 4 '0101
                    Case MachineState.Exit2_DR
                        tms_bits = 21 : tms_count = 5 '10101
                    Case MachineState.Update_DR
                        tms_bits = 13 : tms_count = 4 '1101
                    Case MachineState.Select_IR
                        tms_bits = 3 : tms_count = 2 '11
                    Case MachineState.Capture_IR
                        tms_bits = 3 : tms_count = 3 '011
                    Case MachineState.Shift_IR
                        tms_bits = 3 : tms_count = 4 '0011
                    Case MachineState.Exit1_IR
                        tms_bits = 11 : tms_count = 4 '1011
                    Case MachineState.Pause_IR
                        tms_bits = 11 : tms_count = 5 '01011
                    Case MachineState.Exit2_IR
                        tms_bits = 43 : tms_count = 6 '101011
                    Case MachineState.Update_IR
                        tms_bits = 27 : tms_count = 5 '11011
                End Select
            Case MachineState.Select_DR
                Select Case to_state
                    Case MachineState.TestLogicReset
                        tms_bits = 3 : tms_count = 2 '11
                    Case MachineState.RunTestIdle
                        tms_bits = 3 : tms_count = 3 '011
                    Case MachineState.Capture_DR
                        tms_bits = 0 : tms_count = 1 '0
                    Case MachineState.Shift_DR
                        tms_bits = 0 : tms_count = 2 '00
                    Case MachineState.Exit1_DR
                        tms_bits = 2 : tms_count = 2 '10
                    Case MachineState.Pause_DR
                        tms_bits = 2 : tms_count = 3 '010
                    Case MachineState.Exit2_DR
                        tms_bits = 10 : tms_count = 4 '1010
                    Case MachineState.Update_DR
                        tms_bits = 6 : tms_count = 3 '110
                    Case MachineState.Select_IR
                        tms_bits = 1 : tms_count = 1 '1
                    Case MachineState.Capture_IR
                        tms_bits = 1 : tms_count = 2 '01
                    Case MachineState.Shift_IR
                        tms_bits = 1 : tms_count = 3 '001
                    Case MachineState.Exit1_IR
                        tms_bits = 5 : tms_count = 3 '101
                    Case MachineState.Pause_IR
                        tms_bits = 5 : tms_count = 4 '0101
                    Case MachineState.Exit2_IR
                        tms_bits = 21 : tms_count = 5 '10101
                    Case MachineState.Update_IR
                        tms_bits = 13 : tms_count = 4 '1101
                End Select
            Case MachineState.Capture_DR
                Select Case to_state
                    Case MachineState.TestLogicReset
                        tms_bits = 31 : tms_count = 5 '11111
                    Case MachineState.RunTestIdle
                        tms_bits = 3 : tms_count = 3 '011
                    Case MachineState.Select_DR
                        tms_bits = 7 : tms_count = 3 '111
                    Case MachineState.Shift_DR
                        tms_bits = 0 : tms_count = 1 '0
                    Case MachineState.Exit1_DR
                        tms_bits = 1 : tms_count = 1 '1
                    Case MachineState.Pause_DR
                        tms_bits = 1 : tms_count = 2 '01
                    Case MachineState.Exit2_DR
                        tms_bits = 5 : tms_count = 3 '101
                    Case MachineState.Update_DR
                        tms_bits = 3 : tms_count = 2 '11
                    Case MachineState.Select_IR
                        tms_bits = 15 : tms_count = 4 '1111
                    Case MachineState.Capture_IR
                        tms_bits = 15 : tms_count = 5 '01111
                    Case MachineState.Shift_IR
                        tms_bits = 15 : tms_count = 6 '001111
                    Case MachineState.Exit1_IR
                        tms_bits = 47 : tms_count = 6 '101111
                    Case MachineState.Pause_IR
                        tms_bits = 47 : tms_count = 7 '0101111
                    Case MachineState.Exit2_IR
                        tms_bits = 175 : tms_count = 8 '10101111
                    Case MachineState.Update_IR
                        tms_bits = 111 : tms_count = 7 '1101111
                End Select
            Case MachineState.Shift_DR
                Select Case to_state
                    Case MachineState.TestLogicReset
                        tms_bits = 31 : tms_count = 5 '11111
                    Case MachineState.RunTestIdle
                        tms_bits = 3 : tms_count = 3 '011
                    Case MachineState.Select_DR
                        tms_bits = 7 : tms_count = 3 '111
                    Case MachineState.Capture_DR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case MachineState.Exit1_DR
                        tms_bits = 1 : tms_count = 1 '1
                    Case MachineState.Pause_DR
                        tms_bits = 1 : tms_count = 2 '01
                    Case MachineState.Exit2_DR
                        tms_bits = 5 : tms_count = 3 '101
                    Case MachineState.Update_DR
                        tms_bits = 3 : tms_count = 2 '11
                    Case MachineState.Select_IR
                        tms_bits = 15 : tms_count = 4 '1111
                    Case MachineState.Capture_IR
                        tms_bits = 15 : tms_count = 5 '01111
                    Case MachineState.Shift_IR
                        tms_bits = 15 : tms_count = 6 '001111
                    Case MachineState.Exit1_IR
                        tms_bits = 47 : tms_count = 6 '101111
                    Case MachineState.Pause_IR
                        tms_bits = 47 : tms_count = 7 '0101111
                    Case MachineState.Exit2_IR
                        tms_bits = 175 : tms_count = 8 '10101111
                    Case MachineState.Update_IR
                        tms_bits = 111 : tms_count = 7 '1101111
                End Select
            Case MachineState.Exit1_DR
                Select Case to_state
                    Case MachineState.TestLogicReset
                        tms_bits = 15 : tms_count = 4 '1111
                    Case MachineState.RunTestIdle
                        tms_bits = 1 : tms_count = 2 '01
                    Case MachineState.Select_DR
                        tms_bits = 3 : tms_count = 2 '11
                    Case MachineState.Capture_DR
                        tms_bits = 3 : tms_count = 3 '011
                    Case MachineState.Shift_DR
                        tms_bits = 2 : tms_count = 3 '010
                    Case MachineState.Pause_DR
                        tms_bits = 0 : tms_count = 1 '0
                    Case MachineState.Exit2_DR
                        tms_bits = 2 : tms_count = 2 '10
                    Case MachineState.Update_DR
                        tms_bits = 1 : tms_count = 1 '1
                    Case MachineState.Select_IR
                        tms_bits = 7 : tms_count = 3 '111
                    Case MachineState.Capture_IR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case MachineState.Shift_IR
                        tms_bits = 7 : tms_count = 5 '00111
                    Case MachineState.Exit1_IR
                        tms_bits = 23 : tms_count = 5 '10111
                    Case MachineState.Pause_IR
                        tms_bits = 23 : tms_count = 6 '010111
                    Case MachineState.Exit2_IR
                        tms_bits = 87 : tms_count = 7 '1010111
                    Case MachineState.Update_IR
                        tms_bits = 55 : tms_count = 6 '110111
                End Select
            Case MachineState.Pause_DR
                Select Case to_state
                    Case MachineState.TestLogicReset
                        tms_bits = 31 : tms_count = 5 '11111
                    Case MachineState.RunTestIdle
                        tms_bits = 3 : tms_count = 3 '011
                    Case MachineState.Select_DR
                        tms_bits = 7 : tms_count = 3 '111
                    Case MachineState.Capture_DR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case MachineState.Shift_DR
                        tms_bits = 1 : tms_count = 2 '01
                    Case MachineState.Exit1_DR
                        tms_bits = 5 : tms_count = 3 '101
                    Case MachineState.Exit2_DR
                        tms_bits = 1 : tms_count = 1 '1
                    Case MachineState.Update_DR
                        tms_bits = 3 : tms_count = 2 '11
                    Case MachineState.Select_IR
                        tms_bits = 15 : tms_count = 4 '1111
                    Case MachineState.Capture_IR
                        tms_bits = 15 : tms_count = 5 '01111
                    Case MachineState.Shift_IR
                        tms_bits = 15 : tms_count = 6 '001111
                    Case MachineState.Exit1_IR
                        tms_bits = 47 : tms_count = 6 '101111
                    Case MachineState.Pause_IR
                        tms_bits = 47 : tms_count = 7 '0101111
                    Case MachineState.Exit2_IR
                        tms_bits = 175 : tms_count = 8 '10101111
                    Case MachineState.Update_IR
                        tms_bits = 111 : tms_count = 7 '1101111
                End Select
            Case MachineState.Exit2_DR
                Select Case to_state
                    Case MachineState.TestLogicReset
                        tms_bits = 15 : tms_count = 4 '1111
                    Case MachineState.RunTestIdle
                        tms_bits = 1 : tms_count = 2 '01
                    Case MachineState.Select_DR
                        tms_bits = 3 : tms_count = 2 '11
                    Case MachineState.Capture_DR
                        tms_bits = 3 : tms_count = 3 '011
                    Case MachineState.Shift_DR
                        tms_bits = 0 : tms_count = 1 '0
                    Case MachineState.Exit1_DR
                        tms_bits = 2 : tms_count = 2 '10
                    Case MachineState.Pause_DR
                        tms_bits = 2 : tms_count = 3 '010
                    Case MachineState.Update_DR
                        tms_bits = 1 : tms_count = 1 '1
                    Case MachineState.Select_IR
                        tms_bits = 7 : tms_count = 3 '111
                    Case MachineState.Capture_IR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case MachineState.Shift_IR
                        tms_bits = 7 : tms_count = 5 '00111
                    Case MachineState.Exit1_IR
                        tms_bits = 23 : tms_count = 5 '10111
                    Case MachineState.Pause_IR
                        tms_bits = 23 : tms_count = 6 '010111
                    Case MachineState.Exit2_IR
                        tms_bits = 87 : tms_count = 7 '1010111
                    Case MachineState.Update_IR
                        tms_bits = 55 : tms_count = 6 '110111
                End Select
            Case MachineState.Update_DR
                Select Case to_state
                    Case MachineState.TestLogicReset
                        tms_bits = 7 : tms_count = 3 '111
                    Case MachineState.RunTestIdle
                        tms_bits = 0 : tms_count = 1 '0
                    Case MachineState.Select_DR
                        tms_bits = 1 : tms_count = 1 '1
                    Case MachineState.Capture_DR
                        tms_bits = 1 : tms_count = 2 '01
                    Case MachineState.Shift_DR
                        tms_bits = 1 : tms_count = 3 '001
                    Case MachineState.Exit1_DR
                        tms_bits = 5 : tms_count = 3 '101
                    Case MachineState.Pause_DR
                        tms_bits = 5 : tms_count = 4 '0101
                    Case MachineState.Exit2_DR
                        tms_bits = 21 : tms_count = 5 '10101
                    Case MachineState.Select_IR
                        tms_bits = 3 : tms_count = 2 '11
                    Case MachineState.Capture_IR
                        tms_bits = 3 : tms_count = 3 '011
                    Case MachineState.Shift_IR
                        tms_bits = 3 : tms_count = 4 '0011
                    Case MachineState.Exit1_IR
                        tms_bits = 11 : tms_count = 4 '1011
                    Case MachineState.Pause_IR
                        tms_bits = 11 : tms_count = 5 '01011
                    Case MachineState.Exit2_IR
                        tms_bits = 43 : tms_count = 6 '101011
                    Case MachineState.Update_IR
                        tms_bits = 27 : tms_count = 5 '11011
                End Select
            Case MachineState.Select_IR
                Select Case to_state
                    Case MachineState.TestLogicReset
                        tms_bits = 1 : tms_count = 1 '1
                    Case MachineState.RunTestIdle
                        tms_bits = 1 : tms_count = 2 '01
                    Case MachineState.Select_DR
                        tms_bits = 5 : tms_count = 3 '101
                    Case MachineState.Capture_DR
                        tms_bits = 5 : tms_count = 4 '0101
                    Case MachineState.Shift_DR
                        tms_bits = 5 : tms_count = 5 '00101
                    Case MachineState.Exit1_DR
                        tms_bits = 21 : tms_count = 5 '10101
                    Case MachineState.Pause_DR
                        tms_bits = 21 : tms_count = 6 '010101
                    Case MachineState.Exit2_DR
                        tms_bits = 85 : tms_count = 7 '1010101
                    Case MachineState.Update_DR
                        tms_bits = 53 : tms_count = 6 '110101
                    Case MachineState.Capture_IR
                        tms_bits = 0 : tms_count = 1 '0
                    Case MachineState.Shift_IR
                        tms_bits = 0 : tms_count = 2 '00
                    Case MachineState.Exit1_IR
                        tms_bits = 2 : tms_count = 2 '10
                    Case MachineState.Pause_IR
                        tms_bits = 2 : tms_count = 3 '010
                    Case MachineState.Exit2_IR
                        tms_bits = 10 : tms_count = 4 '1010
                    Case MachineState.Update_IR
                        tms_bits = 6 : tms_count = 3 '110
                End Select
            Case MachineState.Capture_IR
                Select Case to_state
                    Case MachineState.TestLogicReset
                        tms_bits = 31 : tms_count = 5 '11111
                    Case MachineState.RunTestIdle
                        tms_bits = 3 : tms_count = 3 '011
                    Case MachineState.Select_DR
                        tms_bits = 7 : tms_count = 3 '111
                    Case MachineState.Capture_DR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case MachineState.Shift_DR
                        tms_bits = 7 : tms_count = 5 '00111
                    Case MachineState.Exit1_DR
                        tms_bits = 23 : tms_count = 5 '10111
                    Case MachineState.Pause_DR
                        tms_bits = 23 : tms_count = 6 '010111
                    Case MachineState.Exit2_DR
                        tms_bits = 87 : tms_count = 7 '1010111
                    Case MachineState.Update_DR
                        tms_bits = 55 : tms_count = 6 '110111
                    Case MachineState.Select_IR
                        tms_bits = 15 : tms_count = 4 '1111
                    Case MachineState.Shift_IR
                        tms_bits = 0 : tms_count = 1 '0
                    Case MachineState.Exit1_IR
                        tms_bits = 1 : tms_count = 1 '1
                    Case MachineState.Pause_IR
                        tms_bits = 1 : tms_count = 2 '01
                    Case MachineState.Exit2_IR
                        tms_bits = 5 : tms_count = 3 '101
                    Case MachineState.Update_IR
                        tms_bits = 3 : tms_count = 2 '11
                End Select
            Case MachineState.Shift_IR
                Select Case to_state
                    Case MachineState.TestLogicReset
                        tms_bits = 31 : tms_count = 5 '11111
                    Case MachineState.RunTestIdle
                        tms_bits = 3 : tms_count = 3 '011
                    Case MachineState.Select_DR
                        tms_bits = 7 : tms_count = 3 '111
                    Case MachineState.Capture_DR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case MachineState.Shift_DR
                        tms_bits = 7 : tms_count = 5 '00111
                    Case MachineState.Exit1_DR
                        tms_bits = 23 : tms_count = 5 '10111
                    Case MachineState.Pause_DR
                        tms_bits = 23 : tms_count = 6 '010111
                    Case MachineState.Exit2_DR
                        tms_bits = 87 : tms_count = 7 '1010111
                    Case MachineState.Update_DR
                        tms_bits = 55 : tms_count = 6 '110111
                    Case MachineState.Select_IR
                        tms_bits = 15 : tms_count = 4 '1111
                    Case MachineState.Capture_IR
                        tms_bits = 15 : tms_count = 5 '01111
                    Case MachineState.Exit1_IR
                        tms_bits = 1 : tms_count = 1 '1
                    Case MachineState.Pause_IR
                        tms_bits = 1 : tms_count = 2 '01
                    Case MachineState.Exit2_IR
                        tms_bits = 5 : tms_count = 3 '101
                    Case MachineState.Update_IR
                        tms_bits = 3 : tms_count = 2 '11
                End Select
            Case MachineState.Exit1_IR
                Select Case to_state
                    Case MachineState.TestLogicReset
                        tms_bits = 15 : tms_count = 4 '1111
                    Case MachineState.RunTestIdle
                        tms_bits = 1 : tms_count = 2 '01
                    Case MachineState.Select_DR
                        tms_bits = 3 : tms_count = 2 '11
                    Case MachineState.Capture_DR
                        tms_bits = 3 : tms_count = 3 '011
                    Case MachineState.Shift_DR
                        tms_bits = 3 : tms_count = 4 '0011
                    Case MachineState.Exit1_DR
                        tms_bits = 11 : tms_count = 4 '1011
                    Case MachineState.Pause_DR
                        tms_bits = 11 : tms_count = 5 '01011
                    Case MachineState.Exit2_DR
                        tms_bits = 43 : tms_count = 6 '101011
                    Case MachineState.Update_DR
                        tms_bits = 27 : tms_count = 5 '11011
                    Case MachineState.Select_IR
                        tms_bits = 7 : tms_count = 3 '111
                    Case MachineState.Capture_IR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case MachineState.Shift_IR
                        tms_bits = 2 : tms_count = 3 '010
                    Case MachineState.Pause_IR
                        tms_bits = 0 : tms_count = 1 '0
                    Case MachineState.Exit2_IR
                        tms_bits = 2 : tms_count = 2 '10
                    Case MachineState.Update_IR
                        tms_bits = 1 : tms_count = 1 '1
                End Select
            Case MachineState.Pause_IR
                Select Case to_state
                    Case MachineState.TestLogicReset
                        tms_bits = 31 : tms_count = 5 '11111
                    Case MachineState.RunTestIdle
                        tms_bits = 3 : tms_count = 3 '011
                    Case MachineState.Select_DR
                        tms_bits = 7 : tms_count = 3 '111
                    Case MachineState.Capture_DR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case MachineState.Shift_DR
                        tms_bits = 7 : tms_count = 5 '00111
                    Case MachineState.Exit1_DR
                        tms_bits = 23 : tms_count = 5 '10111
                    Case MachineState.Pause_DR
                        tms_bits = 23 : tms_count = 6 '010111
                    Case MachineState.Exit2_DR
                        tms_bits = 87 : tms_count = 7 '1010111
                    Case MachineState.Update_DR
                        tms_bits = 55 : tms_count = 6 '110111
                    Case MachineState.Select_IR
                        tms_bits = 15 : tms_count = 4 '1111
                    Case MachineState.Capture_IR
                        tms_bits = 15 : tms_count = 5 '01111
                    Case MachineState.Shift_IR
                        tms_bits = 1 : tms_count = 2 '01
                    Case MachineState.Exit1_IR
                        tms_bits = 5 : tms_count = 3 '101
                    Case MachineState.Exit2_IR
                        tms_bits = 1 : tms_count = 1 '1
                    Case MachineState.Update_IR
                        tms_bits = 3 : tms_count = 2 '11
                End Select
            Case MachineState.Exit2_IR
                Select Case to_state
                    Case MachineState.TestLogicReset
                        tms_bits = 15 : tms_count = 4 '1111
                    Case MachineState.RunTestIdle
                        tms_bits = 1 : tms_count = 2 '01
                    Case MachineState.Select_DR
                        tms_bits = 3 : tms_count = 2 '11
                    Case MachineState.Capture_DR
                        tms_bits = 3 : tms_count = 3 '011
                    Case MachineState.Shift_DR
                        tms_bits = 3 : tms_count = 4 '0011
                    Case MachineState.Exit1_DR
                        tms_bits = 11 : tms_count = 4 '1011
                    Case MachineState.Pause_DR
                        tms_bits = 11 : tms_count = 5 '01011
                    Case MachineState.Exit2_DR
                        tms_bits = 43 : tms_count = 6 '101011
                    Case MachineState.Update_DR
                        tms_bits = 27 : tms_count = 5 '11011
                    Case MachineState.Select_IR
                        tms_bits = 7 : tms_count = 3 '111
                    Case MachineState.Capture_IR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case MachineState.Shift_IR
                        tms_bits = 0 : tms_count = 1 '0
                    Case MachineState.Exit1_IR
                        tms_bits = 2 : tms_count = 2 '10
                    Case MachineState.Pause_IR
                        tms_bits = 2 : tms_count = 3 '010
                    Case MachineState.Update_IR
                        tms_bits = 1 : tms_count = 1 '1
                End Select
            Case MachineState.Update_IR
                Select Case to_state
                    Case MachineState.TestLogicReset
                        tms_bits = 7 : tms_count = 3 '111
                    Case MachineState.RunTestIdle
                        tms_bits = 0 : tms_count = 1 '0
                    Case MachineState.Select_DR
                        tms_bits = 1 : tms_count = 1 '1
                    Case MachineState.Capture_DR
                        tms_bits = 1 : tms_count = 2 '01
                    Case MachineState.Shift_DR
                        tms_bits = 1 : tms_count = 3 '001
                    Case MachineState.Exit1_DR
                        tms_bits = 5 : tms_count = 3 '101
                    Case MachineState.Pause_DR
                        tms_bits = 5 : tms_count = 4 '0101
                    Case MachineState.Exit2_DR
                        tms_bits = 21 : tms_count = 5 '10101
                    Case MachineState.Update_DR
                        tms_bits = 13 : tms_count = 4 '1101
                    Case MachineState.Select_IR
                        tms_bits = 3 : tms_count = 2 '11
                    Case MachineState.Capture_IR
                        tms_bits = 3 : tms_count = 3 '011
                    Case MachineState.Shift_IR
                        tms_bits = 3 : tms_count = 4 '0011
                    Case MachineState.Exit1_IR
                        tms_bits = 11 : tms_count = 4 '1011
                    Case MachineState.Pause_IR
                        tms_bits = 11 : tms_count = 5 '01011
                    Case MachineState.Exit2_IR
                        tms_bits = 43 : tms_count = 6 '101011
                End Select
        End Select
        RaiseEvent ShiftBits(tms_count, GetBytes_FromUint(0, tms_count), GetBytes_FromUint(tms_bits, tms_count), Nothing)
        Me.STATE = to_state
    End Sub

    Public Sub Reset()
        RaiseEvent ShiftBits(5, {0}, {&HFF}, Nothing) 'Sets machine state to TestLogicReset
        Me.STATE = MachineState.TestLogicReset
        GotoState(MachineState.Select_DR)
    End Sub

    Public Sub ShiftDR(ByVal tdi_bits() As Byte, ByRef tdo_bits() As Byte, ByVal bit_count As Integer, Optional exit_mode As Boolean = True)
        GotoState(MachineState.Shift_DR)
        If exit_mode Then
            tdo_bits = ShiftOut(tdi_bits, bit_count, True)
            Me.STATE = MachineState.Exit1_DR
        Else
            tdo_bits = ShiftOut(tdi_bits, bit_count, False)
        End If
    End Sub

    Public Sub ShiftIR(ByVal tdi_bits() As Byte, ByRef tdo_bits() As Byte, ByVal bit_count As Integer, Optional exit_mode As Boolean = True)
        GotoState(MachineState.Shift_IR)
        If exit_mode Then
            tdo_bits = ShiftOut(tdi_bits, bit_count, True)
            Me.STATE = MachineState.Exit1_IR
        Else
            tdo_bits = ShiftOut(tdi_bits, bit_count, False)
        End If
    End Sub

    Private Function GetBytes_FromUint(ByVal input As UInt32, ByVal MinBits As Integer) As Byte()
        Dim current(3) As Byte
        current(0) = (input And &HFF000000) >> 24
        current(1) = (input And &HFF0000) >> 16
        current(2) = (input And &HFF00) >> 8
        current(3) = (input And &HFF)
        Dim MaxSize As Integer = Math.Ceiling(MinBits / 8)
        Dim out(MaxSize - 1) As Byte
        For i = 0 To MaxSize - 1
            out(out.Length - (1 + i)) = current(current.Length - (1 + i))
        Next
        Return out
    End Function

    Public Function ShiftOut(ByVal TDI_IN() As Byte, ByVal bit_count As UInt32, Optional ByVal exit_mode As Boolean = False) As Byte()
        Dim TotalBytes As UInt32 = Math.Ceiling(bit_count / 8)
        Dim TDO_OUT(TotalBytes - 1) As Byte
        Dim TDI_TOSEND(TotalBytes - 1) As Byte
        Dim TMS(TotalBytes - 1) As Byte
        If (TDI_IN IsNot Nothing) Then 'This writes the LAST byte in TDI_IN into the LAST byte of TDI_TOSEND
            Dim DestPointer As Integer = TDI_TOSEND.Length - 1
            For i = (TDI_IN.Length - 1) To 0 Step -1
                TDI_TOSEND(DestPointer) = TDI_IN(i) 'The last byte in the array
                DestPointer -= 1
                If DestPointer = -1 Then Exit For
            Next
        Else 'We are going to shift zeros out
        End If
        If exit_mode Then 'The last tms bit must be high
            Dim offset As UInt32 = (bit_count Mod 8)
            If offset = 0 Then
                TMS(0) = &H80
            Else
                TMS(0) = (1 << (offset - 1))
            End If
        End If
        RaiseEvent ShiftBits(bit_count, TDI_TOSEND, TMS, TDO_OUT)
        Return TDO_OUT
    End Function


End Class
