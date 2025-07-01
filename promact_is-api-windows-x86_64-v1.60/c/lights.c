/*=========================================================================
| (c) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : lights.c
|--------------------------------------------------------------------------
| Flash the lights on the Promira I2C/SPI Activity Board.
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
// CONSTANTS
//=========================================================================
#define I2C_BITRATE 100 // kHz


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
        printf("Unable to open the channel on %s\n", ip);
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
// STATIC FUNCTIONS
//=========================================================================
static int flash_lights (PromiraChannelHandle channel)
{
    int res, i;
    u16 num_bytes;
    unsigned char data_out[16];

    // Configure I/O expander lines as outputs
    data_out[0] = 0x03;
    data_out[1] = 0x00;
    res = ps_i2c_write(channel, 0x38, PS_I2C_NO_FLAGS, 2, data_out,
        &num_bytes);
    if (res < 0)  return res;

    if (num_bytes == 0) {
        printf("error: slave device 0x38 not found:%d, %d\n", res, num_bytes);
        return 0;
    }

    // Turn lights on in sequence
    i = 0xff;
    do {
        i = ((i<<1) & 0xff);
        data_out[0] = 0x01;
        data_out[1] = i;
        res = ps_i2c_write(channel, 0x38, PS_I2C_NO_FLAGS, 2, data_out,
            &num_bytes);
        if (res < 0)  return res;
        ps_app_sleep_ms(70);
    } while (i != 0);

    // Leave lights on for 100 ms
    ps_app_sleep_ms(100);

    // Turn lights off in sequence
    i = 0x00;
    do {
        i = ((i<<1) | 0x01);
        data_out[0] = 0x01;
        data_out[1] = i;
        res = ps_i2c_write(channel, 0x38, PS_I2C_NO_FLAGS, 2, data_out,
            &num_bytes);
        if (res < 0)  return res;
        ps_app_sleep_ms(70);
    } while (i != 0xff);

    ps_app_sleep_ms(100);

    // Configure I/O expander lines as inputs
    data_out[0] = 0x03;
    data_out[1] = 0xff;
    res = ps_i2c_write(channel, 0x38, PS_I2C_NO_FLAGS, 2, data_out,
        &num_bytes);
    if (res < 0)  return res;

    return 0;
}


//=========================================================================
// MAIN PROGRAM
//=========================================================================
int main (int argc, char *argv[]) {
    Promira pm;
    PromiraConnectionHandle conn;
    PromiraChannelHandle channel;

    const char * ip;
    int   bitrate = 100;
    int   res     = 0;

    if (argc < 2) {
        printf("usage: lights IP\n");
        return 1;
    }

    ip = argv[1];

    // Open the device
    dev_open(ip, &pm, &conn, &channel);

    // Ensure that the I2C subsystem is enabled
    ps_app_configure(channel, PS_APP_CONFIG_SPI | PS_APP_CONFIG_I2C);

    // Enable the I2C bus pullup resistors
    ps_i2c_pullup(channel, PS_I2C_PULLUP_BOTH);

    // Power the board using the Promira adapter's power supply.
    ps_phy_target_power(channel, PS_PHY_TARGET_POWER_BOTH);

    // Set the bitrate
    bitrate = ps_i2c_bitrate(channel, I2C_BITRATE);
    printf("Bitrate set to %d kHz\n", bitrate);

    res = flash_lights(channel);
    if (res < 0)
        printf("error: %d\n", res);

    // Close the device and exit
    dev_close(pm, conn, channel);

    return 0;
}
