/*=========================================================================
| (c) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : i2c_slave.cs
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

using System;
using TotalPhase;


/*=========================================================================
| CLASS
 ========================================================================*/
public class PMI2cSlave {

    /*=====================================================================
    | CONSTANTS
     ====================================================================*/
    public const int BUFFER_SIZE      = 65535;
    public const int SLAVE_RESP_SIZE  = 26;
    public const int INTERVAL_TIMEOUT = 500;


    /*=====================================================================
    | STATIC FUNCTIONS (APP)
     ====================================================================*/
    private const string APP_NAME = "com.totalphase.promact_is";

    private static int DevOpen (string ip, ref int pm,
                                ref int conn, ref int channel)
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
    static void dump (int channel, int timeout_ms)
    {
        int    result;
        byte   addr     = 0;
        int    transNum = 0;
        byte[] dataIn   = new byte[BUFFER_SIZE];
        ushort numBytes = 0;

        Console.WriteLine("Watching slave I2C data...");

        // Wait for data on bus
        result = Promact_isApi.ps_i2c_slave_poll(channel, timeout_ms);
        if (result == (int)PromiraI2cSlaveStatus.PS_I2C_SLAVE_NO_DATA) {
            Console.WriteLine("No data available.");
            return;
        }
        Console.WriteLine();

        // Loop until aa_async_poll times out
        for (;;) {
            // Read the I2C message.
            // This function has an internal timeout (see datasheet), though
            // since we have already checked for data using aa_async_poll,
            // the timeout should never be exercised.
            if (result == (int)PromiraI2cSlaveStatus.PS_I2C_SLAVE_READ) {
                // Get data written by master
                int res = Promact_isApi.ps_i2c_slave_read(
                    channel, ref addr, unchecked((ushort)BUFFER_SIZE),
                    dataIn, ref numBytes);
                int i;

                if (res < 0) {
                    Console.WriteLine("error: {0}", res);
                                      // Promact_isApi.status_string(numBytes));
                    return;
                }

                // Dump the data to the screen
                Console.WriteLine("*** Transaction #{0:d2}", transNum);
                Console.Write("Data read from master:");
                for (i = 0; i < numBytes; ++i) {
                    if ((i&0x0f) == 0)      Console.Write("\n{0:x4}:  ", i);
                    Console.Write("{0:x2} ", dataIn[i] & 0xff);
                    if (((i+1)&0x07) == 0)  Console.Write(" ");
                }
                Console.WriteLine();
                Console.WriteLine();
            }

            else if (result == (int)PromiraI2cSlaveStatus.PS_I2C_SLAVE_WRITE) {
                // Get number of bytes written to master
                int res = Promact_isApi.ps_i2c_slave_write_stats(
                    channel, ref addr, ref numBytes);

                if (res < 0) {
                    Console.WriteLine("error: {0}", res);
                                      // Promact_isApi.status_string(numBytes));
                    return;
                }

                // Print status information to the screen
                Console.WriteLine("*** Transaction #{0:d2}", transNum);
                Console.WriteLine("Number of bytes written to master: {0:d4}",
                                  numBytes);
                Console.WriteLine();
            }

            else {
                Console.WriteLine("error: non-I2C asynchronous message is " +
                                  "pending");
                return;
            }

            ++transNum;

            // Use aa_async_poll to wait for the next transaction
            result = Promact_isApi.ps_i2c_slave_poll(channel,
                                                     INTERVAL_TIMEOUT);
            if (result == (int)PromiraI2cSlaveStatus.PS_I2C_SLAVE_NO_DATA) {
                Console.WriteLine("No more data available from I2C master.");
                break;
            }
        }
    }


    /*=====================================================================
    | MAIN PROGRAM
     ====================================================================*/
    public static void Main (string[] args) {
        int pm      = 0;
        int conn    = 0;
        int channel = 0;

        string ip      = "";
        byte addr      = 0;
        int  timeoutMs = 0;

        byte[] slaveResp = new byte[SLAVE_RESP_SIZE];
        int i, res;

        if (args.Length != 3) {
            Console.WriteLine("usage: i2c_slave IP SLAVE_ADDR TIMEOUT_MS");
            Console.WriteLine("  SLAVE_ADDR is the slave address for this device");
            Console.WriteLine();
            Console.WriteLine("  The timeout value specifies the time to");
            Console.WriteLine("  block until the first packet is received.");
            Console.WriteLine("  If the timeout is -1, the program will");
            Console.WriteLine("  block indefinitely.");
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

        // Parse the timeout argument
        try {
            timeoutMs = Convert.ToInt32(args[2]);
        }
        catch (Exception) {
            Console.WriteLine("Error: invalid timeout value");
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

        // Disable the Promira adapter's power pins.
        Promact_isApi.ps_phy_target_power(
            channel, Promact_isApi.PS_PHY_TARGET_POWER_NONE);

        // Set the slave response; this won't be used unless the master
        // reads bytes from the slave.
        for (i = 0; i < SLAVE_RESP_SIZE; ++i)
            slaveResp[i] = (byte)('A' + i);

        Promact_isApi.ps_i2c_slave_set_resp(channel, SLAVE_RESP_SIZE,
                                            slaveResp);

        // Enable the slave
        Promact_isApi.ps_i2c_slave_enable(channel, addr, 0, 0);

        // Watch the I2C port
        dump(channel, timeoutMs);

        // Disable the slave and close the device
        Promact_isApi.ps_i2c_slave_disable(channel);
        DevClose(pm, conn, channel);
    }
}
