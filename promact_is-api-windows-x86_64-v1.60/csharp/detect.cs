/*=========================================================================
| (c) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : detect.cs
|--------------------------------------------------------------------------
| Auto-detection test routine
|--------------------------------------------------------------------------
| Redistribution and use of this file in source and binary forms, with
| or without modification, are permitted.
|
| THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
| "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
| LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
| FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE
| COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
| INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
| BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
| LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
| CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
| LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
| ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
| POSSIBILITY OF SUCH DAMAGE.
 ========================================================================*/

using System;
using TotalPhase;


/*=========================================================================
| CLASS
 ========================================================================*/
public class PMDetect {

    /*=====================================================================
    | MAIN PROGRAM
     ====================================================================*/
    public static void Main (string[] args)
    {
        uint[] ips       = new uint[16];
        uint[] uniqueIds = new uint[16];
        uint[] statuses  = new uint[16];
        int    numElem   = 16;
        int    i;

        // Find all the attached devices
        int count = PromiraApi.pm_find_devices_ext(numElem, ips,
            numElem, uniqueIds, numElem, statuses);

        Console.WriteLine("{0} device(s) found:", count);
        if (count > numElem)  count = numElem;

        // Print the information on each device
        for (i = 0; i < count; ++i) {
            // Determine if the device is in-use
            string status = "(avail) ";
            if ((statuses[i] & PromiraApi.PM_DEVICE_NOT_FREE) != 0) {
                status = "(in-use)";
            }

            // Display device port number, in-use status, and serial number
            uint id = unchecked((uint)uniqueIds[i]);
            string ipstr = String.Format("{0:d}.{1:d}.{2:d}.{3:d}",
                ips[0] & 0xff, (ips[0] >> 8) & 0xff,
                (ips[0] >> 16) & 0xff, (ips[0] >> 24) & 0xff);

            Console.WriteLine("    ip = {0} {1} ({2:d4}-{3:d6})",
                ipstr, status, id / 1000000,
                id % 1000000);

            if ((statuses[i] & PromiraApi.PM_DEVICE_NOT_FREE) == 0) {
                int pm = PromiraApi.pm_open(ipstr);
                if (pm > 0) {
                    byte[] buf = new byte[256];
                    int ret = PromiraApi.pm_apps(pm, 256, buf);
                    if (ret > 0) {
                        string apps = System.Text.Encoding.UTF8.GetString(buf, 0, ret);
                        Console.WriteLine("    - apps = {0}", apps);
                    }
                    PromiraApi.pm_close(pm);
                }
            }
        }
    }
}
