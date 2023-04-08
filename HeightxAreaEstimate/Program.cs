using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HeightxAreaEstimate
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            // Read the GeoJSON string from a text file
            string filePath = "geojson.txt";
            string geoJsonString = File.ReadAllText(filePath);

            Console.WriteLine("Reading GeoJSON from file...");
            try
            {
                geoJsonString = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file '{filePath}': {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                return;
            }

            GeoJsonFeature feature = JsonConvert.DeserializeObject<GeoJsonFeature>(geoJsonString);

            Console.WriteLine("Processing GeoJSON coordinates...");

            for (int i = 0; i < feature.Geometry.Coordinates.Count; i++)
            {
                List<double> coordinate = feature.Geometry.Coordinates[i];
                if (coordinate.Count < 3)
                {
                    double latitude = coordinate[1];
                    double longitude = coordinate[0];
                    double elevation = await GetElevationFromOpenTopoDataAsync(latitude, longitude);
                    coordinate.Add(elevation);
                    string logMessage = $"Elevation found for ({latitude}, {longitude})";
                    Console.WriteLine(logMessage);
                }
            }

            string updatedGeoJsonString = JsonConvert.SerializeObject(feature, Formatting.Indented);
            Console.WriteLine("Updated GeoJSON: \n" + updatedGeoJsonString);

            // Save the updated GeoJSON to output.log
            File.WriteAllText("output.log", updatedGeoJsonString);

            Console.ReadLine();

        }

        public static async Task<double> GetElevationFromOpenTopoDataAsync(double latitude, double longitude)
        {
            string baseUrl = "https://api.opentopodata.org/v1/srtm90m";
            string requestUrl = $"{baseUrl}?locations={latitude},{longitude}";

            HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(content);
                JArray results = (JArray)json["results"];
                double? elevation = (double?)results[0]["elevation"];

                if (elevation.HasValue)
                {
                    return elevation.Value;
                }
                else
                {
                    // Replace with your desired default value or error handling
                    Console.WriteLine("Elevation data is null. Using default value.");
                    return 0;
                }
            }
            else
            {
                throw new Exception($"Failed to fetch elevation data: {response.ReasonPhrase}");
            }
        }


        public class GeoJsonFeature
        {
            public string Type { get; set; }
            public GeoJsonProperties Properties { get; set; }
            public GeoJsonGeometry Geometry { get; set; }
            public string Id { get; set; }
        }

        public class GeoJsonProperties
        {
            // Add other properties as needed
            public string Title { get; set; }
        }

        public class GeoJsonGeometry
        {
            public string Type { get; set; }
            public List<List<double>> Coordinates { get; set; }
        }
    }
}
