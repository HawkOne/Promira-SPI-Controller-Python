/*=========================================================================
| (c) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : detect.c
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

//=========================================================================
// INCLUDES
//=========================================================================
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "promira.h"


//=========================================================================
// MAIN PROGRAM ENTRY POINT
//=========================================================================
int main (int argc, char *argv[])
{
    u32 ips[16];
    u32 unique_ids[16];
    u32 statuses[16];
    int nelem = 16;
    char ipstr[32];

    // Find all the attached devices
    int count = pm_find_devices_ext(nelem, ips, nelem, unique_ids,
                                    nelem, statuses);
    int i;

    printf("%d device(s) found:\n", count);

    // Print the information on each device
    if (count > nelem)  count = nelem;
    for (i = 0; i < count; ++i) {
        // Determine if the device is in-use
        const char *status = "(avail) ";
        if (statuses[i] & PM_DEVICE_NOT_FREE) {
            status = "(in-use)";
        }

        sprintf(ipstr, "%u.%u.%u.%u",
                ips[i] & 0xff, (ips[i] >> 8) & 0xff,
                (ips[i] >> 16) & 0xff, (ips[i] >> 24) & 0xff);

        // Display device ip address, in-use status, and serial number
        printf("    ip = %s %s (%04d-%06d)\n", ipstr, status,
               unique_ids[i] / 1000000, unique_ids[i] % 1000000);

        if (!(statuses[i] & PM_DEVICE_NOT_FREE)) {
            Promira pm = pm_open(ipstr);
            if (pm > 0) {
                unsigned char apps[256];
                pm_apps(pm, sizeof(apps), apps);
                printf("    - apps = %.*s\n", (int)sizeof(apps), apps);
                pm_close(pm);
            }
        }
    }

    return 0;
}
