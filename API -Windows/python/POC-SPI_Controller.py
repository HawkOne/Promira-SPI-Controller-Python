#!/usr/bin/env python3
#==========================================================================
# Stefano Ampolo.
#--------------------------------------------------------------------------
# Project : POC- SPI Controller
# File    : POC-SPI_Controller.py
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
from array import array

from promira_py import *
from promact_is_py import *


#==========================================================================
# CONSTANTS
#==========================================================================
MB = 1 * 1024 * 1024
KB = 1 * 1024

"""
POC - SPI Characteristics

Freq = 1 MHZ
CS active LOW
SPI MODE 0
CPOL 0
CPHA 0

The MISO output follows the MOSI with 128 clocks delay
"""

#Settings for Both Controllers

SPI_Frequency = 1000                # Frequency in KHz
#SPI_LS_Voltage = 0.9               # Voltage Level of the Level-Shifter
SPI_LS_Voltage = 0.9                # Voltage Level of the Level-Shifter
SPI_SS_MASK = 1                     # BitMask for Slave Selection (We will use Slave 1 (Pin 9 - SS0)
SPI_SS_MASK_Active = 0x00           #(All slaves are active low)
SPI_SS_Delay = 0.001                # 1 Millisecond between CS and Data
SPI_Word_Delay = 0                  # Spacing in time between bytes (Delay fo internal buffers)     

SPI_BUFFER_SIZE = 2048              # Size of the SPI Buffer (For File Transfers)
SPI_Word_Size = 32                  #
SPI_Write_Number_of_Words = 1       #

SPI_TARGET_POWER_LEVEL = PS_PHY_TARGET_POWER_TARGET1_3V     # Setting to provide =>3.3V or 5V to pin 4 ( TARGET POWER )


#IO : 0 - standard, 2 - dual, 4 - quad ( DATA RATE )
SPI_DUAL_DATA_RATE = PS_SPI_IO_STANDARD   # SPI Data Rate Selection
SPI_MODE = PS_SPI_MODE_0            # SPI MODE Selection
SPI_BIT_ORDER = PS_SPI_BITORDER_MSB #



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
        filedata = f.read(SPI_BUFFER_SIZE)
        if len(filedata) == 0:
            break

        # Clear the queue
        ps_queue_clear(queue)

        # Write the data to the bus
        data_out = array('B', filedata)
        ps_queue_spi_ss(queue, SPI_SS_MASK)
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
                # Open the device North and return the channel (channel)
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

def normalize_hex_data(hex_data):
    """
    Accepts either a list of individual bytes or a list with a single large hex int.
    Returns an array('B') of bytes.
    """
    if len(hex_data) == 1 and isinstance(hex_data[0], int) and hex_data[0] > 0xFF:
        # Single large number: convert to bytes
        big_int = hex_data[0]
        num_bytes = (big_int.bit_length() + 7) // 8
        byte_data = big_int.to_bytes(num_bytes, byteorder='big')
        return array('B', byte_data)
    else:
        # Assume already a list of individual bytes
        return array('B', hex_data)


def MYnormalize(hex_array):
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
    return array('B', result)

def SPI_Write_Hex_Array(conn, channel, hex_array):
        
    filename = "hex_array.bin"
    #Create File
    with open('hex_array.bin', 'wb') as f:
        f.write(bytearray(normalize_hex_array(hex_array)))


    # Create a queue for SPI transactions
    queue = ps_queue_create(conn, PS_MODULE_ID_SPI_ACTIVE)

    # Enable master output
    spi_master_oe(channel, queue, 1)

    # Perform the operation
    blast_bytes(channel, queue, SPI_DUAL_DATA_RATE, filename)

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


"""" 
def SPI_Init( name , HardwareID):

    
    (pm_North, conn_North, HANDLER_North, IP_North) = Select_SPI_Controller_Handler("NORTH" , 0)
    if ( HANDLER_North == 0) : print(f"The North SPI Controller is Missing")


    
def SPI_Init_Both():
    SPI_Init( "North", 2416713000)

    SPI_Init( "South", 2416711301)


"""

def blast_bytes_array(channel, queue, byte_array):
    """
    Write the given byte array directly over SPI without reading from a file.
    """
    trans_num = 0
    offset = 0
    while offset < len(byte_array):
        # Slice the data into chunks of BUFFER_SIZE
        chunk = byte_array[offset:offset + SPI_BUFFER_SIZE]
        offset += len(chunk)

        # Clear the queue
        ps_queue_clear(queue)

        # Prepare and write data
        data_out = array('B', chunk)
        ps_queue_spi_ss(queue, SPI_SS_MASK)
        ps_queue_spi_write(queue, SPI_DUAL_DATA_RATE, 8, len(data_out), data_out)
        ps_queue_spi_ss(queue, 0)

        collect, _ = ps_queue_submit(queue, channel, 0)
        data_in = dev_collect(collect)

        count = len(data_in)
        if count != len(data_out):
            print("error: only a partial number of bytes written")
            print("  (%d) instead of full (%d)" % (count, len(data_out)))

        sys.stdout.write("*** Transaction #%02d\n" % trans_num)
        sys.stdout.write("Data read from device:")
        for i in range(count):
            if (i & 0x0f) == 0:
                sys.stdout.write("\n%04x:  " % i)
            sys.stdout.write("%02x " % (data_in[i] & 0xff))
            if ((i + 1) & 0x07) == 0:
                sys.stdout.write(" ")
        sys.stdout.write("\n\n")

        trans_num += 1


def SPI_Transaction_Array(channel, queue, byte_array):
    """
    Write the given byte array directly over SPI without reading from a file.
    Returns all data read from the SPI device as a single array.
    """
    from array import array

    data_out = MYnormalize(byte_array)

    received_data = array('B')
    offset = 0

    while offset < len(byte_array):
        # Slice the data into chunks of BUFFER_SIZE
        chunk = byte_array[offset:offset + SPI_BUFFER_SIZE]
        offset += len(chunk)

        # Clear the queue
        ps_queue_clear(queue)

        # Prepare and write data
        # data_out = array('B', chunk)
        ps_queue_spi_ss(queue, SPI_SS_MASK)
        ps_queue_spi_write(queue, SPI_DUAL_DATA_RATE, 8, len(data_out), data_out)
        ps_queue_spi_ss(queue, 0)

        collect, _ = ps_queue_submit(queue, channel, 0)
        data_in = dev_collect(collect)

        if len(data_in) != len(data_out):
            print("Warning: Partial write occurred (%d of %d bytes)" % (len(data_in), len(data_out)))

        received_data.extend(data_in)

    return received_data



#==========================================================================
# MAIN PROGRAM
#==========================================================================

# Detecting the SPI Controllers connected and printing their IDs
#ip = Detect_SPI_Controller()

# Getting the Handlers for the two SPI Controllers 0 is for NO Verbose


## INIT OF THE SPI CONTROLLERS (TWO)


# SPI_INIT_BOTH(PS_APP_CONFIG_SPI,PS_PHY_TARGET_POWER_TARGET1_3V,SPI_LS_Voltage,SPI_Word_Delay,  PS_SPI_MODE_0, PS_SPI_BITORDER_MSB, SlaveBitmask, SPI_SS_MASK)

(pm_North, conn_North, HANDLER_North, IP_North) = Select_SPI_Controller_Handler("NORTH" , 0)
(pm_South, conn_South, HANDLER_South, IP_South) = Select_SPI_Controller_Handler("South" , 0)

if ( HANDLER_North == 0) : print(f"The North SPI Controller is Missing")
if ( HANDLER_South == 0) : print(f"The South SPI Controller is Missing")


if ( HANDLER_North != 0) :
    print(f"The North SPI Controller is Connected")

    channel = HANDLER_North
    name = "North_SPI"

    # Device is already opened
    #     
    # Ensure that the SPI subsystem is enabled
    ps_app_configure(channel, PS_APP_CONFIG_SPI)

    # Select Target Power supply voltage level.
    ps_phy_target_power(channel, PS_PHY_TARGET_POWER_TARGET1_3V)

    # Configure the power of output signal.
    #a = ps_phy_level_shift (HANDLER_South, 0.9)
    a = ps_phy_level_shift (channel, SPI_LS_Voltage)
    print(f"{name} Level Shift Configured --> Level = { a } .")

    # Configure the Word Delay 
    a = ps_spi_configure_delays (channel, SPI_Word_Delay)
    if (a == PS_APP_OK ) : print(f"{name} Word Delay set Successfully.")

    # Setup the clock phase
    ps_spi_configure(channel, PS_SPI_MODE_0, PS_SPI_BITORDER_MSB, SPI_SS_MASK_Active)

    # Configure SS
    ps_spi_enable_ss(channel, SPI_SS_MASK)

    # Set the bitrate
    bitrate = ps_spi_bitrate(channel, SPI_Frequency)
    print(f"{name} Bitrate set to {bitrate} kHz.")

if ( HANDLER_South != 0) : 
    print(f"The South SPI Controller is Connected")

    channel = HANDLER_South
    name = "South_SPI"

    # Device is already opened
    #     
    # Ensure that the SPI subsystem is enabled
    ps_app_configure(channel, PS_APP_CONFIG_SPI)

    # Select Target Power supply voltage level.
    ps_phy_target_power(channel, PS_PHY_TARGET_POWER_TARGET1_3V)

    # Configure the power of output signal.
    #a = ps_phy_level_shift (HANDLER_South, 0.9)
    a = ps_phy_level_shift (channel, SPI_LS_Voltage)
    print(f"{name} Level Shift Configured --> Level = { a } .")

    # Configure the Word Delay 
    a = ps_spi_configure_delays (channel, SPI_Word_Delay)
    if (a == PS_APP_OK ) : print(f"{name} Word Delay set Successfully.")

    # Setup the clock phase
    ps_spi_configure(channel, SPI_MODE, PS_SPI_BITORDER_MSB, SPI_SS_MASK_Active)

    # Configure SS
    ps_spi_enable_ss(channel, SPI_SS_MASK)

    # Set the bitrate
    bitrate = ps_spi_bitrate(channel, 8000)
    print(f"{name} Bitrate set to {bitrate} kHz.")




#hex_array0 = [0xA0, 0xA0, 0xFF, 0xFF, 0x00, 0x0E]
hex_array = [0xA0A0FFFF000E]

conn = conn_North
channel = HANDLER_North
pm = pm_North

"""
SPI_Write_Hex_Array( conn, channel, hex_array)
SPI_Write_Hex_Array( conn, channel, [0xABCD])
SPI_Write_Hex_Array( conn, channel, [0x0F0F])
SPI_Write_Hex_Array( conn, channel, [0xEEEE])

SPI_Read( conn, channel, 4)



hex_data = [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC]
byte_array = array('B', hex_data)


# Create a queue for SPI transactions
queue = ps_queue_create(conn, PS_MODULE_ID_SPI_ACTIVE)

# Enable master output
spi_master_oe(channel, queue, 1)

# Perform the operation
blast_bytes_array(channel, queue, IO, byte_array)

# Disable master output
spi_master_oe(channel, queue, 0)


"""

hex_data = [0xAABBCCDDEEFF]
byte_array = hex_data

# Create a queue for SPI transactions
queue = ps_queue_create(conn, PS_MODULE_ID_SPI_ACTIVE)

# Enable master output
spi_master_oe(channel, queue, 1)

# Perform the Transaction
data_received = SPI_Transaction_Array(channel, queue, byte_array)

# Disable master output
spi_master_oe(channel, queue, 0)


print("Received data from SPI:")
print("[" + ", ".join(f"0x{byte:02X}" for byte in data_received) + "]")


# Close the device and exit
dev_close(pm, conn, channel)

  
  
  
