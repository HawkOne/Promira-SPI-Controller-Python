'==========================================================================
' (c) 2015  Total Phase, Inc.
'--------------------------------------------------------------------------
' Project : Promira Sample Code
' File    : spi_eeprom.vb
'--------------------------------------------------------------------------
' Perform simple read and write operations to an SPI EEPROM device.
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

Module spi_eeprom

    '==========================================================================
    ' CONSTANTS
    '==========================================================================
    Const APP_NAME As String = "com.totalphase.promact_is"
    Const SPI_BITRATE As Integer = 1000  'kHz


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

    Private Function dev_collect(collect As Integer, buf() As Byte) As Integer
        Dim ret As Integer
        Dim offset As Integer
        Dim t As Integer
        Dim length As Integer
        Dim result As Integer
        Dim word_size As Byte

        offset = 0

        If (collect < 0) Then
            Console.WriteLine(Promact_isApi.ps_app_status_string(collect) & "\n")
            Return collect
        End If

        While (True)
            t = Promact_isApi.ps_collect_resp(collect, length, result, -1)
            If (t = PromiraAppStatus.PS_APP_NO_MORE_CMDS_TO_COLLECT) Then
                Exit While
            ElseIf (t < 0) Then
                Console.WriteLine(Promact_isApi.ps_app_status_string(t) & "\n")
                Return t
            End If
            If (t = PromiraSpiCommand.PS_SPI_CMD_READ) Then
                If (buf.Length > offset) Then
                    Dim dataIn(buf.Length - offset) As Byte
                    ret = Promact_isApi.ps_collect_spi_read(collect, word_size, buf.Length - offset, dataIn)
                    Call Array.Copy(dataIn, 0, buf, offset, ret)
                    offset += ret
                End If
            End If
        End While

        Return offset
    End Function

    Private Sub spi_master_oe(channel As Integer, queue As Integer, enable As Byte)
        Dim collect As Integer
        Dim queue_type As Byte
        Dim buf(0) As Byte

        Call Promact_isApi.ps_queue_clear(queue)
        Call Promact_isApi.ps_queue_spi_oe(queue, enable)
        collect = Promact_isApi.ps_queue_submit(queue, channel, 0, queue_type)
        dev_collect(collect, buf)
    End Sub


    '==========================================================================
    ' MAIN PROGRAM
    '==========================================================================
    Sub spi_eeprom_run()
        Dim pm As Integer
        Dim conn As Integer
        Dim channel As Integer
        Dim queue As Integer
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

        ' Ensure that the SPI subsystem is enabled
        Call Promact_isApi.ps_app_configure(channel, Promact_isApi.PS_APP_CONFIG_I2C Or Promact_isApi.PS_APP_CONFIG_SPI)

        ' Power the board using the Promira adapter's power supply.
        Call Promact_isApi.ps_phy_target_power(channel, Promact_isApi.PS_PHY_TARGET_POWER_BOTH)

        ' Setup the clock phase.
        Call Promact_isApi.ps_spi_configure(channel, PromiraSpiMode.PS_SPI_MODE_0, PromiraSpiBitorder.PS_SPI_BITORDER_MSB, 0)

        ' Configure SS
        Call Promact_isApi.ps_spi_enable_ss(channel, &H1)

        ' Set the bitrate
        Dim bitrate As Long
        bitrate = Promact_isApi.ps_spi_bitrate(channel, SPI_BITRATE)
        Console.WriteLine("Bitrate set to " & bitrate & " kHz")

        ' Create a queue for SPI transactions
        queue = Promact_isApi.ps_queue_create(conn, Promact_isApi.PS_MODULE_ID_SPI_ACTIVE)

        ' Enable master output
        Call spi_master_oe(channel, queue, 1)

        ' Write the offset and read the data
        Dim data_out(3) As Byte
        Dim data_in(18) As Byte
        Dim result As Long
        Dim collect As Integer
        Dim queue_type As Byte

        ' Set read command and address
        data_out(0) = &H3
        data_out(1) = 0
        data_out(2) = 0

        ' Write the transaction
        Call Promact_isApi.ps_queue_spi_ss(queue, &H1)
        Call Promact_isApi.ps_queue_spi_write(queue, PromiraSpiIOMode.PS_SPI_IO_STANDARD,
                                              8, 3, data_out)
        Call Promact_isApi.ps_queue_spi_write_word(queue, PromiraSpiIOMode.PS_SPI_IO_STANDARD,
                                                   8, 16, 0)
        Call Promact_isApi.ps_queue_spi_ss(queue, &H0)

        collect = Promact_isApi.ps_queue_submit(queue, channel, 0, queue_type)
        result = dev_collect(collect, data_in)

        If result < 0 Then
            Console.WriteLine("spi write error")
        Else
            Dim i As Integer
            Console.WriteLine("Read data bytes:")
            For i = 0 To 15
                ' First 3 bytes are command and address, so add 3
                Console.WriteLine(data_in(i + 3))
            Next
        End If

        ' Disable master output
        Call spi_master_oe(channel, queue, 0)

        ' Destroy the queue
        Call Promact_isApi.ps_queue_destroy(queue)

        ' Close the device and exit
        dev_close(pm, conn, channel)
    End Sub
End Module
