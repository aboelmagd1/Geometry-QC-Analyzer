using System;
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

        public int MaxFeaturesToProcess
        {
            get => _settings.MaxFeaturesToProcess;
            set
            {
                int val = Math.Clamp(value, 1, 100000);
                if (_settings.MaxFeaturesToProcess != val)
                {
                    _settings.MaxFeaturesToProcess = val;
                    NotifyPropertyChanged();
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

        public bool FlagArcLineJunctions
        {
            get => _settings.FlagArcLineJunctions;
            set { _settings.FlagArcLineJunctions = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public double ArcLineAngleToleranceDegrees
        {
            get => _settings.ArcLineAngleToleranceDegrees;
            set { _settings.ArcLineAngleToleranceDegrees = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public bool ShowWarningsOnMap
        {
            get => _settings.ShowWarningsOnMap;
            set
            {
                if (_settings.ShowWarningsOnMap != value)
                {
                    _settings.ShowWarningsOnMap = value;
                    NotifyPropertyChanged();
                    SaveSettings();

                    var resultsVm = FrameworkApplication.DockPaneManager.Find(ResultsDockPaneViewModel.DockPaneId) as ResultsDockPaneViewModel;
                    resultsVm?.RefreshMapGraphics();
                }
            }
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

        // 11. Flat Pseudo-Arcs
        public bool CheckFlatPseudoArcs
        {
            get => _settings.CheckFlatPseudoArcs;
            set { _settings.CheckFlatPseudoArcs = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public double FlatArcSagittaToleranceCm
        {
            get => _settings.FlatArcSagittaToleranceCm;
            set { _settings.FlatArcSagittaToleranceCm = value; NotifyPropertyChanged(); SaveSettings(); }
        }

        public double FlatArcAreaToleranceCm2
        {
            get => _settings.FlatArcAreaToleranceCm2;
            set { _settings.FlatArcAreaToleranceCm2 = value; NotifyPropertyChanged(); SaveSettings(); }
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
            _settings.EnableForServiceLayers = false;
            _settings.MaxFeaturesToProcess = 500;

            _settings.CheckInvalidGeometry = true;
            _settings.CheckOverlaps = true;
            _settings.OverlapMinAreaSqMeters = 0.00001; // 0.00001 m²
            _settings.CheckDuplicates = true;
            _settings.CheckGaps = true;
            _settings.GapMinAreaSqMeters = 0.00001; // 0.00001 m²
            _settings.CheckMultiPart = true;
            _settings.CheckShortSegments = true;
            _settings.ShortSegmentThresholdCm = 15.0; // 15.0 cm
            _settings.CheckAngleIssues = true;
            _settings.AngleThresholdDegrees = 30.0; // 30°
            _settings.CheckSnapIssues = true;
            _settings.SnapToleranceCm = 15.0; // 15 cm
            _settings.CheckRedundantVertices = true;
            _settings.RedundantVertexAngleDegrees = 175.0; // 175°
            _settings.FlagArcLineJunctions = false;
            _settings.ArcLineAngleToleranceDegrees = 175.0;
            _settings.ShowWarningsOnMap = true;
            _settings.CheckMissingJunctions = true;
            _settings.MissingJunctionToleranceCm = 10.0; // 10 cm
            _settings.CheckFlatPseudoArcs = false; // Check 11: disabled by default
            _settings.FlatArcSagittaToleranceCm = 0.01; // 0.01 cm (0.1 mm)
            _settings.FlatArcAreaToleranceCm2 = 10.0; // 10.0 cm²

            SaveSettings();

            // Refresh all properties
            NotifyPropertyChanged(string.Empty);
        }
    }
}
