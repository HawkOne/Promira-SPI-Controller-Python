'==========================================================================
' (c) 2015  Total Phase, Inc.
'--------------------------------------------------------------------------
' Project : Promira Sample Code
' File    : detect.vb
'--------------------------------------------------------------------------
' Auto-detection test routine
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

Module detect

    '==========================================================================
    ' MAIN PROGRAM
    '==========================================================================
    Public Sub detect_run()
        Console.WriteLine("Detecting Promira platforms...")
        Dim num As Long
        Dim ips(15) As UInt32
        Dim uniqueIds(15) As UInt32
        Dim statuses(15) As UInt32

        ' Find all the attached devices
        num = PromiraApi.pm_find_devices_ext(16, ips, 16, uniqueIds,
                                             16, statuses)

        If num > 0 Then
            Console.WriteLine("Found " & num & " device(s)")

            Dim ip As UInt32
            Dim uid As UInt32

            Dim inuse As String
            Dim i As Long

            ' Print the information on each device
            For i = 0 To num - 1
                ip = ips(i)
                uid = uniqueIds(i)

                ' Determine if the device is in-use
                inuse = "   (avail)"
                If statuses(i) And PromiraApi.PM_DEVICE_NOT_FREE Then
                    inuse = "   (in-use)"
                End If

                Dim ipstr As String = String.Format("{0:d}.{1:d}.{2:d}.{3:d}",
                    ip And &HFF, ((ip >> 8) And &HFF),
                    ((ip >> 16) And &HFF), ((ip >> 24) And &HFF))

                ' Display device port number, in-use status, and serial number
                Console.WriteLine("    ip = {0} {1} ({2:d4}-{3:d6})",
                                  ipstr, inuse, CInt(uid / 1000000),
                                  uid Mod 1000000)

                If Not (statuses(i) And PromiraApi.PM_DEVICE_NOT_FREE) Then
                    Dim pm As Integer
                    Dim buf(256) As Byte
                    Dim apps As String
                    Dim ret As Integer

                    pm = PromiraApi.pm_open(ipstr)
                    If pm > 0 Then
                        ret = PromiraApi.pm_apps(pm, 256, buf)
                        If ret > 0 Then
                            apps = System.Text.Encoding.ASCII.GetString(buf, 0, ret)

                            Console.WriteLine("    - apps = {0}", Trim(apps))
                        End If
                        PromiraApi.pm_close(pm)
                    End If
                End If
            Next
        Else
            Console.WriteLine("No devices found.")
        End If
    End Sub
End Module
