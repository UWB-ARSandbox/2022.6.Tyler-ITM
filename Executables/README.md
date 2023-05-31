# RT.exe CLI Application  

**Developed by UW Bothell graduate student Tyler Choi (tchoi94@uw.edu)in support of Washington State Search and Rescue in 2023.**

This command line tool, named routelength.exe or rt.exe, is designed to calculate the length of specified routes in a given data file.

# Options

The following options are available:

-l: Allows you to specify a custom log filename. If this option is not used, the default log filename will be rt.log.
-v: Enables verbose mode, which prints detailed output.
Usage
To run the program, enter the following command:

Copy code
- rt datafile
This will output one number per route, representing the length in kilometers. For example, if there are five routes over three regions in the datafile, the output will be five numbers ordered according to the route in the datafile.

If you want to specify a custom log filename, use the following command:

Copy code
- rt -l filename datafile
This will produce the same output as the previous command, but the log file name will be filename.

If you want to enable verbose mode, use the following command:

Copy code
- rt -v datafile
This will print the contents of rt.log to the screen.

If you want to both print the log to a file and to the screen, use the following command:

Copy code
- rt -v -l filename datafile
This will print the log to both filename and the screen.

Note that datafile should be replaced with the actual name of the data file you wish to calculate the route lengths for.