'==========================================================================
' (c) 2015  Total Phase, Inc.
'--------------------------------------------------------------------------
' Project : Promira Sample Code
' File    : i2c_eeprom.bas
'--------------------------------------------------------------------------
' Perform simple read and write operations to an I2C EEPROM device.
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

Module i2c_eeprom

    '==========================================================================
    ' CONSTANTS
    '==========================================================================
    Const APP_NAME As String = "com.totalphase.promact_is"
    Const I2C_BITRATE As Integer = 100  'kHz
    Const I2C_BUS_TIMEOUT As UShort = 150  'ms
    Const I2C_DEVICE As UShort = &H50


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
    ' MAIN PROGRAM
    '==========================================================================
    Sub i2c_eeprom_run()
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

        ' Set the bus lock timeout
        Dim bus_timeout As Long
        bus_timeout = Promact_isApi.ps_i2c_bus_timeout(channel, I2C_BUS_TIMEOUT)
        Console.WriteLine("Bus lock timeout set to " & bus_timeout & " ms")

        ' Write the offset and read the data
        Dim offset(0) As Byte
        Dim data(15) As Byte
        Dim result As Long
        Dim num As Integer

        offset(0) = 0
        Call Promact_isApi.ps_i2c_write(channel, I2C_DEVICE, PromiraI2cFlags.PS_I2C_NO_STOP, 1, offset, num)

        result = Promact_isApi.ps_i2c_read(channel, I2C_DEVICE, PromiraI2cFlags.PS_I2C_NO_FLAGS, 16, data, num)
        If result < 0 Then
            Console.WriteLine("i2c read error")
        Else
            Dim i As Integer
            Console.WriteLine("Read data bytes:")
            For i = 0 To 15
                Console.WriteLine(data(i))
            Next
        End If

        ' Close the device and exit
        dev_close(pm, conn, channel)
    End Sub
End Module
