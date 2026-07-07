using System.Windows;
using HardwareInserter.App.ViewModels;

namespace HardwareInserter.App.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel(App.CatalogJsonPath);
            DataContext = _viewModel;
            Closed += (_, _) => _viewModel.Dispose();
        }
    }
}
