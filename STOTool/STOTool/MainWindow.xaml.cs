using STOTool.Core;

namespace STOTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Hide();
            LogWindow.Instance.Show();
            
            Init.Initialize();
        }
    }
}