using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection.Emit;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;
using Path = System.IO.Path;

namespace DAoC_Chat_Gator
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Spell> spells { get; set; }  = new ObservableCollection<Spell>();
        private ObservableCollection<Heals> heals { get; set; } = new ObservableCollection<Heals>();

        private ObservableCollection<Weapon> weapons { get; set; } = new ObservableCollection<Weapon>();

        private ObservableCollection<Armor> armor { get; set; } = new ObservableCollection<Armor>();

        private ObservableCollection<Kills> kills { get; set; } = new ObservableCollection<Kills>();

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

        private void ResetLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if the file exists before attempting to delete it
                if (filePath != null && File.Exists(filePath))
                {
                    File.Delete(filePath);
                    spells.Clear();
                    weapons.Clear();
                    heals.Clear();
                    armor.Clear();
                    kills.Clear();
                }

                if (copiedFilePath != null && File.Exists(copiedFilePath))
                {
                    File.Delete(copiedFilePath);
                }
            }
            catch (Exception)
            {
                
            }
        }

        static void CopyFile(string sourceFilePath, string destinationFilePath)
        {
            try
            {
                File.Copy(sourceFilePath, destinationFilePath, true);
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
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (StreamReader streamReader = new StreamReader(fileStream))
                {
                    string[] allLines = streamReader.ReadToEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                    spells.Clear();

                    string spellName = null;
                    string styleName = null;
                    string weaponName = null;
                    int styleGrowth = 0;
                    string bodyPart = null;

                    foreach (string line in allLines)
                    {
                        if (line.Contains("@@"))
                        {
                            continue;
                        }

                            if (line.Contains("You cast a"))
                        {
                            spellName = ExtractSpellName(line);
                            styleName = "";
                            weaponName = "";
                            bodyPart = "";
                        }
                        // You heal.*for.*hit points
                        if (line.Contains("You hit "))
                        {
                            if (styleName != "") {
                                spellName = styleName;
                            } 
                            else if (bodyPart != "")
                            {
                                spellName = bodyPart;
                            }
                            else if (weaponName != "")
                            {
                                spellName = weaponName;
                            }
                            int damageValue = ExtractDamageValue(line);
                            if (damageValue > 0)
                            {
                                Spell spell = spells.FirstOrDefault(s => s.Name == spellName);
                                if (spell != null)
                                {
                                    spell.Output.Add(damageValue);
                                }
                                else
                                {
                                    Spell newSpell = new Spell
                                    {
                                        Name = spellName,
                                        Output = { damageValue }
                                    };
                                    spells.Add(newSpell);
                                }
                            }
                        } 
                        else if (line.Contains("You heal "))
                        {
                            int healValue = ExtractHealValue(line);
                            if (weaponName != "")
                            {
                                spellName = weaponName;
                            }
                            else if (bodyPart != "")
                            {
                                spellName = bodyPart;
                            }
                            else if (styleName != "")
                            {
                                spellName = styleName;
                            }
                            Heals heal = heals.FirstOrDefault(s => s.Name == spellName);
                            if (heal != null)
                            {
                                heal.Output.Add(healValue);
                            }
                            else
                            {
                                Heals newHeal = new Heals
                                {
                                    Name = spellName,
                                    Output = { healValue }
                                };
                                heals.Add(newHeal);
                            }
                        }
                        else if (line.Contains("You just killed "))
                        {
                            string killName = ExtractKillName(line);
                            Kills kill = kills.FirstOrDefault(s => s.Name == killName);
                            if (kill != null)
                            {
                                kill.Output.Add(1);
                            }
                            else
                            {
                                Kills newKill = new Kills
                                {
                                    Name = killName,
                                    Output = { 1 }
                                };
                                kills.Add(newKill);
                            }
                        }
                        else if (line.Contains("hits your "))
                        {
                            bodyPart = ExtractBodyPart(line);
                            int damageTaken = ExtractDamageTaken(line);
                            Armor part = armor.FirstOrDefault(s => s.Name == bodyPart);
                            if (part != null)
                            {
                                part.Output.Add(damageTaken);
                            }
                            else
                            {
                                Armor newPart = new Armor
                                {
                                    Name = bodyPart,
                                    Output = { damageTaken }
                                };
                                armor.Add(newPart);
                            }
                            spellName = "";
                            styleName = "";
                            weaponName = "";
                        }
                        else if (line.Contains("You perform your "))
                        {
                            styleName = ExtractStyle(line);
                            styleGrowth = ExtractGrowth(line);
                        }
                        else if (line.Contains(" You attack "))
                        {
                            weaponName = ExtractWeapon(line);
                            int attackDamage = ExtractAttackDamage(line);

                            Weapon weap = weapons.FirstOrDefault(s => s.StyleName == styleName && s.WeaponName == weaponName);
                            if (weap != null)
                            {
                                weap.Output.Add(attackDamage);
                                weap.Growths.Add(styleGrowth);
                            }
                            else
                            {
                                Weapon newWeap = new Weapon
                                {
                                    StyleName = styleName,
                                    Output = { attackDamage },
                                    WeaponName = weaponName,
                                    Growths = { styleGrowth }
                                };
                                weapons.Add(newWeap);
                            }
                            spellName = "";
                            bodyPart = "";
                        }
                        // [15:08:20] You perform your Bash perfectly! (+35, Growth Rate: 0.87)
                        // [15:08:20] You attack the grimwood willow with your Basalt Buckler of Oblivion and hit for 125 damage! (Damage Modifier: 1869)

                    }
                }
                lastParseTime = File.GetLastWriteTime(filePath);
            }
            catch (Exception)
            {
            }
        }

        private string ExtractWeapon(string line)
        {
            int startIndex = line.IndexOf(" with your ") + " with your ".Length;
            int endIndex = line.IndexOf(" and hit for ");
            return line.Substring(startIndex, endIndex - startIndex);
        }
        private int ExtractAttackDamage(string line)
        {
            int startIndex = line.IndexOf("and hit for ") + "and hit for ".Length;
            int endIndex = 0;
            if (line.Contains(" (+"))
            {
                endIndex = line.IndexOf(" (+");
            } 
            else if (line.Contains(" (-"))
            {
                endIndex = line.IndexOf(" (-");
            }
            else
            {
                endIndex = line.IndexOf(" damage!");
            }
            string damageString = line.Substring(startIndex, endIndex - startIndex);
            int damageValue;
            int.TryParse(damageString, out damageValue);
            return damageValue;
        }
        private string ExtractStyle(string line)
        {
            int startIndex = line.IndexOf("You perform your ") + "You perform your ".Length;
            int endIndex = line.IndexOf(" perfectly!");
            return line.Substring(startIndex, endIndex - startIndex);
        }
        private int ExtractGrowth(string line)
        {
            int startIndex = line.IndexOf("(+") + "(+".Length;
            int endIndex = line.IndexOf(", Growth Rate");
            string growthString = line.Substring(startIndex, endIndex - startIndex);
            int growthValue;
            int.TryParse(growthString, out growthValue);
            return growthValue;
        }
        //[14:40:08] The grimwood willow hits your arm for 124 (-50) damage! (Damage Modifier: 2060)
        private string ExtractBodyPart(string line)
        {
            int startIndex = line.IndexOf(" hits your ") + " hits your ".Length;
            int endIndex = line.IndexOf(" for ");
            return line.Substring(startIndex, endIndex - startIndex);
        }
        private int ExtractDamageTaken(string line)
        {
            int startIndex = line.IndexOf(" for ") + " for ".Length;
            int endIndex = 0;
            if (line.Contains("("))
            {
                endIndex = line.IndexOf(" (");
            }
            else
            {
                endIndex = line.IndexOf(" damage");
            }
            string damageString = line.Substring(startIndex, endIndex - startIndex);
            int damageValue;
            int.TryParse(damageString, out damageValue);
            return damageValue;
        }
        private string ExtractSpellName(string line)
        {
            int startIndex = line.IndexOf("You cast a ") + "You cast a ".Length;
            int endIndex = line.IndexOf(" spell!");
            return line.Substring(startIndex, endIndex - startIndex);
        }
        private string ExtractKillName(string line)
        {
            int startIndex = line.IndexOf("You just killed ") + "You just killed ".Length;
            int endIndex = line.IndexOf("!");
            return line.Substring(startIndex, endIndex - startIndex);
        }
        private int ExtractDamageValue(string line)
        {
            // [10:33:44] You hit Level 1 Training Dummy for 65 damage!
            // [10:33:44] You hit Level 50 Training Dummy for 23(-8) damage!
            int startIndex = line.IndexOf(" for ") + " for ".Length;
            int endIndex = 0;
            if (line.Contains("("))
            {
                endIndex = line.IndexOf("(");
            }
            else {
                endIndex = line.IndexOf(" damage");
            }
            string damageString = line.Substring(startIndex, endIndex - startIndex);

            int damageValue;
            int.TryParse(damageString, out damageValue);
            return damageValue;
        }
        private int ExtractHealValue(string line)
        {
            //[10:33:44] You hit Level 1 Training Dummy for 65 damage!
            //[10:33:44] You hit Level 50 Training Dummy for 23(-8) damage!
            int startIndex = line.IndexOf(" for ") + " for ".Length;
            int endIndex = line.IndexOf(" hit points");

            string healString = line.Substring(startIndex, endIndex - startIndex);

            int healValue;
            int.TryParse(healString, out healValue);
            return healValue;
        }
        public void PopulateTabsWithData()
        {
            SpellView.ItemsSource = spells;
            StyleView.ItemsSource = weapons;
            HealView.ItemsSource = heals;
            ArmorView.ItemsSource = armor;
            KillView.ItemsSource = kills;
        }
        public void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Tab switching logic
            if (TabController.SelectedItem == SpellTab)
            {
                // Switch to Tab 1 data
            }
        }
        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView =
              CollectionViewSource.GetDefaultView(SpellView.ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();


            ICollectionView styleData =
  CollectionViewSource.GetDefaultView(StyleView.ItemsSource);

            styleData.SortDescriptions.Clear();
            SortDescription styleSD = new SortDescription(sortBy, direction);
            styleData.SortDescriptions.Add(styleSD);
            styleData.Refresh();

            ICollectionView healData =
CollectionViewSource.GetDefaultView(HealView.ItemsSource);

            healData.SortDescriptions.Clear();
            SortDescription healSD = new SortDescription(sortBy, direction);
            healData.SortDescriptions.Add(healSD);
            healData.Refresh();

            ICollectionView armorData =
CollectionViewSource.GetDefaultView(ArmorView.ItemsSource);

            armorData.SortDescriptions.Clear();
            SortDescription armorSD = new SortDescription(sortBy, direction);
            armorData.SortDescriptions.Add(armorSD);
            armorData.Refresh();

            ICollectionView killData =
CollectionViewSource.GetDefaultView(KillView.ItemsSource);

            killData.SortDescriptions.Clear();
            SortDescription killSD = new SortDescription(sortBy, direction);
            killData.SortDescriptions.Add(killSD);
            killData.Refresh();
        }

        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        void GridViewColumnHeaderClickedHandler(object sender,
                                                RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                    var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                    Sort(sortBy, direction);

                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastHeaderClicked.Column.HeaderTemplate = null;
                    }

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

    }

    public class Spell
    {
        public string Name { get; set; }
        public List<int> Output { get; set;  } = new List<int>();

        public int TotalDamage => Output.Sum();
        public int MinDamage => Output.Min();
        public int MaxDamage => Output.Max();
        public double AverageDamage => Output.Count > 0 ? Math.Round(Output.Average(), 2) : 0;
    }

    public class Weapon
    {
        public string WeaponName { get; set; }
        public string StyleName { get; set; }
        public List<int> Output { get; set; } = new List<int>();

        public int WeaponTotalDamage => Output.Sum();
        public int WeaponMinDamage => Output.Min();
        public int WeaponMaxDamage => Output.Max();
        public double WeaponAverageDamage => Output.Count > 0 ? Math.Round(Output.Average(), 2) : 0;

        public List<int> Growths { get; set; } = new List<int>();

        public int MinGrowth => Growths.Min();
        public int MaxGrowth => Growths.Max();
        public double AverageGrowth => Growths.Count > 0 ? Math.Round(Growths.Average(), 2) : 0;
    }

    public class Heals
    {
        public string Name { get; set; }
        public List<int> Output { get; set; } = new List<int>();

        public int TotalHeal => Output.Sum();
        public int MinHeal => Output.Min();
        public int MaxHeal => Output.Max();
        public double AverageHeal => Output.Count > 0 ? Math.Round(Output.Average(), 2) : 0;
    }

    public class Armor
    {
        public string Name { get; set; }
        public List<int> Output { get; set; } = new List<int>();

        public int TotalDamage => Output.Sum();
        public int Hits => Output.Count;
        public int MinDamage => Output.Min();
        public int MaxDamage => Output.Max();
        public double AverageDamage => Output.Count > 0 ? Math.Round(Output.Average(), 2) : 0;
    }

    public class Kills
    {
        public string Name { get; set; }
        public List<int> Output { get; set; } = new List<int>();

        public int Count => Output.Sum();
    }
}