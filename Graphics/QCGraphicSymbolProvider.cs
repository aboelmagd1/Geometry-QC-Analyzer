using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Mapping;

namespace GeometryQCAddIn.Graphics
{
    /// <summary>
    /// Central provider for high-visibility, theme-compatible CIM symbols corresponding to each QC check.
    /// Follows the color specifications:
    /// - Invalid Geometry: Purple
    /// - Overlap: Red
    /// - Duplicate: Dark Red / Maroon
    /// - Gap: Yellow / Amber
    /// - Multi-Part: Orange
    /// - Short Segment: Blue
    /// - Angle Issue: Cyan
    /// - Snap Issue: Deep Blue
    /// - Redundant Vertex: Gray
    /// - Missing Junction: Magenta
    /// </summary>
    public static class QCGraphicSymbolProvider
    {
        public static CIMSymbol? GetSymbolForIssue(string checkId, string issueType)
        {
            switch (checkId)
            {
                case "CHK_INVALID_GEOM":
                    // Purple point / polygon outline
                    return SymbolFactory.Instance.ConstructPointSymbol(
                        CIMColor.CreateRGBColor(160, 32, 240), 10.0, SimpleMarkerStyle.Cross);

                case "CHK_OVERLAP":
                    // Semi-transparent red fill with bright red outline
                    var redFill = CIMColor.CreateRGBColor(255, 0, 0, 100);
                    var redOutline = SymbolFactory.Instance.ConstructStroke(
                        CIMColor.CreateRGBColor(255, 0, 0), 2.0, SimpleLineStyle.Solid);
                    return SymbolFactory.Instance.ConstructPolygonSymbol(redFill, SimpleFillStyle.Solid, redOutline);

                case "CHK_DUPLICATE":
                    // Dark red / Maroon hatched polygon
                    var darkRed = CIMColor.CreateRGBColor(139, 0, 0);
                    var darkRedOutline = SymbolFactory.Instance.ConstructStroke(darkRed, 2.5, SimpleLineStyle.Solid);
                    return SymbolFactory.Instance.ConstructPolygonSymbol(
                        CIMColor.CreateRGBColor(139, 0, 0, 80), SimpleFillStyle.DiagonalCross, darkRedOutline);

                case "CHK_GAP":
                    // Bright yellow / amber polygon fill
                    var yellowFill = CIMColor.CreateRGBColor(255, 215, 0, 150);
                    var yellowOutline = SymbolFactory.Instance.ConstructStroke(
                        CIMColor.CreateRGBColor(255, 165, 0), 2.0, SimpleLineStyle.Solid);
                    return SymbolFactory.Instance.ConstructPolygonSymbol(yellowFill, SimpleFillStyle.Solid, yellowOutline);

                case "CHK_MULTIPART":
                    // Orange diamond point symbol
                    return SymbolFactory.Instance.ConstructPointSymbol(
                        CIMColor.CreateRGBColor(255, 140, 0), 10.0, SimpleMarkerStyle.Diamond);

                case "CHK_SHORT_SEG":
                    // Vivid Blue line symbol
                    return SymbolFactory.Instance.ConstructLineSymbol(
                        CIMColor.CreateRGBColor(0, 120, 255), 3.0, SimpleLineStyle.Solid);

                case "CHK_ANGLE":
                    // Cyan triangle point symbol
                    return SymbolFactory.Instance.ConstructPointSymbol(
                        CIMColor.CreateRGBColor(0, 220, 220), 9.0, SimpleMarkerStyle.Triangle);

                case "CHK_SNAP":
                    // Blue circle point symbol with white border
                    return SymbolFactory.Instance.ConstructPointSymbol(
                        CIMColor.CreateRGBColor(30, 144, 255), 8.0, SimpleMarkerStyle.Circle);

                case "CHK_REDUNDANT":
                    // Gray square point symbol
                    return SymbolFactory.Instance.ConstructPointSymbol(
                        CIMColor.CreateRGBColor(128, 128, 128), 7.0, SimpleMarkerStyle.Square);

                case "CHK_JUNCTION":
                    // Magenta star / cross point symbol
                    return SymbolFactory.Instance.ConstructPointSymbol(
                        CIMColor.CreateRGBColor(255, 0, 255), 11.0, SimpleMarkerStyle.X);

                default:
                    return SymbolFactory.Instance.ConstructPointSymbol(
                        CIMColor.CreateRGBColor(255, 69, 0), 8.0, SimpleMarkerStyle.Circle);
            }
        }

        public static CIMSymbol? GetHighlightSymbol()
        {
            return SymbolFactory.Instance.ConstructPointSymbol(
                CIMColor.CreateRGBColor(255, 255, 0), 14.0, SimpleMarkerStyle.Circle);
        }
    }
}
