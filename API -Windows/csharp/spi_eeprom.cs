/*=========================================================================
| (c) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : spi_eeprom.cs
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

using System;
using TotalPhase;


/*=========================================================================
| CLASS
 ========================================================================*/
public class PMSpiEeprom {

    /*=====================================================================
    | CONSTANTS
     ====================================================================*/
    public const int PAGE_SIZE = 32;
    public const int SS_MASK   = 0x1;


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
    static void _writeMemory (int channel, int queue,
                              short addr, short length, bool zero)
    {
        short i, n;
        int collect;
        byte queue_type = 0;

        byte[] dataOut = null;
        byte[] writeEnableBuf = new byte[1];

        // Write to the SPI EEPROM
        //
        // The AT25080A EEPROM has 32 byte pages.  Data can written
        // in pages, to reduce the number of overall SPI transactions
        // executed through the Aardvark adapter.
        n = 0;
        while (n < length) {
            // Clear the queue
            Promact_isApi.ps_queue_clear(queue);

            // Calculate the amount of data to be written this iteration
            // and make sure dataOut is just large enough for it.
            int size = Math.Min(((addr & (PAGE_SIZE - 1)) ^ (PAGE_SIZE - 1)) + 1,
                                length - n);
            byte[] data = new byte[size];

            if (dataOut == null)
                dataOut = new byte[3];

            // Send write enable command
            writeEnableBuf[0] = 0x06;
            Promact_isApi.ps_queue_spi_ss(queue, SS_MASK);
            Promact_isApi.ps_queue_spi_write(queue, 0, 8,
                                             1, writeEnableBuf);
            Promact_isApi.ps_queue_spi_ss(queue, 0);

            // Assemble write command and address
            dataOut[0] = 0x02;
            dataOut[1] = (byte)(((addr + n) >> 8) & 0xff);
            dataOut[2] = (byte)(((addr + n) >> 0) & 0xff);

            // Assemble a page of data
            i = 0;
            do {
                data[i++] = zero ? (byte)0 : (byte) n;
                ++n;
            } while (n < length && ((addr + n) & (PAGE_SIZE - 1)) != 0);

            // Write the transaction
            Promact_isApi.ps_queue_spi_ss(queue, SS_MASK);
            Promact_isApi.ps_queue_spi_write(queue, 0, 8,
                                             (uint)dataOut.Length, dataOut);
            Promact_isApi.ps_queue_spi_write(queue, 0, 8,
                                             (uint)data.Length, data);
            Promact_isApi.ps_queue_spi_ss(queue, 0);
            Promact_isApi.ps_queue_spi_delay_ns(queue, 10 * 1000 * 1000);

            collect = Promact_isApi.ps_queue_submit(queue, channel, 0,
                                                    ref queue_type);
            DevCollect(collect, new byte[0]);
        }
    }

    static void _readMemory (int channel, int queue, short addr, short length)
    {
        int count;
        int i;
        int collect;
        byte queue_type = 0;

        byte[] dataOut = new byte[3];
        byte[] dataIn  = new byte[length + 3];

        // Clear the queue
        Promact_isApi.ps_queue_clear(queue);

        // Assemble read command and address
        dataOut[0] = 0x03;
        dataOut[1] = (byte)((addr >> 8) & 0xff);
        dataOut[2] = (byte)((addr >> 0) & 0xff);

        // Write length+3 bytes for data plus command and 2 address bytes
        Promact_isApi.ps_queue_spi_ss(queue, SS_MASK);
        Promact_isApi.ps_queue_spi_write(queue, (PromiraSpiIOMode)0, 8,
                                         (uint)dataOut.Length, dataOut);
        Promact_isApi.ps_queue_spi_write_word(queue, 0, 8,
                                              (uint)length, 0);
        Promact_isApi.ps_queue_spi_ss(queue, 0);

        collect = Promact_isApi.ps_queue_submit(queue, channel, 0,
                                                ref queue_type);
        count = DevCollect(collect, dataIn);

        if (count < 0) {
            Console.WriteLine("error: {0}", count);
        }
        else if (count != length + 3) {
            Console.WriteLine("error: read {0} bytes (expected {1})",
                              count - 3, length);
        }

        // Dump the data to the screen
        Console.Write("\nData read from device:");
        for (i = 0; i < length; ++i) {
            if ((i & 0x0f) == 0)
                Console.Write("\n{0:x4}:  ", addr + i);
            Console.Write("{0:x2} ", dataIn[i + 3] & 0xff);
            if (((i + 1) & 0x07) == 0)
                Console.Write(" ");
        }
        Console.WriteLine();
    }


    /*=====================================================================
    | MAIN PROGRAM
     ====================================================================*/
    public static void Main (string[] args) {
        int pm      = 0;
        int conn    = 0;
        int channel = 0;
        int queue   = 0;

        string ip      = "";
        int    bitrate = 100;
        int    mode    = 0;
        short  addr;
        short  length;
        int    res;

        if (args.Length != 6) {
            Console.WriteLine(
                "usage: spi_eeprom IP BITRATE read  MODE ADDR LENGTH");
            Console.WriteLine(
                "usage: spi_eeprom IP BITRATE write MODE ADDR LENGTH");
            Console.WriteLine(
                "usage: spi_eeprom IP BITRATE zero  MODE ADDR LENGTH");
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

        // Parse the bitrate argument
        try {
            bitrate = Convert.ToInt32(args[1]);
        }
        catch (Exception) {
            Console.WriteLine("Error: invalid bitrate");
            return;
        }

        string command = args[2];

        // Parse the mode argument
        try {
            mode = Convert.ToInt32(args[3]) & 0x3;
        }
        catch (Exception) {
            Console.WriteLine("Error: invalid mode");
            return;
        }

        // Parse the memory offset argument
        try {
            if (args[4].StartsWith("0x"))
                addr = Convert.ToByte(args[4].Substring(2), 16);
            else
                addr = Convert.ToByte(args[4]);
        }
        catch (Exception) {
            Console.WriteLine("Error: invalid memory addr");
            return;
        }

        // Parse the length
        try {
            length = Convert.ToInt16(args[5]);
        }
        catch (Exception) {
            Console.WriteLine("Error: invalid length");
            return;
        }

        if (mode == 1 || mode == 2) {
            Console.WriteLine(
                "error: spi modes 1 and 2 are not supported by the AT25080A");
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

        // Set the bitrate
        bitrate = Promact_isApi.ps_spi_bitrate(channel, (ushort)bitrate);
        Console.WriteLine("Bitrate set to {0} kHz", bitrate);

        // Create a queue for SPI transactions
        queue = Promact_isApi.ps_queue_create(
            conn, Promact_isApi.PS_MODULE_ID_SPI_ACTIVE);

        // Enable master output
        SpiMasterOE(channel, queue, 1);

        // Perform the operation
        if (command == "write") {
            _writeMemory(channel, queue, addr, length, false);
            Console.WriteLine("Wrote to EEPROM");
        }
        else if (command == "read") {
            _readMemory(channel, queue, addr, length);
        }
        else if (command == "zero") {
            _writeMemory(channel, queue, addr, length, true);
            Console.WriteLine("Zeroed EEPROM");
        }
        else {
            Console.WriteLine("unknown command: {0}", command);
        }

        // Disable master output
        SpiMasterOE(channel, queue, 0);

        // Destroy the queue
        Promact_isApi.ps_queue_destroy(queue);

        // Close the device and exit
        DevClose(pm, conn, channel);
    }
}
