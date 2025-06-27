/*=========================================================================
| Promira Management Interface Library
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
| To access Promira devices through the API:
|
| 1) Use one of the following shared objects:
|      promira.so       --  Linux or macOS shared object
|      promira.dll      --  Windows dynamic link library
|
| 2) Along with one of the following language modules:
|      promira.c/h      --  C/C++ API header file and interface module
|      promira_py.py    --  Python API
|      promira.cs       --  C# .NET source
 ========================================================================*/

using System;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitleAttribute("Promira .NET binding")]
[assembly: AssemblyDescriptionAttribute(".NET binding for Promira")]
[assembly: AssemblyCompanyAttribute("Total Phase, Inc.")]
[assembly: AssemblyProductAttribute("Promira")]
[assembly: AssemblyCopyrightAttribute("Total Phase, Inc. 2022")]

namespace TotalPhase {

public enum PromiraStatus : int {
    /* General codes (0 to -99) */
    PM_OK                        =    0,
    PM_UNABLE_TO_LOAD_LIBRARY    =   -1,
    PM_UNABLE_TO_LOAD_DRIVER     =   -2,
    PM_UNABLE_TO_LOAD_FUNCTION   =   -3,
    PM_INCOMPATIBLE_LIBRARY      =   -4,
    PM_INCOMPATIBLE_DEVICE       =   -5,
    PM_COMMUNICATION_ERROR       =   -6,
    PM_UNABLE_TO_OPEN            =   -7,
    PM_UNABLE_TO_CLOSE           =   -8,
    PM_INVALID_HANDLE            =   -9,
    PM_CONFIG_ERROR              =  -10,
    PM_SHORT_BUFFER              =  -11,
    PM_FUNCTION_NOT_AVAILABLE    =  -12,

    /* Load command error codes (-100 to -199) */
    PM_APP_NOT_FOUND             = -101,
    PM_INVALID_LICENSE           = -102,
    PM_UNABLE_TO_LOAD_APP        = -103,
    PM_INVALID_DEVICE            = -104,
    PM_INVALID_DATE              = -105,
    PM_NOT_LICENSED              = -106,
    PM_INVALID_APP               = -107,
    PM_INVALID_FEATURE           = -108,
    PM_UNLICENSED_APP            = -109,
    PM_APP_ALREADY_LOADED        = -110,

    /* Network info error codes (-200 to -299) */
    PM_NETCONFIG_ERROR           = -201,
    PM_INVALID_IPADDR            = -202,
    PM_INVALID_NETMASK           = -203,
    PM_INVALID_SUBNET            = -204,
    PM_NETCONFIG_UNSUPPORTED     = -205,
    PM_NETCONFIG_LOST_CONNECTION = -206
}

public enum PromiraNetCommand : int {
    PM_NET_ETH_ENABLE      = 0,
    PM_NET_ETH_IP          = 1,
    PM_NET_ETH_NETMASK     = 2,
    PM_NET_ETH_MAC         = 3,
    PM_NET_ETH_DHCP_ENABLE = 4,
    PM_NET_ETH_DHCP_RENEW  = 5,
    PM_NET_USB_IP          = 6,
    PM_NET_USB_NETMASK     = 7,
    PM_NET_USB_MAC         = 8
}

public enum PromiraLoadFlags : int {
    PM_LOAD_NO_FLAGS = 0x00,
    PM_LOAD_UNLOAD   = 0x01
}


public class PromiraApi {

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
[DllImport ("promira")]
private static extern int pm_c_version ();

public const int PM_API_VERSION    = 0x013c;   // v1.60
public const int PM_REQ_SW_VERSION = 0x0128;   // v1.40

private static short PM_SW_VERSION;
private static short PM_REQ_API_VERSION;
private static bool  PM_LIBRARY_LOADED;

static PromiraApi () {
    PM_SW_VERSION      = (short)(pm_c_version() & 0xffff);
    PM_REQ_API_VERSION = (short)((pm_c_version() >> 16) & 0xffff);
    PM_LIBRARY_LOADED  = 
        ((PM_SW_VERSION >= PM_REQ_SW_VERSION) &&
         (PM_API_VERSION >= PM_REQ_API_VERSION));
}

/*=========================================================================
| STATUS CODES
 ========================================================================*/
/*
 * All API functions return an integer which is the result of the
 * transaction, or a status code if negative.  The status codes are
 * defined as follows:
 */
// enum PromiraStatus  (from declaration above)
//     PM_OK                        =    0
//     PM_UNABLE_TO_LOAD_LIBRARY    =   -1
//     PM_UNABLE_TO_LOAD_DRIVER     =   -2
//     PM_UNABLE_TO_LOAD_FUNCTION   =   -3
//     PM_INCOMPATIBLE_LIBRARY      =   -4
//     PM_INCOMPATIBLE_DEVICE       =   -5
//     PM_COMMUNICATION_ERROR       =   -6
//     PM_UNABLE_TO_OPEN            =   -7
//     PM_UNABLE_TO_CLOSE           =   -8
//     PM_INVALID_HANDLE            =   -9
//     PM_CONFIG_ERROR              =  -10
//     PM_SHORT_BUFFER              =  -11
//     PM_FUNCTION_NOT_AVAILABLE    =  -12
//     PM_APP_NOT_FOUND             = -101
//     PM_INVALID_LICENSE           = -102
//     PM_UNABLE_TO_LOAD_APP        = -103
//     PM_INVALID_DEVICE            = -104
//     PM_INVALID_DATE              = -105
//     PM_NOT_LICENSED              = -106
//     PM_INVALID_APP               = -107
//     PM_INVALID_FEATURE           = -108
//     PM_UNLICENSED_APP            = -109
//     PM_APP_ALREADY_LOADED        = -110
//     PM_NETCONFIG_ERROR           = -201
//     PM_INVALID_IPADDR            = -202
//     PM_INVALID_NETMASK           = -203
//     PM_INVALID_SUBNET            = -204
//     PM_NETCONFIG_UNSUPPORTED     = -205
//     PM_NETCONFIG_LOST_CONNECTION = -206


/*=========================================================================
| GENERAL TYPE DEFINITIONS
 ========================================================================*/
/* Promira handle type definition */
/* typedef Promira => int */

/*
 * Promira version matrix.
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
public struct PromiraVersion {
    /* Software and firmware versions. */
    public ushort software;
    public ushort firmware;
    public ushort hardware;

    /* Firmware requires that software must be >= this version. */
    public ushort sw_req_by_fw;

    /* Software requires that firmware must be >= this version. */
    public ushort fw_req_by_sw;

    /* Software requires that the API interface must be >= this version. */
    public ushort api_req_by_sw;

    /* (year << 24) | (month << 16) | (day << 8) | build */
    public uint   build;
}


/*=========================================================================
| MANAGEMENT API
 ========================================================================*/
/*
 * Get a list of Promira devices on the network.
 *
 * nelem   = maximum number of elements to return
 * devices = array into which the IP addresses are returned
 *
 * Each element of the array is written with the IP address.
 *
 * If the array is NULL, it is not filled with any values.
 * If there are more devices than the array size, only the
 * first nmemb IP addresses will be written into the array.
 *
 * Returns the number of devices found, regardless of the
 * array size.
 */
public static int pm_find_devices (
    int     num_devices,
    uint[]  devices
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    int devices_num_devices = (int)tp_min(num_devices, devices.Length);
    return net_pm_find_devices(devices_num_devices, devices);
}

/*
 * Get a list of Promira devices on the network.
 *
 * This function is the same as pm_find_devices() except that
 * it returns the unique ID and status of each Promira device.
 * The IDs are guaranteed to be non-zero if valid.
 *
 * The IDs are the unsigned integer representation of the 10-digit
 * serial numbers.
 *
 * If status is PM_DEVICE_NOT_FREE, the device is in-use by
 * another host and is not ready for connection.
 */
public const uint PM_DEVICE_NOT_FREE = 1;
public static int pm_find_devices_ext (
    int     num_devices,
    uint[]  devices,
    int     num_ids,
    uint[]  unique_ids,
    int     num_statuses,
    uint[]  statuses
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    int devices_num_devices = (int)tp_min(num_devices, devices.Length);
    int unique_ids_num_ids = (int)tp_min(num_ids, unique_ids.Length);
    int statuses_num_statuses = (int)tp_min(num_statuses, statuses.Length);
    return net_pm_find_devices_ext(devices_num_devices, devices, unique_ids_num_ids, unique_ids, statuses_num_statuses, statuses);
}

/*
 * Open the Promira device
 *
 * Returns a Promira handle, which is guaranteed to be
 * greater than zero if it is valid.
 *
 * This function is recommended for use in simple applications
 * where extended information is not required.  For more complex
 * applications, the use of pm_open_ext() is recommended.
 *   str net_addr
 */
public static int pm_open (
    string  net_addr
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    return net_pm_open(net_addr);
}

/*
 * Return the version matrix for the device attached to the
 * given handle.  If the handle is 0 or invalid, only the
 * software and required api versions are set.
 */
public static int pm_version (
    int                 promira,
    ref PromiraVersion  version
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    return net_pm_version(promira, ref version);
}

/*
 * Return the version matrix for the application to the
 * given application name. Only firmware is valid and other
 * will be set to 0.
 */
public static int pm_app_version (
    int                 promira,
    string              app_name,
    ref PromiraVersion  app_version
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    return net_pm_app_version(promira, app_name, ref app_version);
}

/* Sleep for the specified number of milliseconds */
public static int pm_sleep_ms (
    uint  milliseconds
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    return net_pm_sleep_ms(milliseconds);
}

/* Network related commands */
// enum PromiraNetCommand  (from declaration above)
//     PM_NET_ETH_ENABLE      = 0
//     PM_NET_ETH_IP          = 1
//     PM_NET_ETH_NETMASK     = 2
//     PM_NET_ETH_MAC         = 3
//     PM_NET_ETH_DHCP_ENABLE = 4
//     PM_NET_ETH_DHCP_RENEW  = 5
//     PM_NET_USB_IP          = 6
//     PM_NET_USB_NETMASK     = 7
//     PM_NET_USB_MAC         = 8

/* Configure the network settings */
public static int pm_query_net (
    int                promira,
    PromiraNetCommand  cmd,
    int                buf_size,
    byte[]             buf
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    int buf_buf_size = (int)tp_min(buf_size, buf.Length);
    return net_pm_query_net(promira, cmd, buf_buf_size, buf);
}

public static int pm_config_net (
    int                promira,
    PromiraNetCommand  cmd,
    string             data
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    return net_pm_config_net(promira, cmd, data);
}

/* Configure the preferences */
public static int pm_query_pref (
    int     promira,
    string  key,
    int     buf_size,
    byte[]  buf
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    int buf_buf_size = (int)tp_min(buf_size, buf.Length);
    return net_pm_query_pref(promira, key, buf_buf_size, buf);
}

public static int pm_config_pref (
    int     promira,
    string  key,
    string  data
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    return net_pm_config_pref(promira, key, data);
}

/* Get a list of apps installed on the device. */
public static int pm_apps (
    int     promira,
    ushort  apps_size,
    byte[]  apps
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    ushort apps_apps_size = (ushort)tp_min(apps_size, apps.Length);
    return net_pm_apps(promira, apps_apps_size, apps);
}

/* Get a list of licensed apps installed on the device. */
public static int pm_licensed_apps (
    int     promira,
    ushort  apps_size,
    byte[]  apps
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    ushort apps_apps_size = (ushort)tp_min(apps_size, apps.Length);
    return net_pm_licensed_apps(promira, apps_apps_size, apps);
}

/* Launch an app */
public static int pm_load (
    int     promira,
    string  app_name
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    return net_pm_load(promira, app_name);
}

// enum PromiraLoadFlags  (from declaration above)
//     PM_LOAD_NO_FLAGS = 0x00
//     PM_LOAD_UNLOAD   = 0x01

/* Launch an app with the flags */
public static int pm_load_ext (
    int               promira,
    string            app_name,
    PromiraLoadFlags  flags
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    return net_pm_load_ext(promira, app_name, flags);
}

/*
 * Retrieve the network address that was used to open the device.
 * Return NULL if the handle is invalid.
 *
 * C programmers should not free the string returned.  It should be
 * valid for as long as the device remains opens, but C callers are
 * advised to reference the value as soon as they can and not cache
 * it for later use.
 */
public static string pm_get_net_addr (
    int  promira
)
{
    if (!PM_LIBRARY_LOADED) return null;
    return Marshal.PtrToStringAnsi(net_pm_get_net_addr(promira));
}

/* Close the Promira device */
public static int pm_close (
    int  promira
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    return net_pm_close(promira);
}

/*
 * Return the unique ID for this Promira host adapter.
 * IDs are guaranteed to be non-zero if valid.
 * The ID is the unsigned integer representation of the
 * 10-digit serial number.
 */
public static uint pm_unique_id (
    int  promira
)
{
    if (!PM_LIBRARY_LOADED) return 0;
    return net_pm_unique_id(promira);
}

/*
 * Return the status string for the given status code.
 * If the code is not valid or the library function cannot
 * be loaded, return a NULL string.
 */
public static string pm_status_string (
    int  status
)
{
    if (!PM_LIBRARY_LOADED) return null;
    return Marshal.PtrToStringAnsi(net_pm_status_string(status));
}

/*
 * Wipe the device in preparation for an OS update
 *
 * Use with caution!!!
 */
public static int pm_init_device (
    int  promira
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    return net_pm_init_device(promira);
}

/*
 * Read currently installed license
 * Pass 0 as buf_size to get the required size of the buffer
 */
public static int pm_read_license (
    int     promira,
    int     buf_size,
    byte[]  buf
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    int buf_buf_size = (int)tp_min(buf_size, buf.Length);
    return net_pm_read_license(promira, buf_buf_size, buf);
}

/*
 * Generate a colon separated string containig the names of features
 * that are licensed for the supplied app.
 */
public static int pm_features (
    int     promira,
    string  app,
    ushort  features_size,
    byte[]  features
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    ushort features_features_size = (ushort)tp_min(features_size, features.Length);
    return net_pm_features(promira, app, features_features_size, features);
}

/*
 * Return the value for the specified feature.  The value returned is
 * the string representation of the value.
 */
public static int pm_feature_value (
    int     promira,
    string  app,
    string  feature,
    ushort  value_size,
    byte[]  value
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    ushort value_value_size = (ushort)tp_min(value_size, value.Length);
    return net_pm_feature_value(promira, app, feature, value_value_size, value);
}

/*
 * Return the description for the specified feature.  The description
 * returned is a string.
 */
public static int pm_feature_description (
    int     promira,
    string  app,
    string  feature,
    ushort  desc_size,
    byte[]  desc
)
{
    if (!PM_LIBRARY_LOADED) return (int)PromiraStatus.PM_INCOMPATIBLE_LIBRARY;
    ushort desc_desc_size = (ushort)tp_min(desc_size, desc.Length);
    return net_pm_feature_description(promira, app, feature, desc_desc_size, desc);
}


/*=========================================================================
| NATIVE DLL BINDINGS
 ========================================================================*/
[DllImport ("promira")]
private static extern int net_pm_find_devices (int num_devices, [Out] uint[] devices);

[DllImport ("promira")]
private static extern int net_pm_find_devices_ext (int num_devices, [Out] uint[] devices, int num_ids, [Out] uint[] unique_ids, int num_statuses, [Out] uint[] statuses);

[DllImport ("promira")]
private static extern int net_pm_open (string net_addr);

[DllImport ("promira")]
private static extern int net_pm_version (int promira, ref PromiraVersion version);

[DllImport ("promira")]
private static extern int net_pm_app_version (int promira, string app_name, ref PromiraVersion app_version);

[DllImport ("promira")]
private static extern int net_pm_sleep_ms (uint milliseconds);

[DllImport ("promira")]
private static extern int net_pm_query_net (int promira, PromiraNetCommand cmd, int buf_size, [Out] byte[] buf);

[DllImport ("promira")]
private static extern int net_pm_config_net (int promira, PromiraNetCommand cmd, string data);

[DllImport ("promira")]
private static extern int net_pm_query_pref (int promira, string key, int buf_size, [Out] byte[] buf);

[DllImport ("promira")]
private static extern int net_pm_config_pref (int promira, string key, string data);

[DllImport ("promira")]
private static extern int net_pm_apps (int promira, ushort apps_size, [Out] byte[] apps);

[DllImport ("promira")]
private static extern int net_pm_licensed_apps (int promira, ushort apps_size, [Out] byte[] apps);

[DllImport ("promira")]
private static extern int net_pm_load (int promira, string app_name);

[DllImport ("promira")]
private static extern int net_pm_load_ext (int promira, string app_name, PromiraLoadFlags flags);

[DllImport ("promira")]
private static extern IntPtr net_pm_get_net_addr (int promira);

[DllImport ("promira")]
private static extern int net_pm_close (int promira);

[DllImport ("promira")]
private static extern uint net_pm_unique_id (int promira);

[DllImport ("promira")]
private static extern IntPtr net_pm_status_string (int status);

[DllImport ("promira")]
private static extern int net_pm_init_device (int promira);

[DllImport ("promira")]
private static extern int net_pm_read_license (int promira, int buf_size, [Out] byte[] buf);

[DllImport ("promira")]
private static extern int net_pm_features (int promira, string app, ushort features_size, [Out] byte[] features);

[DllImport ("promira")]
private static extern int net_pm_feature_value (int promira, string app, string feature, ushort value_size, [Out] byte[] value);

[DllImport ("promira")]
private static extern int net_pm_feature_description (int promira, string app, string feature, ushort desc_size, [Out] byte[] desc);


} // class PromiraApi

} // namespace TotalPhase
