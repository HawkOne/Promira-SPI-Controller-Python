'==========================================================================
' (c) 2015  Total Phase, Inc.
'--------------------------------------------------------------------------
' Project : Promira Sample Code
' File    : gpio.vb
'--------------------------------------------------------------------------
' Perform some simple GPIO operations with a single Promira platform.
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

Module gpio

    '==========================================================================
    ' CONSTANTS
    '==========================================================================
    Const APP_NAME As String = "com.totalphase.promact_is"


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
    Sub gpio_run()
        Dim pm As Integer
        Dim conn As Integer
        Dim channel As Integer
        Dim ip As String
        Dim i As Integer
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

        Console.WriteLine("Opened Promira adapter")

        ' Configure the Promira platform so all pins
        ' are now controlled by the GPIO subsystem
        Call Promact_isApi.ps_app_configure(channel, _
            Promact_isApi.PS_APP_CONFIG_GPIO)

        ' Turn off the external I2C line pullups
        Call Promact_isApi.ps_i2c_pullup(channel, Promact_isApi.PS_I2C_PULLUP_NONE)

        ' Make sure the charge has dissipated on those lines
        Call Promact_isApi.ps_gpio_set(channel, &H0)
        Call Promact_isApi.ps_gpio_direction(channel, &HFFFF)

        ' By default all GPIO pins are inputs.  Writing 1 to the
        ' bit position corresponding to the appropriate line will
        ' configure that line as an output
        Call Promact_isApi.ps_gpio_direction(channel, &H1 Or &H8)

        ' By default all GPIO outputs are logic low.  Writing a 1
        ' to the appropriate bit position will force that line
        ' high provided it is configured as an output.  If it is
        ' not configured as an output the line state will be
        ' cached such that if the direction later changed, the
        ' latest output value for the line will be enforced.
        Call Promact_isApi.ps_gpio_set(channel, &H1)
        Console.WriteLine("Setting GPIO0 to logic low")

        ' The get method will return the line states of all inputs.
        ' If a line is not configured as an input the value of
        ' that particular bit position in the mask will be 0.
        Dim val As Long
        val = Promact_isApi.ps_gpio_get(channel)

        ' Check the state of GPIO1
        If (val And &H2) Then
            Console.WriteLine("Read the GPIO1 line as logic high")
        Else
            Console.WriteLine("Read the GPIO1 line as logic low")
        End If

        ' Now reading the GPIO2 line should give a logic high,
        ' provided there is nothing attached to the Promira
        ' platform that is driving the pin low.
        val = Promact_isApi.ps_gpio_get(channel)
        If (val And &H4) Then
            Console.WriteLine("Read the GPIO2 line as logic high " & _
                              "(passive pullup)")
        Else
            Console.WriteLine("Read the GPIO2 line as logic low " & _
                              "(is pin driven low?)")
        End If

        ' Now do a 1000 gets from the GPIO to test performance
        Console.WriteLine("Doing 1000 iterations of GPIO read...")
        For i = 1 To 1000
            Promact_isApi.ps_gpio_get(channel)
        Next

        ' Demonstrate use of ps_gpio_change
        Call Promact_isApi.ps_gpio_direction(channel, &H0)
        Dim oldval As Long
        Dim newval As Long
        oldval = Promact_isApi.ps_gpio_get(channel)
        Console.WriteLine("Calling ps_gpio_change for 2 seconds...")
        newval = Promact_isApi.ps_gpio_change(channel, 2000)
        If (newval <> oldval) Then
            Console.WriteLine("  GPIO inputs changed.")
        Else
            Console.WriteLine("  GPIO inputs did not change.")
        End If

        ' Turn on the I2C line pullups since we are done
        Call Promact_isApi.ps_i2c_pullup(channel, Promact_isApi.PS_I2C_PULLUP_BOTH)

        ' Configure the Promira platform back to SPI/I2C mode.
        Call Promact_isApi.ps_app_configure(channel, Promact_isApi.PS_APP_CONFIG_I2C Or Promact_isApi.PS_APP_CONFIG_SPI)

        ' Close the device
        dev_close(pm, conn, channel)
        Console.WriteLine("Finished")
    End Sub
End Module
