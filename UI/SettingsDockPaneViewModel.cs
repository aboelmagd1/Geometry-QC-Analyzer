using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using GeometryQCAddIn.Config;
using GeometryQCAddIn.Services;

namespace GeometryQCAddIn.UI
{
    public class SettingsDockPaneViewModel : DockPane
    {
        public const string DockPaneId = "GeometryQC_SettingsDockPane";

        private readonly GeometryQCSettings _settings = GeometryQCSettings.Instance;

        public bool IsQCEnabled
        {
            get => _settings.IsEnabled;
            set
            {
                if (_settings.IsEnabled != value)
                {
                    _settings.IsEnabled = value;
                    NotifyPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool IsDisplayCacheMode
        {
            get => _settings.DataSource == DataSourceMode.DisplayCache;
            set
            {
                if (value)
                {
                    _settings.DataSource = DataSourceMode.DisplayCache;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(IsLiveQueryMode));
                    SaveSettings();
                }
            }
        }

        public bool IsLiveQueryMode
        {
            get => _settings.DataSource == DataSourceMode.LiveQuery;
            set
            {
                if (value)
                {
                    _settings.DataSource = DataSourceMode.LiveQuery;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(IsDisplayCacheMode));
                    SaveSettings();
                }
            }
        }

        public bool EnableForServiceLayers
        {
            get => _settings.EnableForServiceLayers;
            set
            {
                if (_settings.EnableForServiceLayers != value)
                {
                    _settings.EnableForServiceLayers = value;
                    NotifyPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool IsAutoRunMode
        {
            get => _settings.ExecutionMode == ExecutionMode.Auto;
            set
            {
                if (value)
                {
                    _settings.ExecutionMode = ExecutionMode.Auto;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(IsManualMode));
                    SaveSettings();
                }
            }
        }

        public bool IsManualMode
        {
            get => _settings.ExecutionMode == ExecutionMode.Manual;
            set
            {
                if (value)
                {
                    _settings.ExecutionMode = ExecutionMode.Manual;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(IsAutoRunMode));
                    SaveSettings();
                }
            }
        }

        // Checks toggles
        public bool CheckInvalidGeometry
        {
            get => _settings.CheckInvalidGeometry;
            set { _settings.CheckInvalidGeometry = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public bool CheckOverlaps
        {
            get => _settings.CheckOverlaps;
            set { _settings.CheckOverlaps = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public double OverlapMinArea
        {
            get => _settings.OverlapMinAreaSqMeters;
            set { _settings.OverlapMinAreaSqMeters = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public bool CheckDuplicates
        {
            get => _settings.CheckDuplicates;
            set { _settings.CheckDuplicates = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public bool CheckGaps
        {
            get => _settings.CheckGaps;
            set { _settings.CheckGaps = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public double GapMinArea
        {
            get => _settings.GapMinAreaSqMeters;
            set { _settings.GapMinAreaSqMeters = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public bool CheckMultiPart
        {
            get => _settings.CheckMultiPart;
            set { _settings.CheckMultiPart = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public bool CheckShortSegments
        {
            get => _settings.CheckShortSegments;
            set { _settings.CheckShortSegments = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public double ShortSegmentThresholdCm
        {
            get => _settings.ShortSegmentThresholdCm;
            set { _settings.ShortSegmentThresholdCm = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public bool CheckAngleIssues
        {
            get => _settings.CheckAngleIssues;
            set { _settings.CheckAngleIssues = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public double AngleThresholdDegrees
        {
            get => _settings.AngleThresholdDegrees;
            set { _settings.AngleThresholdDegrees = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public bool CheckSnapIssues
        {
            get => _settings.CheckSnapIssues;
            set { _settings.CheckSnapIssues = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public double SnapToleranceCm
        {
            get => _settings.SnapToleranceCm;
            set { _settings.SnapToleranceCm = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public bool CheckRedundantVertices
        {
            get => _settings.CheckRedundantVertices;
            set { _settings.CheckRedundantVertices = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public double RedundantVertexAngleDegrees
        {
            get => _settings.RedundantVertexAngleDegrees;
            set { _settings.RedundantVertexAngleDegrees = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public bool CheckMissingJunctions
        {
            get => _settings.CheckMissingJunctions;
            set { _settings.CheckMissingJunctions = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public double MissingJunctionToleranceCm
        {
            get => _settings.MissingJunctionToleranceCm;
            set { _settings.MissingJunctionToleranceCm = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public ICommand ResetDefaultsCommand { get; }

        public SettingsDockPaneViewModel()
        {
            ThemeService.Initialize();
            ResetDefaultsCommand = new RelayCommand(ResetDefaults);
        }

        public static void Show()
        {
            var pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
            if (pane != null)
            {
                pane.Activate();
            }
        }

        private void SaveSettings()
        {
            _settings.Save();
        }

        private void ResetDefaults()
        {
            _settings.IsEnabled = true;
            _settings.DataSource = DataSourceMode.DisplayCache;
            _settings.EnableForServiceLayers = false;
            _settings.ExecutionMode = ExecutionMode.Manual;

            _settings.CheckInvalidGeometry = true;
            _settings.CheckOverlaps = true;
            _settings.OverlapMinAreaSqMeters = 0.0001;
            _settings.CheckDuplicates = true;
            _settings.CheckGaps = true;
            _settings.GapMinAreaSqMeters = 0.001;
            _settings.CheckMultiPart = true;
            _settings.CheckShortSegments = true;
            _settings.ShortSegmentThresholdCm = 10.0;
            _settings.CheckAngleIssues = true;
            _settings.AngleThresholdDegrees = 5.0;
            _settings.CheckSnapIssues = true;
            _settings.SnapToleranceCm = 1.0;
            _settings.CheckRedundantVertices = true;
            _settings.RedundantVertexAngleDegrees = 179.9;
            _settings.CheckMissingJunctions = true;
            _settings.MissingJunctionToleranceCm = 10.0;

            SaveSettings();

            // Refresh all properties
            NotifyPropertyChanged(string.Empty);
        }
    }
}
