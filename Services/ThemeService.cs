using System;
using System.Windows;
using System.Windows.Media;
using ArcGIS.Desktop.Framework;

namespace GeometryQCAddIn.Services
{
    /// <summary>
    /// Manages the custom Navy/Cyan/Turquoise color palette across ArcGIS Pro Light and Dark themes.
    /// Updates dynamic resource brushes in real-time so all UI components adapt seamlessly.
    /// </summary>
    public static class ThemeService
    {
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                ApplyTheme(IsDarkTheme());
            }
            catch (Exception ex)
            {
                LoggingService.Error("ThemeService.Initialize error", ex);
            }
        }

        public static bool IsDarkTheme()
        {
            try
            {
                if (Application.Current?.Resources["Esri_TextStyleDefaultBrush"] is SolidColorBrush b)
                {
                    // In dark theme, standard text is light (> 128 average brightness)
                    return (b.Color.R + b.Color.G + b.Color.B) / 3 > 128;
                }

                var prop = typeof(FrameworkApplication).GetProperty("ApplicationTheme");
                if (prop != null)
                {
                    var val = prop.GetValue(null)?.ToString();
                    if (val != null && val.IndexOf("Dark", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                    if (val != null && val.IndexOf("Light", StringComparison.OrdinalIgnoreCase) >= 0)
                        return false;
                }
            }
            catch { }
            return true; // Default to the user's signature Dark Navy palette
        }

        public static void ApplyTheme(bool isDark)
        {
            if (Application.Current == null) return;
            var res = Application.Current.Resources;

            if (isDark)
            {
                // Curated Dark Navy / Cyan / Turquoise Palette
                SetBrush(res, "MainBackgroundBrush", Color.FromRgb(0x0B, 0x13, 0x2B));          // Navy Black #0B132B
                SetBrush(res, "SidebarHeaderBackgroundBrush", Color.FromRgb(0x0C, 0x15, 0x2E)); // Dark Navy #0C152E
                SetBrush(res, "CardBackgroundBrush", Color.FromRgb(0x13, 0x1C, 0x38));          // Blue Gray Navy #131C38
                SetBrush(res, "InputBackgroundBrush", Color.FromRgb(0x0E, 0x17, 0x36));         // Dark Navy #0E1736
                SetBrush(res, "BorderBrush", Color.FromRgb(0x1C, 0x2B, 0x51));                  // Muted Blue #1C2B51
                SetBrush(res, "PrimaryAccentBrush", Color.FromRgb(0x00, 0xE4, 0xFC));           // Cyan #00E4FC
                SetBrush(res, "SecondaryAccentBrush", Color.FromRgb(0x00, 0xBF, 0xA5));         // Turquoise #00BFA5
                SetBrush(res, "PrimaryTextBrush", Color.FromRgb(0xF4, 0xF4, 0xF4));             // Near White #F4F4F4
                SetBrush(res, "SecondaryTextBrush", Color.FromRgb(0xE4, 0xE4, 0xE4));           // Light Gray #E4E4E4
                SetBrush(res, "MutedTextBrush", Color.FromRgb(0x60, 0x9C, 0x9C));               // Blue Gray #609C9C
                SetBrush(res, "ButtonBlueBrush", Color.FromRgb(0x1C, 0x3A, 0x70));              // Dark Blue #1C3A70
                SetBrush(res, "ActionButtonBrush", Color.FromRgb(0x00, 0xBF, 0xA5));            // Turquoise #00BFA5
                SetBrush(res, "ActionButtonHoverBrush", Color.FromRgb(0x00, 0xE4, 0xFC));       // Cyan #00E4FC
                SetBrush(res, "ButtonForegroundBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));        // Pure White #FFFFFF
                SetBrush(res, "BadgeBackgroundBrush", Color.FromRgb(0x00, 0xE4, 0xFC));         // Cyan #00E4FC
                SetBrush(res, "BadgeForegroundBrush", Color.FromRgb(0x0B, 0x13, 0x2B));         // Navy Black text #0B132B
            }
            else
            {
                // Harmonious Light Theme Counterpart
                SetBrush(res, "MainBackgroundBrush", Color.FromRgb(0xF4, 0xF6, 0xFB));          // Light Neutral Canvas
                SetBrush(res, "SidebarHeaderBackgroundBrush", Color.FromRgb(0xE8, 0xEE, 0xF8)); // Clean Cool Header
                SetBrush(res, "CardBackgroundBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));          // Crisp White Cards
                SetBrush(res, "InputBackgroundBrush", Color.FromRgb(0xFA, 0xFB, 0xFD));         // Soft Input Surface
                SetBrush(res, "BorderBrush", Color.FromRgb(0xD1, 0xDC, 0xEE));                  // Subtle Slate Border
                SetBrush(res, "PrimaryAccentBrush", Color.FromRgb(0x00, 0x95, 0xB6));           // High-Contrast Cyan
                SetBrush(res, "SecondaryAccentBrush", Color.FromRgb(0x00, 0x9B, 0x87));         // Emerald Turquoise
                SetBrush(res, "PrimaryTextBrush", Color.FromRgb(0x0B, 0x13, 0x2B));             // Dark Navy Text #0B132B
                SetBrush(res, "SecondaryTextBrush", Color.FromRgb(0x3B, 0x48, 0x64));           // Slate Gray
                SetBrush(res, "MutedTextBrush", Color.FromRgb(0x65, 0x7A, 0x96));               // Muted Gray
                SetBrush(res, "ButtonBlueBrush", Color.FromRgb(0x23, 0x4C, 0x94));              // Royal Blue
                SetBrush(res, "ActionButtonBrush", Color.FromRgb(0x00, 0x9B, 0x87));            // Turquoise Action
                SetBrush(res, "ActionButtonHoverBrush", Color.FromRgb(0x00, 0xB4, 0x9D));       // Vibrant Mint
                SetBrush(res, "ButtonForegroundBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));        // White
                SetBrush(res, "BadgeBackgroundBrush", Color.FromRgb(0x00, 0x95, 0xB6));         // Cyan
                SetBrush(res, "BadgeForegroundBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));         // White
            }
        }

        private static void SetBrush(ResourceDictionary res, string key, Color color)
        {
            if (res[key] is SolidColorBrush brush && !brush.IsFrozen)
            {
                brush.Color = color;
            }
            else
            {
                var newBrush = new SolidColorBrush(color);
                res[key] = newBrush;
            }
        }
    }
}
