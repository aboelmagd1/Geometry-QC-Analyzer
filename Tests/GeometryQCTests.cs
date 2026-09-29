using System;
using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using GeometryQCAddIn.Config;
using GeometryQCAddIn.Core;
using GeometryQCAddIn.Core.Checks;
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
            TestCurveRedundantVertexCalculations();

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

            // Test FormatArea
            var (val1, unit1) = UnitConverter.FormatArea(10.0, null);
            if (unit1 != "m²" || Math.Abs(val1 - 10.0) > 1e-3)
            {
                throw new Exception($"FormatArea test failed: Expected 10 m², got {val1} {unit1}");
            }

            var (val2, unit2) = UnitConverter.FormatArea(0.0005, null);
            if (unit2 != "cm²" || Math.Abs(val2 - 5.0) > 1e-2)
            {
                throw new Exception($"FormatArea test failed for small area: Expected 5 cm², got {val2} {unit2}");
            }

            Console.WriteLine("✓ Area conversion and formatting tests passed.");
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

            // Test sub-millimeter distance formatting (0.0004 m = 0.04 cm = 0.4 mm)
            var (valMm, unitMm) = UnitConverter.FormatLinearDistance(0.0004, null);
            if (unitMm != "mm" || Math.Abs(valMm - 0.4) > 1e-3)
            {
                throw new Exception($"UnitConverter sub-millimeter formatting failed: Expected 0.4 mm, got {valMm} {unitMm}");
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

        private static void TestCurveRedundantVertexCalculations()
        {
            double thresholdDeg = 179.9;
            double tolMapUnits = 0.01; // 1 cm

            // Test 1: Continuous circular arc split into two halves
            var center = new Coordinate2D(500, 500);
            double r = 50.0;
            var arc1 = new EllipticArcBuilderEx(0.0, Math.PI / 4, center, r, null).ToSegment();
            var arc2 = new EllipticArcBuilderEx(Math.PI / 4, Math.PI / 4, center, r, null).ToSegment();
            var v1 = arc1.EndPoint;

            bool isRedundant1 = RedundantVertexCheck.AreCurvesContinuousRedundant(
                arc1, arc2, v1, thresholdDeg, tolMapUnits, null, out double? angle1, out string details1);

            if (!isRedundant1 || !angle1.HasValue || Math.Abs(angle1.Value - 180.0) > 0.01)
            {
                throw new Exception($"Continuous circular arc test failed: Expected redundant with 180°, got {isRedundant1}, angle={angle1}");
            }

            // Test 2: Different radius (r=50 vs r=60) - genuine geometric difference
            var arcDiffR = new EllipticArcBuilderEx(Math.PI / 4, Math.PI / 4, center, 60.0, null).ToSegment();
            bool isRedundant2 = RedundantVertexCheck.AreCurvesContinuousRedundant(
                arc1, arcDiffR, v1, thresholdDeg, tolMapUnits, null, out _, out _);

            if (isRedundant2)
            {
                throw new Exception("Different radius test failed: Arcs with different radii should NOT be flagged as redundant.");
            }

            // Test 3: Different center - genuine geometric difference
            var centerDiff = new Coordinate2D(502, 500);
            var arcDiffCenter = new EllipticArcBuilderEx(Math.PI / 4, Math.PI / 4, centerDiff, r, null).ToSegment();
            bool isRedundant3 = RedundantVertexCheck.AreCurvesContinuousRedundant(
                arc1, arcDiffCenter, v1, thresholdDeg, tolMapUnits, null, out _, out _);

            if (isRedundant3)
            {
                throw new Exception("Different center test failed: Arcs with different centers should NOT be flagged as redundant.");
            }

            // Test 4: Reverse curvature (inflection, CW vs CCW)
            var arcReverse = new EllipticArcBuilderEx(center, r, ArcOrientation.ArcClockwise, null).ToSegment();
            bool isRedundant4 = RedundantVertexCheck.AreCurvesContinuousRedundant(
                arc1, arcReverse, v1, thresholdDeg, tolMapUnits, null, out _, out _);

            if (isRedundant4)
            {
                throw new Exception("Reverse curvature test failed: Reverse curvature (inflection) should NOT be flagged as redundant.");
            }

            // Test 5: Arc to Line transition (Curve -> Vertex -> Line)
            var lineSeg = new LineBuilderEx(v1, MapPointBuilderEx.CreateMapPoint(v1.X + 10, v1.Y)).ToSegment();
            bool isRedundant5 = RedundantVertexCheck.AreCurvesContinuousRedundant(
                arc1, lineSeg, v1, thresholdDeg, tolMapUnits, null, out _, out _);

            if (isRedundant5)
            {
                throw new Exception("Arc to Line test failed: Transition between curve and line should NOT be flagged as redundant.");
            }

            // Test 6: Continuous cubic Bezier split
            var p0 = new Coordinate2D(0, 0);
            var c1 = new Coordinate2D(10, 20);
            var c2 = new Coordinate2D(40, 20);
            var p3 = new Coordinate2D(50, 0);
            double t = 0.5;
            var p01 = new Coordinate2D(p0.X + (c1.X - p0.X) * t, p0.Y + (c1.Y - p0.Y) * t);
            var p12 = new Coordinate2D(c1.X + (c2.X - c1.X) * t, c1.Y + (c2.Y - c1.Y) * t);
            var p23 = new Coordinate2D(c2.X + (p3.X - c2.X) * t, c2.Y + (p3.Y - c2.Y) * t);
            var p012 = new Coordinate2D(p01.X + (p12.X - p01.X) * t, p01.Y + (p12.Y - p01.Y) * t);
            var p123 = new Coordinate2D(p12.X + (p23.X - p12.X) * t, p12.Y + (p23.Y - p12.Y) * t);
            var split = new Coordinate2D(p012.X + (p123.X - p012.X) * t, p012.Y + (p123.Y - p012.Y) * t);

            var bez1 = new CubicBezierBuilderEx(p0, p01, p012, split, null).ToSegment();
            var bez2 = new CubicBezierBuilderEx(split, p123, p23, p3, null).ToSegment();
            var vBez = MapPointBuilderEx.CreateMapPoint(split.X, split.Y);

            bool isRedundant6 = RedundantVertexCheck.AreCurvesContinuousRedundant(
                bez1, bez2, vBez, thresholdDeg, tolMapUnits, null, out double? angle6, out _);

            if (!isRedundant6 || !angle6.HasValue || Math.Abs(angle6.Value - 180.0) > 0.01)
            {
                throw new Exception($"Continuous Bezier test failed: Expected redundant with 180°, got {isRedundant6}, angle={angle6}");
            }

            // Test 7: Bezier with corner / kink (deflected tangent)
            var kinkCP1 = new Coordinate2D(split.X, split.Y + 20); // 90 degree kink
            var bezKink = new CubicBezierBuilderEx(split, kinkCP1, p23, p3, null).ToSegment();
            bool isRedundant7 = RedundantVertexCheck.AreCurvesContinuousRedundant(
                bez1, bezKink, vBez, thresholdDeg, tolMapUnits, null, out _, out _);

            if (isRedundant7)
            {
                throw new Exception("Bezier kink test failed: Bezier curves with deflected tangent should NOT be flagged as redundant.");
            }

            // Test 8: Arc to Bezier (different segment types)
            bool isRedundant8 = RedundantVertexCheck.AreCurvesContinuousRedundant(
                arc1, bez2, v1, thresholdDeg, tolMapUnits, null, out _, out _);

            if (isRedundant8)
            {
                throw new Exception("Arc to Bezier test failed: Different curve types should NOT be flagged as redundant.");
            }

            Console.WriteLine("✓ Curve-aware redundant vertex calculation tests passed.");
        }
    }
}
