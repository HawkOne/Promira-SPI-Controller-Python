                     Total Phase Promira Software
                     ----------------------------

Introduction
------------
This software is used to interface with the Promira Serial Platform.
It provides APIs in multiple languages for development flexibility.


Directory Structure
-------------------
c/      - C/C++ language binding files and examples
python/ - Python language binding files and examples
csharp/ - C# language binding files and examples (Windows only)
vb.net/ - Visual Basic .NET binding files and examples (Windows only)
util/   - Promira management utility

See the EXAMPLES.txt and the README.txt in each language subdirectory
for details on the included source code that demonstrates the usage of
the API.


Ethernet over USB
-----------------
On Windows, the Ethernet over USB driver will be installed when the
device is first plugged in.  Refer to the Connectivity section in the
Promira Serial Platform user manual if the device driver fails to
install.

On Linux and macOS, a specific kernel mode or user mode driver is
not required.

For all platforms, follow the instructions in the Connectivity section
of the Promira Serial Platform user manual to configure the Ethernet
over USB network interface on the host PC.


Gigabit Ethernet
----------------
The Promira platform can be configured for static IP addressing or
dynamic IP addressing (DHCP) on the Ethernet port. The default network
configuration is static IP address 192.168.11.1.

For all platforms, follow the instructions in the Connectivity section
of the Promira Serial Platform user manual to configure the Ethernet
network interface on the host PC.


C/C++ bindings
--------------
1) Create a new C/C++ project or open an existing project
2) Add promira.c, promira.h, promact_is.c and promact_is.h to the project
3) Place promira.dll and promact_is.dll (*.so on Linux/macOS) in the PATH
4) Put the following lines in any module that uses the API:
   #include "promira.h"
   #include "promact_is.h"
5) Develop, compile, and run your project


Python bindings
---------------
If not already installed, download Python from:
http://www.python.org/

API bindings support Python 3.2+ and 2.7

1) Copy promira_py.py and promact_is_py.py to a new folder
2) Copy promira.dll and promact_is.dll (*.so on Linux/macOS) to the folder
3) Create a new script (i.e. program.py)
4) Put the following lines in your script file:
   from promira_py import *
   from promact_is_py import *
5) Develop and run your script

There are two main differences between the Promira API documented in
the user manual and the Promira Python API.

1) The "array" type is used for passing data to and from the Promira
Python API.  See the Python documentation for more information on this
type.

2) Promira Python API functions can return multiple arguments on the
left hand side.  This reduces the need to pass pointers to output
arguments on the right hand side of API functions.

3) There are a variety of ways to call API functions that have array
arguments.

  All arrays can be passed into the API as an ArrayType object or as
  a tuple (array, length), where array is an ArrayType object and
  length is an integer.  The user-specified length would then serve
  as the length argument to the API funtion (please refer to the
  product user manual).  If only the array is provided, the array's
  intrinsic length is used as the argument to the underlying API
  function.

  The ability to pass in a pre-allocated array along with a separate
  length allows the performance-minded programmer to use a single
  array for repeated calls to the API, simply changing the contents
  of the array and/or the specified length as needed.

  Additionally, for arrays that are filled by the API function, an
  integer can be passed in place of the array argument and the API
  will automatically create an array of that length.  All output
  arrays, whether passed in or generated, are passed back in the
  returned tuple.

The calling convention for each API function is documented in the
comments of the promira_py.py and promact_is_py.py files.


C# bindings
-----------
1) Create a new C# project or open an existing project
2) Add promira.cs and promact_is.cs to the project
3) Place promira.dll and promact_is.dll in the PATH
4) Develop, compile, and run your project

Every API function that accepts an array argument must be accompanied
by a separate length field, even though the array itself intrinsically
has a length.  See the discussion of the Python API above explaining
the rationale for this interface.

For C#, structures that contain arrays do not have the length field in
the structure as documented in the user manual.  Instead, the intrinsic
length of the array is used when the structure is an argument to an
API function.

In cases where the API function ignores the structure argument, a dummy
structure should be used instead of null.


.NET and VB.NET bindings
------------------------
Copy promira.dll, promact_is.dll, promira_net.dll, and promact_is_net.dll
to your application development environment.  The *_net.dll files provide
the .NET interface to the native API DLLs.  For detailed documentation
of APIs refer to the user manual and the comments in the C# binding
source file (promact_is.cs).

As in C#, every API function that accepts an array argument must be
accompanied by a separate length field.  Also as in C#, arrays in
structures use their intrinsic length instead of having a separate
length field in the structure, and dummy structures should be used
instead of passing null when the API is expected to ignore the
structure argument.

Due to the use of unsigned arguments, the .NET bindings are no longer
fully Common Language Specification (CLS) compliant.  As a result,
Microsoft .NET 2.0 or later is required for any VB.NET applications
using the bindings.
