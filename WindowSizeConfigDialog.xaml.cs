using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace DAoC_Chat_Gator
{
    public partial class WindowSizeConfigDialog : Window
    {
        public double SelectedWidth { get; set; }
        public double SelectedHeight { get; set; }

        public WindowSizeConfigDialog(double currentWidth, double currentHeight)
        {
            InitializeComponent();
            WidthTextBox.Text = currentWidth.ToString();
            HeightTextBox.Text = currentHeight.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate and save the new window size
            if (double.TryParse(WidthTextBox.Text, out double width) && double.TryParse(HeightTextBox.Text, out double height))
            {
                SelectedWidth = width;
                SelectedHeight = height;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Invalid input. Please enter valid numeric values for width and height.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}