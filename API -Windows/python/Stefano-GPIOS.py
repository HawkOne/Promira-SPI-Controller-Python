#!/usr/bin/env python3
#==========================================================================
# (c) 2014-2019 Total Phase, Inc.
#--------------------------------------------------------------------------
# Project : Promira Sample Code
# File    : gpio.py
#--------------------------------------------------------------------------
# Perform some simple GPIO operations with a single Promira adapter.
#--------------------------------------------------------------------------
# Redistribution and use of this file in source and binary forms, with
# or without modification, are permitted.
#
# THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
# "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
# LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
# FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE
# COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
# INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
# BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
# LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
# CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
# LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
# ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
# POSSIBILITY OF SUCH DAMAGE.
#==========================================================================

#==========================================================================
# IMPORTS
#==========================================================================
from __future__ import division, with_statement, print_function
import sys
import time

from promira_py import *
from promact_is_py import *


#==========================================================================
# FUNCTION (APP)
#==========================================================================
APP_NAME = "com.totalphase.promact_is"
def dev_open (ip):
    pm = pm_open(ip)
    if pm <= 0:
         print("Unable to open Promira platform on %s" % ip)
         print("Error code = %d" % pm)
         sys.exit()

    ret = pm_load(pm, APP_NAME)
    if ret < 0:
         print("Unable to load the application(%s)" % APP_NAME)
         print("Error code = %d" % ret)
         sys.exit()

    conn = ps_app_connect(ip)
    if conn <= 0:
         print("Unable to open the application on %s" % ip)
         print("Error code = %d" % conn)
         sys.exit()

    channel = ps_channel_open(conn)
    if channel <= 0:
         print("Unable to open the channel")
         print("Error code = %d" % channel)
         sys.exit()

    return pm, conn, channel

def dev_close (pm, app, channel):
    ps_channel_close(channel)
    ps_app_disconnect(conn)
    pm_close(pm)


#==========================================================================
# MAIN PROGRAM
#==========================================================================

#Detection Phase (0 to disable , 1 to enable)

if(0):
    #looking for devices to use --> In case the device is already identified we can skip this and use the IP Directly.
    print("Detecting the Promira platforms...")

    # Find all the attached devices
    (num, ips, unique_ids, statuses) = pm_find_devices_ext(16, 16, 16)

    if num > 0:
        print("%d device(s) found:" % num)

        # Print the information on each device
        for i in range(num):
            ip        = ips[i]
            unique_id = unique_ids[i]
            status    = statuses[i]

            # Determine if the device is in-use
            if status & PM_DEVICE_NOT_FREE:
                inuse = "(in-use)"
            else:
                inuse = "(avail)"

            # Display device ip address, in-use status, and serial number
            ipstr = "%u.%u.%u.%u" \
                % (ip & 0xff, ip >> 8 & 0xff, ip >> 16 & 0xff, ip >> 24 & 0xff)
            print("    ip = %s   %s  (%04d-%06d)"
                % (ipstr, inuse, unique_id // 1000000, unique_id % 1000000))

            if not status & PM_DEVICE_NOT_FREE:
                pm   = pm_open(ipstr)
                data = pm_apps(pm, 1024)[1]
                apps = ''.join([ chr(c) for c in data ])
                print("    - apps = %s" % apps)
                pm_close(pm)

    else:
        print("No devices found.")


#Check if the arguments passed were correct for the IP

#if (len(sys.argv) < 2):
#    print("usage: gpio IP")
#    sys.exit()
#
#ip = sys.argv[1]


#This doesn't work yet
#ip=ips[1]
# Setting a FIXED IP 
#ip= "10.1.67.185"
ip= "10.1.66.45"


# Open the device
pm, conn, channel = dev_open(ip)

# Configure the Promira adapter so all pins
# are now controlled by the GPIO subsystem
ps_app_configure(channel, PS_APP_CONFIG_GPIO)

# Turn off the external I2C line pullups
ps_i2c_pullup(channel, PS_I2C_PULLUP_NONE)

# Make sure the charge has dissipated on those lines
ps_gpio_set(channel, 0x0)
ps_gpio_direction(channel, 0xffff)


# Experimenting

#This returns the mask of GPIOS (4095) -> 0XFFF
#a= ps_gpio_query (channel)
#print(a)



# By default all GPIO pins are inputs.  Writing 1 to the
# bit position corresponding to the appropriate line will
# configure that line as an output
#ps_gpio_direction(channel, 0x1 | 0x8)

#GPIO_PIN_NUMBER= 25

#debug = int(format(int(hex(GPIO_PIN_NUMBER), 16), 'd'))

#debug = 0x800
debug = 0x800

ps_gpio_direction(channel, 0x1 | debug)

# By default all GPIO outputs are logic low.  Writing a 1
# to the appropriate bit position will force that line
# high provided it is configured as an output.  If it is
# not configured as an output the line state will be
# cached such that if the direction later changed, the
# latest output value for the line will be enforced.

#ps_gpio_set(channel, 0x1)

ps_gpio_set(channel, debug)

print("Setting GPIO0 to logic low")

# The get method will return the line states of all inputs.
# If a line is not configured as an input the value of
# that particular bit position in the mask will be 0.
val = ps_gpio_get(channel)

# Check the state of GPIO1
if (val & 0x2):
    print("Read the GPIO1 line as logic high")
else:
    print("Read the GPIO1 line as logic low")

# Now reading the MISO line should give a logic high,
# provided there is nothing attached to the Promira
# adapter that is driving the pin low.
val = ps_gpio_get(channel)
if (val & 0x4):
    print("Read the GPIO2 line as logic high (passive pullup)")
else:
    print("Read the GPIO2 line as logic low (is pin driven low?)")

# Now do a 1000 gets from the GPIO to test performance
for i in range(1000):
    ps_gpio_get(channel)

# Demonstrate use of ps_gpio_change
ps_gpio_direction(channel, 0x00)
oldval = ps_gpio_get(channel)
print("Calling ps_gpio_change for 1 second...")
newval = ps_gpio_change(channel, 1000)
if (newval != oldval):
    print("  GPIO inputs changed.")
else:
    print("  GPIO inputs did not change.")

# Turn on the I2C line pullups since we are done
ps_i2c_pullup(channel, PS_I2C_PULLUP_BOTH)

# Configure the Promira adapter back to SPI/I2C mode.
ps_app_configure(channel, PS_APP_CONFIG_SPI | PS_APP_CONFIG_I2C)

# Close the device and exit
dev_close(pm, conn, channel)
