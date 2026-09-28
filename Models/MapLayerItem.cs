using System;

namespace GeometryQCAddIn.Models
{
    /// <summary>
    /// Represents a selectable map layer option in the QC user interface.
    /// </summary>
    public class MapLayerItem
    {
        public string DisplayName { get; }
        public string? Uri { get; }
        public bool IsAll { get; }
        public bool IsLocal { get; }

        public MapLayerItem(string displayName, string? uri = null, bool isAll = false, bool isLocal = true)
        {
            DisplayName = displayName ?? string.Empty;
            Uri = uri;
            IsAll = isAll;
            IsLocal = isLocal;
        }

        public override string ToString() => DisplayName;

        public static MapLayerItem AllPolygonLayers => new MapLayerItem("All Polygon Layers", null, true, true);
    }
}
