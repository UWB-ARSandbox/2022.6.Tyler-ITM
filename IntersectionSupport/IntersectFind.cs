﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IntersectionSupport;

namespace IntersectionSupport
{
    public class IntersectionResult
    {
        public string RouteName { get; set; }
        public double Distance { get; set; }
    }

    public class AreaResult
    {
        public string Name { get; set; }
        public List<IntersectionResult> IntersectionResults { get; set; }

        public AreaResult(string name)
        {
            Name = name;
            IntersectionResults = new List<IntersectionResult>();
        }
    }


    public class Intersection
    {
        public int Index { get; set; }
        public Vector3 Point { get; set; }

        public static Vector3 PositiveInfinity => new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        public Intersection(int index, Vector3 position)
        {
            Index = index;
            Point = position;
        }
    }

    public class IntersectFind 
    {
        public List<AreaResult> AreaResults { get; private set; }

        private List<LineString> lineStrings;
        private List<Polygon> polygons;
        private const double EarthRadius = 6371.0; // Radius of the earth in kilometers
        private static List<Intersection> intersections;
        public double _distanceThreshold = 0.003; // in kilometers

        // WGS84 reference ellipsoid parameters
        const double WGS84_A = 6378137.0; // semi-major axis in meters
        const double WGS84_B = 6356752.314245; // semi-minor axis in meters
        const double WGS84_F = 1.0 / 298.257223563; // flattening factor

        public void FindIntersectionsCLI(string jsonString, string logFileName)
        {
            intersections = new List<Intersection>();
            InitSequence(jsonString, logFileName);
        }

        public void FindIntersectionsGUI(string jsonString)
        {
            intersections = new List<Intersection>();
            InitSequence(jsonString);
        }

        private void InitSequence(string jsonString, string logFileName = null)
        {
            List<object> objects = JsonFilter.ParseGeoJsonFile(jsonString);

            lineStrings = new List<LineString>();
            polygons = new List<Polygon>();

            foreach (object obj in objects)
            {
                if (obj is LineString lineString)
                {
                    lineStrings.Add(lineString);
                }
                else if (obj is Polygon polygon)
                {
                    polygons.Add(polygon);
                }
            }

            string output = "Results:\n";
            output += $"Processing file: {jsonString}\n";
            output += "------------------------------------------------------------------------------------\n";

            List<LineString> routes = lineStrings.Where(ls => !ls.Name.StartsWith("Segment ")).ToList();
            List<object> areas = new List<object>();
            areas.AddRange(polygons);
            areas.AddRange(lineStrings.Where(ls => ls.Name.StartsWith("Segment ")));

            output += "Areas:\n";
            foreach (object area in areas)
            {
                output += $"  {(area is LineString ? (area as LineString).Name : (area as Polygon).Name)}\n";
            }

            output += "\nRoutes:\n";
            foreach (LineString route in routes)
            {
                output += $"  {route.Name}\n";
            }

            output += "\nIntersection Results:\n";
            output += "------------------------------------------------------------------------------------\n";

            AreaResults = new List<AreaResult>();

            foreach (object area in areas)
            {
                string areaName = (area is LineString ? (area as LineString).Name : (area as Polygon).Name);
                output += $"Area: {areaName}\n";

                AreaResult areaResult = new AreaResult(areaName);

                bool areaHasIntersectingRoutes = false;
                double totalAreaDistance = 0;
                foreach (LineString route in routes)
                {
                    var result = FindIntersections(route, area);
                    List<Intersection> intersections = result.Intersections;

                    if (intersections.Count > 0)
                    {
                        areaHasIntersectingRoutes = true;

                        output += $"--> Route: {route.Name}\n";

                        intersections.Sort((x, y) => x.Index.CompareTo(y.Index));

                        double addedDistance = result.addedDistance;
                        double distanceInKilometers = CalculateDistanceBetweenIntersections(intersections, route) + addedDistance;
                        totalAreaDistance += distanceInKilometers;

                        areaResult.IntersectionResults.Add(new IntersectionResult
                        {
                            RouteName = route.Name,
                            Distance = distanceInKilometers
                        });

                        output += $"  Total distance of route: {distanceInKilometers:f2} kilometers\n";
                    }
                }

                if (!areaHasIntersectingRoutes)
                {
                    output += "  No intersecting routes\n";
                }
                else
                {
                    output += $"Total traversed in Area {areaName} --> {totalAreaDistance:f2} kilometers\n";
                }

                output += "------------------------------------------------------------------------------------\n";

                AreaResults.Add(areaResult);
            }

            WriteOutputToFile(logFileName, output);
        }

        private (List<Intersection> Intersections, double addedDistance) FindIntersections(LineString targetLineString, object selectedArea)
        {
            {
                List<Intersection> intersections = new List<Intersection>();
                // Code to handle LineString starting inside an area
                bool startPointInside = false;
                bool endPointInside = false;
                double startOrEndDistance = 0;

                if (selectedArea is LineString lineStringArea)
                {
                    // This is a LineString
                    startPointInside = IsPointInsideLineStringArea(targetLineString.Points[0], lineStringArea);
                    endPointInside = IsPointInsideLineStringArea(targetLineString.Points[targetLineString.Points.Count - 1], lineStringArea);

                    // Find the intersections between the LineString and the target LineString
                    for (int i = 0; i < lineStringArea.Points.Count - 1; i++)
                    {
                        Point p1 = lineStringArea.Points[i];
                        Vector3 p1Meters = PointToMeters(p1);
                        Vector3 v1 = new Vector3(p1Meters.X, p1Meters.Y, p1Meters.Z);
                        Point p2 = lineStringArea.Points[(i + 1) % lineStringArea.Points.Count];
                        Vector3 p2Meters = PointToMeters(p2);
                        Vector3 v2 = new Vector3(p2Meters.X, p2Meters.Y, p2Meters.Z);

                        for (int j = 0; j < targetLineString.Points.Count - 1; j++)
                        {
                            Point q1 = targetLineString.Points[j];
                            Vector3 q1Meters = PointToMeters(q1);
                            Vector3 w1 = new Vector3(q1Meters.X, q1Meters.Y, q1Meters.Z);
                            Point q2 = targetLineString.Points[(j + 1) % targetLineString.Points.Count];
                            Vector3 q2Meters = PointToMeters(q2);
                            Vector3 w2 = new Vector3(q2Meters.X, q2Meters.Y, q2Meters.Z);

                            Intersection intersection = new Intersection(j, Intersection.PositiveInfinity)
                            {
                                Point = this.LineSegmentIntersection(v1, v2, w1, w2),
                                Index = j
                            };

                            if (intersection.Point != Intersection.PositiveInfinity)
                            {
                                // Check if intersection point lies within the bounds of both line segments
                                float dot1 = Vector3.Dot(intersection.Point - w1, w2 - w1);
                                float dot2 = Vector3.Dot(intersection.Point - w2, w1 - w2);
                                float dot3 = Vector3.Dot(intersection.Point - v1, v2 - v1);
                                float dot4 = Vector3.Dot(intersection.Point - v2, v1 - v2);

                                if (dot1 >= 0 && dot2 >= 0 && dot3 >= 0 && dot4 >= 0)
                                {
                                    // Intersection found
                                    bool isDuplicate = false;
                                    foreach (Intersection existingIntersection in intersections)
                                    {
                                        if (Math.Abs(existingIntersection.Point.Y - intersection.Point.Y) < _distanceThreshold
                                            && Math.Abs(existingIntersection.Point.X - intersection.Point.X) < _distanceThreshold)
                                        {
                                            isDuplicate = true;
                                            break;
                                        }
                                    }
                                    if (!isDuplicate)
                                    {
                                        intersections.Add(intersection);
                                        //Console.WriteLine("Intersection found at index " + j);
                                        //Console.WriteLine("Intersection position " + intersection.Point);
                                        //Console.WriteLine("Intersection count: " + intersections.Count);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (selectedArea is Polygon polygonArea)
                {
                    // This is a Polygon

                    startPointInside = IsPointInsidePolygon(targetLineString.Points[0], polygonArea);
                    endPointInside = IsPointInsidePolygon(targetLineString.Points[targetLineString.Points.Count - 1], polygonArea);

                    // Find the intersections between the Polygon and the target LineString
                    foreach (List<Point> ring in polygonArea.Rings)
                    {
                        ProcessRing(ring);
                    }

                    
                }

                void ProcessRing(List<Point> ring)
                {
                    // Find the intersections between the Polygon and the target LineString
                    for (int k = 0; k < ring.Count - 1; k++)
                    {
                        Point p1 = ring[k];
                        Vector3 p1Meters = PointToMeters(p1);
                        Vector3 v1 = new Vector3(p1Meters.X, p1Meters.Y, p1Meters.Z);
                        Point p2 = ring[(k + 1) % ring.Count];
                        Vector3 p2Meters = PointToMeters(p2);
                        Vector3 v2 = new Vector3(p2Meters.X, p2Meters.Y, p2Meters.Z);

                        for (int j = 0; j < targetLineString.Points.Count - 1; j++)
                        {
                            Point q1 = targetLineString.Points[j];
                            Vector3 q1Meters = PointToMeters(q1);
                            Vector3 w1 = new Vector3(q1Meters.X, q1Meters.Y, q1Meters.Z);
                            Point q2 = targetLineString.Points[(j + 1) % targetLineString.Points.Count];
                            Vector3 q2Meters = PointToMeters(q2);
                            Vector3 w2 = new Vector3(q2Meters.X, q2Meters.Y, q2Meters.Z);

                            // Intersection code...
                            Intersection intersection = new Intersection(j, Intersection.PositiveInfinity)
                            {
                                Point = this.LineSegmentIntersection(v1, v2, w1, w2),
                                Index = j
                            };
                            if (intersection.Point != Intersection.PositiveInfinity)
                            {
                                // Check if intersection point lies within the bounds of both line segments
                                float dot1 = Vector3.Dot(intersection.Point - w1, w2 - w1);
                                float dot2 = Vector3.Dot(intersection.Point - w2, w1 - w2);
                                float dot3 = Vector3.Dot(intersection.Point - v1, v2 - v1);
                                float dot4 = Vector3.Dot(intersection.Point - v2, v1 - v2);

                                if (dot1 >= 0 && dot2 >= 0 && dot3 >= 0 && dot4 >= 0)
                                {
                                    // Intersection found
                                    bool isDuplicate = false;
                                    foreach (Intersection existingIntersection in intersections)
                                    {
                                        if (Math.Abs(existingIntersection.Point.Y - intersection.Point.Y) < _distanceThreshold
                                            && Math.Abs(existingIntersection.Point.X - intersection.Point.X) < _distanceThreshold)
                                        {
                                            isDuplicate = true;
                                            break;
                                        }
                                    }
                                    if (!isDuplicate)
                                    {
                                        intersections.Add(intersection);
                                        //Console.WriteLine("Intersection found at index " + j);
                                        //Console.WriteLine("Intersection position " + intersection.Point);
                                        //Console.WriteLine("Intersection count: " + intersections.Count);
                                    }
                                }
                            }
                        }
                    }
                }
                // If startPointInside or endPointInside are true, you can now calculate the distance
                // from the starting point to the first intersection (if startPointInside is true)
                // or from the last intersection to the ending point (if endPointInside is true)
                // and add these distances to the total distance sum.
                if (startPointInside)
                {
                    // Calculate distance from starting point to first intersection along the LineString
                    if (intersections.Count > 0)
                    {
                        Intersection firstIntersection = intersections[0];
                        double distance = CalculateDistanceAlongLineString(targetLineString.Points, 0, firstIntersection.Index);
                        //Console.WriteLine("Distance from starting point to first intersection: " + distance);

                        // Add the distance to the startOrEndDistance sum
                        startOrEndDistance += distance;

                    }
                }

                if (endPointInside)
                {
                    // Calculate distance from last intersection to ending point along the LineString
                    if (intersections.Count > 0)
                    {
                        Intersection lastIntersection = intersections[intersections.Count - 1];
                        double distance = CalculateDistanceAlongLineString(targetLineString.Points, lastIntersection.Index, targetLineString.Points.Count - 1);
                        //Console.WriteLine("Distance from last intersection to ending point: " + distance);

                        // Add the distance to the total distance sum
                        startOrEndDistance += distance;
                    }
                }

                return (intersections, startOrEndDistance);
            }
        }

        private Vector3 LineSegmentIntersection(Vector3 p1, Vector3 p2, Vector3 q1, Vector3 q2)
        {
            Vector3 r = p2 - p1;
            Vector3 s = q2 - q1;
            Vector3 qmp = q1 - p1;

            float rxs = (float)Vector3.Cross(r, s).Length();
            float qmpr = (float)Vector3.Cross(qmp, r).Length();

            // If the cross product magnitude is close to zero, the lines are either collinear or parallel
            if (Math.Abs(rxs) < float.Epsilon)
            {
                return Intersection.PositiveInfinity;
            }

            float t = (float)Vector3.Cross(qmp, s).Length() / rxs;
            float u = qmpr / rxs;

            // Check if the intersection lies within both line segments
            if (t >= -float.Epsilon && t <= 1 + float.Epsilon && u >= -float.Epsilon && u <= 1 + float.Epsilon)
            {
                return p1 + t * r;
            }
            else
            {
                return Intersection.PositiveInfinity;
            }
        }

        public bool IsPointInsidePolygon(Point point, Polygon polygon)
        {
            int intersections = 0;
            double x = point.Latitude;
            double y = point.Longitude;

            for (int i = 0; i < polygon.Rings[0].Count - 1; i++)
            {
                Point vertex1 = polygon.Rings[0][i];
                Point vertex2 = polygon.Rings[0][(i + 1) % polygon.Rings[0].Count];

                if (((vertex1.Longitude > y) != (vertex2.Longitude > y))
                    && (x < (vertex2.Latitude - vertex1.Latitude) * (y - vertex1.Longitude) / (vertex2.Longitude - vertex1.Longitude) + vertex1.Latitude))
                {
                    intersections++;
                }
            }

            return (intersections % 2) == 1;
        }

        public bool IsPointInsideLineStringArea(Point point, LineString lineStringArea)
        {
            // Create a new Polygon by connecting the last point to the first point
            List<Point> closedPoints = new List<Point>(lineStringArea.Points);
            closedPoints.Add(lineStringArea.Points[0]);
            Polygon polygon = new Polygon(new List<List<Point>> { closedPoints });

            // Use the IsPointInsidePolygon method to test if the point is inside the closed shape
            return IsPointInsidePolygon(point, polygon);
        }

        private double CalculateDistanceBetweenIntersections(List<Intersection> intersections, LineString lineString)
        {
            double totalDistance = 0;
            bool insideArea = false;

            for (int i = 0; i < intersections.Count; i++)
            {
                int currentIndex = intersections[i].Index;

                // Toggle the insideArea flag
                insideArea = !insideArea;

                // If inside the area, add the distance to the next intersection or the end of the LineString
                if (insideArea)
                {
                    int nextIndex;

                    // Check if there's another intersection
                    if (i + 1 < intersections.Count)
                    {
                        nextIndex = intersections[i + 1].Index;
                    }
                    else
                    {
                        // If no more intersections, use the last point of the LineString
                        nextIndex = lineString.Points.Count - 1;
                    }

                    totalDistance += CalculateDistanceAlongLineString(lineString.Points, currentIndex, nextIndex);
                }
            }

            return totalDistance;
        }

        private double CalculateDistanceAlongLineString(List<Point> points, int startIndex, int endIndex)
        {
            double totalDistance = 0;
            for (int i = startIndex; i < endIndex; i++)
            {
                Point p1 = points[i];
                Point p2 = points[i + 1];
                totalDistance += Haversine(p1, p2);
            }
            return totalDistance;
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
            double altitude = point.Altitude;// ?? 0;

            // Convert to Cartesian coordinates
            double x = (distance + altitude) * Math.Cos(latRad) * Math.Cos(lonRad);
            double y = (distance + altitude) * Math.Cos(latRad) * Math.Sin(lonRad);
            double z = (distance + altitude) * Math.Sin(latRad);

            // Return new vector in meters
            return new Vector3((float)x, (float)y, (float)z);
        }

        private Point MetersToPoint(Vector3 meters)
        {
            double latitude = meters.X / EarthRadius * (180 / Math.PI);
            double longitude = meters.Y / (EarthRadius * Math.Cos(Math.PI / 180.0 * meters.X)) * (180 / Math.PI);
            return new Point(latitude, longitude);
        }

        public static Vector3 MetersToVector(Vector3 meters)
        {
            double lon = Math.Atan2(meters.Y, meters.X);
            double lat = Math.Atan2(meters.Z, Math.Sqrt(meters.X * meters.X + meters.Y * meters.Y));

            // Convert to degrees
            lon = lon * 180.0 / Math.PI;
            lat = lat * 180.0 / Math.PI;

            // Calculate altitude in meters
            double a = WGS84_A;
            double b = WGS84_B;
            double f = WGS84_F;
            double e2 = 2 * f - f * f;
            double N = a / Math.Sqrt(1 - e2 * Math.Sin(lat * Math.PI / 180.0) * Math.Sin(lat * Math.PI / 180.0));
            double alt = Math.Sqrt(meters.X * meters.X + meters.Y * meters.Y + meters.Z * meters.Z) - N;

            return new Vector3((float)lon, (float)lat, (float)alt);
        }
        private float ConvertKilometersToMiles(double kilometers)
        {
            return (float)kilometers * 0.621371192f;
        }
        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
        private double Haversine(Point p1, Point p2)
        {
            double lat1 = ToRadians(p1.Latitude);
            double lon1 = ToRadians(p1.Longitude);
            double lat2 = ToRadians(p2.Latitude);
            double lon2 = ToRadians(p2.Longitude);
            double alt1 = p1.Altitude;
            double alt2 = p2.Altitude;

            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;
            double dAlt = alt2 - alt1;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2) + Math.Sin(dAlt / 2) * Math.Sin(dAlt / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadius * c;
        }
        private double GetLineStringLength(LineString lineString)
        {
            double length = 0.0;
            for (int i = 0; i < lineString.Points.Count - 1; i++)
            {
                Point p1 = lineString.Points[i];
                Point p2 = lineString.Points[i + 1];
                length += Haversine(p1, p2);
            }
            return length;
        }
        private double GetPolygonPerimeter(Polygon polygon)
        {
            double perimeter = 0.0;
            foreach (List<Point> ring in polygon.Rings)
            {
                for (int i = 0; i < ring.Count - 1; i++)
                {
                    Point p1 = ring[i];
                    Point p2 = ring[i + 1];
                    perimeter += Haversine(p1, p2);
                }

                // Add the distance between the last and first point of the ring
                Point lastPoint = ring[ring.Count - 1];
                Point firstPoint = ring[0];
                perimeter += Haversine(lastPoint, firstPoint);
            }

            return perimeter;
        }
        private void WriteOutputToFile(string outputPath, string output)
        {
            if (string.IsNullOrEmpty(outputPath))
            {
                // If outputPath is null or empty, save the output to a file named "output.log" in the application folder.
                string directoryPath = AppDomain.CurrentDomain.BaseDirectory;
                outputPath = Path.Combine(directoryPath, "output.log");
            }

            try
            {
                // Write the output to the specified file or to a file named "output.log" in the application folder.
                using (StreamWriter writer = new StreamWriter(outputPath))
                {
                    writer.Write(output);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: Failed to write output to file '{outputPath}'. {ex.Message}");
            }
        }



    }
}
