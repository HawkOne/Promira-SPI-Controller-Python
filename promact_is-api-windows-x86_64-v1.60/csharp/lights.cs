/*=========================================================================
| (C) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : lights.cs
|--------------------------------------------------------------------------
| Flash the lights on the Aardvark I2C/SPI Activity Board.
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
using System.IO;
using TotalPhase;


/*=========================================================================
| CLASS
 ========================================================================*/
public class PMLights {

    /*=====================================================================
    | CONSTANTS
     ====================================================================*/
    private const int I2C_BITRATE = 100; // kHz


    /*=====================================================================
    | STATIC FUNCTIONS (APP)
     ====================================================================*/
    private const string APP_NAME = "com.totalphase.promact_is";

    private static int DevOpen (string ip,
                                ref int pm,
                                ref int conn,
                                ref int channel)
    {
        int ret;
        pm = PromiraApi.pm_open(ip);
        if (pm <= 0) {
            Console.WriteLine("Unable to open Promira platform on {0}\n", ip);
            Console.WriteLine("Error code = {0}\n", pm);
            return -1;
        }
        ret = PromiraApi.pm_load(pm, APP_NAME);
        if (ret < 0) {
            Console.WriteLine("Unable to load the application({0})\n",
                              APP_NAME);
            Console.WriteLine("Error code = {0}\n", ret);
            return -1;
        }

        conn = Promact_isApi.ps_app_connect(ip);
        if (conn <= 0) {
            Console.WriteLine("Unable to open the application on {0}\n", ip);
            Console.WriteLine("Error code = {0}\n", conn);
            return -1;
        }

        channel = Promact_isApi.ps_channel_open(conn);
        if (channel <= 0) {
            Console.WriteLine("Unable to open the channel on {0}\n", ip);
            Console.WriteLine("Error code = {0}\n", channel);
            return -1;
        }

        return 0;
    }

    private static void DevClose (int pm, int conn, int channel)
    {
        Promact_isApi.ps_channel_close(channel);
        Promact_isApi.ps_app_disconnect(conn);
        PromiraApi.pm_close(pm);
    }


    /*=====================================================================
    | STATIC FUNCTIONS
     ====================================================================*/
    public static int FlashLights (int channel)
    {
        int res, i;
        byte[] data_out = new byte[2];
        ushort num_bytes = 0;

        // Configure I/O expander lines as outputs
        data_out[0] = 0x03;
        data_out[1] = 0x00;
        res = Promact_isApi.ps_i2c_write(channel, 0x38,
                                         PromiraI2cFlags.PS_I2C_NO_FLAGS, 2,
                                         data_out, ref num_bytes);
        if (res < 0)  return res;

        if (num_bytes == 0) {
            Console.WriteLine("error: slave device 0x38 not found");
            return 0;
        }

        // Turn lights on in sequence
        i = 0xff;
        do {
            i = ((i<<1) & 0xff);
            data_out[0] = 0x01;
            data_out[1] = (byte)i;
            res = Promact_isApi.ps_i2c_write(channel, 0x38,
                                             PromiraI2cFlags.PS_I2C_NO_FLAGS, 2,
                                             data_out, ref num_bytes);
            if (res < 0)  return res;
            Promact_isApi.ps_app_sleep_ms(70);
        } while (i != 0);

        // Leave lights on for 100 ms
        Promact_isApi.ps_app_sleep_ms(100);

        // Turn lights off in sequence
        i = 0x00;
        do {
            i = ((i<<1) | 0x01);
            data_out[0] = 0x01;
            data_out[1] = (byte)i;
            res = Promact_isApi.ps_i2c_write(channel, 0x38,
                                             PromiraI2cFlags.PS_I2C_NO_FLAGS, 2,
                                             data_out, ref num_bytes);
            if (res < 0)  return res;
            Promact_isApi.ps_app_sleep_ms(70);
        } while (i != 0xff);

        Promact_isApi.ps_app_sleep_ms(100);

        // Configure I/O expander lines as inputs
        data_out[0] = 0x03;
        data_out[1] = 0xff;
        res = Promact_isApi.ps_i2c_write(channel, 0x38,
                                         PromiraI2cFlags.PS_I2C_NO_FLAGS, 2,
                                         data_out, ref num_bytes);
        if (res < 0)  return res;

        return 0;
    }


    /*=====================================================================
    | MAIN PROGRAM
     ====================================================================*/
    public static void Main (string[] args) {
        int pm      = 0;
        int conn    = 0;
        int channel = 0;

        string ip      = "";
        int    bitrate = 100;
        int    res     = 0;

        if (args.Length != 1) {
            Console.WriteLine("usage: lights IP");
            return;
        }

        // Parse the IP argument
        try {
            ip = Convert.ToString(args[0]);
        }
        catch (Exception) {
            Console.WriteLine("Error: invalid IP address");
            return;
        }

        // Open the device
        res = DevOpen(ip, ref pm, ref conn, ref channel);
        if (res < 0)
            return;

        // Ensure that the I2C subsystem is enabled
        Promact_isApi.ps_app_configure(channel,
                                       Promact_isApi.PS_APP_CONFIG_SPI |
                                       Promact_isApi.PS_APP_CONFIG_I2C);

        // Enable the I2C bus pullup resistors
        Promact_isApi.ps_i2c_pullup(channel,
                                    Promact_isApi.PS_I2C_PULLUP_BOTH);

        // Power the board using the Promira adapter's power supply.
        Promact_isApi.ps_phy_target_power(
            channel, Promact_isApi.PS_PHY_TARGET_POWER_BOTH);

        // Set the bitrate
        bitrate = Promact_isApi.ps_i2c_bitrate(channel, I2C_BITRATE);
        Console.WriteLine("Bitrate set to {0} kHz", bitrate);

        res = PMLights.FlashLights(channel);
        if (res < 0)
            Console.WriteLine("error: {0}", res);

        // Close the device and exit
        DevClose(pm, conn, channel);
    }
}
