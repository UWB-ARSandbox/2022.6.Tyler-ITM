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
        static Program()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var resourceName = $"{typeof(Program).Namespace}.{new AssemblyName(args.Name).Name}.dll";
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    var assemblyData = new byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            };
        }

        static string ParseGeoJson(string[] args)
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
                return null;
            }

            // Load and process the GeoJson data file
            string outputFilePath = ProcessDataFile(dataFileName, logFileName, verboseMode);
            return outputFilePath;
        }

        static void Main(string[] args)
        {
            // Preload DLLs
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            while (true)
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("Enter command line arguments (example: rt -l logfilename -v datafile):");
                    string userInput = Console.ReadLine();
                    args = userInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                }

                // Handle the GeoJson processing functionality
                string outputFilePath = ParseGeoJson(args);

                if (outputFilePath != null)
                {
                    Console.WriteLine($"Processing complete. Check the output file at {Path.Combine(Environment.CurrentDirectory, outputFilePath)} for results.");
                    break;
                }
                else
                {
                    Console.WriteLine("Failed to process GeoJson file. Please check the input parameters and try again.");
                    args = new string[0];
                }
            }

            Console.WriteLine("Press 0 to quit.");
            while (Console.ReadLine() != "0") { }
        }

        static string ProcessGeoJson(string[] args)
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
                throw new Exception("No data file provided.");
            }

            // Load and process the GeoJson data file
            string outputFilePath = ProcessDataFile(dataFileName, logFileName, verboseMode);

            if (string.IsNullOrEmpty(outputFilePath))
            {
                throw new Exception("Failed to process GeoJSON file.");
            }

            return outputFilePath;
        }


        static string ProcessDataFile(string dataFileName, string logFileName, bool verboseMode)
        {
            try
            {
                // Check if the file has a valid extension (either .json or .txt)
                string fileExtension = Path.GetExtension(dataFileName);
                if (string.IsNullOrEmpty(fileExtension) || !(fileExtension.Equals(".json", StringComparison.OrdinalIgnoreCase) || fileExtension.Equals(".txt", StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine($"Error: Unsupported file format '{fileExtension}'. Please provide a .json or .txt file.");
                    return null;
                }

                if (!File.Exists(dataFileName))
                {
                    Console.WriteLine($"Error: File '{dataFileName}' does not exist.");
                    return null;
                }

                // Load and parse the GeoJson data file
                List<object> geoObjects = IntersectionSupport.JsonFilter.ParseGeoJsonFile(dataFileName);

                // Check if the geoObjects list is null or empty
                if (geoObjects == null || geoObjects.Count == 0)
                {
                    Console.WriteLine($"Error: Failed to parse GeoJSON file '{dataFileName}'. The file may not be a valid GeoJSON file or it may be empty.");
                    return null;
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

                return logFileName;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error: Could not open file '{dataFileName}'. {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Failed to process file '{dataFileName}'. {ex.Message}");
            }

            return null;
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

