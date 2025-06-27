#!/usr/bin/env python3
#==========================================================================
# Stefano Ampolo.
#--------------------------------------------------------------------------
# Project : POC- SPI Controller
# File    : Stefano-SPI-Madeonmyown.py
#--------------------------------------------------------------------------
# Perform simple SPI Transmission
#--------------------------------------------------------------------------
# 
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
# CONSTANTS
#==========================================================================
MB = 1 * 1024 * 1024
KB = 1 * 1024

BITRATE = 40000
SS_MASK = 1
BUFFER_SIZE = 2048


#IO : 0 - standard, 2 - dual, 4 - quad ( DATA RATE )
IO = PS_SPI_IO_STANDARD


SPI_Word_Size = 32
SPI_Write_Number_of_Words = 1

"""
POC - SPI Characteristics

Freq = 1 MHZ
CS active LOW
SPI MODE 0
CPOL 0
CPHA 0

The MISO output follows the MOSI with 128 clocks delay
"""
SPI_Frequency = 8000
SPI_Word_Delay = 128

SlaveBitmask = 0x00 #(All slaves are active low)



#==========================================================================
# FUNCTIONS From the API
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
    ps_app_disconnect(app)
    pm_close(pm)

def dev_collect (collect):
    if collect < 0:
        print(ps_app_status_string(collect))
        return

    response = array('B', [ ])
    while True:
        t, length, result = ps_collect_resp(collect, -1)
        if t == PS_APP_NO_MORE_CMDS_TO_COLLECT:
            break
        elif t < 0:
            print(ps_app_status_string(t))
        if t == PS_SPI_CMD_READ:
            ret, word_size, buf = ps_collect_spi_read(collect, result)
            response += buf
    return response

def spi_master_oe (channel, queue, enable):
    ps_queue_clear(queue)
    ps_queue_spi_oe(queue, enable)
    collect, _, = ps_queue_submit(queue, channel, 0)
    dev_collect(collect)

def blast_bytes (channel, queue, data_io, filename):
    # Open the file
    try:
        f = open(filename, 'rb')
    except:
        print("Unable to open file '" + filename + "'")
        return

    trans_num = 0
    while True:
        # Read from the file
        filedata = f.read(BUFFER_SIZE)
        if len(filedata) == 0:
            break

        # Clear the queue
        ps_queue_clear(queue)

        # Write the data to the bus
        data_out = array('B', filedata)
        ps_queue_spi_ss(queue, SS_MASK)
        ps_queue_spi_write(queue, data_io, 8, len(data_out), data_out)
        ps_queue_spi_ss(queue, 0)

        collect, _ = ps_queue_submit(queue, channel, 0)
        data_in = dev_collect(collect)

        count = len(data_in)
        if (count != len(data_out)):
            print("error: only a partial number of bytes written")
            print("  (%d) instead of full (%d)" % (count, len(data_out)))

        sys.stdout.write("*** Transaction #%02d\n" % trans_num)
        # sys.stdout.write("Data written to device:")
        # for i in range(count):
        #     if ((i & 0x0f) == 0):
        #         sys.stdout.write("\n%04x:  " % i)

        #     sys.stdout.write("%02x " % (data_out[i] & 0xff))
        #     if (((i + 1) & 0x07) == 0):
        #         sys.stdout.write(" ")

        # sys.stdout.write("\n\n")

        sys.stdout.write("Data read from device:")
        for i in range(count):
            if ((i & 0x0f) == 0):
                sys.stdout.write("\n%04x:  " % i)

            sys.stdout.write("%02x " % (data_in[i] & 0xff))
            if (((i + 1) & 0x07) == 0):
                sys.stdout.write(" ")

        sys.stdout.write("\n\n")

        trans_num = trans_num + 1

        # Sleep a tad to make sure slave has time to process this request
        # ps_app_sleep_ms(10)

    f.close()

# This function will look for all Promira SPI Controllers attached to the machine and print out the ip addresses (passing 1 to verbose),
# If there's only one SPI controller attached it will output its IP, otherwise the output will be zero.

#==========================================================================
# CUSTOM FUNCTIONS
#==========================================================================

def Detect_SPI_Controller( verbose = 0 ):
    # Detect SPI Controller

    if(verbose) : print("Detecting the Promira platforms...")

    # Find all the attached devices
    (num, ips, unique_ids, statuses) = pm_find_devices_ext(16, 16, 16)

    if num > 0:
        if(verbose) : print("%d device(s) found:" % num)

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
            if(verbose) : print("    ip = %s   %s  (%04d-%06d)"
                % (ipstr, inuse, unique_id // 1000000, unique_id % 1000000))

            if not status & PM_DEVICE_NOT_FREE:
                pm   = pm_open(ipstr)
                data = pm_apps(pm, 1024)[1]
                apps = ''.join([ chr(c) for c in data ])
                if(verbose) : print("    - apps = %s" % apps)
                pm_close(pm)

    else:
        print("No devices found.")


    # Selection of the SPI Controller by IP Address

    if len(ips) == 1:

        ip = ips[0]
        ipstr = "%u.%u.%u.%u" % (ip & 0xff, ip >> 8 & 0xff, ip >> 16 & 0xff, ip >> 24 & 0xff)

        print(f"Selected SPI Controller ip is: {ipstr}")
        # print("Selected SPI Controller ip is: %s" %(ipstr))
    else:
        ip=0
        print("More than one TotalPhase Promira SPI Controllers detected ")

    # END of Selection of the SPI Controller

    return ip
    # END of SPI Controller Detection

def Select_SPI_Controller_Handler( Requested_Handler , verbose = 0 ):
    # Detect SPI Controller

    if(verbose) : print(f"Selecting which SPI Controller to use: {Requested_Handler}")


    # ----> unique_ids[i]
    # 2416713000 = NORTH SPI CONTROLLER
    # 2416711301 = SOUTH SPI CONTROLLER

    # Find all the attached devices
    (num, ips, unique_ids, statuses) = pm_find_devices_ext(16, 16, 16)
    pm_out =0
    conn_out = 0
    channel_out = 0
    ip_out = 0

    if num > 0:
        if(verbose) : print("%d device(s) found:" % num)
        # Print the information on each device
        for i in range(num):
            ip        = ips[i]
            unique_id = unique_ids[i]
            ipstr = "%u.%u.%u.%u" % (ip & 0xff, ip >> 8 & 0xff, ip >> 16 & 0xff, ip >> 24 & 0xff)


            # Detecting North SPI CONTROLLER
            if (unique_id == 2416713000):
                if(verbose) :
                    print(f" North SPI Controller Found")
                    print(" North ip = %s , North ID = (%d)" % (ipstr, unique_id))
                # Open the device North and return the handler (channel)
                if Requested_Handler.lower() == "north":
                    pm, conn, channel = dev_open(ipstr)
                    print("North SPI Controller Initialized.")
                    ip_out = ipstr
                    pm_out = pm
                    conn_out = conn
                    channel_out = channel


            # Detecting South SPI CONTROLLER
            if (unique_id == 2416711301):
                if(verbose) :
                    print(f" South SPI Controller Found")
                    print(" South ip = %s, South ID = (%d)" % (ipstr, unique_id))
                # Open the device South and return the handler (channel)
                if Requested_Handler.lower() == "south":
                    pm, conn, channel = dev_open(ipstr)
                    print("South SPI Controller Initialized.")
                    ip_out = ipstr
                    pm_out = pm
                    conn_out = conn
                    channel_out = channel                                
    else:
        print("No SPI Controllers found.")
        channel = 0

    
    if(verbose) : print(f"channel = {channel}")

    return pm_out, conn_out, channel_out, ip_out

def normalize_hex_array(hex_array):
    """
    Converts a hex array of any format into a flat list of bytes (integers 0–255).
    
    Args:
        hex_array: List of integers, e.g. [0xA0, 0xA0, 0xFF, 0xFF, 0x00, 0x0E]
                   or [0xA0A0FFFF000E]
    
    Returns:
        List of bytes: [160, 160, 255, 255, 0, 14]
    """
    result = []
    for value in hex_array:
        # Calculate how many bytes are needed (at least 1 for 0)
        num_bytes = (value.bit_length() + 7) // 8 or 1
        # Convert to bytes (big-endian), then extend the result list
        result.extend(value.to_bytes(num_bytes, 'big'))
    return list(result)

def SPI_Write_Hex_Array(conn, channel, hex_array):
    
    data_io  = PS_SPI_IO_STANDARD
    
    filename = "hex_array.bin"
    #Create File
    with open('hex_array.bin', 'wb') as f:
        f.write(bytearray(normalize_hex_array(hex_array)))


    # Create a queue for SPI transactions
    queue = ps_queue_create(conn, PS_MODULE_ID_SPI_ACTIVE)

    # Enable master output
    spi_master_oe(channel, queue, 1)

    # Perform the operation
    blast_bytes(channel, queue, data_io, filename)

    # Disable master output
    spi_master_oe(channel, queue, 0)

    # Destroy the queue
    ps_queue_destroy(queue)

def SPI_Read(conn, channel, size):
    
    #Create Array of Zeros of size lenght
    hex_array = [0x0] * size

    data_io  = PS_SPI_IO_STANDARD
    
    filename = "hex_array.bin"
    #Create File
    with open('hex_array.bin', 'wb') as f:
        f.write(bytearray(normalize_hex_array(hex_array)))


    # Create a queue for SPI transactions
    queue = ps_queue_create(conn, PS_MODULE_ID_SPI_ACTIVE)

    # Enable master output
    spi_master_oe(channel, queue, 1)

    # Perform the operation
    blast_bytes(channel, queue, data_io, filename)

    # Disable master output
    spi_master_oe(channel, queue, 0)

    # Destroy the queue
    ps_queue_destroy(queue)

def SPI_Read_smart_poc(conn, channel, size):
    
    #Create Array of Zeros of size lenght
    hex_array = [0x0] * size

    
    filename = "hex_array.bin"
    #Create File
    with open('hex_array.bin', 'wb') as f:
        f.write(bytearray(normalize_hex_array(hex_array)))


    # Create a queue for SPI transactions
    queue = ps_queue_create(conn, PS_MODULE_ID_SPI_ACTIVE)

    # Enable master output
    spi_master_oe(channel, queue, 1)

    # Perform the operation
    blast_bytes(channel, queue, PS_SPI_IO_STANDARD, filename)

    # Disable master output
    spi_master_oe(channel, queue, 0)

    # Destroy the queue
    ps_queue_destroy(queue)



#==========================================================================
# MAIN PROGRAM
#==========================================================================

# Detecting the SPI Controllers connected and printing their IDs
#ip = Detect_SPI_Controller()

# Getting the Handlers for the two SPI Controllers 0 is for NO Verbose


## INIT OF THE SPI CONTROLLERS (TWO)

(pm_North, conn_North, HANDLER_North, IP_North) = Select_SPI_Controller_Handler("NORTH" , 0)
(pm_South, conn_South, HANDLER_South, IP_South) = Select_SPI_Controller_Handler("South" , 0)

if ( HANDLER_North == 0) : print(f"The North SPI Controller is Missing")
if ( HANDLER_South == 0) : print(f"The South SPI Controller is Missing")


if ( HANDLER_North != 0) :
    print(f"The North SPI Controller is Connected")

if ( HANDLER_South != 0) : 
    print(f"The South SPI Controller is Connected")

    # Device is already opened
    #     
    # Ensure that the SPI subsystem is enabled
    ps_app_configure(HANDLER_South, PS_APP_CONFIG_SPI)

    # Select Target Power supply voltage level.
    ps_phy_target_power(HANDLER_South, PS_PHY_TARGET_POWER_TARGET1_3V)

    # Configure the power of output signal.
    #a = ps_phy_level_shift (HANDLER_South, 0.9)
    a = ps_phy_level_shift (HANDLER_South, 5)
    print(f"Level Shift Configured --> Level = { a }")

    # Configure the Word Delay 
    a = ps_spi_configure_delays (HANDLER_South, SPI_Word_Delay);
    if (a == PS_APP_OK ) : print(f"Word Delay set Successful")

    # Setup the clock phase
    ps_spi_configure(HANDLER_South, PS_SPI_MODE_0, PS_SPI_BITORDER_MSB, SlaveBitmask)

    # Configure SS
    ps_spi_enable_ss(HANDLER_South, SS_MASK)

    # Set the bitrate
    bitrate = ps_spi_bitrate(HANDLER_South, 8000)
    print("Bitrate of South controller set to %d kHz" % bitrate)




#hex_array0 = [0xA0, 0xA0, 0xFF, 0xFF, 0x00, 0x0E]
hex_array = [0xA0A0FFFF000E]

SPI_Write_Hex_Array( conn_South, HANDLER_South, hex_array)
SPI_Write_Hex_Array( conn_South, HANDLER_South, [0xABCD])
SPI_Write_Hex_Array( conn_South, HANDLER_South, [0x0F0F])
SPI_Write_Hex_Array( conn_South, HANDLER_South, [0xEEEE])

SPI_Read( conn_South, HANDLER_South, 8)



# Close the device and exit
dev_close(pm_South, conn_South, HANDLER_South)

  
  
  
