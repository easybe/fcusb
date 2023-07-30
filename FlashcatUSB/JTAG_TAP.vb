'Implements a standard JTAG state-machine for a Test-Access-Port
'Used only by FlashcatUSB Classic. Professional contains internal TAP machine
'This module was developed by EmbeddedComputers.net, ALL-RIGHTS-RESERVED

Imports FlashcatUSB.JTAG

Public Class JTAG_STATE_CONTROLLER
    Public Event Shift_TDI(ByVal BitCount As UInt32, ByVal tdi_bits() As Byte, ByRef tdo_bits() As Byte, exit_tms As Boolean)
    Public Event Shift_TMS(ByVal BitCount As UInt32, ByVal tms_bits() As Byte)
    Public Property STATE As JTAG_MACHINE_STATE 'Is the current state of the JTAG machine

    Sub New()

    End Sub
    'Shift out bits on tms to move the state machine to our desired state (this code was auto-generated for performance)
    Public Sub GotoState(ByVal to_state As JTAG_MACHINE_STATE)
        If Me.STATE = to_state Then Exit Sub
        Dim tms_bits As UInt64 = 0
        Dim tms_count As Integer = 0
        Select Case Me.STATE
            Case JTAG_MACHINE_STATE.TestLogicReset
                Select Case to_state
                    Case JTAG_MACHINE_STATE.RunTestIdle
                        tms_bits = 0 : tms_count = 1 '0
                    Case JTAG_MACHINE_STATE.Select_DR
                        tms_bits = 2 : tms_count = 2 '10
                    Case JTAG_MACHINE_STATE.Capture_DR
                        tms_bits = 2 : tms_count = 3 '010
                    Case JTAG_MACHINE_STATE.Shift_DR
                        tms_bits = 2 : tms_count = 4 '0010
                    Case JTAG_MACHINE_STATE.Exit1_DR
                        tms_bits = 10 : tms_count = 4 '1010
                    Case JTAG_MACHINE_STATE.Pause_DR
                        tms_bits = 10 : tms_count = 5 '01010
                    Case JTAG_MACHINE_STATE.Exit2_DR
                        tms_bits = 42 : tms_count = 6 '101010
                    Case JTAG_MACHINE_STATE.Update_DR
                        tms_bits = 26 : tms_count = 5 '11010
                    Case JTAG_MACHINE_STATE.Select_IR
                        tms_bits = 6 : tms_count = 3 '110
                    Case JTAG_MACHINE_STATE.Capture_IR
                        tms_bits = 6 : tms_count = 4 '0110
                    Case JTAG_MACHINE_STATE.Shift_IR
                        tms_bits = 6 : tms_count = 5 '00110
                    Case JTAG_MACHINE_STATE.Exit1_IR
                        tms_bits = 22 : tms_count = 5 '10110
                    Case JTAG_MACHINE_STATE.Pause_IR
                        tms_bits = 22 : tms_count = 6 '010110
                    Case JTAG_MACHINE_STATE.Exit2_IR
                        tms_bits = 86 : tms_count = 7 '1010110
                    Case JTAG_MACHINE_STATE.Update_IR
                        tms_bits = 54 : tms_count = 6 '110110
                End Select
            Case JTAG_MACHINE_STATE.RunTestIdle
                Select Case to_state
                    Case JTAG_MACHINE_STATE.TestLogicReset
                        tms_bits = 7 : tms_count = 3 '111
                    Case JTAG_MACHINE_STATE.Select_DR
                        tms_bits = 1 : tms_count = 1 '1
                    Case JTAG_MACHINE_STATE.Capture_DR
                        tms_bits = 1 : tms_count = 2 '01
                    Case JTAG_MACHINE_STATE.Shift_DR
                        tms_bits = 1 : tms_count = 3 '001
                    Case JTAG_MACHINE_STATE.Exit1_DR
                        tms_bits = 5 : tms_count = 3 '101
                    Case JTAG_MACHINE_STATE.Pause_DR
                        tms_bits = 5 : tms_count = 4 '0101
                    Case JTAG_MACHINE_STATE.Exit2_DR
                        tms_bits = 21 : tms_count = 5 '10101
                    Case JTAG_MACHINE_STATE.Update_DR
                        tms_bits = 13 : tms_count = 4 '1101
                    Case JTAG_MACHINE_STATE.Select_IR
                        tms_bits = 3 : tms_count = 2 '11
                    Case JTAG_MACHINE_STATE.Capture_IR
                        tms_bits = 3 : tms_count = 3 '011
                    Case JTAG_MACHINE_STATE.Shift_IR
                        tms_bits = 3 : tms_count = 4 '0011
                    Case JTAG_MACHINE_STATE.Exit1_IR
                        tms_bits = 11 : tms_count = 4 '1011
                    Case JTAG_MACHINE_STATE.Pause_IR
                        tms_bits = 11 : tms_count = 5 '01011
                    Case JTAG_MACHINE_STATE.Exit2_IR
                        tms_bits = 43 : tms_count = 6 '101011
                    Case JTAG_MACHINE_STATE.Update_IR
                        tms_bits = 27 : tms_count = 5 '11011
                End Select
            Case JTAG_MACHINE_STATE.Select_DR
                Select Case to_state
                    Case JTAG_MACHINE_STATE.TestLogicReset
                        tms_bits = 3 : tms_count = 2 '11
                    Case JTAG_MACHINE_STATE.RunTestIdle
                        tms_bits = 3 : tms_count = 3 '011
                    Case JTAG_MACHINE_STATE.Capture_DR
                        tms_bits = 0 : tms_count = 1 '0
                    Case JTAG_MACHINE_STATE.Shift_DR
                        tms_bits = 0 : tms_count = 2 '00
                    Case JTAG_MACHINE_STATE.Exit1_DR
                        tms_bits = 2 : tms_count = 2 '10
                    Case JTAG_MACHINE_STATE.Pause_DR
                        tms_bits = 2 : tms_count = 3 '010
                    Case JTAG_MACHINE_STATE.Exit2_DR
                        tms_bits = 10 : tms_count = 4 '1010
                    Case JTAG_MACHINE_STATE.Update_DR
                        tms_bits = 6 : tms_count = 3 '110
                    Case JTAG_MACHINE_STATE.Select_IR
                        tms_bits = 1 : tms_count = 1 '1
                    Case JTAG_MACHINE_STATE.Capture_IR
                        tms_bits = 1 : tms_count = 2 '01
                    Case JTAG_MACHINE_STATE.Shift_IR
                        tms_bits = 1 : tms_count = 3 '001
                    Case JTAG_MACHINE_STATE.Exit1_IR
                        tms_bits = 5 : tms_count = 3 '101
                    Case JTAG_MACHINE_STATE.Pause_IR
                        tms_bits = 5 : tms_count = 4 '0101
                    Case JTAG_MACHINE_STATE.Exit2_IR
                        tms_bits = 21 : tms_count = 5 '10101
                    Case JTAG_MACHINE_STATE.Update_IR
                        tms_bits = 13 : tms_count = 4 '1101
                End Select
            Case JTAG_MACHINE_STATE.Capture_DR
                Select Case to_state
                    Case JTAG_MACHINE_STATE.TestLogicReset
                        tms_bits = 31 : tms_count = 5 '11111
                    Case JTAG_MACHINE_STATE.RunTestIdle
                        tms_bits = 3 : tms_count = 3 '011
                    Case JTAG_MACHINE_STATE.Select_DR
                        tms_bits = 7 : tms_count = 3 '111
                    Case JTAG_MACHINE_STATE.Shift_DR
                        tms_bits = 0 : tms_count = 1 '0
                    Case JTAG_MACHINE_STATE.Exit1_DR
                        tms_bits = 1 : tms_count = 1 '1
                    Case JTAG_MACHINE_STATE.Pause_DR
                        tms_bits = 1 : tms_count = 2 '01
                    Case JTAG_MACHINE_STATE.Exit2_DR
                        tms_bits = 5 : tms_count = 3 '101
                    Case JTAG_MACHINE_STATE.Update_DR
                        tms_bits = 3 : tms_count = 2 '11
                    Case JTAG_MACHINE_STATE.Select_IR
                        tms_bits = 15 : tms_count = 4 '1111
                    Case JTAG_MACHINE_STATE.Capture_IR
                        tms_bits = 15 : tms_count = 5 '01111
                    Case JTAG_MACHINE_STATE.Shift_IR
                        tms_bits = 15 : tms_count = 6 '001111
                    Case JTAG_MACHINE_STATE.Exit1_IR
                        tms_bits = 47 : tms_count = 6 '101111
                    Case JTAG_MACHINE_STATE.Pause_IR
                        tms_bits = 47 : tms_count = 7 '0101111
                    Case JTAG_MACHINE_STATE.Exit2_IR
                        tms_bits = 175 : tms_count = 8 '10101111
                    Case JTAG_MACHINE_STATE.Update_IR
                        tms_bits = 111 : tms_count = 7 '1101111
                End Select
            Case JTAG_MACHINE_STATE.Shift_DR
                Select Case to_state
                    Case JTAG_MACHINE_STATE.TestLogicReset
                        tms_bits = 31 : tms_count = 5 '11111
                    Case JTAG_MACHINE_STATE.RunTestIdle
                        tms_bits = 3 : tms_count = 3 '011
                    Case JTAG_MACHINE_STATE.Select_DR
                        tms_bits = 7 : tms_count = 3 '111
                    Case JTAG_MACHINE_STATE.Capture_DR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case JTAG_MACHINE_STATE.Exit1_DR
                        tms_bits = 1 : tms_count = 1 '1
                    Case JTAG_MACHINE_STATE.Pause_DR
                        tms_bits = 1 : tms_count = 2 '01
                    Case JTAG_MACHINE_STATE.Exit2_DR
                        tms_bits = 5 : tms_count = 3 '101
                    Case JTAG_MACHINE_STATE.Update_DR
                        tms_bits = 3 : tms_count = 2 '11
                    Case JTAG_MACHINE_STATE.Select_IR
                        tms_bits = 15 : tms_count = 4 '1111
                    Case JTAG_MACHINE_STATE.Capture_IR
                        tms_bits = 15 : tms_count = 5 '01111
                    Case JTAG_MACHINE_STATE.Shift_IR
                        tms_bits = 15 : tms_count = 6 '001111
                    Case JTAG_MACHINE_STATE.Exit1_IR
                        tms_bits = 47 : tms_count = 6 '101111
                    Case JTAG_MACHINE_STATE.Pause_IR
                        tms_bits = 47 : tms_count = 7 '0101111
                    Case JTAG_MACHINE_STATE.Exit2_IR
                        tms_bits = 175 : tms_count = 8 '10101111
                    Case JTAG_MACHINE_STATE.Update_IR
                        tms_bits = 111 : tms_count = 7 '1101111
                End Select
            Case JTAG_MACHINE_STATE.Exit1_DR
                Select Case to_state
                    Case JTAG_MACHINE_STATE.TestLogicReset
                        tms_bits = 15 : tms_count = 4 '1111
                    Case JTAG_MACHINE_STATE.RunTestIdle
                        tms_bits = 1 : tms_count = 2 '01
                    Case JTAG_MACHINE_STATE.Select_DR
                        tms_bits = 3 : tms_count = 2 '11
                    Case JTAG_MACHINE_STATE.Capture_DR
                        tms_bits = 3 : tms_count = 3 '011
                    Case JTAG_MACHINE_STATE.Shift_DR
                        tms_bits = 2 : tms_count = 3 '010
                    Case JTAG_MACHINE_STATE.Pause_DR
                        tms_bits = 0 : tms_count = 1 '0
                    Case JTAG_MACHINE_STATE.Exit2_DR
                        tms_bits = 2 : tms_count = 2 '10
                    Case JTAG_MACHINE_STATE.Update_DR
                        tms_bits = 1 : tms_count = 1 '1
                    Case JTAG_MACHINE_STATE.Select_IR
                        tms_bits = 7 : tms_count = 3 '111
                    Case JTAG_MACHINE_STATE.Capture_IR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case JTAG_MACHINE_STATE.Shift_IR
                        tms_bits = 7 : tms_count = 5 '00111
                    Case JTAG_MACHINE_STATE.Exit1_IR
                        tms_bits = 23 : tms_count = 5 '10111
                    Case JTAG_MACHINE_STATE.Pause_IR
                        tms_bits = 23 : tms_count = 6 '010111
                    Case JTAG_MACHINE_STATE.Exit2_IR
                        tms_bits = 87 : tms_count = 7 '1010111
                    Case JTAG_MACHINE_STATE.Update_IR
                        tms_bits = 55 : tms_count = 6 '110111
                End Select
            Case JTAG_MACHINE_STATE.Pause_DR
                Select Case to_state
                    Case JTAG_MACHINE_STATE.TestLogicReset
                        tms_bits = 31 : tms_count = 5 '11111
                    Case JTAG_MACHINE_STATE.RunTestIdle
                        tms_bits = 3 : tms_count = 3 '011
                    Case JTAG_MACHINE_STATE.Select_DR
                        tms_bits = 7 : tms_count = 3 '111
                    Case JTAG_MACHINE_STATE.Capture_DR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case JTAG_MACHINE_STATE.Shift_DR
                        tms_bits = 1 : tms_count = 2 '01
                    Case JTAG_MACHINE_STATE.Exit1_DR
                        tms_bits = 5 : tms_count = 3 '101
                    Case JTAG_MACHINE_STATE.Exit2_DR
                        tms_bits = 1 : tms_count = 1 '1
                    Case JTAG_MACHINE_STATE.Update_DR
                        tms_bits = 3 : tms_count = 2 '11
                    Case JTAG_MACHINE_STATE.Select_IR
                        tms_bits = 15 : tms_count = 4 '1111
                    Case JTAG_MACHINE_STATE.Capture_IR
                        tms_bits = 15 : tms_count = 5 '01111
                    Case JTAG_MACHINE_STATE.Shift_IR
                        tms_bits = 15 : tms_count = 6 '001111
                    Case JTAG_MACHINE_STATE.Exit1_IR
                        tms_bits = 47 : tms_count = 6 '101111
                    Case JTAG_MACHINE_STATE.Pause_IR
                        tms_bits = 47 : tms_count = 7 '0101111
                    Case JTAG_MACHINE_STATE.Exit2_IR
                        tms_bits = 175 : tms_count = 8 '10101111
                    Case JTAG_MACHINE_STATE.Update_IR
                        tms_bits = 111 : tms_count = 7 '1101111
                End Select
            Case JTAG_MACHINE_STATE.Exit2_DR
                Select Case to_state
                    Case JTAG_MACHINE_STATE.TestLogicReset
                        tms_bits = 15 : tms_count = 4 '1111
                    Case JTAG_MACHINE_STATE.RunTestIdle
                        tms_bits = 1 : tms_count = 2 '01
                    Case JTAG_MACHINE_STATE.Select_DR
                        tms_bits = 3 : tms_count = 2 '11
                    Case JTAG_MACHINE_STATE.Capture_DR
                        tms_bits = 3 : tms_count = 3 '011
                    Case JTAG_MACHINE_STATE.Shift_DR
                        tms_bits = 0 : tms_count = 1 '0
                    Case JTAG_MACHINE_STATE.Exit1_DR
                        tms_bits = 2 : tms_count = 2 '10
                    Case JTAG_MACHINE_STATE.Pause_DR
                        tms_bits = 2 : tms_count = 3 '010
                    Case JTAG_MACHINE_STATE.Update_DR
                        tms_bits = 1 : tms_count = 1 '1
                    Case JTAG_MACHINE_STATE.Select_IR
                        tms_bits = 7 : tms_count = 3 '111
                    Case JTAG_MACHINE_STATE.Capture_IR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case JTAG_MACHINE_STATE.Shift_IR
                        tms_bits = 7 : tms_count = 5 '00111
                    Case JTAG_MACHINE_STATE.Exit1_IR
                        tms_bits = 23 : tms_count = 5 '10111
                    Case JTAG_MACHINE_STATE.Pause_IR
                        tms_bits = 23 : tms_count = 6 '010111
                    Case JTAG_MACHINE_STATE.Exit2_IR
                        tms_bits = 87 : tms_count = 7 '1010111
                    Case JTAG_MACHINE_STATE.Update_IR
                        tms_bits = 55 : tms_count = 6 '110111
                End Select
            Case JTAG_MACHINE_STATE.Update_DR
                Select Case to_state
                    Case JTAG_MACHINE_STATE.TestLogicReset
                        tms_bits = 7 : tms_count = 3 '111
                    Case JTAG_MACHINE_STATE.RunTestIdle
                        tms_bits = 0 : tms_count = 1 '0
                    Case JTAG_MACHINE_STATE.Select_DR
                        tms_bits = 1 : tms_count = 1 '1
                    Case JTAG_MACHINE_STATE.Capture_DR
                        tms_bits = 1 : tms_count = 2 '01
                    Case JTAG_MACHINE_STATE.Shift_DR
                        tms_bits = 1 : tms_count = 3 '001
                    Case JTAG_MACHINE_STATE.Exit1_DR
                        tms_bits = 5 : tms_count = 3 '101
                    Case JTAG_MACHINE_STATE.Pause_DR
                        tms_bits = 5 : tms_count = 4 '0101
                    Case JTAG_MACHINE_STATE.Exit2_DR
                        tms_bits = 21 : tms_count = 5 '10101
                    Case JTAG_MACHINE_STATE.Select_IR
                        tms_bits = 3 : tms_count = 2 '11
                    Case JTAG_MACHINE_STATE.Capture_IR
                        tms_bits = 3 : tms_count = 3 '011
                    Case JTAG_MACHINE_STATE.Shift_IR
                        tms_bits = 3 : tms_count = 4 '0011
                    Case JTAG_MACHINE_STATE.Exit1_IR
                        tms_bits = 11 : tms_count = 4 '1011
                    Case JTAG_MACHINE_STATE.Pause_IR
                        tms_bits = 11 : tms_count = 5 '01011
                    Case JTAG_MACHINE_STATE.Exit2_IR
                        tms_bits = 43 : tms_count = 6 '101011
                    Case JTAG_MACHINE_STATE.Update_IR
                        tms_bits = 27 : tms_count = 5 '11011
                End Select
            Case JTAG_MACHINE_STATE.Select_IR
                Select Case to_state
                    Case JTAG_MACHINE_STATE.TestLogicReset
                        tms_bits = 1 : tms_count = 1 '1
                    Case JTAG_MACHINE_STATE.RunTestIdle
                        tms_bits = 1 : tms_count = 2 '01
                    Case JTAG_MACHINE_STATE.Select_DR
                        tms_bits = 5 : tms_count = 3 '101
                    Case JTAG_MACHINE_STATE.Capture_DR
                        tms_bits = 5 : tms_count = 4 '0101
                    Case JTAG_MACHINE_STATE.Shift_DR
                        tms_bits = 5 : tms_count = 5 '00101
                    Case JTAG_MACHINE_STATE.Exit1_DR
                        tms_bits = 21 : tms_count = 5 '10101
                    Case JTAG_MACHINE_STATE.Pause_DR
                        tms_bits = 21 : tms_count = 6 '010101
                    Case JTAG_MACHINE_STATE.Exit2_DR
                        tms_bits = 85 : tms_count = 7 '1010101
                    Case JTAG_MACHINE_STATE.Update_DR
                        tms_bits = 53 : tms_count = 6 '110101
                    Case JTAG_MACHINE_STATE.Capture_IR
                        tms_bits = 0 : tms_count = 1 '0
                    Case JTAG_MACHINE_STATE.Shift_IR
                        tms_bits = 0 : tms_count = 2 '00
                    Case JTAG_MACHINE_STATE.Exit1_IR
                        tms_bits = 2 : tms_count = 2 '10
                    Case JTAG_MACHINE_STATE.Pause_IR
                        tms_bits = 2 : tms_count = 3 '010
                    Case JTAG_MACHINE_STATE.Exit2_IR
                        tms_bits = 10 : tms_count = 4 '1010
                    Case JTAG_MACHINE_STATE.Update_IR
                        tms_bits = 6 : tms_count = 3 '110
                End Select
            Case JTAG_MACHINE_STATE.Capture_IR
                Select Case to_state
                    Case JTAG_MACHINE_STATE.TestLogicReset
                        tms_bits = 31 : tms_count = 5 '11111
                    Case JTAG_MACHINE_STATE.RunTestIdle
                        tms_bits = 3 : tms_count = 3 '011
                    Case JTAG_MACHINE_STATE.Select_DR
                        tms_bits = 7 : tms_count = 3 '111
                    Case JTAG_MACHINE_STATE.Capture_DR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case JTAG_MACHINE_STATE.Shift_DR
                        tms_bits = 7 : tms_count = 5 '00111
                    Case JTAG_MACHINE_STATE.Exit1_DR
                        tms_bits = 23 : tms_count = 5 '10111
                    Case JTAG_MACHINE_STATE.Pause_DR
                        tms_bits = 23 : tms_count = 6 '010111
                    Case JTAG_MACHINE_STATE.Exit2_DR
                        tms_bits = 87 : tms_count = 7 '1010111
                    Case JTAG_MACHINE_STATE.Update_DR
                        tms_bits = 55 : tms_count = 6 '110111
                    Case JTAG_MACHINE_STATE.Select_IR
                        tms_bits = 15 : tms_count = 4 '1111
                    Case JTAG_MACHINE_STATE.Shift_IR
                        tms_bits = 0 : tms_count = 1 '0
                    Case JTAG_MACHINE_STATE.Exit1_IR
                        tms_bits = 1 : tms_count = 1 '1
                    Case JTAG_MACHINE_STATE.Pause_IR
                        tms_bits = 1 : tms_count = 2 '01
                    Case JTAG_MACHINE_STATE.Exit2_IR
                        tms_bits = 5 : tms_count = 3 '101
                    Case JTAG_MACHINE_STATE.Update_IR
                        tms_bits = 3 : tms_count = 2 '11
                End Select
            Case JTAG_MACHINE_STATE.Shift_IR
                Select Case to_state
                    Case JTAG_MACHINE_STATE.TestLogicReset
                        tms_bits = 31 : tms_count = 5 '11111
                    Case JTAG_MACHINE_STATE.RunTestIdle
                        tms_bits = 3 : tms_count = 3 '011
                    Case JTAG_MACHINE_STATE.Select_DR
                        tms_bits = 7 : tms_count = 3 '111
                    Case JTAG_MACHINE_STATE.Capture_DR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case JTAG_MACHINE_STATE.Shift_DR
                        tms_bits = 7 : tms_count = 5 '00111
                    Case JTAG_MACHINE_STATE.Exit1_DR
                        tms_bits = 23 : tms_count = 5 '10111
                    Case JTAG_MACHINE_STATE.Pause_DR
                        tms_bits = 23 : tms_count = 6 '010111
                    Case JTAG_MACHINE_STATE.Exit2_DR
                        tms_bits = 87 : tms_count = 7 '1010111
                    Case JTAG_MACHINE_STATE.Update_DR
                        tms_bits = 55 : tms_count = 6 '110111
                    Case JTAG_MACHINE_STATE.Select_IR
                        tms_bits = 15 : tms_count = 4 '1111
                    Case JTAG_MACHINE_STATE.Capture_IR
                        tms_bits = 15 : tms_count = 5 '01111
                    Case JTAG_MACHINE_STATE.Exit1_IR
                        tms_bits = 1 : tms_count = 1 '1
                    Case JTAG_MACHINE_STATE.Pause_IR
                        tms_bits = 1 : tms_count = 2 '01
                    Case JTAG_MACHINE_STATE.Exit2_IR
                        tms_bits = 5 : tms_count = 3 '101
                    Case JTAG_MACHINE_STATE.Update_IR
                        tms_bits = 3 : tms_count = 2 '11
                End Select
            Case JTAG_MACHINE_STATE.Exit1_IR
                Select Case to_state
                    Case JTAG_MACHINE_STATE.TestLogicReset
                        tms_bits = 15 : tms_count = 4 '1111
                    Case JTAG_MACHINE_STATE.RunTestIdle
                        tms_bits = 1 : tms_count = 2 '01
                    Case JTAG_MACHINE_STATE.Select_DR
                        tms_bits = 3 : tms_count = 2 '11
                    Case JTAG_MACHINE_STATE.Capture_DR
                        tms_bits = 3 : tms_count = 3 '011
                    Case JTAG_MACHINE_STATE.Shift_DR
                        tms_bits = 3 : tms_count = 4 '0011
                    Case JTAG_MACHINE_STATE.Exit1_DR
                        tms_bits = 11 : tms_count = 4 '1011
                    Case JTAG_MACHINE_STATE.Pause_DR
                        tms_bits = 11 : tms_count = 5 '01011
                    Case JTAG_MACHINE_STATE.Exit2_DR
                        tms_bits = 43 : tms_count = 6 '101011
                    Case JTAG_MACHINE_STATE.Update_DR
                        tms_bits = 27 : tms_count = 5 '11011
                    Case JTAG_MACHINE_STATE.Select_IR
                        tms_bits = 7 : tms_count = 3 '111
                    Case JTAG_MACHINE_STATE.Capture_IR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case JTAG_MACHINE_STATE.Shift_IR
                        tms_bits = 2 : tms_count = 3 '010
                    Case JTAG_MACHINE_STATE.Pause_IR
                        tms_bits = 0 : tms_count = 1 '0
                    Case JTAG_MACHINE_STATE.Exit2_IR
                        tms_bits = 2 : tms_count = 2 '10
                    Case JTAG_MACHINE_STATE.Update_IR
                        tms_bits = 1 : tms_count = 1 '1
                End Select
            Case JTAG_MACHINE_STATE.Pause_IR
                Select Case to_state
                    Case JTAG_MACHINE_STATE.TestLogicReset
                        tms_bits = 31 : tms_count = 5 '11111
                    Case JTAG_MACHINE_STATE.RunTestIdle
                        tms_bits = 3 : tms_count = 3 '011
                    Case JTAG_MACHINE_STATE.Select_DR
                        tms_bits = 7 : tms_count = 3 '111
                    Case JTAG_MACHINE_STATE.Capture_DR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case JTAG_MACHINE_STATE.Shift_DR
                        tms_bits = 7 : tms_count = 5 '00111
                    Case JTAG_MACHINE_STATE.Exit1_DR
                        tms_bits = 23 : tms_count = 5 '10111
                    Case JTAG_MACHINE_STATE.Pause_DR
                        tms_bits = 23 : tms_count = 6 '010111
                    Case JTAG_MACHINE_STATE.Exit2_DR
                        tms_bits = 87 : tms_count = 7 '1010111
                    Case JTAG_MACHINE_STATE.Update_DR
                        tms_bits = 55 : tms_count = 6 '110111
                    Case JTAG_MACHINE_STATE.Select_IR
                        tms_bits = 15 : tms_count = 4 '1111
                    Case JTAG_MACHINE_STATE.Capture_IR
                        tms_bits = 15 : tms_count = 5 '01111
                    Case JTAG_MACHINE_STATE.Shift_IR
                        tms_bits = 1 : tms_count = 2 '01
                    Case JTAG_MACHINE_STATE.Exit1_IR
                        tms_bits = 5 : tms_count = 3 '101
                    Case JTAG_MACHINE_STATE.Exit2_IR
                        tms_bits = 1 : tms_count = 1 '1
                    Case JTAG_MACHINE_STATE.Update_IR
                        tms_bits = 3 : tms_count = 2 '11
                End Select
            Case JTAG_MACHINE_STATE.Exit2_IR
                Select Case to_state
                    Case JTAG_MACHINE_STATE.TestLogicReset
                        tms_bits = 15 : tms_count = 4 '1111
                    Case JTAG_MACHINE_STATE.RunTestIdle
                        tms_bits = 1 : tms_count = 2 '01
                    Case JTAG_MACHINE_STATE.Select_DR
                        tms_bits = 3 : tms_count = 2 '11
                    Case JTAG_MACHINE_STATE.Capture_DR
                        tms_bits = 3 : tms_count = 3 '011
                    Case JTAG_MACHINE_STATE.Shift_DR
                        tms_bits = 3 : tms_count = 4 '0011
                    Case JTAG_MACHINE_STATE.Exit1_DR
                        tms_bits = 11 : tms_count = 4 '1011
                    Case JTAG_MACHINE_STATE.Pause_DR
                        tms_bits = 11 : tms_count = 5 '01011
                    Case JTAG_MACHINE_STATE.Exit2_DR
                        tms_bits = 43 : tms_count = 6 '101011
                    Case JTAG_MACHINE_STATE.Update_DR
                        tms_bits = 27 : tms_count = 5 '11011
                    Case JTAG_MACHINE_STATE.Select_IR
                        tms_bits = 7 : tms_count = 3 '111
                    Case JTAG_MACHINE_STATE.Capture_IR
                        tms_bits = 7 : tms_count = 4 '0111
                    Case JTAG_MACHINE_STATE.Shift_IR
                        tms_bits = 0 : tms_count = 1 '0
                    Case JTAG_MACHINE_STATE.Exit1_IR
                        tms_bits = 2 : tms_count = 2 '10
                    Case JTAG_MACHINE_STATE.Pause_IR
                        tms_bits = 2 : tms_count = 3 '010
                    Case JTAG_MACHINE_STATE.Update_IR
                        tms_bits = 1 : tms_count = 1 '1
                End Select
            Case JTAG_MACHINE_STATE.Update_IR
                Select Case to_state
                    Case JTAG_MACHINE_STATE.TestLogicReset
                        tms_bits = 7 : tms_count = 3 '111
                    Case JTAG_MACHINE_STATE.RunTestIdle
                        tms_bits = 0 : tms_count = 1 '0
                    Case JTAG_MACHINE_STATE.Select_DR
                        tms_bits = 1 : tms_count = 1 '1
                    Case JTAG_MACHINE_STATE.Capture_DR
                        tms_bits = 1 : tms_count = 2 '01
                    Case JTAG_MACHINE_STATE.Shift_DR
                        tms_bits = 1 : tms_count = 3 '001
                    Case JTAG_MACHINE_STATE.Exit1_DR
                        tms_bits = 5 : tms_count = 3 '101
                    Case JTAG_MACHINE_STATE.Pause_DR
                        tms_bits = 5 : tms_count = 4 '0101
                    Case JTAG_MACHINE_STATE.Exit2_DR
                        tms_bits = 21 : tms_count = 5 '10101
                    Case JTAG_MACHINE_STATE.Update_DR
                        tms_bits = 13 : tms_count = 4 '1101
                    Case JTAG_MACHINE_STATE.Select_IR
                        tms_bits = 3 : tms_count = 2 '11
                    Case JTAG_MACHINE_STATE.Capture_IR
                        tms_bits = 3 : tms_count = 3 '011
                    Case JTAG_MACHINE_STATE.Shift_IR
                        tms_bits = 3 : tms_count = 4 '0011
                    Case JTAG_MACHINE_STATE.Exit1_IR
                        tms_bits = 11 : tms_count = 4 '1011
                    Case JTAG_MACHINE_STATE.Pause_IR
                        tms_bits = 11 : tms_count = 5 '01011
                    Case JTAG_MACHINE_STATE.Exit2_IR
                        tms_bits = 43 : tms_count = 6 '101011
                End Select
        End Select
        RaiseEvent Shift_TMS(tms_count, GetBytes_FromUint(tms_bits, tms_count))
        Me.STATE = to_state
    End Sub

    Public Sub ExitState()
        Select Case Me.STATE
            Case JTAG_MACHINE_STATE.TestLogicReset
            Case JTAG_MACHINE_STATE.RunTestIdle
                Me.STATE = JTAG_MACHINE_STATE.Select_DR
            Case JTAG_MACHINE_STATE.Select_DR
                Me.STATE = JTAG_MACHINE_STATE.Select_IR
            Case JTAG_MACHINE_STATE.Capture_DR
                Me.STATE = JTAG_MACHINE_STATE.Exit1_DR
            Case JTAG_MACHINE_STATE.Shift_DR
                Me.STATE = JTAG_MACHINE_STATE.Exit1_DR
            Case JTAG_MACHINE_STATE.Exit1_DR
                Me.STATE = JTAG_MACHINE_STATE.Update_DR
            Case JTAG_MACHINE_STATE.Pause_DR
                Me.STATE = JTAG_MACHINE_STATE.Exit2_DR
            Case JTAG_MACHINE_STATE.Exit2_DR
                Me.STATE = JTAG_MACHINE_STATE.Update_DR
            Case JTAG_MACHINE_STATE.Update_DR
                Me.STATE = JTAG_MACHINE_STATE.Select_DR
            Case JTAG_MACHINE_STATE.Select_IR
                Me.STATE = JTAG_MACHINE_STATE.TestLogicReset
            Case JTAG_MACHINE_STATE.Capture_IR
                Me.STATE = JTAG_MACHINE_STATE.Exit1_IR
            Case JTAG_MACHINE_STATE.Shift_IR
                Me.STATE = JTAG_MACHINE_STATE.Exit1_IR
            Case JTAG_MACHINE_STATE.Exit1_IR
                Me.STATE = JTAG_MACHINE_STATE.Update_IR
            Case JTAG_MACHINE_STATE.Pause_IR
                Me.STATE = JTAG_MACHINE_STATE.Exit2_IR
            Case JTAG_MACHINE_STATE.Exit2_IR
                Me.STATE = JTAG_MACHINE_STATE.Update_IR
            Case JTAG_MACHINE_STATE.Update_IR
                Me.STATE = JTAG_MACHINE_STATE.Select_DR
        End Select
    End Sub

    Public Sub Reset()
        RaiseEvent Shift_TMS(5, {255}) 'Sets machine state to TestLogicReset
        Me.STATE = JTAG_MACHINE_STATE.TestLogicReset
        GotoState(JTAG_MACHINE_STATE.Select_DR)
    End Sub

    Public Sub ShiftDR(ByVal tdi_bits() As Byte, ByRef tdo_bits() As Byte, ByVal bit_count As Integer, Optional exit_mode As Boolean = True)
        GotoState(JTAG_MACHINE_STATE.Shift_DR)
        If exit_mode Then
            tdo_bits = ShiftOut(tdi_bits, bit_count, True)
            Me.STATE = JTAG_MACHINE_STATE.Exit1_DR
        Else
            tdo_bits = ShiftOut(tdi_bits, bit_count, False)
        End If
    End Sub

    Public Sub ShiftIR(ByVal tdi_bits() As Byte, ByRef tdo_bits() As Byte, ByVal bit_count As Integer, Optional exit_mode As Boolean = True)
        GotoState(JTAG_MACHINE_STATE.Shift_IR)
        If exit_mode Then
            tdo_bits = ShiftOut(tdi_bits, bit_count, True)
            Me.STATE = JTAG_MACHINE_STATE.Exit1_IR
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
        Array.Reverse(TDI_IN)
        RaiseEvent Shift_TDI(bit_count, TDI_IN, TDO_OUT, exit_mode)
        Array.Reverse(TDO_OUT)
        Return TDO_OUT
    End Function


End Class
