/*=========================================================================
| (c) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : spi_slave.cs
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

using System;
using TotalPhase;


/*=========================================================================
| CLASS
 ========================================================================*/
public class PMSpiSlave {

    /*=====================================================================
    | CONSTANTS
     ====================================================================*/
    public const int BUFFER_SIZE      = 65535;
    public const int SLAVE_RESP_SIZE  = 26;
    public const int INTERVAL_TIMEOUT = 500;
    public const int SS_MASK          = 0x1;

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
    static void dump (int channel, int timeout_ms)
    {
        int    transNum = 0;
        int    result;
        byte[] dataIn = new byte[BUFFER_SIZE];
        Promact_isApi.PromiraSpiSlaveReadInfo read_info =
            new Promact_isApi.PromiraSpiSlaveReadInfo();

        Console.WriteLine("Watching slave SPI data...");

        // Wait for data on bus
        result = Promact_isApi.ps_spi_slave_poll(channel, timeout_ms);
        if (result == (int)PromiraSpiSlaveStatus.PS_SPI_SLAVE_NO_DATA) {
            Console.WriteLine("No data available.");
            return;
        }
        Console.WriteLine();

        // Loop until aa_spi_slave_read times out
        for (;;) {
            int numRead;

            if (result == (int)PromiraSpiSlaveStatus.PS_SPI_SLAVE_DATA) {
                // Read the SPI message.
                // This function has an internal timeout (see datasheet).
                // To use a variable timeout the function aa_async_poll could
                // be used for subsequent messages.
                numRead = Promact_isApi.ps_spi_slave_read(
                    channel, ref read_info, (uint)dataIn.Length, dataIn);

                if (numRead < 0) {
                    Console.WriteLine("error: {0}", numRead);
                    return;
                }
                else if (numRead == 0) {
                    Console.WriteLine("No more data available from " +
                                      "SPI master.");
                    return;
                }
                else {
                    int i;
                    // Dump the data to the screen
                    Console.WriteLine("*** Transaction #{0:d2}", transNum);
                    Console.Write("Data read from device:");
                    for (i = 0; i < numRead; ++i) {
                        if ((i & 0x0f) == 0)
                            Console.Write("\n{0:x4}:  ", i);
                        Console.Write("{0:x2} ", dataIn[i] & 0xff);
                        if (((i + 1) & 0x07) == 0)
                            Console.Write(" ");
                    }
                    Console.WriteLine();
                    Console.WriteLine();

                    ++transNum;
                }
            }
            else if (result ==
                     (int)PromiraSpiSlaveStatus.PS_SPI_SLAVE_DATA_LOST) {
                // Get number of packets lost since queue in the device is full
                result = Promact_isApi.ps_spi_slave_data_lost_stats(channel);

                if (result < 0) {
                    Console.WriteLine("error: {0}", result);
                    return;
                }

                Console.WriteLine("*** Transaction #{0:d2}", transNum);
                Console.WriteLine("Number of packet Lost: {0:d4}", result);
                Console.WriteLine();
            }

            // Use ps_spi_slave_poll to wait for the next transaction
            result = Promact_isApi.ps_spi_slave_poll(channel, INTERVAL_TIMEOUT);
            if (result == (int)PromiraSpiSlaveStatus.PS_SPI_SLAVE_NO_DATA) {
                Console.WriteLine("No more data available from SPI master.\n");
                break;
            }
        }
    }


    /*=====================================================================
    | MAIN PROGRAM
     ====================================================================*/
    static public void Main (string[] args) {
        int pm      = 0;
        int conn    = 0;
        int channel = 0;

        string ip      = "";
        int data_io    = 0;
        int timeoutMs  = 0;
        int res        = 0;

        byte[] slaveResp = new byte[SLAVE_RESP_SIZE];
        int i;

        if (args.Length != 3) {
            Console.WriteLine("usage: spi_slave IP IO TIMEOUT_MS");
            Console.WriteLine("  IO : 0 - standard, 2 - dual, 4 - quad");
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

        // Parse the IO argument
        try {
            data_io = Convert.ToInt32(args[1]);
        }
        catch (Exception) {
            Console.WriteLine("Error: invalid IO");
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

        // Ensure that the SPI subsystem is enabled.
        Promact_isApi.ps_app_configure(channel,
                                       Promact_isApi.PS_APP_CONFIG_SPI |
                                       Promact_isApi.PS_APP_CONFIG_I2C);

        // Power the board using the Promira adapter's power supply.
        Promact_isApi.ps_phy_target_power(
            channel, Promact_isApi.PS_PHY_TARGET_POWER_BOTH);

        // Setup the clock phase
        Promact_isApi.ps_spi_configure(channel,
                                       PromiraSpiMode.PS_SPI_MODE_0,
                                       PromiraSpiBitorder.PS_SPI_BITORDER_MSB,
                                       0);

        // Configure SS
        Promact_isApi.ps_spi_enable_ss(channel, SS_MASK);

        // Set the slave response
        for (i = 0; i < SLAVE_RESP_SIZE; ++i)
            slaveResp[i] = (byte)('A' + i);

        Promact_isApi.ps_spi_std_slave_set_resp(channel, SLAVE_RESP_SIZE,
                                                slaveResp);

        // Enable the slave
        Promact_isApi.ps_spi_slave_enable(
            channel, PromiraSlaveMode.PS_SPI_SLAVE_MODE_STD);

        // Configure SPI slave
        Promact_isApi.ps_spi_std_slave_configure(
            channel, (PromiraSpiIOMode)data_io, 0);

        // Set host read size
        // if one SPI transaction slave received is bigger than this,
        // it will be splited to many.
        Promact_isApi.ps_spi_slave_host_read_size(channel, BUFFER_SIZE);

        // Watch the SPI port
        dump(channel, timeoutMs);

        // Disable the slave
        Promact_isApi.ps_spi_slave_disable(channel);

        // Close the device and exit
        DevClose(pm, conn, channel);
    }
}
