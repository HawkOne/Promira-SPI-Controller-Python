/*=========================================================================
| (c) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : i2c_eeprom.cs
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

using System;
using TotalPhase;


/*=========================================================================
| CLASS
 ========================================================================*/
public class PMI2cEeprom {

    /*=====================================================================
    | CONSTANTS
     ====================================================================*/
    public const int PAGE_SIZE   = 8;
    public const int BUS_TIMEOUT = 150;  // ms


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
    | FUNCTION
     ====================================================================*/
    static void _writeMemory (int channel, byte device, byte addr,
                              short length, bool zero)
    {
        short i, n;
        int res;
        byte[] dataOut = null;
        ushort num_bytes = 0;

        // Write to the I2C EEPROM
        //
        // The AT24C02 EEPROM has 8 byte pages.  Data can written
        // in pages, to reduce the number of overall I2C transactions
        // executed through the Aardvark adapter.
        n = 0;
        while (n < length) {
            // Calculate the amount of data to be written this iteration
            // and make sure dataOut is just large enough for it.
            int size = Math.Min(((addr & (PAGE_SIZE-1)) ^ (PAGE_SIZE-1)) + 1,
                                length - n);
            size++;  // Add 1 for the address field
            if (dataOut == null || dataOut.Length != size)
                dataOut = new byte[size];

            // Fill the packet with data
            dataOut[0] = addr;

            // Assemble a page of data
            i = 1;
            do {
                dataOut[i++] = zero ? (byte)0 : (byte)n;
                ++addr; ++n;
            } while ( n < length && (addr & (PAGE_SIZE-1)) != 0 );

            // Write the address and data
            res = Promact_isApi.ps_i2c_write(channel, device,
                                             PromiraI2cFlags.PS_I2C_NO_FLAGS,
                                             (ushort)size, dataOut,
                                             ref num_bytes);
            if (res < 0) {
                Console.WriteLine("error: {0}\n", res);
                return;
            }
            if (num_bytes == 0) {
                Console.WriteLine("error: no bytes written");
                Console.WriteLine("  are you sure you have the right slave address?");
                return;
            }
            Promact_isApi.ps_app_sleep_ms(10);
        }
    }


    static void _readMemory (int channel, byte device, byte addr,
                             short length)
    {
        int res, i;
        byte[] dataOut = { addr };
        byte[] dataIn  = new byte[length];
        ushort num_bytes = 0;

        // Write the address
        Promact_isApi.ps_i2c_write(channel, device,
                                   PromiraI2cFlags.PS_I2C_NO_STOP,
                                   (ushort)length, dataOut, ref num_bytes);

        res = Promact_isApi.ps_i2c_read(channel, device,
                                        PromiraI2cFlags.PS_I2C_NO_FLAGS,
                                        (ushort)length, dataIn, ref num_bytes);
        if (res < 0) {
            Console.WriteLine("error: {0}\n", res);
//                              Promact_isApi.status_string(count));
            return;
        }
        if (num_bytes == 0) {
            Console.WriteLine("error: no bytes read");
            Console.WriteLine("  are you sure you have the right slave address?");
            return;
        }
        else if (num_bytes != length) {
            Console.WriteLine("error: read {0} bytes (expected {1})",
                              num_bytes, length);
        }

        // Dump the data to the screen
        Console.Write("\nData read from device:");
        for (i = 0; i < num_bytes; ++i) {
            if ((i&0x0f) == 0)      Console.Write("\n{0:x4}:  ", addr+i);
            Console.Write("{0:x2} ", dataIn[i] & 0xff);
            if (((i+1)&0x07) == 0)  Console.Write(" ");
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

        string ip      = "";
        int    bitrate = 100;
        int    bus_timeout;
        byte   device;
        byte   addr;
        short  length;
        int    res;

        if (args.Length != 6) {
            Console.WriteLine("usage: i2c_eeprom IP BITRATE read  SLAVE_ADDR OFFSET LENGTH");
            Console.WriteLine("usage: i2c_eeprom IP BITRATE write SLAVE_ADDR OFFSET LENGTH");
            Console.WriteLine("usage: i2c_eeprom IP BITRATE zero  SLAVE_ADDR OFFSET LENGTH");
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

        // Parse the slave address argument
        try {
            if (args[3].StartsWith("0x"))
                device = Convert.ToByte(args[3].Substring(2), 16);
            else
                device = Convert.ToByte(args[3]);
        }
        catch (Exception) {
            Console.WriteLine("Error: invalid device number");
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
        bitrate = Promact_isApi.ps_i2c_bitrate(channel, (ushort)bitrate);
        Console.WriteLine("Bitrate set to {0} kHz", bitrate);

        // Set the bus lock timeout
        bus_timeout = Promact_isApi.ps_i2c_bus_timeout(channel, BUS_TIMEOUT);
        Console.WriteLine("Bus lock timeout set to {0} ms", bus_timeout);

        // Perform the operation
        if (command == "write") {
            _writeMemory(channel, device, addr, length, false);
            Console.WriteLine("Wrote to EEPROM");
        }
        else if (command == "read") {
            _readMemory(channel, device, addr, length);
        }
        else if (command == "zero") {
            _writeMemory(channel, device, addr, length, true);
            Console.WriteLine("Zeroed EEPROM");
        }
        else {
            Console.WriteLine("unknown command: {0}", command);
        }

        // Close the device and exit
        DevClose(pm, conn, channel);
    }
}
