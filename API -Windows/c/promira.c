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


/*=========================================================================
| INCLUDES
 ========================================================================*/
/* This #include can be customized to conform to the user's build paths. */
#include "promira.h"


/*=========================================================================
| VERSION CHECK
 ========================================================================*/
#define PM_CFILE_VERSION   0x013c   /* v1.60 */
#define PM_REQ_SW_VERSION  0x0128   /* v1.40 */

/*
 * Make sure that the header file was included and that
 * the version numbers match.
 */
#ifndef PM_HEADER_VERSION
#  error Unable to include header file. Please check include path.

#elif PM_HEADER_VERSION != PM_CFILE_VERSION
#  error Version mismatch between source and header files.

#endif


/*=========================================================================
| DEFINES
 ========================================================================*/
#define API_NAME                     "promira"
#define API_DEBUG                    PM_DEBUG
#define API_OK                       PM_OK
#define API_UNABLE_TO_LOAD_LIBRARY   PM_UNABLE_TO_LOAD_LIBRARY
#define API_INCOMPATIBLE_LIBRARY     PM_INCOMPATIBLE_LIBRARY
#define API_UNABLE_TO_LOAD_FUNCTION  PM_UNABLE_TO_LOAD_FUNCTION
#define API_HEADER_VERSION           PM_HEADER_VERSION
#define API_REQ_SW_VERSION           PM_REQ_SW_VERSION


/*=========================================================================
| LINUX AND DARWIN SUPPORT
 ========================================================================*/
#if defined(__APPLE_CC__) && !defined(DARWIN)
#define DARWIN
#endif

#if defined(linux) || defined(DARWIN)

#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <string.h>
#include <fcntl.h>

#ifdef DARWIN
#define DLOPEN_NO_WARN
extern int _NSGetExecutablePath (char *buf, unsigned long *bufsize);
#endif

#include <dlfcn.h>

#define DLL_HANDLE  void *
#define MAX_SO_PATH 256

static char SO_NAME[MAX_SO_PATH+1] = API_NAME ".so";

/*
 * These functions allow the Linux behavior to emulate
 * the Windows behavior as specified below in the Windows
 * support section.
 * 
 * First, search for the shared object in the application
 * binary path, then in the current working directory.
 * 
 * Searching the application binary path requires /proc
 * filesystem support, which is standard in 2.4.x kernels.
 * 
 * If the /proc filesystem is not present, the shared object
 * will not be loaded from the execution path unless that path
 * is either the current working directory or explicitly
 * specified in LD_LIBRARY_PATH.
 */
static int _checkPath (const char *path) {
    char *filename = (char *)malloc(strlen(path) +1 + strlen(SO_NAME) +1);
    int   fd;

    // Check if the file is readable
    sprintf(filename, "%s/%s", path, SO_NAME);
    fd = open(filename, O_RDONLY);
    if (fd >= 0) {
        strncpy(SO_NAME, filename, MAX_SO_PATH);
        close(fd);
    }

    // Clean up and exit
    free(filename);
    return (fd >= 0);
}

static int _getExecPath (char *path, unsigned long maxlen) {
#ifdef linux
    return readlink("/proc/self/exe", path, maxlen);
#endif

#ifdef DARWIN
    _NSGetExecutablePath(path, &maxlen);
    return maxlen;
#endif
}

static void _setSearchPath () {
    char  path[MAX_SO_PATH+1];
    int   count;
    char *p;

    /* Make sure that SO_NAME is not an absolute path. */
    if (SO_NAME[0] == '/')  return;

    /* Check the execution directory name. */
    memset(path, 0, sizeof(path));
    count = _getExecPath(path, MAX_SO_PATH);

    if (count > 0) {
        char *p = strrchr(path, '/');
        if (p == path)  ++p;
        if (p != 0)     *p = '\0';

        /* If there is a match, return immediately. */
        if (_checkPath(path))  return;
    }

    /* Check the current working directory. */
    p = getcwd(path, MAX_SO_PATH);
    if (p != 0)  _checkPath(path);
}

#endif


/*=========================================================================
| WINDOWS SUPPORT
 ========================================================================*/
#if defined(WIN32) || defined(_WIN32)

#include <stdio.h>
#include <windows.h>

#define DLL_HANDLE           HINSTANCE
#define dlopen(name, flags)  LoadLibraryA(name)
#define dlsym(handle, name)  GetProcAddress(handle, name)
#define dlerror()            "Exiting program"
#define SO_NAME              API_NAME ".dll"

/*
 * Use the default Windows DLL loading rules:
 *   1.  The directory from which the application binary was loaded.
 *   2.  The application's current directory.
 *   3a. [Windows NT/2000/XP only] 32-bit system directory
 *       (default: c:\winnt\System32)
 *   3b. 16-bit system directory
 *       (default: c:\winnt\System or c:\windows\system)
 *   4.  The windows directory
 *       (default: c:\winnt or c:\windows)
 *   5.  The directories listed in the PATH environment variable
 */
static void _setSearchPath () {
    /* Do nothing */
}

#endif


/*=========================================================================
| SHARED LIBRARY LOADER
 ========================================================================*/
/* The error conditions can be customized depending on the application. */
static void *_loadFunction (const char *name, int *result) {
    static DLL_HANDLE handle = 0;
    void * function = 0;

    /* Load the shared library if necessary */
    if (handle == 0) {
        u32 (*version) (void);
        u16 sw_version;
        u16 api_version_req;

        _setSearchPath();
        handle = dlopen(SO_NAME, RTLD_LAZY);
        if (handle == 0) {
#if API_DEBUG
            fprintf(stderr, "Unable to load %s\n", SO_NAME);
            fprintf(stderr, "%s\n", dlerror());
#endif
            *result = API_UNABLE_TO_LOAD_LIBRARY;
            return 0;
        }

        version = (void *)dlsym(handle, "pm_c_version");
        if (version == 0) {
#if API_DEBUG
            fprintf(stderr, "Unable to bind pm_c_version() in %s\n",
                    SO_NAME);
            fprintf(stderr, "%s\n", dlerror());
#endif
            handle  = 0;
            *result = API_INCOMPATIBLE_LIBRARY;
            return 0;
        }

        sw_version      = (u16)((version() >>  0) & 0xffff);
        api_version_req = (u16)((version() >> 16) & 0xffff);
        if (sw_version  < API_REQ_SW_VERSION ||
            API_HEADER_VERSION < api_version_req)
        {
#if API_DEBUG
            fprintf(stderr, "\nIncompatible versions:\n");

            fprintf(stderr, "  Header version  = v%d.%02d  ",
                    (API_HEADER_VERSION >> 8) & 0xff, API_HEADER_VERSION & 0xff);

            if (sw_version < API_REQ_SW_VERSION)
                fprintf(stderr, "(requires library >= %d.%02d)\n",
                        (API_REQ_SW_VERSION >> 8) & 0xff,
                        API_REQ_SW_VERSION & 0xff);
            else
                fprintf(stderr, "(library version OK)\n");
                        
                   
            fprintf(stderr, "  Library version = v%d.%02d  ",
                    (sw_version >> 8) & 0xff,
                    (sw_version >> 0) & 0xff);

            if (API_HEADER_VERSION < api_version_req)
                fprintf(stderr, "(requires header >= %d.%02d)\n",
                        (api_version_req >> 8) & 0xff,
                        (api_version_req >> 0) & 0xff);
            else
                fprintf(stderr, "(header version OK)\n");
#endif
            handle  = 0;
            *result = API_INCOMPATIBLE_LIBRARY;
            return 0;
        }
    }

    /* Bind the requested function in the shared library */
    function = (void *)dlsym(handle, name);
    *result  = function ? API_OK : API_UNABLE_TO_LOAD_FUNCTION;
    return function;
}


/*=========================================================================
| FUNCTIONS
 ========================================================================*/
static int (*c_pm_find_devices) (int, u32 *) = 0;
int pm_find_devices (
    int   num_devices,
    u32 * devices
)
{
    if (c_pm_find_devices == 0) {
        int res = 0;
        if (!(c_pm_find_devices = _loadFunction("c_pm_find_devices", &res)))
            return res;
    }
    return c_pm_find_devices(num_devices, devices);
}


static int (*c_pm_find_devices_ext) (int, u32 *, int, u32 *, int, u32 *) = 0;
int pm_find_devices_ext (
    int   num_devices,
    u32 * devices,
    int   num_ids,
    u32 * unique_ids,
    int   num_statuses,
    u32 * statuses
)
{
    if (c_pm_find_devices_ext == 0) {
        int res = 0;
        if (!(c_pm_find_devices_ext = _loadFunction("c_pm_find_devices_ext", &res)))
            return res;
    }
    return c_pm_find_devices_ext(num_devices, devices, num_ids, unique_ids, num_statuses, statuses);
}


static Promira (*c_pm_open) (const char *) = 0;
Promira pm_open (
    const char * net_addr
)
{
    if (c_pm_open == 0) {
        int res = 0;
        if (!(c_pm_open = _loadFunction("c_pm_open", &res)))
            return res;
    }
    return c_pm_open(net_addr);
}


static int (*c_pm_version) (Promira, PromiraVersion *) = 0;
int pm_version (
    Promira          promira,
    PromiraVersion * version
)
{
    if (c_pm_version == 0) {
        int res = 0;
        if (!(c_pm_version = _loadFunction("c_pm_version", &res)))
            return res;
    }
    return c_pm_version(promira, version);
}


static int (*c_pm_app_version) (Promira, const char *, PromiraVersion *) = 0;
int pm_app_version (
    Promira          promira,
    const char *     app_name,
    PromiraVersion * app_version
)
{
    if (c_pm_app_version == 0) {
        int res = 0;
        if (!(c_pm_app_version = _loadFunction("c_pm_app_version", &res)))
            return res;
    }
    return c_pm_app_version(promira, app_name, app_version);
}


static int (*c_pm_sleep_ms) (u32) = 0;
int pm_sleep_ms (
    u32 milliseconds
)
{
    if (c_pm_sleep_ms == 0) {
        int res = 0;
        if (!(c_pm_sleep_ms = _loadFunction("c_pm_sleep_ms", &res)))
            return res;
    }
    return c_pm_sleep_ms(milliseconds);
}


static int (*c_pm_query_net) (Promira, PromiraNetCommand, int, u08 *) = 0;
int pm_query_net (
    Promira           promira,
    PromiraNetCommand cmd,
    int               buf_size,
    u08 *             buf
)
{
    if (c_pm_query_net == 0) {
        int res = 0;
        if (!(c_pm_query_net = _loadFunction("c_pm_query_net", &res)))
            return res;
    }
    return c_pm_query_net(promira, cmd, buf_size, buf);
}


static int (*c_pm_config_net) (Promira, PromiraNetCommand, const char *) = 0;
int pm_config_net (
    Promira           promira,
    PromiraNetCommand cmd,
    const char *      data
)
{
    if (c_pm_config_net == 0) {
        int res = 0;
        if (!(c_pm_config_net = _loadFunction("c_pm_config_net", &res)))
            return res;
    }
    return c_pm_config_net(promira, cmd, data);
}


static int (*c_pm_query_pref) (Promira, const char *, int, u08 *) = 0;
int pm_query_pref (
    Promira      promira,
    const char * key,
    int          buf_size,
    u08 *        buf
)
{
    if (c_pm_query_pref == 0) {
        int res = 0;
        if (!(c_pm_query_pref = _loadFunction("c_pm_query_pref", &res)))
            return res;
    }
    return c_pm_query_pref(promira, key, buf_size, buf);
}


static int (*c_pm_config_pref) (Promira, const char *, const char *) = 0;
int pm_config_pref (
    Promira      promira,
    const char * key,
    const char * data
)
{
    if (c_pm_config_pref == 0) {
        int res = 0;
        if (!(c_pm_config_pref = _loadFunction("c_pm_config_pref", &res)))
            return res;
    }
    return c_pm_config_pref(promira, key, data);
}


static int (*c_pm_apps) (Promira, u16, u08 *) = 0;
int pm_apps (
    Promira promira,
    u16     apps_size,
    u08 *   apps
)
{
    if (c_pm_apps == 0) {
        int res = 0;
        if (!(c_pm_apps = _loadFunction("c_pm_apps", &res)))
            return res;
    }
    return c_pm_apps(promira, apps_size, apps);
}


static int (*c_pm_licensed_apps) (Promira, u16, u08 *) = 0;
int pm_licensed_apps (
    Promira promira,
    u16     apps_size,
    u08 *   apps
)
{
    if (c_pm_licensed_apps == 0) {
        int res = 0;
        if (!(c_pm_licensed_apps = _loadFunction("c_pm_licensed_apps", &res)))
            return res;
    }
    return c_pm_licensed_apps(promira, apps_size, apps);
}


static int (*c_pm_load) (Promira, const char *) = 0;
int pm_load (
    Promira      promira,
    const char * app_name
)
{
    if (c_pm_load == 0) {
        int res = 0;
        if (!(c_pm_load = _loadFunction("c_pm_load", &res)))
            return res;
    }
    return c_pm_load(promira, app_name);
}


static int (*c_pm_load_ext) (Promira, const char *, PromiraLoadFlags) = 0;
int pm_load_ext (
    Promira          promira,
    const char *     app_name,
    PromiraLoadFlags flags
)
{
    if (c_pm_load_ext == 0) {
        int res = 0;
        if (!(c_pm_load_ext = _loadFunction("c_pm_load_ext", &res)))
            return res;
    }
    return c_pm_load_ext(promira, app_name, flags);
}


static const char * (*c_pm_get_net_addr) (Promira) = 0;
const char * pm_get_net_addr (
    Promira promira
)
{
    if (c_pm_get_net_addr == 0) {
        int res = 0;
        if (!(c_pm_get_net_addr = _loadFunction("c_pm_get_net_addr", &res)))
            return 0;
    }
    return c_pm_get_net_addr(promira);
}


static int (*c_pm_close) (Promira) = 0;
int pm_close (
    Promira promira
)
{
    if (c_pm_close == 0) {
        int res = 0;
        if (!(c_pm_close = _loadFunction("c_pm_close", &res)))
            return res;
    }
    return c_pm_close(promira);
}


static u32 (*c_pm_unique_id) (Promira) = 0;
u32 pm_unique_id (
    Promira promira
)
{
    if (c_pm_unique_id == 0) {
        int res = 0;
        if (!(c_pm_unique_id = _loadFunction("c_pm_unique_id", &res)))
            return res;
    }
    return c_pm_unique_id(promira);
}


static const char * (*c_pm_status_string) (int) = 0;
const char * pm_status_string (
    int status
)
{
    if (c_pm_status_string == 0) {
        int res = 0;
        if (!(c_pm_status_string = _loadFunction("c_pm_status_string", &res)))
            return 0;
    }
    return c_pm_status_string(status);
}


static int (*c_pm_init_device) (Promira) = 0;
int pm_init_device (
    Promira promira
)
{
    if (c_pm_init_device == 0) {
        int res = 0;
        if (!(c_pm_init_device = _loadFunction("c_pm_init_device", &res)))
            return res;
    }
    return c_pm_init_device(promira);
}


static int (*c_pm_read_license) (Promira, int, u08 *) = 0;
int pm_read_license (
    Promira promira,
    int     buf_size,
    u08 *   buf
)
{
    if (c_pm_read_license == 0) {
        int res = 0;
        if (!(c_pm_read_license = _loadFunction("c_pm_read_license", &res)))
            return res;
    }
    return c_pm_read_license(promira, buf_size, buf);
}


static int (*c_pm_features) (Promira, const char *, u16, u08 *) = 0;
int pm_features (
    Promira      promira,
    const char * app,
    u16          features_size,
    u08 *        features
)
{
    if (c_pm_features == 0) {
        int res = 0;
        if (!(c_pm_features = _loadFunction("c_pm_features", &res)))
            return res;
    }
    return c_pm_features(promira, app, features_size, features);
}


static int (*c_pm_feature_value) (Promira, const char *, const char *, u16, u08 *) = 0;
int pm_feature_value (
    Promira      promira,
    const char * app,
    const char * feature,
    u16          value_size,
    u08 *        value
)
{
    if (c_pm_feature_value == 0) {
        int res = 0;
        if (!(c_pm_feature_value = _loadFunction("c_pm_feature_value", &res)))
            return res;
    }
    return c_pm_feature_value(promira, app, feature, value_size, value);
}


static int (*c_pm_feature_description) (Promira, const char *, const char *, u16, u08 *) = 0;
int pm_feature_description (
    Promira      promira,
    const char * app,
    const char * feature,
    u16          desc_size,
    u08 *        desc
)
{
    if (c_pm_feature_description == 0) {
        int res = 0;
        if (!(c_pm_feature_description = _loadFunction("c_pm_feature_description", &res)))
            return res;
    }
    return c_pm_feature_description(promira, app, feature, desc_size, desc);
}


