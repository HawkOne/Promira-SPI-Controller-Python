/*=========================================================================
| (c) 2015 Total Phase, Inc.
|--------------------------------------------------------------------------
| Project : Promira Sample Code
| File    : gpio.cs
|--------------------------------------------------------------------------
| Perform some simple GPIO operations with a single Aardvark adapter.
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
public class PMGpio {

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
    | MAIN PROGRAM
     ====================================================================*/
    public static void Main (string[] args) {
        int pm      = 0;
        int conn    = 0;
        int channel = 0;

        string ip   = "";
        uint   val  = 0;
        int    res  = 0;

        if (args.Length != 1) {
            Console.WriteLine("usage: gpio IP");
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

        // Configure the Promira adapter so all pins
        // are now controlled by the GPIO subsystem
        Promact_isApi.ps_app_configure(channel,
                                       Promact_isApi.PS_APP_CONFIG_GPIO);

        // Turn off the external I2C line pullups
        Promact_isApi.ps_i2c_pullup(channel,
                                    Promact_isApi.PS_I2C_PULLUP_NONE);

        // Make sure the charge has dissipated on those lines
        Promact_isApi.ps_gpio_set(channel, 0x00);
        Promact_isApi.ps_gpio_direction(channel, 0xffff);

        // By default all GPIO pins are inputs.  Writing 1 to the
        // bit position corresponding to the appropriate line will
        // configure that line as an output
        Promact_isApi.ps_gpio_direction(channel, 0x1 | 0x8);

        // By default all GPIO outputs are logic low.  Writing a 1
        // to the appropriate bit position will force that line
        // high provided it is configured as an output.  If it is
        // not configured as an output the line state will be
        // cached such that if the direction later changed, the
        // latest output value for the line will be enforced.
        Promact_isApi.ps_gpio_set(channel, 0x1);
        Console.WriteLine("Setting GPIO0 to logic low");

        // The get method will return the line states of all inputs.
        // If a line is not configured as an input the value of
        // that particular bit position in the mask will be 0.
        val = (byte)Promact_isApi.ps_gpio_get(channel);

        // Check the state of GPIO1
        if ((val & 0x2) != 0)
            Console.WriteLine("Read the GPIO1 line as logic high");
        else
            Console.WriteLine("Read the GPIO1 line as logic low");

        // Now reading the GPIO2 line should give a logic high,
        // provided there is nothing attached to the Aardvark
        // adapter that is driving the pin low.
        val = (byte)Promact_isApi.ps_gpio_get(channel);
        if ((val & 0x4) != 0)
            Console.WriteLine(
                "Read the GPIO2 line as logic high (passive pullup)");
        else
            Console.WriteLine(
                "Read the GPIO2 line as logic low (is pin driven low?)");


        // Now do a 1000 gets from the GPIO to test performance
        for (int i = 0; i < 1000; ++i)
            Promact_isApi.ps_gpio_get(channel);

        int oldval, newval;

        // Demonstrate use of ps_gpio_change
        Promact_isApi.ps_gpio_direction(channel, 0x00);
        oldval = Promact_isApi.ps_gpio_get(channel);

        Console.WriteLine("Calling ps_gpio_change for 2 seconds...");
        newval = Promact_isApi.ps_gpio_change(channel, 2000);

        if (newval != oldval)
            Console.WriteLine("  GPIO inputs changed.\n");
        else
            Console.WriteLine("  GPIO inputs did not change.\n");

        // Turn on the I2C line pullups since we are done
        Promact_isApi.ps_i2c_pullup(channel,
                                    Promact_isApi.PS_I2C_PULLUP_BOTH);

        // Configure the Aardvark adapter back to SPI/I2C mode.
        Promact_isApi.ps_app_configure(channel,
                                       Promact_isApi.PS_APP_CONFIG_SPI |
                                       Promact_isApi.PS_APP_CONFIG_I2C);

        // Close the device
        DevClose(pm, conn, channel);
    }
}
