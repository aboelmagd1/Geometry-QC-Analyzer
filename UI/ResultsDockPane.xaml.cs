using System;
using System.Windows.Controls;

namespace GeometryQCAddIn.UI
{
    /// <summary>
    /// Interaction logic for ResultsDockPane.xaml
    /// </summary>
    public partial class ResultsDockPaneView : UserControl
    {
        public ResultsDockPaneView()
        {
            InitializeComponent();
        }

        private async void LayerComboBox_DropDownOpened(object sender, EventArgs e)
        {
            if (DataContext is ResultsDockPaneViewModel vm)
            {
                await vm.RefreshLayersAsync();
            }
        }
    }
}
