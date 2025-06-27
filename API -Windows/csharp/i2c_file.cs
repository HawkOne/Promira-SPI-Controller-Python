/*=========================================================================
| (c) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : i2c_file.cs
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

using System;
using System.IO;
using TotalPhase;


/*=========================================================================
| CLASS
 ========================================================================*/
public class PMI2cFile {

    /*=====================================================================
    | CONSTANTS
     ====================================================================*/
    public const int BUFFER_SIZE = 2048;
    public const int I2C_BITRATE = 400;


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
    | FUNCTIONS
     ====================================================================*/
    static void blastBytes (int channel, byte slave_addr, string filename)
    {
        FileStream file;
        int        trans_num = 0;
        byte[]     dataOut   = new byte[BUFFER_SIZE];
        ushort     num_bytes = 0;

        // Open the file
        try {
            file = new FileStream(filename, FileMode.Open, FileAccess.Read);
        }
        catch (Exception) {
            Console.WriteLine("Unable to open file '{0}'", filename);
            return;
        }

        while (file.Length != file.Position) {
            int numWrite, res;
            int i;

            // Read from the file
            numWrite = file.Read(dataOut, 0, BUFFER_SIZE);
            if (numWrite == 0)  break;

            if (numWrite < BUFFER_SIZE) {
                byte[] temp = new byte[numWrite];
                for (i = 0; i < numWrite; i++)
                    temp[i] = dataOut[i];
                dataOut = temp;
            }

            // Write the data to the bus
            res = Promact_isApi.ps_i2c_write(channel, slave_addr,
                                             PromiraI2cFlags.PS_I2C_NO_FLAGS,
                                             BUFFER_SIZE, dataOut,
                                             ref num_bytes);
            if (res < 0) {
                Console.WriteLine("error: {0}", res);
                                  // Promact_isApi.status_string(count));
                break;
            } else if (num_bytes == 0) {
                Console.WriteLine("error: no bytes written");
                Console.WriteLine("  are you sure you have the right slave " +
                                  "address?");
                break;
            } else if (num_bytes != numWrite) {
                Console.WriteLine("error: only a partial number of bytes " +
                                  "written");
                Console.WriteLine("  ({0}) instead of full ({1})",
                                  num_bytes, numWrite);
                break;
            }

            Console.WriteLine("*** Transaction #{0:d2}", trans_num);

            // Dump the data to the screen
            Console.WriteLine("Data written to device:");
            for (i = 0; i < num_bytes; ++i) {
                if ((i&0x0f) == 0)      Console.Write("\n{0:x4}:  ", i);
                Console.Write("{0:x2} ", dataOut[i] & 0xff);
                if (((i+1)&0x07) == 0)  Console.Write(" ");
            }
            Console.WriteLine();
            Console.WriteLine();

            ++trans_num;

            // Sleep a tad to make sure slave has time to process this request
            Promact_isApi.ps_app_sleep_ms(10);
        }

        file.Close();
    }


    /*=====================================================================
    | MAIN PROGRAM
     ====================================================================*/
    public static void Main (string[] args) {
        int pm      = 0;
        int conn    = 0;
        int channel = 0;

        string ip = "";
        byte addr = 0;
        int res   = 0;

        string filename;

        int bitrate;

        if (args.Length != 3) {
            Console.WriteLine("usage: i2c_file PORT SLAVE_ADDR filename");
            Console.WriteLine("  SLAVE_ADDR is the target slave address");
            Console.WriteLine();
            Console.WriteLine("  'filename' should contain data to be sent");
            Console.WriteLine("  to the downstream i2c device");
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

        // Parse the device address argument
        try {
            if (args[1].StartsWith("0x"))
                addr = Convert.ToByte(args[1].Substring(2), 16);
            else
                addr = Convert.ToByte(args[1]);
        }
        catch (Exception) {
            Console.WriteLine("Error: invalid device addr");
            return;
        }

        filename = args[2];

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

        // Setup the bitrate
        bitrate = Promact_isApi.ps_i2c_bitrate(channel, I2C_BITRATE);
        Console.WriteLine("Bitrate set to {0} kHz", bitrate);

        blastBytes(channel, addr, filename);

        // Close the device and exit
        DevClose(pm, conn, channel);
    }
}
