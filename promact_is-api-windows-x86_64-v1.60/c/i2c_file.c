/*=========================================================================
| (c) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : i2c_file.c
|--------------------------------------------------------------------------
| Configure the device as an I2C master and send data.
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
#define BUFFER_SIZE  2048
#define I2C_BITRATE   400


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
// FUNCTIONS
//=========================================================================
static u08 data_out[BUFFER_SIZE];

static void blast_bytes (PromiraChannelHandle channel, u08 slave_addr,
    char *filename)
{
    FILE *file;
    int trans_num = 0;

    // Open the file
    file = fopen(filename, "rb");
    if (!file) {
        printf("Unable to open file '%s'\n", filename);
        return;
    }

    while (!feof(file)) {
        int ret, num_write;
        u16 count;
        int i;

        // Read from the file
        num_write = fread((void *)data_out, 1, BUFFER_SIZE, file);
        if (!num_write)  break;

        // Write the data to the bus
        ret = ps_i2c_write(channel, slave_addr, PS_I2C_NO_FLAGS,
            (u16)num_write, data_out, &count);
        if (ret < 0) {
            printf("error: %d\n", ret);
            goto cleanup;
        } else if (count == 0) {
            printf("error: no bytes written\n");
            printf("  are you sure you have the right slave address?\n");
            goto cleanup;
        } else if (count != num_write) {
            printf("error: only a partial number of bytes written\n");
            printf("  (%d) instead of full (%d)\n", count, num_write);
            goto cleanup;
        }

        printf("*** Transaction #%02d\n", trans_num);

        // Dump the data to the screen
        printf("Data written to device:");
        for (i = 0; i < count; ++i) {
            if ((i&0x0f) == 0)      printf("\n%04x:  ", i);
            printf("%02x ", data_out[i] & 0xff);
            if (((i+1)&0x07) == 0)  printf(" ");
        }
        printf("\n\n");

        ++trans_num;

        // Sleep a tad to make sure slave has time to process this request
        ps_app_sleep_ms(10);
    }

cleanup:
    fclose(file);
}



//=========================================================================
// MAIN PROGRAM
//=========================================================================
int main (int argc, char *argv[]) {
    Promira pm;
    PromiraConnectionHandle conn;
    PromiraChannelHandle channel;

    const char * ip;
    u08 addr  = 0;

    char *filename;

    int bitrate;

    if (argc < 4) {
        printf("usage: i2c_file IP SLAVE_ADDR filename\n");
        printf("  SLAVE_ADDR is the target slave address\n");
        printf("\n");
        printf("  'filename' should contain data to be sent\n");
        printf("  to the downstream i2c device\n");
        return 1;
    }

    ip      = argv[1];
    addr    = (u08)strtol(argv[2], 0, 0);

    filename = argv[3];

    // Open the device
    dev_open(ip, &pm, &conn, &channel);

    // Ensure that the I2C subsystem is enabled
    ps_app_configure(channel, PS_APP_CONFIG_SPI | PS_APP_CONFIG_I2C);

    // Enable the I2C bus pullup resistors
    ps_i2c_pullup(channel, PS_I2C_PULLUP_BOTH);

    // Enable the Promira adapter's power pins.
    ps_phy_target_power(channel, PS_PHY_TARGET_POWER_BOTH);

    // Setup the bitrate
    bitrate = ps_i2c_bitrate(channel, I2C_BITRATE);
    printf("Bitrate set to %d kHz\n", bitrate);

    blast_bytes(channel, addr, filename);

    // Close the device
    dev_close(pm, conn, channel);

    return 0;
}
