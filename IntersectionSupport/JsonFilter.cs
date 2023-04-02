using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using IntersectionSupport;
using Newtonsoft.Json.Linq;

namespace IntersectionSupport
{
    public class Point
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Altitude { get; set; }
        public string Name { get; set; }

        public Point()
        {
        }
        public Point(double longitude, double latitude)
        {
            Longitude = longitude;
            Latitude = latitude;
        }
        public Point(double longitude, double latitude, double altitude)
        {
            Longitude = longitude;
            Latitude = latitude;
            Altitude = altitude;
        }

    }

    public class LineString
    {
        public List<Point> Points { get; set; }
        public string Name { get; set; }

        private Point _lastPoint;
        private double _distanceThreshold = 0.001; // in kilometers
        private const double EarthRadius = 6371.0; // Radius of the earth in kilometers

        public LineString()
        {
            Points = new List<Point>();
        }

        public LineString(string name, List<Point> points)
        {
            Name = name;
            Points = points;
        }

        public void AddPoint(Point point)
        {
            if (_lastPoint == null || DistanceToMeters(_lastPoint, point) > _distanceThreshold)
            {
                Points.Add(point);
            }

            _lastPoint = point;
        }

        private double DistanceToMeters(Point p1, Point p2)
        {
            Vector3 v1 = PointToMeters(p1);
            Vector3 v2 = PointToMeters(p2);

            return Vector3.Distance(v1, v2);
        }

        public static Vector3 PointToMeters(Point point)
        {
            // Convert longitude and latitude to radians
            double lonRad = ToRadians(point.Longitude);
            double latRad = ToRadians(point.Latitude);

            // Haversine formula to calculate distance between two points on the surface of a sphere
            double a = Math.Pow(Math.Sin((latRad - 0) / 2), 2) + Math.Cos(latRad) * Math.Cos(0) * Math.Pow(Math.Sin((lonRad - 0) / 2), 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = EarthRadius * c;

            // Convert altitude to meters
            double altitude = point.Altitude;

            // Convert to Cartesian coordinates
            double x = (distance + altitude) * Math.Cos(latRad) * Math.Cos(lonRad);
            double y = (distance + altitude) * Math.Cos(latRad) * Math.Sin(lonRad);
            double z = (distance + altitude) * Math.Sin(latRad);

            // Return new vector in meters
            return new Vector3((float)x, (float)y, (float)z);
        }

        private static double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }
    }

    public class Polygon
    {
        public List<List<Point>> Rings { get; set; }
        public List<float> Altitudes { get; set; }
        public string Name { get; set; }

        public Polygon()
        {
            Rings = new List<List<Point>>();
        }

        public Polygon(List<List<Point>> rings)
        {
            Rings = rings;
        }

        public Polygon(List<List<Point>> rings, string name)
        {
            Rings = rings;
            Name = name;
        }
    }

    public class JsonFilter
    {
        public static List<object> ParseGeoJsonFile(string filePath)
        {
            try
            {
                //Console.WriteLine("Reading GeoJSON file: " + filePath);
                List<object> objects = new List<object>();

                string fileContent = File.ReadAllText(filePath);
                JObject jObject = JObject.Parse(fileContent);

                JArray features = (JArray)jObject["features"];
                //Console.WriteLine("Number of features: " + features.Count);
                List<LineString> lineStrings = new List<LineString>();

                foreach (JObject feature in features)
                {
                    JObject geometry = (JObject)feature["geometry"];
                    string type = (string)geometry["type"];
                    JObject geoProperties = (JObject)feature["properties"];
                    // JArray coordinates = (JArray)geometry["coordinates"];
                    string name = (string)geoProperties["title"]?.ToString() ?? "";

                    if (type == "Point")
                    {
                        Point point = new Point();
                        point.Name = name;

                        JArray pointCoords = (JArray)geometry["coordinates"];

                        if (pointCoords.Count >= 2)
                        {
                            point.Longitude = (double)pointCoords[0];
                            point.Latitude = (double)pointCoords[1];
                        }

                        if (pointCoords.Count >= 3)
                        {
                            point.Altitude = (double)pointCoords[2];
                        }
                        // timestamp is pointCoords[3], not considered yet
                        objects.Add(point);
                        //Console.WriteLine("Point name: " + name);
                    }
                    else if (type == "LineString")
                    {
                        LineString lineString = new LineString();
                        lineString.Name = name;

                        JArray lineStringCoords = (JArray)geometry["coordinates"];

                        foreach (var coordinate in lineStringCoords)
                        {
                            double longitude = (double)coordinate[0];
                            double latitude = (double)coordinate[1];
                            double altitude = 0.0f;

                            if (coordinate.Count() > 2)
                            {
                                altitude = (double)coordinate[2];
                            }

                            Point point = new Point(longitude, latitude, altitude);

                            lineString.AddPoint(point);
                        }

                        objects.Add(lineString);
                        //Console.WriteLine("LineString name: " + name);
                    }

                    else if (type == "Polygon")
                    {
                        Polygon polygon = new Polygon();
                        polygon.Rings = new List<List<Point>>();
                        polygon.Name = name;

                        JArray polygonCoords = (JArray)geometry["coordinates"];

                        foreach (var ring in polygonCoords)
                        {
                            List<Point> points = new List<Point>();

                            foreach (var coordinate in ring)
                            {
                                Point point = new Point();
                                point.Longitude = (double)coordinate[0];
                                point.Latitude = (double)coordinate[1];
                                points.Add(point);

                                if (coordinate.Count() > 2)
                                {
                                    float altitude = (float)coordinate[2];
                                    point.Altitude = altitude;
                                }
                            }

                            polygon.Rings.Add(points);
                        }

                        objects.Add(polygon);
                        //Console.WriteLine("Polygon name: " + name);
                    }

                }
                return objects;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error while parsing JSON file: {ex.Message}");
                return null;
            }
            
        }

    }

}
