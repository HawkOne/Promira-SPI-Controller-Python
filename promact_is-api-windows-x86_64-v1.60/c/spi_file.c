/*=========================================================================
| (c) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : spi_file.c
|--------------------------------------------------------------------------
| Configure the device as an SPI master and send data.
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
#define SPI_BITRATE  20000
#define SS_MASK      0x1


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

static int dev_collect(PromiraCollectHandle collect, int size, u08 *buf) {
    int ret = 0, offset = 0;
    int t, length, result;
    if (collect < 0) {
        printf("%s\n", ps_app_status_string(collect));
        return collect;
    }

    while (1) {
        t = ps_collect_resp(collect, &length, &result, -1);
        if (t == PS_APP_NO_MORE_CMDS_TO_COLLECT)
            break;
        else if (t < 0) {
            printf("%s\n", ps_app_status_string(t));
            return t;
        }
        if (t == PS_SPI_CMD_READ) {
            if (size > offset && buf)
                ret = ps_collect_spi_read(collect, NULL,
                                          size - offset, &buf[offset]);

            offset += ret;
        }
    }
    return offset;
}

static void spi_master_oe(PromiraChannelHandle channel,
                          PromiraQueueHandle   queue,
                          u08                  enable) {
    PromiraCollectHandle collect;
    ps_queue_clear(queue);
    ps_queue_spi_oe(queue, enable);
    collect = ps_queue_submit(queue, channel, 0, NULL);
    dev_collect(collect, 0, NULL);
}


//=========================================================================
// FUNCTIONS
//=========================================================================
static u08 data_in[BUFFER_SIZE];
static u08 data_out[BUFFER_SIZE];

static void blast_bytes (PromiraChannelHandle channel,
                         PromiraQueueHandle   queue,
                         PromiraSpiIOMode     data_io,
                         char                *filename) {
    FILE *file;
    int trans_num = 0;
    PromiraCollectHandle collect;

    // Open the file
    file = fopen(filename, "rb");
    if (!file) {
        printf("Unable to open file '%s'\n", filename);
        return;
    }

    while (!feof(file)) {
        int num_write, count;
        int i;

        // Read from the file
        num_write = fread((void *)data_out, 1, BUFFER_SIZE, file);
        if (!num_write)
            break;

        // Clear the queue
        ps_queue_clear(queue);

        // Write the data to the bus
        ps_queue_spi_ss(queue, SS_MASK);
        ps_queue_spi_write(queue, data_io, 8, num_write, data_out);
        ps_queue_spi_ss(queue, 0);

        collect = ps_queue_submit(queue, channel, 0, NULL);
        count = dev_collect(collect, sizeof(data_in), data_in);
        if (count != num_write) {
            printf("error: only a partial number of bytes written\n");
            printf("  (%d) instead of full (%d)\n", count, num_write);
        }

        printf("*** Transaction #%02d\n", trans_num);

        // Dump the data to the screen
        /* printf("Data written to device:"); */
        /* for (i = 0; i < count; ++i) { */
        /*     if ((i & 0x0f) == 0) */
        /*         printf("\n%04x:  ", i); */
        /*     printf("%02x ", data_out[i] & 0xff); */
        /*     if (((i + 1) & 0x07) == 0) */
        /*         printf(" "); */
        /* } */
        /* printf("\n\n"); */

        // Dump the data to the screen
        printf("Data read from device:");
        for (i = 0; i < count; ++i) {
            if ((i & 0x0f) == 0)
                printf("\n%04x:  ", i);
            printf("%02x ", data_in[i] & 0xff);
            if (((i + 1) & 0x07) == 0)
                printf(" ");
        }
        printf("\n\n");

        ++trans_num;

        // Sleep a tad to make sure slave has time to process this request
        /* ps_app_sleep_ms(10); */
    }
}



//=========================================================================
// MAIN PROGRAM
//=========================================================================
int main (int argc, char *argv[]) {
    Promira pm;
    PromiraConnectionHandle conn;
    PromiraChannelHandle channel;
    PromiraChannelHandle queue;

    const char * ip;
    int data_io = 0;

    char *filename;

    int bitrate;

    if (argc < 4) {
        printf("usage: spi_file IP IO filename\n");
        printf("  IO : 0 - standard, 2 - dual, 4 - quad");
        printf("\n");
        printf("  'filename' should contain data to be sent\n");
        printf("  to the downstream spi device\n");
        return 1;
    }

    ip      = argv[1];
    data_io = atoi(argv[2]);
    filename = argv[3];

    // Open the device
    dev_open(ip, &pm, &conn, &channel);

    // Ensure that the SPI subsystem is enabled
    ps_app_configure(channel, PS_APP_CONFIG_SPI | PS_APP_CONFIG_I2C);

    // Power the EEPROM using the Promira adapter's power supply.
    ps_phy_target_power(channel, PS_PHY_TARGET_POWER_BOTH);

    // Setup the clock phase
    ps_spi_configure(channel, PS_SPI_MODE_0, PS_SPI_BITORDER_MSB, 0);

    // Configure SS
    ps_spi_enable_ss(channel, SS_MASK);

    // Set the bitrate
    bitrate = ps_spi_bitrate(channel, SPI_BITRATE);
    printf("Bitrate set to %d kHz\n", bitrate);

    // Create a queue for SPI transactions
    queue = ps_queue_create(conn, PS_MODULE_ID_SPI_ACTIVE);

    // Enable master output
    spi_master_oe(channel, queue, 1);

    // Perform the operation
    blast_bytes(channel, queue, data_io, filename);

    // Disable master output
    spi_master_oe(channel, queue, 0);

    // Destroy the queue
    ps_queue_destroy(queue);

    // Close the device
    dev_close(pm, conn, channel);

    return 0;
}
