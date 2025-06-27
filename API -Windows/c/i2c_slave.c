/*=========================================================================
| (c) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : i2c_slave.c
|--------------------------------------------------------------------------
| Configure the device as an I2C slave and watch incoming data.
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
#define BUFFER_SIZE      65535

#define SLAVE_RESP_SIZE     26

#define INTERVAL_TIMEOUT   500


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
static u08 data_in[BUFFER_SIZE];

static void dump (PromiraChannelHandle channel, int timeout_ms)
{
    int trans_num = 0;
    int result;
    u08 addr;
    u16 num_bytes;

    printf("Watching slave I2C data...\n");

    // Wait for data on bus
    result = ps_i2c_slave_poll(channel, timeout_ms);
    if (result == PS_I2C_SLAVE_NO_DATA) {
        printf("No data available.\n");
        return;
    }

    printf("\n");

    // Loop until ps_i2c_slave_poll times out
    for (;;) {
        // Read the I2C message.
        // This function has an internal timeout (see datasheet), though
        // since we have already checked for data using ps_i2c_slave_poll,
        // the timeout should never be exercised.
        if (result == PS_I2C_SLAVE_READ) {
            // Get data written by master
            result = ps_i2c_slave_read(channel, &addr,
                BUFFER_SIZE, data_in, &num_bytes);
            int i;

            if (result < 0) {
                printf("error: %d\n", result);
                return;
            }

            // Dump the data to the screen
            printf("*** Transaction #%02d\n", trans_num);
            printf("Data read from master:");
            for (i = 0; i < num_bytes; ++i) {
                if ((i&0x0f) == 0)      printf("\n%04x:  ", i);
                printf("%02x ", data_in[i] & 0xff);
                if (((i+1)&0x07) == 0)  printf(" ");
            }
            printf("\n\n");
        }

        else if (result == PS_I2C_SLAVE_WRITE) {
            // Get number of bytes written to master
            result = ps_i2c_slave_write_stats(channel, &addr, &num_bytes);

            if (result < 0) {
                printf("error: %d\n", result);
                return;
            }

            // Print status information to the screen
            printf("*** Transaction #%02d\n", trans_num);
            printf("Number of bytes written to master: %04d\n", num_bytes);

            printf("\n");
        }

        else {
            printf("error: non-I2C asynchronous message is pending\n");
            return;
        }

        ++trans_num;

        // Use ps_i2c_slave_poll to wait for the next transaction
        result = ps_i2c_slave_poll(channel, INTERVAL_TIMEOUT);
        if (result == PS_I2C_SLAVE_NO_DATA) {
            printf("No more data available from I2C master.\n");
            break;
        }
    }
}



//=========================================================================
// MAIN PROGRAM
//=========================================================================
int main (int argc, char *argv[]) {
    Promira pm;
    PromiraConnectionHandle conn;
    PromiraChannelHandle channel;

    const char * ip;
    u08 addr       = 0;
    int timeout_ms = 0;

    u08 slave_resp[SLAVE_RESP_SIZE];
    int i;

    if (argc < 4) {
        printf("usage: i2c_slave IP SLAVE_ADDR TIMEOUT_MS\n");
        printf("  SLAVE_ADDR is the slave address for this device\n");
        printf("\n");
        printf("  The timeout value specifies the time to\n");
        printf("  block until the first packet is received.\n");
        printf("  If the timeout is -1, the program will\n");
        printf("  block indefinitely.\n");
        return 1;
    }

    ip         = argv[1];
    addr       = (u08)strtol(argv[2], 0, 0);
    timeout_ms = atoi(argv[3]);

    // Open the device
    dev_open(ip, &pm, &conn, &channel);

    // Ensure that the I2C subsystem is enabled
    ps_app_configure(channel, PS_APP_CONFIG_SPI | PS_APP_CONFIG_I2C);

    // Enable the I2C bus pullup resistors
    ps_i2c_pullup(channel, PS_I2C_PULLUP_BOTH);

    // Disable the Promira adapter's power pins.
    ps_phy_target_power(channel, PS_PHY_TARGET_POWER_BOTH);

    // Set the slave response; this won't be used unless the master
    // reads bytes from the slave.
    for (i=0; i<SLAVE_RESP_SIZE; ++i)
        slave_resp[i] = 'A' + i;

    ps_i2c_slave_set_resp(channel, SLAVE_RESP_SIZE, slave_resp);

    // Enable the slave
    ps_i2c_slave_enable(channel, addr, 0, 0);

    // Watch the I2C port
    dump(channel, timeout_ms);

    // Disable the slave and close the device
    ps_i2c_slave_disable(channel);
    dev_close(pm, conn, channel);

    return 0;
}
