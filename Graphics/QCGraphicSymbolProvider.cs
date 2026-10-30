using System;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Mapping;

namespace GeometryQCAddIn.Graphics
{
    /// <summary>
    /// Central provider for high-visibility, theme-compatible CIM symbols corresponding to each QC check.
    /// Supports user-configurable transparency levels across all shape elements, including fills, lines, and strokes.
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
    /// - Flat Pseudo-Arc: Teal
    /// </summary>
    public static class QCGraphicSymbolProvider
    {
        /// <summary>
        /// Creates a CIMColor with opacity scaled by the user's transparency setting.
        /// In ArcGIS Pro SDK, CIMColor alpha is defined as an opacity percentage from 0.0 (transparent) to 100.0 (opaque).
        /// </summary>
        private static CIMColor GetColor(int r, int g, int b, double baseOpacityPercent, double transparencyPercent)
        {
            // transparency: 0% = fully opaque (factor 1.0), 10% = default (factor 0.90), up to 90%
            double factor = Math.Clamp(1.0 - (transparencyPercent / 100.0), 0.05, 1.0);
            double effectiveAlpha = Math.Clamp(baseOpacityPercent * factor, 5.0, 100.0);
            return CIMColor.CreateRGBColor(r, g, b, effectiveAlpha);
        }

        public static CIMSymbol? GetSymbolForIssue(string checkId, string issueType, double transparencyPercent = 10.0)
        {
            switch (checkId)
            {
                case "CHK_INVALID_GEOM":
                    // Purple point cross / polygon outline
                    return SymbolFactory.Instance.ConstructPointSymbol(
                        GetColor(160, 32, 240, 100.0, transparencyPercent), 10.0, SimpleMarkerStyle.Cross);

                case "CHK_OVERLAP":
                    // Semi-transparent red fill with matching transparent bright red outline
                    var redFill = GetColor(255, 0, 0, 40.0, transparencyPercent);
                    var redOutline = SymbolFactory.Instance.ConstructStroke(
                        GetColor(255, 0, 0, 100.0, transparencyPercent), 2.0, SimpleLineStyle.Solid);
                    return SymbolFactory.Instance.ConstructPolygonSymbol(redFill, SimpleFillStyle.Solid, redOutline);

                case "CHK_DUPLICATE":
                    // Dark red / Maroon hatched polygon
                    var darkRedOutline = SymbolFactory.Instance.ConstructStroke(
                        GetColor(139, 0, 0, 100.0, transparencyPercent), 2.5, SimpleLineStyle.Solid);
                    return SymbolFactory.Instance.ConstructPolygonSymbol(
                        GetColor(139, 0, 0, 35.0, transparencyPercent), SimpleFillStyle.DiagonalCross, darkRedOutline);

                case "CHK_GAP":
                    // Yellow / amber polygon fill with amber outline
                    var yellowFill = GetColor(255, 215, 0, 50.0, transparencyPercent);
                    var yellowOutline = SymbolFactory.Instance.ConstructStroke(
                        GetColor(255, 165, 0, 100.0, transparencyPercent), 2.0, SimpleLineStyle.Solid);
                    return SymbolFactory.Instance.ConstructPolygonSymbol(yellowFill, SimpleFillStyle.Solid, yellowOutline);

                case "CHK_MULTIPART":
                    // Orange diamond point symbol
                    return SymbolFactory.Instance.ConstructPointSymbol(
                        GetColor(255, 140, 0, 100.0, transparencyPercent), 10.0, SimpleMarkerStyle.Diamond);

                case "CHK_SHORT_SEG":
                    // Vivid Blue line symbol
                    return SymbolFactory.Instance.ConstructLineSymbol(
                        GetColor(0, 120, 255, 100.0, transparencyPercent), 3.0, SimpleLineStyle.Solid);

                case "CHK_ANGLE":
                    // Cyan triangle point symbol
                    return SymbolFactory.Instance.ConstructPointSymbol(
                        GetColor(0, 220, 220, 100.0, transparencyPercent), 9.0, SimpleMarkerStyle.Triangle);

                case "CHK_SNAP":
                    // Blue circle point symbol with white border
                    return SymbolFactory.Instance.ConstructPointSymbol(
                        GetColor(30, 144, 255, 100.0, transparencyPercent), 8.0, SimpleMarkerStyle.Circle);

                case "CHK_REDUNDANT":
                    // Gray square point symbol
                    return SymbolFactory.Instance.ConstructPointSymbol(
                        GetColor(128, 128, 128, 100.0, transparencyPercent), 7.0, SimpleMarkerStyle.Square);

                case "CHK_REDUNDANT_ARCLINE":
                    // Warning Coral triangle point symbol
                    return SymbolFactory.Instance.ConstructPointSymbol(
                        GetColor(255, 127, 80, 100.0, transparencyPercent), 9.0, SimpleMarkerStyle.Triangle);

                case "CHK_JUNCTION":
                    // Magenta star / cross point symbol
                    return SymbolFactory.Instance.ConstructPointSymbol(
                        GetColor(255, 0, 255, 100.0, transparencyPercent), 11.0, SimpleMarkerStyle.X);

                case "CHK_FLAT_PSEUDO_ARC":
                    // Warning Teal dashed line symbol for flat pseudo-arcs
                    return SymbolFactory.Instance.ConstructLineSymbol(
                        GetColor(0, 206, 209, 100.0, transparencyPercent), 3.0, SimpleLineStyle.Dash);

                default:
                    return SymbolFactory.Instance.ConstructPointSymbol(
                        GetColor(255, 69, 0, 100.0, transparencyPercent), 8.0, SimpleMarkerStyle.Circle);
            }
        }

        public static CIMSymbol? GetHighlightSymbol()
        {
            return SymbolFactory.Instance.ConstructPointSymbol(
                CIMColor.CreateRGBColor(255, 255, 0, 100.0), 14.0, SimpleMarkerStyle.Circle);
        }
    }
}
