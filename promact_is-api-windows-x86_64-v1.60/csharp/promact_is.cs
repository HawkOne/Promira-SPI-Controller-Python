/*=========================================================================
| promact_is Interface Library
|--------------------------------------------------------------------------
| Copyright (c) 2014-2022 Total Phase, Inc.
| All rights reserved.
| www.totalphase.com
|
| Redistribution and use in source and binary forms, with or without
| modification, are permitted provided that the following conditions
| are met:
|
| - Redistributions of source code must retain the above copyright
|   notice, this list of conditions and the following disclaimer.
|
| - Redistributions in binary form must reproduce the above copyright
|   notice, this list of conditions and the following disclaimer in the
|   documentation and/or other materials provided with the distribution.
|
| - Neither the name of Total Phase, Inc. nor the names of its
|   contributors may be used to endorse or promote products derived from
|   this software without specific prior written permission.
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
|--------------------------------------------------------------------------
| To access promact_is through the API:
|
| 1) Use one of the following shared objects:
|      promact_is.so     --  Linux or macOS shared object
|      promact_is.dll    --  Windows dynamic link library
|
| 2) Along with one of the following language modules:
|      promact_is.c/h    --  C/C++ API header file and interface module
|      promact_is_py.py  --  Python API
|      promact_is.cs     --  C# .NET source
 ========================================================================*/

using System;
using System.Reflection;
using System.Runtime.InteropServices;

//[assembly: AssemblyTitleAttribute("Promact_is .NET binding")]
//[assembly: AssemblyDescriptionAttribute(".NET binding for Promact_is")]
//[assembly: AssemblyCompanyAttribute("Total Phase, Inc.")]
//[assembly: AssemblyProductAttribute("Promact_is")]
//[assembly: AssemblyCopyrightAttribute("Total Phase, Inc. 2022")]

namespace TotalPhase {

public enum PromiraAppStatus : int {
    /* General codes (0 to -99) */
    PS_APP_OK                        =    0,
    PS_APP_UNABLE_TO_LOAD_LIBRARY    =   -1,
    PS_APP_UNABLE_TO_LOAD_DRIVER     =   -2,
    PS_APP_UNABLE_TO_LOAD_FUNCTION   =   -3,
    PS_APP_INCOMPATIBLE_LIBRARY      =   -4,
    PS_APP_INCOMPATIBLE_DEVICE       =   -5,
    PS_APP_COMMUNICATION_ERROR       =   -6,
    PS_APP_UNABLE_TO_OPEN            =   -7,
    PS_APP_UNABLE_TO_CLOSE           =   -8,
    PS_APP_INVALID_HANDLE            =   -9,
    PS_APP_CONFIG_ERROR              =  -10,
    PS_APP_MEMORY_ALLOC_ERROR        =  -11,
    PS_APP_UNABLE_TO_INIT_SUBSYSTEM  =  -12,
    PS_APP_INVALID_LICENSE           =  -13,

    /* Channel related code */
    PS_APP_PENDING_ASYNC_CMD         =  -30,
    PS_APP_TIMEOUT                   =  -31,
    PS_APP_CONNECTION_LOST           =  -32,
    PS_APP_CONNECTION_FULL           =  -33,

    /* Queue related code */
    PS_APP_QUEUE_FULL                =  -50,
    PS_APP_QUEUE_INVALID_CMD_TYPE    =  -51,
    PS_APP_QUEUE_EMPTY               =  -52,

    /* Collect related code */
    PS_APP_NO_MORE_CMDS_TO_COLLECT   =  -80,
    PS_APP_NO_MORE_QUEUES_TO_COLLECT =  -81,
    PS_APP_MISMATCHED_CMD            =  -82,
    PS_APP_UNKNOWN_CMD               =  -83,
    PS_APP_LOST_RESPONSE             =  -84,



    /* I2C codes (-100 to -199) */
    PS_I2C_NOT_AVAILABLE             = -100,
    PS_I2C_NOT_ENABLED               = -101,
    PS_I2C_READ_ERROR                = -102,
    PS_I2C_WRITE_ERROR               = -103,
    PS_I2C_SLAVE_BAD_CONFIG          = -104,
    PS_I2C_SLAVE_READ_ERROR          = -105,
    PS_I2C_SLAVE_TIMEOUT             = -106,
    PS_I2C_DROPPED_EXCESS_BYTES      = -107,
    PS_I2C_BUS_ALREADY_FREE          = -108,

    /* SPI codes (-200 to -299) */
    PS_SPI_NOT_AVAILABLE             = -200,
    PS_SPI_NOT_ENABLED               = -201,
    PS_SPI_WRITE_0_BYTES             = -202,
    PS_SPI_SLAVE_READ_ERROR          = -203,
    PS_SPI_SLAVE_TIMEOUT             = -204,
    PS_SPI_DROPPED_EXCESS_BYTES      = -205,
    PS_SPI_SLAVE_CMD_ERROR           = -206,
    PS_SPI_SLAVE_3WIRE               = -207,

    /* SPI code from Promira */
    PS_SPI_OUTPUT_NOT_ENABLED        = -250,
    PS_SPI_SLAVE_ENABLED             = -251,
    PS_SPI_OUTPUT_ENABLED            = -252
    /* GPIO codes (-300 to -399) */


    /* PHY Serial codes (-1000 to -1099) */

}

public enum PromiraI2cCommand : int {
    PS_I2C_CMD_WRITE    = 100,
    PS_I2C_CMD_READ     = 101,
    PS_I2C_CMD_DELAY_MS = 102
}

public enum PromiraI2cFlags : int {
    PS_I2C_NO_FLAGS          = 0x00,
    PS_I2C_10_BIT_ADDR       = 0x01,
    PS_I2C_COMBINED_FMT      = 0x02,
    PS_I2C_NO_STOP           = 0x04,
    PS_I2C_SIZED_READ        = 0x10,
    PS_I2C_SIZED_READ_EXTRA1 = 0x20
}

public enum PromiraI2cStatus : int {
    PS_I2C_STATUS_OK            = 0,
    PS_I2C_STATUS_BUS_ERROR     = 1,
    PS_I2C_STATUS_SLAVE_ACK     = 2,
    PS_I2C_STATUS_SLAVE_NACK    = 3,
    PS_I2C_STATUS_DATA_NACK     = 4,
    PS_I2C_STATUS_ARB_LOST      = 5,
    PS_I2C_STATUS_BUS_LOCKED    = 6,
    PS_I2C_STATUS_LAST_DATA_ACK = 7
}

public enum PromiraI2cSlaveStatus : int {
    PS_I2C_SLAVE_NO_DATA   = 0x00,
    PS_I2C_SLAVE_READ      = 0x01,
    PS_I2C_SLAVE_WRITE     = 0x02,
    PS_I2C_SLAVE_DATA_LOST = 0x80
}

public enum PromiraSpiCommand : int {
    PS_SPI_CMD_OE           = 200,
    PS_SPI_CMD_SS           = 201,
    PS_SPI_CMD_DELAY_MS     = 202,
    PS_SPI_CMD_DELAY_CYCLES = 203,
    PS_SPI_CMD_DELAY_NS     = 204,
    PS_SPI_CMD_READ         = 205
}

public enum PromiraSpiMode : int {
    PS_SPI_MODE_0 = 0,
    PS_SPI_MODE_1 = 1,
    PS_SPI_MODE_2 = 2,
    PS_SPI_MODE_3 = 3
}

public enum PromiraSpiBitorder : int {
    PS_SPI_BITORDER_MSB = 0,
    PS_SPI_BITORDER_LSB = 1
}

public enum PromiraSpiIOMode : int {
    PS_SPI_IO_STANDARD = 0,
    PS_SPI_IO_DUAL     = 2,
    PS_SPI_IO_QUAD     = 4
}

public enum PromiraSlaveMode : int {
    PS_SPI_SLAVE_MODE_STD = 0
}

public enum PromiraSpiSlaveStatus : int {
    PS_SPI_SLAVE_NO_DATA   = 0x00,
    PS_SPI_SLAVE_DATA      = 0x01,
    PS_SPI_SLAVE_DATA_LOST = 0x80
}

public enum PromiraGpioCommand : int {
    PS_GPIO_CMD_DIRECTION = 300,
    PS_GPIO_CMD_GET       = 301,
    PS_GPIO_CMD_SET       = 302,
    PS_GPIO_CMD_CHANGE    = 303,
    PS_GPIO_CMD_DELAY_MS  = 304
}


public class Promact_isApi {

/*=========================================================================
| HELPER FUNCTIONS / CLASSES
 ========================================================================*/
static long tp_min(long x, long y) { return x < y ? x : y; }

private class GCContext {
    GCHandle[] handles;
    int index;
    public GCContext () {
        handles = new GCHandle[16];
        index   = 0;
    }
    public void add (GCHandle gch) {
        handles[index] = gch;
        index++;
    }
    public void free () {
        while (index != 0) {
            index--;
            handles[index].Free();
        }
    }
}

/*=========================================================================
| VERSION
 ========================================================================*/
[DllImport ("promact_is")]
private static extern int ps_app_c_version ();

public const int PS_APP_API_VERSION    = 0x013c;   // v1.60
public const int PS_APP_REQ_SW_VERSION = 0x0128;   // v1.40

private static short PS_APP_SW_VERSION;
private static short PS_APP_REQ_API_VERSION;
private static bool  PS_APP_LIBRARY_LOADED;

static Promact_isApi () {
    PS_APP_SW_VERSION      = (short)(ps_app_c_version() & 0xffff);
    PS_APP_REQ_API_VERSION = (short)((ps_app_c_version() >> 16) & 0xffff);
    PS_APP_LIBRARY_LOADED  = 
        ((PS_APP_SW_VERSION >= PS_APP_REQ_SW_VERSION) &&
         (PS_APP_API_VERSION >= PS_APP_REQ_API_VERSION));
}

/*=========================================================================
| STATUS CODES
 ========================================================================*/
/*
 * All API functions return an integer which is the result of the
 * transaction, or a status code if negative.  The status codes are
 * defined as follows:
 */
// enum PromiraAppStatus  (from declaration above)
//     PS_APP_OK                        =    0
//     PS_APP_UNABLE_TO_LOAD_LIBRARY    =   -1
//     PS_APP_UNABLE_TO_LOAD_DRIVER     =   -2
//     PS_APP_UNABLE_TO_LOAD_FUNCTION   =   -3
//     PS_APP_INCOMPATIBLE_LIBRARY      =   -4
//     PS_APP_INCOMPATIBLE_DEVICE       =   -5
//     PS_APP_COMMUNICATION_ERROR       =   -6
//     PS_APP_UNABLE_TO_OPEN            =   -7
//     PS_APP_UNABLE_TO_CLOSE           =   -8
//     PS_APP_INVALID_HANDLE            =   -9
//     PS_APP_CONFIG_ERROR              =  -10
//     PS_APP_MEMORY_ALLOC_ERROR        =  -11
//     PS_APP_UNABLE_TO_INIT_SUBSYSTEM  =  -12
//     PS_APP_INVALID_LICENSE           =  -13
//     PS_APP_PENDING_ASYNC_CMD         =  -30
//     PS_APP_TIMEOUT                   =  -31
//     PS_APP_CONNECTION_LOST           =  -32
//     PS_APP_CONNECTION_FULL           =  -33
//     PS_APP_QUEUE_FULL                =  -50
//     PS_APP_QUEUE_INVALID_CMD_TYPE    =  -51
//     PS_APP_QUEUE_EMPTY               =  -52
//     PS_APP_NO_MORE_CMDS_TO_COLLECT   =  -80
//     PS_APP_NO_MORE_QUEUES_TO_COLLECT =  -81
//     PS_APP_MISMATCHED_CMD            =  -82
//     PS_APP_UNKNOWN_CMD               =  -83
//     PS_APP_LOST_RESPONSE             =  -84
//     PS_I2C_NOT_AVAILABLE             = -100
//     PS_I2C_NOT_ENABLED               = -101
//     PS_I2C_READ_ERROR                = -102
//     PS_I2C_WRITE_ERROR               = -103
//     PS_I2C_SLAVE_BAD_CONFIG          = -104
//     PS_I2C_SLAVE_READ_ERROR          = -105
//     PS_I2C_SLAVE_TIMEOUT             = -106
//     PS_I2C_DROPPED_EXCESS_BYTES      = -107
//     PS_I2C_BUS_ALREADY_FREE          = -108
//     PS_SPI_NOT_AVAILABLE             = -200
//     PS_SPI_NOT_ENABLED               = -201
//     PS_SPI_WRITE_0_BYTES             = -202
//     PS_SPI_SLAVE_READ_ERROR          = -203
//     PS_SPI_SLAVE_TIMEOUT             = -204
//     PS_SPI_DROPPED_EXCESS_BYTES      = -205
//     PS_SPI_SLAVE_CMD_ERROR           = -206
//     PS_SPI_SLAVE_3WIRE               = -207
//     PS_SPI_OUTPUT_NOT_ENABLED        = -250
//     PS_SPI_SLAVE_ENABLED             = -251
//     PS_SPI_OUTPUT_ENABLED            = -252


/*=========================================================================
| GENERAL TYPE DEFINITIONS
 ========================================================================*/
/* Connection handle type definition */
/* typedef PromiraConnectionHandle => int */

/* Channel handle type definition */
/* typedef PromiraChannelHandle => int */

/* Queue handle type definition */
/* typedef PromiraQueueHandle => int */

/* Collect handle type definition */
/* typedef PromiraCollectHandle => int */

/*
 * Version matrix.
 *
 * This matrix describes the various version dependencies
 * of Promira components.  It can be used to determine
 * which component caused an incompatibility error.
 *
 * All version numbers are of the format:
 *   (major << 8) | minor
 *
 * ex. v1.20 would be encoded as:  0x0114
 */
[StructLayout(LayoutKind.Sequential)]
public struct PromiraAppVersion {
    /* Software, firmware, and hardware versions. */
    public ushort software;
    public ushort firmware;
    public ushort hardware;

    /* Firmware requires that software must be >= this version. */
    public ushort sw_req_by_fw;

    /* Software requires that firmware must be >= this version. */
    public ushort fw_req_by_sw;

    /* Software requires that the API interface must be >= this version. */
    public ushort api_req_by_sw;
}


/*=========================================================================
| GENERAL API
 ==========================================================================
| Connect to the application
|
| Returns a connection handle, which is guaranteed to be
| greater than zero if it is vali*/
public static int ps_app_connect (
    string  net_addr
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_app_connect(net_addr);
}

/* Disconnect from the application */
public static int ps_app_disconnect (
    int  conn
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_app_disconnect(conn);
}

/*
 * Return the version matrix for the device attached to the
 * given handle.  If the handle is 0 or invalid, only the
 * software and required api versions are set.
 */
public static int ps_app_version (
    int                    channel,
    ref PromiraAppVersion  version
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_app_version(channel, ref version);
}

/* Sleep for the specified number of milliseconds */
public static int ps_app_sleep_ms (
    uint  milliseconds
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_app_sleep_ms(milliseconds);
}

/*
 * Return the status string for the given status code.
 * If the code is not valid or the library function cannot
 * be loaded, return a NULL string.
 */
public static string ps_app_status_string (
    int  status
)
{
    if (!PS_APP_LIBRARY_LOADED) return null;
    return Marshal.PtrToStringAnsi(net_ps_app_status_string(status));
}


/*=========================================================================
| CHANNEL API
 ==========================================================================
| Channel is a logical communication layer that talks to the application.
|
| Open a logical channel to communicate with the application.
|
| Returns a connection handle, which is guaranteed to be
| greater than zero if it is vali*/
public static int ps_channel_open (
    int  conn
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_channel_open(conn);
}

/* Close a logical channel. */
public static int ps_channel_close (
    int  channel
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_channel_close(channel);
}

/* Get the number of queues submitted, but not collected through the channel */
public static int ps_channel_submitted_count (
    int  channel
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_channel_submitted_count(channel);
}

/* Get the number of queues completed and uncollected through the channel */
public static int ps_channel_uncollected_count (
    int  channel
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_channel_uncollected_count(channel);
}


/*=========================================================================
| QUEUE API
 ==========================================================================
| In order to use the Promira to send data across the bus at high
| speed, commands are accumulated in a queue until a call is made to
| submit all of the queued commands.  A queue can contain only commands
| with same type. For instance, any SPI command cannot be appended to
| I2C queue.
|
| Create the queue.  queue_type is one of the PS_MODULE_ID_* value*/
public static int ps_queue_create (
    int   conn,
    byte  queue_type
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_create(conn, queue_type);
}

/* Destroy the queue. */
public static int ps_queue_destroy (
    int  queue
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_destroy(queue);
}

/*
 * Clear all commands in the queue.
 * Please note that the queue is not cleared after it has been submitted.
 */
public static int ps_queue_clear (
    int  queue
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_clear(queue);
}

/* Append a delay commmand into the queue. */
public static int ps_queue_delay_ms (
    int  queue,
    int  milliseconds
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_delay_ms(queue, milliseconds);
}

/*
 * Append a sync command into the queue.  Waits for all previous
 * commands to compelte entirely.
 */
public static int ps_queue_sync (
    int  queue
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_sync(queue);
}

/* Return the number of commands in the queue. */
public static int ps_queue_size (
    int  queue
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_size(queue);
}

/*
 * Submit the queue.
 *
 * After the operation completes, the queue is untouched.  Therefore,
 * this function may be called repeatedly in rapid succession.  Waits
 * for the first command in the queue to complete.  Returns a
 * PromiraCollectHandle.
 */
public static int ps_queue_submit (
    int       queue,
    int       channel,
    byte      ctrl_id,
    ref byte  queue_type
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_submit(queue, channel, ctrl_id, ref queue_type);
}

/*
 * Submit the queue asynchronously.
 *
 * After the operation completes, the queue is untouched.  Therefore,
 * this function may be called repeatedly in rapid succession.
 */
public static int ps_queue_async_submit (
    int   queue,
    int   channel,
    byte  ctrl_id
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_async_submit(queue, channel, ctrl_id);
}

/*
 * Collect the next queue that was previously submitted asynchronously.
 * Waits for the first command in the queue to complete.  queue_type is
 * one of the PS_MODULE_ID_* values.  Returns a PromiraCollectHandle.
 */
public static int ps_queue_async_collect (
    int       channel,
    ref byte  queue_type
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_async_collect(channel, ref queue_type);
}


/*=========================================================================
| Collect API
 ==========================================================================
| Collect the next command response.  Waits for the next command to
| complete or timeout. Returns the command type.  Get the result of
| the command and the length of data to be returned when the command
| specific collect function is calle*/
public static int ps_collect_resp (
    int      collect,
    ref int  length,
    ref int  result,
    int      timeout
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_collect_resp(collect, ref length, ref result, timeout);
}


/*=========================================================================
| I2C Active API
 ========================================================================*/
public const int PS_MODULE_ID_I2C_ACTIVE = 0x00000001;
// enum PromiraI2cCommand  (from declaration above)
//     PS_I2C_CMD_WRITE    = 100
//     PS_I2C_CMD_READ     = 101
//     PS_I2C_CMD_DELAY_MS = 102

/* Free the I2C bus. */
public static int ps_i2c_free_bus (
    int  channel
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_i2c_free_bus(channel);
}

/*
 * Set the bus lock timeout.  If a zero is passed as the timeout,
 * the timeout is unchanged and the current timeout is returned.
 */
public static int ps_i2c_bus_timeout (
    int     channel,
    ushort  timeout_ms
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_i2c_bus_timeout(channel, timeout_ms);
}

/*
 * Set the I2C bit rate in kilohertz.  If a zero is passed as the
 * bitrate, the bitrate is unchanged and the current bitrate is
 * returned.
 */
public static int ps_i2c_bitrate (
    int     channel,
    ushort  bitrate_khz
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_i2c_bitrate(channel, bitrate_khz);
}

// enum PromiraI2cFlags  (from declaration above)
//     PS_I2C_NO_FLAGS          = 0x00
//     PS_I2C_10_BIT_ADDR       = 0x01
//     PS_I2C_COMBINED_FMT      = 0x02
//     PS_I2C_NO_STOP           = 0x04
//     PS_I2C_SIZED_READ        = 0x10
//     PS_I2C_SIZED_READ_EXTRA1 = 0x20

// enum PromiraI2cStatus  (from declaration above)
//     PS_I2C_STATUS_OK            = 0
//     PS_I2C_STATUS_BUS_ERROR     = 1
//     PS_I2C_STATUS_SLAVE_ACK     = 2
//     PS_I2C_STATUS_SLAVE_NACK    = 3
//     PS_I2C_STATUS_DATA_NACK     = 4
//     PS_I2C_STATUS_ARB_LOST      = 5
//     PS_I2C_STATUS_BUS_LOCKED    = 6
//     PS_I2C_STATUS_LAST_DATA_ACK = 7

/*
 * Read a stream of bytes from the I2C slave device.  Fills the number
 * of bytes read into the num_read variable.  The return value of the
 * function is a status code.
 */
public static int ps_i2c_read (
    int              channel,
    ushort           slave_addr,
    PromiraI2cFlags  flags,
    ushort           num_bytes,
    byte[]           data_in,
    ref ushort       num_read
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    ushort data_in_num_bytes = (ushort)tp_min(num_bytes, data_in.Length);
    return net_ps_i2c_read(channel, slave_addr, flags, data_in_num_bytes, data_in, ref num_read);
}

/* Queue I2C read. */
public static int ps_queue_i2c_read (
    int              queue,
    ushort           slave_addr,
    PromiraI2cFlags  flags,
    ushort           num_bytes
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_i2c_read(queue, slave_addr, flags, num_bytes);
}

/* Collect I2C read. */
public static int ps_collect_i2c_read (
    int         collect,
    ushort      num_bytes,
    byte[]      data_in,
    ref ushort  num_read
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    ushort data_in_num_bytes = (ushort)tp_min(num_bytes, data_in.Length);
    return net_ps_collect_i2c_read(collect, data_in_num_bytes, data_in, ref num_read);
}

/*
 * Write a stream of bytes to the I2C slave device.  Fills the number
 * of bytes written into the num_written variable.  The return value of
 * the function is a status code.
 */
public static int ps_i2c_write (
    int              channel,
    ushort           slave_addr,
    PromiraI2cFlags  flags,
    ushort           num_bytes,
    byte[]           data_out,
    ref ushort       num_written
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    ushort data_out_num_bytes = (ushort)tp_min(num_bytes, data_out.Length);
    return net_ps_i2c_write(channel, slave_addr, flags, data_out_num_bytes, data_out, ref num_written);
}

/* Queue I2C write. */
public static int ps_queue_i2c_write (
    int              queue,
    ushort           slave_addr,
    PromiraI2cFlags  flags,
    ushort           num_bytes,
    byte[]           data_out
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    ushort data_out_num_bytes = (ushort)tp_min(num_bytes, data_out.Length);
    return net_ps_queue_i2c_write(queue, slave_addr, flags, data_out_num_bytes, data_out);
}

/* Collect I2C write. */
public static int ps_collect_i2c_write (
    int         collect,
    ref ushort  num_written
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_collect_i2c_write(collect, ref num_written);
}

/* Enable the Promira as an I2C slave */
public static int ps_i2c_slave_enable (
    int     channel,
    ushort  addr,
    ushort  maxTxBytes,
    ushort  maxRxBytes
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_i2c_slave_enable(channel, addr, maxTxBytes, maxRxBytes);
}

/* Disable the Promira as an I2C slave */
public static int ps_i2c_slave_disable (
    int  channel
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_i2c_slave_disable(channel);
}

/* Set the slave response. */
public static int ps_i2c_slave_set_resp (
    int     channel,
    byte    num_bytes,
    byte[]  data_out
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    byte data_out_num_bytes = (byte)tp_min(num_bytes, data_out.Length);
    return net_ps_i2c_slave_set_resp(channel, data_out_num_bytes, data_out);
}

/*
 * Polling function to check if there are any asynchronous
 * messages pending for processing.  The function takes a timeout
 * value in units of milliseconds.  If the timeout is < 0, the
 * function will block until data is received.  If the timeout is 0,
 * the function will perform a non-blocking check.
 */
// enum PromiraI2cSlaveStatus  (from declaration above)
//     PS_I2C_SLAVE_NO_DATA   = 0x00
//     PS_I2C_SLAVE_READ      = 0x01
//     PS_I2C_SLAVE_WRITE     = 0x02
//     PS_I2C_SLAVE_DATA_LOST = 0x80

public static int ps_i2c_slave_poll (
    int  channel,
    int  timeout
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_i2c_slave_poll(channel, timeout);
}

/*
 * Return number of bytes written and status code from a previous
 * Promira->I2C_master transmission.  Since the transmission is
 * happening asynchronously with respect to the PC host software, there
 * could be responses queued up from many previous write transactions.
 */
public static int ps_i2c_slave_write_stats (
    int         channel,
    ref byte    addr,
    ref ushort  num_written
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_i2c_slave_write_stats(channel, ref addr, ref num_written);
}

/* Read the bytes from an I2C slave reception */
public static int ps_i2c_slave_read (
    int         channel,
    ref byte    addr,
    ushort      num_bytes,
    byte[]      data_in,
    ref ushort  num_read
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    ushort data_in_num_bytes = (ushort)tp_min(num_bytes, data_in.Length);
    return net_ps_i2c_slave_read(channel, ref addr, data_in_num_bytes, data_in, ref num_read);
}

/*
 * Return number of I2C transactions lost due to the overflow of the
 * internal Promira buffer.
 */
public static int ps_i2c_slave_data_lost_stats (
    int  channel
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_i2c_slave_data_lost_stats(channel);
}


/*=========================================================================
| SPI Active API
 ========================================================================*/
public const int PS_MODULE_ID_SPI_ACTIVE = 0x00000002;
// enum PromiraSpiCommand  (from declaration above)
//     PS_SPI_CMD_OE           = 200
//     PS_SPI_CMD_SS           = 201
//     PS_SPI_CMD_DELAY_MS     = 202
//     PS_SPI_CMD_DELAY_CYCLES = 203
//     PS_SPI_CMD_DELAY_NS     = 204
//     PS_SPI_CMD_READ         = 205

/*
 * Set the SPI bitrate in kilohertz.  If a zero is passed as the
 * bitrate, the bitrate is unchanged and the current bitrate is
 * returned.
 */
public static int ps_spi_bitrate (
    int   channel,
    uint  bitrate_khz
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_spi_bitrate(channel, bitrate_khz);
}

/*
 * These configuration parameters specify how to clock the bits that
 * are sent and received on SPI interface.  See the the Promira User
 * Manual for more details.
 */
// enum PromiraSpiMode  (from declaration above)
//     PS_SPI_MODE_0 = 0
//     PS_SPI_MODE_1 = 1
//     PS_SPI_MODE_2 = 2
//     PS_SPI_MODE_3 = 3

// enum PromiraSpiBitorder  (from declaration above)
//     PS_SPI_BITORDER_MSB = 0
//     PS_SPI_BITORDER_LSB = 1

/*
 * Configure the SPI master and slave interface.  Each of bits of
 * ss_polarity corresponds to each of the SS lines.
 */
public static int ps_spi_configure (
    int                 channel,
    PromiraSpiMode      mode,
    PromiraSpiBitorder  bitorder,
    byte                ss_polarity
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_spi_configure(channel, mode, bitorder, ss_polarity);
}

/*
 * Configure the delays. word_delay is the the number of clock cycles
 * between words.  word_delay can not be 1.  There is no gap after the
 * last word.
 */
public static int ps_spi_configure_delays (
    int   channel,
    byte  word_delay
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_spi_configure_delays(channel, word_delay);
}

/*
 * Enable/disable slave select lines.  When a slave select line is
 * enabled, the corresponding GPIO is disabled.
 */
public static int ps_spi_enable_ss (
    int   channel,
    byte  ss_enable
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_spi_enable_ss(channel, ss_enable);
}

/* Enable/disable driving the SPI data and clock signals */
public static int ps_queue_spi_oe (
    int   queue,
    byte  oe
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_spi_oe(queue, oe);
}

/*
 * Assert the slave select lines.  Set the bit value for a given SS
 * line to 1 to assert the line or 0 to deassert the line.  The
 * polarity is determined by ps_spi_configure.
 */
public static int ps_queue_spi_ss (
    int   queue,
    byte  ss_assert
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_spi_ss(queue, ss_assert);
}

/* Queue a delay in clock cycles. */
public static int ps_queue_spi_delay_cycles (
    int   queue,
    uint  cycles
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_spi_delay_cycles(queue, cycles);
}

/* Queue a delay in nanoseconds. Maximum delay is 2 secs. */
public static int ps_queue_spi_delay_ns (
    int   queue,
    uint  nanoseconds
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_spi_delay_ns(queue, nanoseconds);
}

// enum PromiraSpiIOMode  (from declaration above)
//     PS_SPI_IO_STANDARD = 0
//     PS_SPI_IO_DUAL     = 2
//     PS_SPI_IO_QUAD     = 4

/*
 * Queue an array to be shifted.  data_out is a byte array containing
 * packed words.  word_size is the number of bits in a word and is
 * between 2 and 32.
 */
public static int ps_queue_spi_write (
    int               queue,
    PromiraSpiIOMode  io,
    byte              word_size,
    uint              out_num_words,
    byte[]            data_out
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    int data_out_0xFFFFF = (int)tp_min(0xFFFFF, data_out.Length);
    return net_ps_queue_spi_write(queue, io, word_size, out_num_words, data_out);
}

/*
 * Queue a single word out_num_word times.  word_size is the number of
 * bits in a word and is between 2 and 32.
 */
public static int ps_queue_spi_write_word (
    int               queue,
    PromiraSpiIOMode  io,
    byte              word_size,
    uint              out_num_words,
    uint              word
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_spi_write_word(queue, io, word_size, out_num_words, word);
}

/*
 * Queue an SPI read command.  Equivalent to ps_queue_spi_write_word
 * with word equal to 0 when io is PS_SPI_IO_STANDARD.  When io is
 * PS_SPI_IO_DUAL or PS_SPI_IO_QUAD, the clock is generated and the
 * data lines are set to inputs.
 */
public static int ps_queue_spi_read (
    int               queue,
    PromiraSpiIOMode  io,
    byte              word_size,
    uint              in_num_words
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_spi_read(queue, io, word_size, in_num_words);
}

/*
 * Collect the actual SPI data received.  Returns number of words
 * received.  This should be called after ps_collect_resp returns
 * PS_SPI_CMD_READ.
 */
public static int ps_collect_spi_read (
    int       collect,
    ref byte  word_size,
    uint      in_num_bytes,
    byte[]    data_in
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    uint data_in_in_num_bytes = (uint)tp_min(in_num_bytes, data_in.Length);
    return net_ps_collect_spi_read(collect, ref word_size, data_in_in_num_bytes, data_in);
}

// enum PromiraSlaveMode  (from declaration above)
//     PS_SPI_SLAVE_MODE_STD = 0

/* Enable the Promira as an SPI slave device */
public static int ps_spi_slave_enable (
    int               channel,
    PromiraSlaveMode  mode
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_spi_slave_enable(channel, mode);
}

/* Disable the Promira as an SPI slave device */
public static int ps_spi_slave_disable (
    int  channel
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_spi_slave_disable(channel);
}

/* Configure the slave parameters */
public const int PS_SPI_SLAVE_NO_SS = 0x00000001;
public const int PS_SPI_MULTI_IO_WRITE = 0x00000000;
public const int PS_SPI_MULTI_IO_READ = 0x00000002;
public static int ps_spi_std_slave_configure (
    int               channel,
    PromiraSpiIOMode  io,
    byte              flags
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_spi_std_slave_configure(channel, io, flags);
}

/*
 * When PS_SPI_SLAVE_NO_SS is configured, this timeout determines a SPI
 * transaction.  Returns actual ns to be used.  Minimum is 1us.
 */
public static int ps_spi_slave_timeout (
    int   channel,
    uint  timeout_ns
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_spi_slave_timeout(channel, timeout_ns);
}

/* Allow collecting partial slave data. */
public static int ps_spi_slave_host_read_size (
    int   channel,
    uint  read_size
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_spi_slave_host_read_size(channel, read_size);
}

/* Set the slave response. */
public static int ps_spi_std_slave_set_resp (
    int     channel,
    ushort  num_bytes,
    byte[]  resp
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    ushort resp_num_bytes = (ushort)tp_min(num_bytes, resp.Length);
    return net_ps_spi_std_slave_set_resp(channel, resp_num_bytes, resp);
}

/*
 * Polling function to check if there are any asynchronous messages
 * pending for processing.  The function takes a timeout value in units
 * of milliseconds.  If the timeout is < 0, the function will block
 * until data is received.  If the timeout is 0, the function will
 * perform a non-blocking check.
 */
// enum PromiraSpiSlaveStatus  (from declaration above)
//     PS_SPI_SLAVE_NO_DATA   = 0x00
//     PS_SPI_SLAVE_DATA      = 0x01
//     PS_SPI_SLAVE_DATA_LOST = 0x80

public static int ps_spi_slave_poll (
    int  channel,
    int  timeout
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_spi_slave_poll(channel, timeout);
}

[StructLayout(LayoutKind.Sequential)]
public struct PromiraSpiSlaveReadInfo {
    public uint in_data_bits;
    public uint out_data_bits;
    public byte header_bits;
    public byte resp_id;
    public byte ss_mask;
    public byte is_last;
}

/*
 * Read the data from an SPI slave reception.  Returns number of bytes
 * put into data_in.
 */
public static int ps_spi_slave_read (
    int                          channel,
    ref PromiraSpiSlaveReadInfo  read_info,
    uint                         in_num_bytes,
    byte[]                       data_in
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    uint data_in_in_num_bytes = (uint)tp_min(in_num_bytes, data_in.Length);
    return net_ps_spi_slave_read(channel, ref read_info, data_in_in_num_bytes, data_in);
}

/*
 * Return number of SPI transactions lost due to the overflow of the
 * internal Promira buffer.
 */
public static int ps_spi_slave_data_lost_stats (
    int  channel
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_spi_slave_data_lost_stats(channel);
}


/*=========================================================================
| GPIO API
 ========================================================================*/
public const int PS_MODULE_ID_GPIO = 0x00000003;
// enum PromiraGpioCommand  (from declaration above)
//     PS_GPIO_CMD_DIRECTION = 300
//     PS_GPIO_CMD_GET       = 301
//     PS_GPIO_CMD_SET       = 302
//     PS_GPIO_CMD_CHANGE    = 303
//     PS_GPIO_CMD_DELAY_MS  = 304

/* Returns which GPIO pins are currently active. */
public static int ps_gpio_query (
    int  channel
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_gpio_query(channel);
}

/*
 * Configure the GPIO, specifying the direction of each bit.
 *
 * A call to this function will not change the value of the pullup
 * mask in the Promira.  This is illustrated by the following
 * example:
 *   (1) Direction mask is first set to 0x00
 *   (2) Pullup is set to 0x01
 *   (3) Direction mask is set to 0x01
 *   (4) Direction mask is later set back to 0x00.
 *
 * The pullup will be active after (4).
 *
 * On Promira power-up, the default value of the direction
 * mask is 0x00.
 */
public const byte PS_GPIO_DIR_INPUT = 0;
public const byte PS_GPIO_DIR_OUTPUT = 1;
public static int ps_gpio_direction (
    int   channel,
    uint  direction_mask
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_gpio_direction(channel, direction_mask);
}

public static int ps_queue_gpio_direction (
    int   queue,
    uint  direction_mask
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_gpio_direction(queue, direction_mask);
}

/*
 * Read the current digital values on the GPIO input lines.
 *
 * If a line is configured as an output, its corresponding bit
 * position in the mask will be undefined.
 */
public static int ps_gpio_get (
    int  channel
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_gpio_get(channel);
}

public static int ps_queue_gpio_get (
    int  queue
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_gpio_get(queue);
}

/*
 * Set the outputs on the GPIO lines.
 *
 * Note: If a line is configured as an input, it will not be
 * affected by this call, but the output value for that line
 * will be cached in the event that the line is later
 * configured as an output.
 */
public static int ps_gpio_set (
    int   channel,
    uint  value
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_gpio_set(channel, value);
}

public static int ps_queue_gpio_set (
    int   queue,
    uint  value
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_gpio_set(queue, value);
}

/*
 * Block until there is a change on the GPIO input lines.
 * Pins configured as outputs will be ignored.
 *
 * The function will return either when a change has occurred or
 * the timeout expires.  If the timeout expires, this function
 * will return the current state of the GPIO lines.
 *
 * If the function ps_gpio_get is called before calling
 * ps_gpio_change, ps_gpio_change will only register any changes
 * from the value last returned by ps_gpio_get.
 */
public static int ps_gpio_change (
    int  channel,
    int  timeout
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_gpio_change(channel, timeout);
}

public static int ps_queue_gpio_change (
    int  queue,
    int  timeout
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_queue_gpio_change(queue, timeout);
}


/*=========================================================================
| Serial PHY Module API
 ========================================================================*/
public const int PS_MODULE_ID_PHY = 0x000000ff;
/*
 * Configure the device by enabling/disabling I2C, SPI, and
 * GPIO functions.
 */
public const int PS_APP_CONFIG_GPIO = 0x00000000;
public const int PS_APP_CONFIG_SPI = 0x00000001;
public const int PS_APP_CONFIG_I2C = 0x00000010;
public const int PS_APP_CONFIG_QUERY = -1;
public static int ps_app_configure (
    int  channel,
    int  config
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_app_configure(channel, config);
}

/* Configure the I2C pullup resistors. */
public const byte PS_I2C_PULLUP_NONE = 0x00;
public const byte PS_I2C_PULLUP_BOTH = 0x03;
public const byte PS_I2C_PULLUP_QUERY = 0x80;
public static int ps_i2c_pullup (
    int   channel,
    byte  pullup_mask
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_i2c_pullup(channel, pullup_mask);
}

/* Configure the target power pins. */
public const byte PS_PHY_TARGET_POWER_NONE = 0x00;
public const byte PS_PHY_TARGET_POWER_TARGET1_5V = 0x01;
public const byte PS_PHY_TARGET_POWER_TARGET1_3V = 0x05;
public const byte PS_PHY_TARGET_POWER_TARGET2 = 0x02;
public const byte PS_PHY_TARGET_POWER_BOTH = 0x03;
public const byte PS_PHY_TARGET_POWER_QUERY = 0x80;
public static int ps_phy_target_power (
    int   channel,
    byte  power_mask
)
{
    if (!PS_APP_LIBRARY_LOADED) return (int)PromiraAppStatus.PS_APP_INCOMPATIBLE_LIBRARY;
    return net_ps_phy_target_power(channel, power_mask);
}

/* Configure the power of output signal. */
public const float PS_PHY_LEVEL_SHIFT_QUERY = -1;
public static float ps_phy_level_shift (
    int    channel,
    float  level
)
{
    if (!PS_APP_LIBRARY_LOADED) return 0;
    return net_ps_phy_level_shift(channel, level);
}


/*=========================================================================
| NATIVE DLL BINDINGS
 ========================================================================*/
[DllImport ("promact_is")]
private static extern int net_ps_app_connect (string net_addr);

[DllImport ("promact_is")]
private static extern int net_ps_app_disconnect (int conn);

[DllImport ("promact_is")]
private static extern int net_ps_app_version (int channel, ref PromiraAppVersion version);

[DllImport ("promact_is")]
private static extern int net_ps_app_sleep_ms (uint milliseconds);

[DllImport ("promact_is")]
private static extern IntPtr net_ps_app_status_string (int status);

[DllImport ("promact_is")]
private static extern int net_ps_channel_open (int conn);

[DllImport ("promact_is")]
private static extern int net_ps_channel_close (int channel);

[DllImport ("promact_is")]
private static extern int net_ps_channel_submitted_count (int channel);

[DllImport ("promact_is")]
private static extern int net_ps_channel_uncollected_count (int channel);

[DllImport ("promact_is")]
private static extern int net_ps_queue_create (int conn, byte queue_type);

[DllImport ("promact_is")]
private static extern int net_ps_queue_destroy (int queue);

[DllImport ("promact_is")]
private static extern int net_ps_queue_clear (int queue);

[DllImport ("promact_is")]
private static extern int net_ps_queue_delay_ms (int queue, int milliseconds);

[DllImport ("promact_is")]
private static extern int net_ps_queue_sync (int queue);

[DllImport ("promact_is")]
private static extern int net_ps_queue_size (int queue);

[DllImport ("promact_is")]
private static extern int net_ps_queue_submit (int queue, int channel, byte ctrl_id, ref byte queue_type);

[DllImport ("promact_is")]
private static extern int net_ps_queue_async_submit (int queue, int channel, byte ctrl_id);

[DllImport ("promact_is")]
private static extern int net_ps_queue_async_collect (int channel, ref byte queue_type);

[DllImport ("promact_is")]
private static extern int net_ps_collect_resp (int collect, ref int length, ref int result, int timeout);

[DllImport ("promact_is")]
private static extern int net_ps_i2c_free_bus (int channel);

[DllImport ("promact_is")]
private static extern int net_ps_i2c_bus_timeout (int channel, ushort timeout_ms);

[DllImport ("promact_is")]
private static extern int net_ps_i2c_bitrate (int channel, ushort bitrate_khz);

[DllImport ("promact_is")]
private static extern int net_ps_i2c_read (int channel, ushort slave_addr, PromiraI2cFlags flags, ushort num_bytes, [Out] byte[] data_in, ref ushort num_read);

[DllImport ("promact_is")]
private static extern int net_ps_queue_i2c_read (int queue, ushort slave_addr, PromiraI2cFlags flags, ushort num_bytes);

[DllImport ("promact_is")]
private static extern int net_ps_collect_i2c_read (int collect, ushort num_bytes, [Out] byte[] data_in, ref ushort num_read);

[DllImport ("promact_is")]
private static extern int net_ps_i2c_write (int channel, ushort slave_addr, PromiraI2cFlags flags, ushort num_bytes, [In] byte[] data_out, ref ushort num_written);

[DllImport ("promact_is")]
private static extern int net_ps_queue_i2c_write (int queue, ushort slave_addr, PromiraI2cFlags flags, ushort num_bytes, [In] byte[] data_out);

[DllImport ("promact_is")]
private static extern int net_ps_collect_i2c_write (int collect, ref ushort num_written);

[DllImport ("promact_is")]
private static extern int net_ps_i2c_slave_enable (int channel, ushort addr, ushort maxTxBytes, ushort maxRxBytes);

[DllImport ("promact_is")]
private static extern int net_ps_i2c_slave_disable (int channel);

[DllImport ("promact_is")]
private static extern int net_ps_i2c_slave_set_resp (int channel, byte num_bytes, [In] byte[] data_out);

[DllImport ("promact_is")]
private static extern int net_ps_i2c_slave_poll (int channel, int timeout);

[DllImport ("promact_is")]
private static extern int net_ps_i2c_slave_write_stats (int channel, ref byte addr, ref ushort num_written);

[DllImport ("promact_is")]
private static extern int net_ps_i2c_slave_read (int channel, ref byte addr, ushort num_bytes, [Out] byte[] data_in, ref ushort num_read);

[DllImport ("promact_is")]
private static extern int net_ps_i2c_slave_data_lost_stats (int channel);

[DllImport ("promact_is")]
private static extern int net_ps_spi_bitrate (int channel, uint bitrate_khz);

[DllImport ("promact_is")]
private static extern int net_ps_spi_configure (int channel, PromiraSpiMode mode, PromiraSpiBitorder bitorder, byte ss_polarity);

[DllImport ("promact_is")]
private static extern int net_ps_spi_configure_delays (int channel, byte word_delay);

[DllImport ("promact_is")]
private static extern int net_ps_spi_enable_ss (int channel, byte ss_enable);

[DllImport ("promact_is")]
private static extern int net_ps_queue_spi_oe (int queue, byte oe);

[DllImport ("promact_is")]
private static extern int net_ps_queue_spi_ss (int queue, byte ss_assert);

[DllImport ("promact_is")]
private static extern int net_ps_queue_spi_delay_cycles (int queue, uint cycles);

[DllImport ("promact_is")]
private static extern int net_ps_queue_spi_delay_ns (int queue, uint nanoseconds);

[DllImport ("promact_is")]
private static extern int net_ps_queue_spi_write (int queue, PromiraSpiIOMode io, byte word_size, uint out_num_words, [In] byte[] data_out);

[DllImport ("promact_is")]
private static extern int net_ps_queue_spi_write_word (int queue, PromiraSpiIOMode io, byte word_size, uint out_num_words, uint word);

[DllImport ("promact_is")]
private static extern int net_ps_queue_spi_read (int queue, PromiraSpiIOMode io, byte word_size, uint in_num_words);

[DllImport ("promact_is")]
private static extern int net_ps_collect_spi_read (int collect, ref byte word_size, uint in_num_bytes, [Out] byte[] data_in);

[DllImport ("promact_is")]
private static extern int net_ps_spi_slave_enable (int channel, PromiraSlaveMode mode);

[DllImport ("promact_is")]
private static extern int net_ps_spi_slave_disable (int channel);

[DllImport ("promact_is")]
private static extern int net_ps_spi_std_slave_configure (int channel, PromiraSpiIOMode io, byte flags);

[DllImport ("promact_is")]
private static extern int net_ps_spi_slave_timeout (int channel, uint timeout_ns);

[DllImport ("promact_is")]
private static extern int net_ps_spi_slave_host_read_size (int channel, uint read_size);

[DllImport ("promact_is")]
private static extern int net_ps_spi_std_slave_set_resp (int channel, ushort num_bytes, [In] byte[] resp);

[DllImport ("promact_is")]
private static extern int net_ps_spi_slave_poll (int channel, int timeout);

[DllImport ("promact_is")]
private static extern int net_ps_spi_slave_read (int channel, ref PromiraSpiSlaveReadInfo read_info, uint in_num_bytes, [Out] byte[] data_in);

[DllImport ("promact_is")]
private static extern int net_ps_spi_slave_data_lost_stats (int channel);

[DllImport ("promact_is")]
private static extern int net_ps_gpio_query (int channel);

[DllImport ("promact_is")]
private static extern int net_ps_gpio_direction (int channel, uint direction_mask);

[DllImport ("promact_is")]
private static extern int net_ps_queue_gpio_direction (int queue, uint direction_mask);

[DllImport ("promact_is")]
private static extern int net_ps_gpio_get (int channel);

[DllImport ("promact_is")]
private static extern int net_ps_queue_gpio_get (int queue);

[DllImport ("promact_is")]
private static extern int net_ps_gpio_set (int channel, uint value);

[DllImport ("promact_is")]
private static extern int net_ps_queue_gpio_set (int queue, uint value);

[DllImport ("promact_is")]
private static extern int net_ps_gpio_change (int channel, int timeout);

[DllImport ("promact_is")]
private static extern int net_ps_queue_gpio_change (int queue, int timeout);

[DllImport ("promact_is")]
private static extern int net_ps_app_configure (int channel, int config);

[DllImport ("promact_is")]
private static extern int net_ps_i2c_pullup (int channel, byte pullup_mask);

[DllImport ("promact_is")]
private static extern int net_ps_phy_target_power (int channel, byte power_mask);

[DllImport ("promact_is")]
private static extern float net_ps_phy_level_shift (int channel, float level);


} // class Promact_isApi

} // namespace TotalPhase
