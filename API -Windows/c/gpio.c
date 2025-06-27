/*=========================================================================
| (c) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : gpio.c
|--------------------------------------------------------------------------
| Perform some simple GPIO operations with a single Promira adapter.
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
#include "promact_is.h"


//=========================================================================
// FUNCTIONS (APP)
//=========================================================================
#define APP_NAME            "com.totalphase.promact_is"

static int dev_open(const char              *ip,
                    Promira                 *pm,
                    PromiraConnectionHandle *conn,
                    PromiraChannelHandle    *channel) {
    int ret;
    *pm = pm_open(ip);
    if (*pm <= 0) {
        printf("Unable to open Promira platform on %s\n", ip);
        printf("Error code = %d\n", *pm);
        exit(1);
    }
    ret = pm_load(*pm, APP_NAME);
    if (ret < 0) {
        printf("Unable to load the application(%s)\n", APP_NAME);
        printf("Error code = %d\n", ret);
        exit(1);
    }

    *conn = ps_app_connect(ip);
    if (*conn <= 0) {
        printf("Unable to open the application on %s\n", ip);
        printf("Error code = %d\n", *conn);
        exit(1);
    }

    *channel = ps_channel_open(*conn);
    if (*channel <= 0) {
        printf("Unable to open the channel222 on %s\n", ip);
        printf("Error code = %d\n", *channel);
        exit(1);
    }

    return 0;
}

static void dev_close(Promira                 pm,
                      PromiraConnectionHandle conn,
                      PromiraChannelHandle    channel) {
    ps_channel_close(channel);
    ps_app_disconnect(conn);
    pm_close(pm);
}


//=========================================================================
// MAIN PROGRAM
//=========================================================================
int main (int argc, char *argv[]) {
    Promira pm;
    PromiraConnectionHandle conn;
    PromiraChannelHandle channel;

    const char * ip;
    u08 val;

    if (argc < 2) {
        printf("usage: gpio IP\n");
        return 1;
    }

    ip      = argv[1];

    // Open the device
    dev_open(ip, &pm, &conn, &channel);

    // Configure the Promira adapter so all pins
    // are now controlled by the GPIO subsystem
    ps_app_configure(channel, PS_APP_CONFIG_GPIO);

    // Turn off the external I2C line pullups
    ps_i2c_pullup(channel, PS_I2C_PULLUP_NONE);

    // Make sure the charge has dissipated on those lines
    ps_gpio_set(channel, 0x0);
    ps_gpio_direction(channel, 0xffff);

    // By default all GPIO pins are inputs.  Writing 1 to the
    // bit position corresponding to the appropriate line will
    // configure that line as an output
    ps_gpio_direction(channel, 0x1 | 0x8);

    // By default all GPIO outputs are logic low.  Writing a 1
    // to the appropriate bit position will force that line
    // high provided it is configured as an output.  If it is
    // not configured as an output the line state will be
    // cached such that if the direction later changed, the
    // latest output value for the line will be enforced.
    ps_gpio_set(channel, 0x1);
    printf("Setting GPIO0 to logic low\n");

    // The get method will return the line states of all inputs.
    // If a line is not configured as an input the value of
    // that particular bit position in the mask will be 0.
    val = ps_gpio_get(channel);

    // Check the state of GPIO1
    if (val & 0x2)
        printf("Read the GPIO1 line as logic high\n");
    else
        printf("Read the GPIO1 line as logic low\n");

    // Now reading the GPIO2 line should give a logic high,
    // provided there is nothing attached to the Promira
    // adapter that is driving the pin low.
    val = ps_gpio_get(channel);
    if (val & 0x4)
        printf("Read the GPIO2 line as logic high (passive pullup)\n");
    else
        printf("Read the GPIO2 line as logic low (is pin driven low?)\n");


    // Now do a 1000 gets from the GPIO to test performance
    {
        int i;
        for (i=0; i<1000; ++i) {
            ps_gpio_get(channel);
        }
    }

    {
        u08 oldval, newval;

        // Demonstrate use of ps_gpio_change
        ps_gpio_direction(channel, 0x00);
        oldval = ps_gpio_get(channel);

        printf("Calling ps_gpio_change for 2 seconds...\n");
        newval = ps_gpio_change(channel, 2000);

        if (newval != oldval)
            printf("  GPIO inputs changed.\n");
        else
            printf("  GPIO inputs did not change.\n");
    }

    // Turn on the I2C line pullups since we are done
    ps_i2c_pullup(channel, PS_I2C_PULLUP_BOTH);

    // Configure the Promira adapter back to SPI/I2C mode.
    ps_app_configure(channel, PS_APP_CONFIG_SPI | PS_APP_CONFIG_I2C);

    // Close the device and exit
    dev_close(pm, conn, channel);

    return 0;
}
