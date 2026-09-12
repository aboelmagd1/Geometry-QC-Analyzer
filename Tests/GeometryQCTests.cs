using System;
using System.Collections.Generic;
using GeometryQCAddIn.Config;
using GeometryQCAddIn.Core;
using GeometryQCAddIn.Models;

namespace GeometryQCAddIn.Tests
{
    /// <summary>
    /// Verification and testing suite for Geometry QC calculations and algorithms.
    /// </summary>
    public static class GeometryQCTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("Running Geometry QC Validation Tests...");

            TestUnitConverter();
            TestCollinearAngleCalculation();
            TestAcuteAngleCalculation();
            TestSegmentPointDistance();
            TestMetricJunctionInterior();
            TestAreaConversion();

            Console.WriteLine("All Geometry QC Core Tests Passed Successfully.");
        }

        private static void TestMetricJunctionInterior()
        {
            // Segment from (0,0) to (1000, 0) (1 km segment)
            double p1x = 0.0, p1y = 0.0;
            double p2x = 1000.0, p2y = 0.0;
            double segDx = p2x - p1x;
            double segDy = p2y - p1y;
            double segLenSq = segDx * segDx + segDy * segDy;
            double tolMapUnits = 0.01; // 1 cm

            // Test A: A vertex at (2.0, 0.005) - 2 meters from endpoint, near segment by 5mm
            // Under previous relative percentage (t > 0.01), t = 2/1000 = 0.002, which would have been skipped!
            double vx = 2.0, vy = 0.005;
            double t = ((vx - p1x) * segDx + (vy - p1y) * segDy) / segLenSq;
            double projX = p1x + t * segDx;
            double projY = p1y + t * segDy;
            double d1Sq = (projX - p1x) * (projX - p1x) + (projY - p1y) * (projY - p1y);
            double d2Sq = (projX - p2x) * (projX - p2x) + (projY - p2y) * (projY - p2y);
            double tolSq = tolMapUnits * tolMapUnits;

            if (!(t >= 0.0 && t <= 1.0) || !(d1Sq > tolSq) || !(d2Sq > tolSq))
            {
                throw new Exception("Metric junction interior test failed: 2m from end of 1km segment should be recognized as interior.");
            }

            // Test B: A vertex at (0.005, 0.005) - only 5mm from endpoint
            // Should be recognized as coincident with endpoint (not interior T-junction)
            double vxEnd = 0.005, vyEnd = 0.005;
            double tEnd = ((vxEnd - p1x) * segDx + (vyEnd - p1y) * segDy) / segLenSq;
            double projXEnd = p1x + tEnd * segDx;
            double projYEnd = p1y + tEnd * segDy;
            double d1EndSq = (projXEnd - p1x) * (projXEnd - p1x) + (projYEnd - p1y) * (projYEnd - p1y);

            if (d1EndSq > tolSq)
            {
                throw new Exception("Metric junction endpoint test failed: 5mm from endpoint should NOT be recognized as interior junction.");
            }

            Console.WriteLine("✓ Metric junction interior test passed.");
        }

        private static void TestAreaConversion()
        {
            // 1 sq meter = 1.0 map units sq when sr is null
            double mapUnitsSq = UnitConverter.SqMetersToMapUnitsSq(10.0, null);
            if (Math.Abs(mapUnitsSq - 10.0) > 1e-6)
            {
                throw new Exception($"Area conversion test failed: Expected 10.0, got {mapUnitsSq}");
            }
            Console.WriteLine("✓ Area conversion test passed.");
        }

        private static void TestUnitConverter()
        {
            // Test 1: Linear unit conversion (10 cm = 0.1 m)
            double mapUnits = UnitConverter.CentimetersToMapUnits(10.0, null);
            if (Math.Abs(mapUnits - 0.1) > 1e-6)
            {
                throw new Exception($"UnitConverter Test Failed: Expected 0.1, got {mapUnits}");
            }

            var (val, unit) = UnitConverter.FormatLinearDistance(0.062, null);
            if (unit != "cm" || Math.Abs(val - 6.2) > 0.1)
            {
                throw new Exception($"UnitConverter formatting failed: Expected 6.2 cm, got {val} {unit}");
            }

            Console.WriteLine("✓ UnitConverter tests passed.");
        }

        private static void TestCollinearAngleCalculation()
        {
            // Three collinear points: (0,0), (5,0), (10,0)
            double p1x = 0, p1y = 0;
            double currX = 5, currY = 0;
            double p2x = 10, p2y = 0;

            double inX = currX - p1x;
            double inY = currY - p1y;
            double outX = p2x - currX;
            double outY = p2y - currY;

            double dot = (inX * outX + inY * outY) / (Math.Sqrt(inX * inX + inY * inY) * Math.Sqrt(outX * outX + outY * outY));
            double deflectionAngle = Math.Acos(Math.Clamp(dot, -1.0, 1.0)) * (180.0 / Math.PI);
            double straightAngle = 180.0 - deflectionAngle;

            if (Math.Abs(straightAngle - 180.0) > 1e-4)
            {
                throw new Exception($"Collinear test failed: Expected 180.0 deg, got {straightAngle}");
            }

            Console.WriteLine("✓ Collinear angle calculation test passed.");
        }

        private static void TestAcuteAngleCalculation()
        {
            // Sharp angle: (0, 10) -> (0, 0) -> (0.1, 10)
            double v1x = 0 - 0;
            double v1y = 10 - 0;
            double v2x = 0.1 - 0;
            double v2y = 10 - 0;

            double len1 = Math.Sqrt(v1x * v1x + v1y * v1y);
            double len2 = Math.Sqrt(v2x * v2x + v2y * v2y);
            double dot = (v1x * v2x + v1y * v2y) / (len1 * len2);
            double angleDeg = Math.Acos(Math.Clamp(dot, -1.0, 1.0)) * (180.0 / Math.PI);

            if (angleDeg > 2.0)
            {
                throw new Exception($"Acute angle calculation test failed: Expected < 2.0 deg, got {angleDeg}");
            }

            Console.WriteLine($"✓ Acute angle calculation test passed ({angleDeg:F2}°).");
        }

        private static void TestSegmentPointDistance()
        {
            // Point (5, 0.005) near segment (0,0) to (10,0)
            double px = 5.0, py = 0.005;
            double p1x = 0.0, p1y = 0.0;
            double p2x = 10.0, p2y = 0.0;

            double segDx = p2x - p1x;
            double segDy = p2y - p1y;
            double segLenSq = segDx * segDx + segDy * segDy;

            double t = ((px - p1x) * segDx + (py - p1y) * segDy) / segLenSq;
            double projX = p1x + t * segDx;
            double projY = p1y + t * segDy;
            double dist = Math.Sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));

            if (Math.Abs(dist - 0.005) > 1e-6 || Math.Abs(t - 0.5) > 1e-6)
            {
                throw new Exception($"Point-to-segment distance test failed: Expected 0.005, got {dist}");
            }

            Console.WriteLine("✓ Point-to-segment distance test passed.");
        }
    }
}
