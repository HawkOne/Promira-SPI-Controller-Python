/*=========================================================================
| (c) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : spi_slave.c
|--------------------------------------------------------------------------
| Configure the device as an SPI slave and watch incoming data.
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
#define BUFFER_SIZE       65535
#define SLAVE_RESP_SIZE   26
#define INTERVAL_TIMEOUT  500
#define SS_MASK           0x1


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

    printf("Watching slave SPI data...\n");

    // Wait for data on bus
    result = ps_spi_slave_poll(channel, timeout_ms);
    if (result == PS_SPI_SLAVE_NO_DATA) {
        printf("No data available.\n");
        return;
    }

    printf("\n");

    // Loop until _spi_slave_read times out
    for (;;) {
        int num_read;

        if (result == PS_SPI_SLAVE_DATA) {
            // Read the SPI message.
            // This function has an internal timeout (see datasheet).
            // To use a variable timeout the function ps_spi_slave_poll could
            // be used for subsequent messages.
            num_read = ps_spi_slave_read(channel, NULL, BUFFER_SIZE, data_in);

            if (num_read < 0) {
                printf("error: %d\n", num_read);
                return;
            }
            else if (num_read == 0) {
                printf("No more data available from SPI master.\n");
                return;
            }
            else {
                int i;
                // Dump the data to the screen
                printf("*** Transaction #%02d\n", trans_num);
                printf("Data read from device:");
                for (i = 0; i < num_read; ++i) {
                    if ((i & 0x0f) == 0)
                        printf("\n%04x:  ", i);
                    printf("%02x ", data_in[i] & 0xff);
                    if (((i + 1) & 0x07) == 0)
                        printf(" ");
                }
                printf("\n\n");

                ++trans_num;
            }
        }
        else if (result == PS_SPI_SLAVE_DATA_LOST) {
            // Get number of packets lost since queue in the device is full
            result = ps_spi_slave_data_lost_stats(channel);

            if (result < 0) {
                printf("error: %d\n", result);
                return;
            }

            printf("*** Transaction #%02d\n", trans_num);
            printf("Number of packet Lost: %04d\n\n", result);
        }

        // Use ps_spi_slave_poll to wait for the next transaction
        result = ps_spi_slave_poll(channel, INTERVAL_TIMEOUT);
        if (result == PS_SPI_SLAVE_NO_DATA) {
            printf("No more data available from SPI master.\n");
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
    int data_io    = 0;
    int timeout_ms = 0;

    u08 slave_resp[SLAVE_RESP_SIZE];
    int i;

    if (argc < 4) {
        printf("usage: spi_slave IP IO TIMEOUT_MS\n");
        printf("  IO : 0 - standard, 2 - dual, 4 - quad");
        printf("\n");
        printf("  The timeout value specifies the time to\n");
        printf("  block until the first packet is received.\n");
        printf("  If the timeout is -1, the program will\n");
        printf("  block indefinitely.\n");
        return 1;
    }

    ip         = argv[1];
    data_io    = atoi(argv[2]);
    timeout_ms = atoi(argv[3]);

    // Open the device
    dev_open(ip, &pm, &conn, &channel);

    // Ensure that the SPI subsystem is enabled
    ps_app_configure(channel, PS_APP_CONFIG_SPI | PS_APP_CONFIG_I2C);

    // Disable the Promira adapter's power pins.
    ps_phy_target_power(channel, PS_PHY_TARGET_POWER_BOTH);

    // Setup the clock phase
    ps_spi_configure(channel, PS_SPI_MODE_0, PS_SPI_BITORDER_MSB, 0);

    // Configure SS
    ps_spi_enable_ss(channel, SS_MASK);

    // Set the slave response
    for (i = 0; i < SLAVE_RESP_SIZE; ++i)
        slave_resp[i] = 'A' + i;

    ps_spi_std_slave_set_resp(channel, SLAVE_RESP_SIZE, slave_resp);

    // Enable the slave
    ps_spi_slave_enable(channel, PS_SPI_SLAVE_MODE_STD);

    // Configure SPI slave
    ps_spi_std_slave_configure(channel, data_io, 0);

    // Set host read size
    // if one SPI transaction slave received is bigger than this,
    // it will be splited to many.
    ps_spi_slave_host_read_size(channel, BUFFER_SIZE);

    // Watch the SPI port
    dump(channel, timeout_ms);

    // Disable the slave
    ps_spi_slave_disable(channel);

    // Close the device and exit
    dev_close(pm, conn, channel);

    return 0;
}
