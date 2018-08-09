using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfControls;

namespace cleanup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static StartupEventArgs Args;
        public MainWindow()
        {
            InitializeComponent();

            ViewModel.Instance.Cleanup = new ObservableCollection<CleanupPath>()
            {
                //new CleanupPath(){Enable = false, Path = "hello"},
                //new CleanupPath(){Enable = true, Path = "world"}
            };

            TbAuto.Provider = new SuggestionProvider((text) =>
            {
                var dir = System.IO.Path.GetDirectoryName(text);
                var root = System.IO.Path.GetPathRoot(text);
                var file = System.IO.Path.GetFileName(text);
                var d = Directory.GetDirectoryRoot(text);
                var baseDir = string.IsNullOrEmpty(dir) ? root : dir;
                if (!Directory.Exists(baseDir))
                    return Enumerable.Empty<string>();
                var paths = Directory.GetDirectories(baseDir, string.Format("{0}*",file), SearchOption.TopDirectoryOnly);
                return paths;
            });

            Loaded += delegate
            {
                if (Args.Args.Length > 0)
                {
                    //TbAuto.Text = Args.Args[0];.
                    ViewModel.Instance.Root = Args.Args[0];
                    //Keyboard.ClearFocus();
                    TbAuto_OnLostFocus(null, null);
                }
            };
        }


        private void TbAuto_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(TbAuto.Text))
                return;

            var dirs = Directory.GetDirectories(TbAuto.Text, "*", SearchOption.AllDirectories)
                .Where(dir => System.IO.Path.GetFileName(dir) == "bin" || System.IO.Path.GetFileName(dir) == "obj")
                .Select(dir => new CleanupPath()
                { Enable = true, Path = dir });

            ThreadPool.QueueUserWorkItem((o) =>
            {
                App.Current.Dispatcher.Invoke(() => ViewModel.Instance.Cleanup.Clear());
                
                foreach (var cp in dirs)
                {
                    App.Current.Dispatcher.Invoke(() => ViewModel.Instance.Cleanup.Add(cp));
                }
            });
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var cleanupPath in ViewModel.Instance.Cleanup.Where(c => c.Enable).ToArray())
            {
                try
                {
                    Directory.Delete(cleanupPath.Path, true);
                    ViewModel.Instance.Cleanup.Remove(cleanupPath);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message);
                }
            }
        }
    }

    public class ViewModel: INotifyPropertyChanged
    {
        public static ViewModel Instance { get; private set; }
        static ViewModel() { Instance = new ViewModel(); }

        private string root;

        public string Root
        {
            get { return root; }
            set { root = value; propChanged("Root"); }
        }
        


        private ObservableCollection<CleanupPath> cleanup = new ObservableCollection<CleanupPath>();

        public ObservableCollection<CleanupPath> Cleanup
        {
            get { return cleanup; }
            set { cleanup = value; propChanged("Cleanup"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void propChanged(string prop)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }

    public class CleanupPath : INotifyPropertyChanged
    {
        private bool enable;

        public bool Enable
        {
            get { return enable; }
            set { enable = value; propChanged("Enable"); }
        }

        private string path;

        public string Path
        {
            get { return path; }
            set { path = value; propChanged("Path"); }
        }

        public override string ToString()
        {
            return string.Format("{0}{1}", enable ? "on\t":"off\t", path);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void propChanged(string prop)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
