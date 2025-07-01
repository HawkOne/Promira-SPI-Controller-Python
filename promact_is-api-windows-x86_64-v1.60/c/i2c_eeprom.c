/*=========================================================================
| (c) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : i2c_eeprom.c
|--------------------------------------------------------------------------
| Perform simple read and write operations to an I2C EEPROM device.
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
#define PAGE_SIZE   8
#define BUS_TIMEOUT 150  // ms


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
// FUNCTION
//=========================================================================
static void _writeMemory (PromiraChannelHandle channel, u08 device, u08 addr,
    u16 length, int zero)
{
    u16 i, n, num_written;
    int ret;
    u08 data_out[1+PAGE_SIZE];

    // Write to the I2C EEPROM
    //
    // The AT24C02 EEPROM has 8 byte pages.  Data can written
    // in pages, to reduce the number of overall I2C transactions
    // executed through the Promira adapter.
    n = 0;
    while (n < length) {
        // Fill the packet with data
        data_out[0] = addr;

        // Assemble a page of data
        i = 1;
        do {
            data_out[i++] = zero ? 0 : (u08)n;
            ++addr; ++n;
        } while ( n < length && (addr & (PAGE_SIZE-1)) );

        // Write the address and data
        ret = ps_i2c_write(channel, device, PS_I2C_NO_FLAGS, i, data_out,
                           &num_written);
        if (ret < 0) {
            printf("error: %d\n", ret);
            return;
        }
        if (num_written == 0) {
            printf("error: no bytes written\n");
            printf("  are you sure you have the right slave address?\n");
            return;
        }
        ps_app_sleep_ms(10);
    }
}


static void _readMemory (PromiraChannelHandle channel, u08 device, u08 addr,
    u16 length)
{
    int ret, i;
    u16 count;
    u08 *data_in = (u08 *)malloc(length);

    // Write the address
    ps_i2c_write(channel, device, PS_I2C_NO_STOP, 1, &addr, NULL);

    ret = ps_i2c_read(channel, device, PS_I2C_NO_FLAGS, length, data_in,
        &count);
    if (ret < 0) {
        printf("error: %d\n", ret);
        return;
    }
    if (count == 0) {
        printf("error: no bytes read\n");
        printf("  are you sure you have the right slave address?\n");
        return;
    }
    else if (count != length) {
        printf("error: read %d bytes (expected %d)\n", count, length);
    }

    // Dump the data to the screen
    printf("\nData read from device:");
    for (i = 0; i < count; ++i) {
        if ((i&0x0f) == 0)      printf("\n%04x:  ", addr+i);
        printf("%02x ", data_in[i] & 0xff);
        if (((i+1)&0x07) == 0)  printf(" ");
    }
    printf("\n");

    // Free the data_in pointer
    free(data_in);
}


//=========================================================================
// MAIN PROGRAM
//=========================================================================
int main (int argc, char *argv[]) {
    Promira pm;
    PromiraConnectionHandle conn;
    PromiraChannelHandle channel;

    const char * ip;
    int bitrate = 100;
    u08 device;
    u08 addr;
    u16 length;
    int bus_timeout;

    const char *command;

    if (argc < 7) {
        printf("usage: i2c_eeprom IP BITRATE read  SLAVE_ADDR OFFSET LENGTH\n");
        printf("usage: i2c_eeprom IP BITRATE write SLAVE_ADDR OFFSET LENGTH\n");
        printf("usage: i2c_eeprom IP BITRATE zero  SLAVE_ADDR OFFSET LENGTH\n");
        return 1;
    }

    ip      = argv[1];
    bitrate = atoi(argv[2]);
    command = argv[3];
    device  = (u08)strtol(argv[4], 0, 0);
    addr    = (u08)strtol(argv[5], 0, 0);
    length  = atoi(argv[6]);

    // Open the device
    dev_open(ip, &pm, &conn, &channel);

    // Ensure that the I2C subsystem is enabled
    ps_app_configure(channel, PS_APP_CONFIG_SPI | PS_APP_CONFIG_I2C);

    // Enable the I2C bus pullup resistors
    ps_i2c_pullup(channel, PS_I2C_PULLUP_BOTH);

    // Power the board using the Promira adapter's power supply.
    ps_phy_target_power(channel, PS_PHY_TARGET_POWER_BOTH);

    // Set the bitrate
    bitrate = ps_i2c_bitrate(channel, bitrate);
    printf("Bitrate set to %d kHz\n", bitrate);

    // Set the bus lock timeout
    bus_timeout = ps_i2c_bus_timeout(channel, BUS_TIMEOUT);
    printf("Bus lock timeout set to %d ms\n", bus_timeout);

    // Perform the operation
    if (strcmp(command, "write") == 0) {
        _writeMemory(channel, device, addr, length, 0);
        printf("Wrote to EEPROM\n");
    }
    else if (strcmp(command, "read") == 0) {
        _readMemory(channel, device, addr, length);
    }
    else if (strcmp(command, "zero") == 0) {
        _writeMemory(channel, device, addr, length, 1);
        printf("Zeroed EEPROM\n");
    }
    else {
        printf("unknown command: %s\n", command);
    }

    // Close the device and exit
    dev_close(pm, conn, channel);

    return 0;
}
