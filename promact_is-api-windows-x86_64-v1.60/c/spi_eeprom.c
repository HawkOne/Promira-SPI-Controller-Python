/*=========================================================================
| (c) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : spi_eeprom.c
|--------------------------------------------------------------------------
| Perform simple read and write operations to an SPI EEPROM device.
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
#define PAGE_SIZE 32
#define SS_MASK   0x1

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
static void _writeMemory (PromiraChannelHandle channel,
                          PromiraQueueHandle   queue,
                          u16                  addr,
                          u16                  length,
                          int                  zero)
{
    u16 i, n;
    PromiraCollectHandle collect;

    u08 data_out[3];
    u08 data[PAGE_SIZE];

    // Write to the SPI EEPROM
    //
    // The AT25080A EEPROM has 32 byte pages.  Data can written
    // in pages, to reduce the number of overall SPI transactions
    // executed through the Promira adapter.
    n = 0;
    while (n < length) {
        // Clear the queue
        ps_queue_clear(queue);

        // Send write enable command
        data_out[0] = 0x06;
        ps_queue_spi_ss(queue, SS_MASK);
        ps_queue_spi_write(queue, 0, 8, 1, data_out);
        ps_queue_spi_ss(queue, 0);

        // Assemble write command and address
        data_out[0] = 0x02;
        data_out[1] = ((addr + n) >> 8) & 0xff;
        data_out[2] = ((addr + n) >> 0) & 0xff;

        // Assemble a page of data
        i = 0;
        do {
            data[i++] = zero ? 0 : (u08)n;
            ++n;
        } while (n < length && ((addr + n) & (PAGE_SIZE - 1)));

        // Write the transaction
        ps_queue_spi_ss(queue, SS_MASK);
        ps_queue_spi_write(queue, 0, 8, 3, data_out);
        ps_queue_spi_write(queue, 0, 8, i, data);
        ps_queue_spi_ss(queue, 0);
        ps_queue_spi_delay_ns(queue, 10 * 1000 * 1000);

        collect = ps_queue_submit(queue, channel, 0, NULL);
        dev_collect(collect, 0, NULL);
    }
}


static void _readMemory (PromiraChannelHandle channel,
                         PromiraQueueHandle   queue,
                         u16                  addr,
                         u16                  length)
{
    int count;
    int i;
    PromiraCollectHandle collect;

    u08 data_out[3];
    u08 *data_in = (u08 *)malloc(length + 3);

    memset(data_in,  0, length + 3);

    // Clear the queue
    ps_queue_clear(queue);

    // Assemble read command and address
    data_out[0] = 0x03;
    data_out[1] = (addr >> 8) & 0xff;
    data_out[2] = (addr >> 0) & 0xff;

    // Write length+3 bytes for data plus command and 2 address bytes
    ps_queue_spi_ss(queue, SS_MASK);
    ps_queue_spi_write(queue, 0, 8, 3, data_out);
    ps_queue_spi_write_word(queue, 0, 8, length, 0);
    ps_queue_spi_ss(queue, 0);

    collect = ps_queue_submit(queue, channel, 0, NULL);
    count = dev_collect(collect, length + 3, data_in);

    if (count < 0) {
        printf("error: %d\n", count);
    }
    else if (count != length + 3) {
        printf("error: read %d bytes (expected %d)\n", count - 3, length);
    }

    // Dump the data to the screen
    printf("\nData read from device:");
    for (i = 0; i < length; ++i) {
        if ((i & 0x0f) == 0)
            printf("\n%04x:  ", addr + i);
        printf("%02x ", data_in[i + 3] & 0xff);
        if (((i + 1) & 0x07) == 0)
            printf(" ");
    }
    printf("\n");

    // Free the packet
    free(data_in);
}


//=========================================================================
// MAIN PROGRAM
//=========================================================================
int main (int argc, char *argv[]) {
    Promira pm;
    PromiraConnectionHandle conn;
    PromiraChannelHandle channel;
    PromiraQueueHandle queue;

    const char * ip;
    int bitrate = 100;
    int mode    = 0;
    u16 addr;
    u16 length;

    const char *command;

    if (argc < 7) {
        printf("usage: spi_eeprom IP BITRATE read  MODE ADDR LENGTH\n");
        printf("usage: spi_eeprom IP BITRATE write MODE ADDR LENGTH\n");
        printf("usage: spi_eeprom IP BITRATE zero  MODE ADDR LENGTH\n");
        return 1;
    }

    ip      = argv[1];
    bitrate = atoi(argv[2]);
    command = argv[3];
    mode    = atoi(argv[4]);
    addr    = (unsigned short)strtol(argv[5], 0, 0);
    length  = atoi(argv[6]);

    // Open the device
    dev_open(ip, &pm, &conn, &channel);

    // Ensure that the SPI subsystem is enabled.
    ps_app_configure(channel, PS_APP_CONFIG_SPI | PS_APP_CONFIG_I2C);

    // Power the EEPROM using the Promira adapter's power supply.
    ps_phy_target_power(channel, PS_PHY_TARGET_POWER_BOTH);

    // Setup the clock phase
    ps_spi_configure(channel, mode, PS_SPI_BITORDER_MSB, 0);

    // Configure SS
    ps_spi_enable_ss(channel, SS_MASK);

    // Set the bitrate
    bitrate = ps_spi_bitrate(channel, bitrate);
    printf("Bitrate set to %d kHz\n", bitrate);

    // Create a queue for SPI transactions
    queue = ps_queue_create(conn, PS_MODULE_ID_SPI_ACTIVE);

    // Enable master output
    spi_master_oe(channel, queue, 1);

    // Perform the operation
    if (strcmp(command, "write") == 0) {
        _writeMemory(channel, queue, addr, length, 0);
        printf("Wrote to EEPROM\n");
    }
    else if (strcmp(command, "read") == 0) {
        _readMemory(channel, queue, addr, length);
    }
    else if (strcmp(command, "zero") == 0) {
        _writeMemory(channel, queue, addr, length, 1);
        printf("Zeroed EEPROM\n");
    }
    else {
        printf("unknown command: %s\n", command);
    }

    // Disable master output
    spi_master_oe(channel, queue, 0);

    // Destroy the queue
    ps_queue_destroy(queue);

    // Close the device
    dev_close(pm, conn, channel);

    return 0;
}
