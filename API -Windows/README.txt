                     Total Phase Promira Software
                     ----------------------------

Gigabit Ethernet
----------------
The Promira platform can be configured for static IP addressing or
dynamic IP addressing (DHCP) on the Ethernet port. The default network
configuration is static IP address 192.168.11.1.

For all platforms, follow the instructions in the Connectivity section
of the Promira Serial Platform user manual to configure the Ethernet
network interface on the host PC.


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



