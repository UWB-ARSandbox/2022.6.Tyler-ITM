using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using IntersectionSupport;

namespace WaSARxCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            // Preload DLLs
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            if (args.Length > 0)
            {
                // Start the stopwatch
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                // Handle the GeoJson processing functionality
                ProcessGeoJson(args);

                // Stop the stopwatch
                stopwatch.Stop();

                // Calculate the elapsed time in milliseconds
                long elapsedTime = stopwatch.ElapsedMilliseconds;

                Console.WriteLine($"Processing complete. Check the output file for results.");
                Console.WriteLine($"Execution time: {elapsedTime} ms");
            }
            else
            {
                Console.WriteLine("Usage: rt [-l logfilename] [-v] datafile");
            }
        }

        static void ProcessGeoJson(string[] args)
        {
            string logFileName = "rt.log";
            bool verboseMode = false;
            string dataFileName = string.Empty;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-l" && i + 1 < args.Length)
                {
                    logFileName = args[++i];
                }
                else if (args[i] == "-v")
                {
                    verboseMode = true;
                }
                else
                {
                    dataFileName = args[i];
                }
            }

            if (string.IsNullOrEmpty(dataFileName))
            {
                Console.WriteLine("No data file provided. Exiting...");
                return;
            }

            // Load and process the GeoJson data file
            ProcessDataFile(dataFileName, logFileName, verboseMode);
        }

        static void ProcessDataFile(string dataFileName, string logFileName, bool verboseMode)
        {
            try
            {
                // Load and parse the GeoJson data file
                List<object> geoObjects = IntersectionSupport.JsonFilter.ParseGeoJsonFile(dataFileName);

                // Check if the geoObjects list is null or empty
                if (geoObjects == null || geoObjects.Count == 0)
                {
                    Console.WriteLine("Failed to parse GeoJSON file.");
                    return;
                }

                Console.WriteLine("GeoJSON file has been read successfully.");
                Console.WriteLine("Area x Route intersections processing...");

                IntersectionSupport.IntersectFind intersectFind = new IntersectionSupport.IntersectFind();
                intersectFind.FindIntersectionsCLI(dataFileName, logFileName);

                if (verboseMode)
                {
                    // Print the log file content to the console
                    Console.Write(File.ReadAllText(logFileName));
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Error: Could not open file '{dataFileName}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Failed to process file '{dataFileName}'. {ex.Message}");
            }
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assemblyName = new AssemblyName(args.Name).Name;

            if (assemblyName.Equals("IntersectionSupport", StringComparison.OrdinalIgnoreCase))
            {
                string assemblyPath = Path.Combine(path, "IntersectionSupport.dll");
                return Assembly.LoadFrom(assemblyPath);
            }
            else if (assemblyName.Equals("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase))
            {
                string assemblyPath = Path.Combine(path, "Newtonsoft.Json.dll");
                return Assembly.LoadFrom(assemblyPath);
            }

            return null;
        }

    }
}

