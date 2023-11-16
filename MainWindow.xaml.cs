using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace DAoC_Chat_Gator
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<DataItem> Tab1Data { get; set; } = new ObservableCollection<DataItem>();
        public ObservableCollection<DataItem> Tab2Data { get; set; } = new ObservableCollection<DataItem>();
        // Add more ObservableCollection properties for additional tabs as needed
        private readonly System.Timers.Timer fileCheckTimer = new System.Timers.Timer();
        private string filePath;
        private string copiedFilePath;
        public MainWindow()
        {
            InitializeComponent();
            OpacitySlider.ValueChanged += OpacitySlider_ValueChanged;
            fileCheckTimer.Interval = 1000; // 1 seconds
            fileCheckTimer.Elapsed += FileCheckTimer_Elapsed;
            fileCheckTimer.Start();

            // Initialize the file path (you may set it initially or through user interaction)
            filePath = string.Empty;
            copiedFilePath = string.Empty;

            PopulateTabsWithData();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Update window opacity when the slider value changes
            this.Opacity = OpacitySlider.Value;
        }
        private DateTime lastParseTime = DateTime.MinValue;
        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Open file dialog to choose a file
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Log files (*.log)|*.log";

            if (openFileDialog.ShowDialog() == true)
            {
                // Read and parse the chosen file
                filePath = openFileDialog.FileName;
                string directoryPath = Path.GetDirectoryName(filePath);
                copiedFilePath = Path.Combine(directoryPath, "chatlog_gator.log");
                CopyFile(filePath, copiedFilePath);
                ReadAndParseFile(copiedFilePath);
            }
        }

        static void CopyFile(string sourceFilePath, string destinationFilePath)
        {
            try
            {
                File.Copy(sourceFilePath, destinationFilePath, true); // Set overwrite to true to overwrite if the file already exists
            }
            catch (Exception ex)
            {
                // Handle exceptions while copying
                Console.WriteLine($"Error copying the file: {ex.Message}");
            }
        }

        private void FileCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Check if the file exists and has been modified
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                DateTime lastModified = File.GetLastWriteTime(filePath);
                if (lastModified > lastParseTime)
                {
                    // File has been modified since the last parse
                    lastParseTime = lastModified;
                    CopyFile(filePath, copiedFilePath);
                    Dispatcher.Invoke(() => ReadAndParseFile(copiedFilePath));
                }
            }
        }

        private void ReadAndParseFile(string filePath)
        {
            
            try
            {
                // Open the file with shared read access
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (StreamReader streamReader = new StreamReader(fileStream))
                {
                    string[] allLines = streamReader.ReadToEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                    // Implement your log file parsing logic here
                    Tab1Data.Clear();
                    foreach (string line in allLines)
                    {
                        // Parse data from the line and add it to the respective ObservableCollection
                        // For simplicity, let's assume the log format is CSV
                        string[] result = line.Split(new[] { "You hit " }, StringSplitOptions.None);

                        if (result.Length >= 2)
                        {
                            string userName = result[1].Split(new[] { " for" }, StringSplitOptions.None)[0];
                            string damage = result[1].Split(new[] { "for " }, StringSplitOptions.None)[1].Split(new[] { " damage" }, StringSplitOptions.None)[0];
                            Console.WriteLine(userName, damage);
                            DataItem dataItem = new DataItem { Column1 = userName, Column2 = damage };
                            Tab1Data.Add(dataItem);
                        }
                    }
                }
                lastParseTime = File.GetLastWriteTime(filePath);
            }
            catch (Exception es)
            {
                Console.WriteLine(es);
                // Handle exceptions, e.g., file not found, permission issues, etc.
            }
        }

        public void PopulateTabsWithData()
        {
            // Assign the ObservableCollection to the respective ListView
            SpellView.ItemsSource = Tab1Data;
            // Assign other ObservableCollections to their respective ListViews for additional tabs
        }

        public void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Add logic to update displayed information based on the selected tab
            if (TabController.SelectedItem == SpellTab)
            {
                // Switch to Tab 1 data
                // You can add similar conditions for other tabs
            }
        }
    }

    public class DataItem
    {
        // Define properties for your data columns
        public string? Column1 { get; set; }
        public string? Column2 { get; set; }
        // Add more properties as needed
    }


}