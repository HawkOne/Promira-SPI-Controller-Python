'==========================================================================
' (c) 2015  Total Phase, Inc.
'--------------------------------------------------------------------------
' Project : Promira Sample Code
' File    : lights.vb
'--------------------------------------------------------------------------
' Flash the lights on the Aardvark I2C/SPI Activity Board.
'--------------------------------------------------------------------------
' Redistribution and use of this file in source and binary forms, with
' or without modification, are permitted.
'
' THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
' "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
' LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
' FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE
' COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
' INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
' BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
' LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
' CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
' LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
' ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
' POSSIBILITY OF SUCH DAMAGE.
'==========================================================================
Imports TotalPhase

Module lights

    '==========================================================================
    ' CONSTANTS
    '==========================================================================
    Const APP_NAME As String = "com.totalphase.promact_is"
    Const I2C_BITRATE As Integer = 100  'kHz


    '==========================================================================
    ' FUNCTIONS
    '==========================================================================
    Private Function find_device_ip() As String
        Dim num As Long
        Dim ips(15) As UInt32
        Dim uniqueIds(15) As UInt32
        Dim statuses(15) As UInt32

        ' Find all the attached devices
        num = PromiraApi.pm_find_devices_ext(16, ips, 16, uniqueIds,
                                             16, statuses)

        If num > 0 Then
            Dim ip As UInt32
            Dim i As Long

            ' Print the information on each device
            For i = 0 To num - 1
                ip = ips(i)

                ' Determine if the device is in-use
                If Not (statuses(i) And PromiraApi.PM_DEVICE_NOT_FREE) Then
                    Return (ip And &HFF) & "." & ((ip >> 8) And &HFF) & "." &
                           ((ip >> 16) And &HFF) & "." & ((ip >> 24) And &HFF)
                End If
            Next
        End If
        Return ""
    End Function

    Private Function dev_open(ip As String, ByRef pm As Integer,
                  ByRef conn As Integer, ByRef channel As Integer) As Integer
        Dim ret As Integer
        pm = PromiraApi.pm_open(ip)
        If pm <= 0 Then
            Console.WriteLine("Unable to open Promira platform on " & ip & "\n")
            Console.WriteLine("Error code = " & pm & "\n")
            Return -1
        End If
        ret = PromiraApi.pm_load(pm, APP_NAME)
        If ret < 0 Then
            Console.WriteLine("Unable to load the application(" &
                              APP_NAME & ")\n")
            Console.WriteLine("Error code = " & ret & "\n")
            Return -1
        End If

        conn = Promact_isApi.ps_app_connect(ip)
        If conn <= 0 Then
            Console.WriteLine("Unable to open the application on " & ip & "\n")
            Console.WriteLine("Error code = " & conn & "\n")
            Return -1
        End If

        channel = Promact_isApi.ps_channel_open(conn)
        If channel <= 0 Then
            Console.WriteLine("Unable to open the channel on " & ip & "\n")
            Console.WriteLine("Error code = " & channel & "\n")
            Return -1
        End If

        Return 0
    End Function

    Private Sub dev_close(pm As Integer, conn As Integer, channel As Integer)
        Promact_isApi.ps_channel_close(channel)
        Promact_isApi.ps_app_disconnect(conn)
        PromiraApi.pm_close(pm)
    End Sub


    '==========================================================================
    ' FUNCTIONS
    '==========================================================================
    Sub flash_lights(ByVal channel As Integer)
        Dim data_out(1) As Byte
        data_out(0) = 0
        data_out(1) = 0

        Dim res As Long
        Dim num As Integer

        ' Configure I/O expander lines as outputs
        data_out(0) = &H3
        data_out(1) = &H0
        res = Promact_isApi.ps_i2c_write(channel, &H38, PromiraI2cFlags.PS_I2C_NO_FLAGS, 2, data_out, num)
        If (res < 0) Then
            Exit Sub
        End If

        If (num = 0) Then
            Console.WriteLine("error: slave device 0x38 not found")
            Exit Sub
        End If

        ' Turn lights on in sequence
        Dim i As Byte
        i = &HFF
        While (i <> 0)
            i = (i * 2) And &HFF
            data_out(0) = &H1
            data_out(1) = i
            res = Promact_isApi.ps_i2c_write(channel, &H38, PromiraI2cFlags.PS_I2C_NO_FLAGS, 2, data_out, num)
            If (res < 0) Then
                Exit Sub
            End If
            Promact_isApi.ps_app_sleep_ms(70)
        End While

        ' Leave lights on for 100 ms
        Promact_isApi.ps_app_sleep_ms(100)

        ' Turn lights off in sequence
        i = &H0
        While (i <> &HFF)
            i = (i * 2) Or &H1
            data_out(0) = &H1
            data_out(1) = i
            res = Promact_isApi.ps_i2c_write(channel, &H38, PromiraI2cFlags.PS_I2C_NO_FLAGS, 2, data_out, num)
            If (res < 0) Then
                Exit Sub
            End If
            Promact_isApi.ps_app_sleep_ms(70)
        End While

        Promact_isApi.ps_app_sleep_ms(100)

        ' Configure I/O expander lines as inputs
        data_out(0) = &H3
        data_out(1) = &HFF
        res = Promact_isApi.ps_i2c_write(channel, &H38, PromiraI2cFlags.PS_I2C_NO_FLAGS, 2, data_out, num)
        If (res < 0) Then
            Exit Sub
        End If
    End Sub


    '==========================================================================
    ' MAIN PROGRAM
    '==========================================================================
    Sub lights_run()
        Dim pm As Integer
        Dim conn As Integer
        Dim channel As Integer
        Dim ip As String
        Dim res As Integer

        ' Find device
        ip = find_device_ip()
        If ip.Length = 0 Then
            Console.WriteLine("error: no available port")
            Exit Sub
        End If

        ' Open the device
        res = dev_open(ip, pm, conn, channel)
        If res < 0 Then
            Console.WriteLine("Unable to open Promira platform")
            Console.WriteLine("Error code = " & res)
            Exit Sub
        End If

        ' Ensure that the I2C subsystem is enabled
        Call Promact_isApi.ps_app_configure(channel, Promact_isApi.PS_APP_CONFIG_I2C Or Promact_isApi.PS_APP_CONFIG_SPI)

        ' Enable the I2C bus pullup resistors.
        Call Promact_isApi.ps_i2c_pullup(channel, Promact_isApi.PS_I2C_PULLUP_BOTH)

        ' Power the board using the Promira adapter's power supply.
        Call Promact_isApi.ps_phy_target_power(channel, Promact_isApi.PS_PHY_TARGET_POWER_BOTH)

        ' Set the bitrate
        Dim bitrate As Long
        bitrate = Promact_isApi.ps_i2c_bitrate(channel, I2C_BITRATE)
        Console.WriteLine("Bitrate set to " & bitrate & " kHz")

        flash_lights(channel)

        ' Close the device and exit
        dev_close(pm, conn, channel)
    End Sub
End Module
