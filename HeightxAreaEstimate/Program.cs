using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IntersectionSupport;
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
            string filePath = "MarbleMt.txt";
            List<object> objects = JsonFilter.ParseGeoJsonFile(filePath);
            if (objects == null || objects.Count == 0)
            {
                Console.WriteLine("Error reading GeoJSON data.");
                return;
            }

            Console.WriteLine($"Total number of objects in the GeoJSON data: {objects.Count}");

            // Extract the first LineString feature from the list of objects
            LineString feature = objects.OfType<LineString>().FirstOrDefault();
            if (feature == null)
            {
                Console.WriteLine("No LineString features found in the GeoJSON data.");
                return;
            }

            Console.WriteLine($"Title of the LineString feature: {feature.Name}");
            Console.WriteLine($"Total number of points in the LineString feature before filtering: {feature.Points.Count}");

            Console.WriteLine("Processing GeoJSON coordinates...");

            List<Point> filteredPoints = new List<Point>();
            double minDistance = 1; // Minimum distance in meters
            filteredPoints.Add(feature.Points[0]);

            for (int i = 1; i < feature.Points.Count; i++)
            {
                Point prevPoint = filteredPoints[filteredPoints.Count - 1];
                Point currentPoint = feature.Points[i];
                double distance = HaversineDistance(prevPoint.Latitude, prevPoint.Longitude, currentPoint.Latitude, currentPoint.Longitude);

                if (distance >= minDistance)
                {
                    filteredPoints.Add(currentPoint);
                }
            }

            feature.Points = filteredPoints;

            Console.WriteLine($"Total number of points in the LineString feature after filtering: {feature.Points.Count}");

            // Limit the number of points to the first 500
            int pointLimit = 10;
            if (feature.Points.Count > pointLimit)
            {
                feature.Points = feature.Points.GetRange(0, pointLimit);
            }

            Console.WriteLine($"Minimum distance between points: {minDistance} meters");
            Console.WriteLine($"Number of points used for calculations: {feature.Points.Count}");

            // Calculate the distance between the first 500 points
            double total2DDistance = 0;
            double total3DDistance = 0;

            for (int i = 0; i < feature.Points.Count - 1; i++)
            {
                Point point1 = feature.Points[i];
                Point point2 = feature.Points[i + 1];

                double twoDimensionalDistance = Calculate2DDistance(point1.Latitude, point1.Longitude, point2.Latitude, point2.Longitude);
                double threeDimensionalDistance = Calculate3DDistance(point1.Latitude, point1.Longitude, point1.Altitude, point2.Latitude, point2.Longitude, point2.Altitude);

                total2DDistance += twoDimensionalDistance;
                total3DDistance += threeDimensionalDistance;
            }

            // Output the calculated distances
            string output = $"Total 2D distance: {total2DDistance} meters\n";
            output += $"Total 3D distance: {total3DDistance} meters\n";
            output += $"Difference between 2D and 3D distances: {Math.Abs(total2DDistance - total3DDistance)} meters\n";

            Console.WriteLine(output);

            // Save the output to output.log
            File.WriteAllText("output.log", output);

            Console.ReadLine();
        }

        //elevation query with sleep 
        public static async Task<List<double>> GetElevationsFromOpenTopoDataAsync(List<List<double>> coordinates)
        {
            string baseUrl = "https://api.opentopodata.org/v1/ned10m";
            string locationsParam = string.Join("|", coordinates.Select(coord => $"{coord[1]},{coord[0]}"));
            string requestUrl = $"{baseUrl}?locations={locationsParam}";

            HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(content);
                JArray results = (JArray)json["results"];

                List<double> elevations = new List<double>();
                foreach (JObject result in results)
                {
                    double? elevation = (double?)result["elevation"];

                    if (elevation.HasValue)
                    {
                        elevations.Add(elevation.Value);
                    }
                    else
                    {
                        // Replace with your desired default value or error handling
                        Console.WriteLine("Elevation data is null. Using default value.");
                        elevations.Add(0);
                    }
                }

                return elevations;
            }
            else
            {
                throw new Exception($"Failed to fetch elevation data: {response.ReasonPhrase}");
            }
        }

        public static async Task<double> GetElevationFromOpenTopoDataAsync(double latitude, double longitude)
        {
            string baseUrl = "https://api.opentopodata.org/v1/ned10m"; //calls ned10m dataset that has 10m resolution
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
                    Console.WriteLine("Elevation data is null. Using default value.");
                    return 0;
                }
            }
            else
            {
                throw new Exception($"Failed to fetch elevation data: {response.ReasonPhrase}");
            }
        }

        public static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371000; // Earth's radius in meters
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                        Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                        Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = R * c;
            return distance;
        }

        public static double ToRadians(double angle)
        {
            return angle * (Math.PI / 180);
        }

        public static double Calculate2DDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double earthRadius = 6371e3; // Earth radius in meters
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = earthRadius * c;

            return distance;
        }

        public static double Calculate3DDistance(double lat1, double lon1, double ele1, double lat2, double lon2, double ele2)
        {
            double twoDimensionalDistance = Calculate2DDistance(lat1, lon1, lat2, lon2);
            double elevationDifference = Math.Abs(ele1 - ele2);

            double threeDimensionalDistance = Math.Sqrt(Math.Pow(twoDimensionalDistance, 2) + Math.Pow(elevationDifference, 2));

            return threeDimensionalDistance;
        }
    }
}
