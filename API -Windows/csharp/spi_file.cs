/*=========================================================================
| (c) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : spi_file.cs
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

using System;
using System.IO;
using TotalPhase;


/*=========================================================================
| CLASS
 ========================================================================*/
public class PMSpiFile {

    /*=====================================================================
    | CONSTANTS
     ====================================================================*/
    public const int BUFFER_SIZE = 2048;
    public const int SPI_BITRATE = 20000;
    public const int SS_MASK     = 0x1;


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

    private static int DevCollect (int collect, byte[] buf)
    {
        int ret = 0, offset = 0;
        int t, length = 0, result = 0;
        if (collect < 0) {
            Console.WriteLine(
                "{0}\n", Promact_isApi.ps_app_status_string(collect));
            return collect;
        }
        while (true) {
            t = Promact_isApi.ps_collect_resp(collect, ref length,
                                              ref result, -1);
            if (t == (int)PromiraAppStatus.PS_APP_NO_MORE_CMDS_TO_COLLECT)
                break;
            else if (t < 0) {
                Console.WriteLine(
                    "{0}\n", Promact_isApi.ps_app_status_string(t));
                return t;
            }
            if (t == (int)PromiraSpiCommand.PS_SPI_CMD_READ) {
                if (buf.Length > offset && buf.Length != 0) {
                    byte[] dataIn = new byte[length];
                    byte word_size = 0;
                    ret = Promact_isApi.ps_collect_spi_read(
                        collect, ref word_size,
                        (uint)(buf.Length - offset), dataIn);

                    Array.Copy(dataIn, 0, buf, offset, ret);
                }

                offset += ret;
            }
        }
        return offset;
    }

    private static void SpiMasterOE (int channel, int queue, byte enable)
    {
        int collect;
        byte queue_type = 0;
        Promact_isApi.ps_queue_clear(queue);
        Promact_isApi.ps_queue_spi_oe(queue, enable);
        collect = Promact_isApi.ps_queue_submit(
            queue, channel, 0, ref queue_type);
        DevCollect(collect, new byte[0]);
    }


    /*=====================================================================
    | FUNCTIONS
     ====================================================================*/
    static void blastBytes (int              channel,
                            int              queue,
                            PromiraSpiIOMode data_io,
                            string           filename)
    {
        FileStream file;
        int        trans_num = 0;
        byte[]     dataIn    = new byte[BUFFER_SIZE];
        byte[]     dataOut   = new byte[BUFFER_SIZE];
        int        collect;
        byte       queue_type = 0;

        // Open the file
        try {
            file = new FileStream(filename, FileMode.Open, FileAccess.Read);
        }
        catch (Exception) {
            Console.WriteLine("Unable to open file '{0}'", filename);
            return;
        }

        while (file.Length != file.Position) {
            int numWrite, count;
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

            // Clear the queue
            Promact_isApi.ps_queue_clear(queue);

            // Write the data to the bus
            Promact_isApi.ps_queue_spi_ss(queue, SS_MASK);
            Promact_isApi.ps_queue_spi_write(queue, data_io, 8,
                                             (uint)numWrite, dataOut);
            Promact_isApi.ps_queue_spi_ss(queue, 0);

            collect = Promact_isApi.ps_queue_submit(queue, channel, 0,
                                                    ref queue_type);
            count = DevCollect(collect, dataIn);
            if (count < 0) {
                Console.WriteLine("error: {0}", count);
                break;
            }
            else if (count != numWrite) {
                Console.WriteLine("error: only a partial number of bytes " +
                                  "written");
                Console.WriteLine("  ({0}) instead of full ({1})",
                                  count, numWrite);
                break;
            }

            Console.WriteLine("*** Transaction #{0:d2}", trans_num);

            // Dump the data to the screen
            // Console.Write("Data written to device:");
            // for (i = 0; i < count; ++i) {
            //     if ((i & 0x0f) == 0)
            //         Console.Write("\n{0:x4}:  ", i);
            //     Console.Write("{0:x2} ", dataOut[i] & 0xff);
            //     if (((i + 1) & 0x07) == 0)
            //         Console.Write(" ");
            // }
            // Console.WriteLine();
            // Console.WriteLine();

            // Dump the data to the screen
            Console.Write("Data read from device:");
            for (i = 0; i < count; ++i) {
                if ((i & 0x0f) == 0)
                    Console.Write("\n{0:x4}:  ", i);
                Console.Write("{0:x2} ", dataIn[i] & 0xff);
                if (((i + 1) & 0x07) == 0)
                    Console.Write(" ");
            }
            Console.WriteLine();
            Console.WriteLine();

            ++trans_num;

            // Sleep a tad to make sure slave has time to process this request
            // Promact_isApi.ps_app_sleep_ms(10);
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
        int queue   = 0;

        string ip   = "";
        int data_io = 0;
        int res     = 0;

        string filename;

        int bitrate;

        if (args.Length != 3) {
            Console.WriteLine("usage: spi_file IP IO filename");
            Console.WriteLine("  IO : 0 - standard, 2 - dual, 4 - quad");
            Console.WriteLine();
            Console.WriteLine("  'filename' should contain data to be sent");
            Console.WriteLine("  to the downstream spi device");
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

        filename = args[2];

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

        // Set the bitrate
        bitrate = Promact_isApi.ps_spi_bitrate(channel, SPI_BITRATE);
        Console.WriteLine("Bitrate set to {0} kHz", bitrate);

        // Create a queue for SPI transactions
        queue = Promact_isApi.ps_queue_create(
            conn, Promact_isApi.PS_MODULE_ID_SPI_ACTIVE);

        // Enable master output
        SpiMasterOE(channel, queue, 1);

        // Perform the operation
        blastBytes(channel, queue, (PromiraSpiIOMode)data_io, filename);

        // Disable master output
        SpiMasterOE(channel, queue, 0);

        // Destroy the queue
        Promact_isApi.ps_queue_destroy(queue);

        // Close the device and exit
        DevClose(pm, conn, channel);
    }
}
