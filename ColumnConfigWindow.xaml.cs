using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace DAoC_Chat_Gator
{

        public partial class ColumnConfigWindow : Window
        {
            // This class represents a column item in the configuration window
            public class ColumnItem
            {
                public string Header { get; set; }
                public bool IsVisible { get; set; }
            }

            public List<ColumnItem> Columns { get; set; }

            public ColumnConfigWindow(List<string> columnHeaders)
            {
                InitializeComponent();
                Columns = columnHeaders.Select(header => new ColumnItem { Header = header, IsVisible = true }).ToList();
                ColumnListBox.ItemsSource = Columns;
            }

            private void SaveButton_Click(object sender, RoutedEventArgs e)
            {
                // Save the configuration and close the window
                DialogResult = true;
            }
        }
    }