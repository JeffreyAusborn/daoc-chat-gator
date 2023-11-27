using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
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
        private readonly object lockObject = new object();
        private ObservableCollection<Totals> totals { get; set; } = new ObservableCollection<Totals>();
        private ObservableCollection<Ability> spells { get; set; } = new ObservableCollection<Ability>();
        private ObservableCollection<Ability> heals { get; set; } = new ObservableCollection<Ability>();

        private ObservableCollection<Ability> weapons { get; set; } = new ObservableCollection<Ability>();

        private ObservableCollection<Armor> armor { get; set; } = new ObservableCollection<Armor>();

        private ObservableCollection<Kills> kills { get; set; } = new ObservableCollection<Kills>();

        private readonly System.Timers.Timer fileCheckTimer = new System.Timers.Timer();
        private string filePath;
        private string copiedFilePath;

        public ListView viewBase;
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

        private void SetWindowSize_Click(object sender, RoutedEventArgs e)
        {
            // Create and show the window size configuration dialog
            var sizeConfigDialog = new WindowSizeConfigDialog(Width, Height);
            if (sizeConfigDialog.ShowDialog() == true)
            {
                // Apply the new window size
                Width = sizeConfigDialog.SelectedWidth;
                Height = sizeConfigDialog.SelectedHeight;
            }
        }

        private void ConfigureColumns_Click(object sender, RoutedEventArgs e)
        {
            // Get the column headers from the GridView
            if (viewBase != null && viewBase.View != null && viewBase.View is GridView gridView)
            {
                List<string> columnHeaders = gridView.Columns.Cast<GridViewColumn>().Select(column => column.Header.ToString()).ToList();

                if (columnHeaders != null && columnHeaders.Any())
                {
                    var configWindow = new ColumnConfigWindow(columnHeaders);

                    // Create and show the configuration window
                    
                    if (configWindow.ShowDialog() == true)
                    {
                        // Apply the column visibility changes based on user preferences
                        foreach (var columnItem in configWindow.Columns)
                        {
                            var column = gridView.Columns.FirstOrDefault(c => c.Header.ToString() == columnItem.Header);
                            if (column != null)
                            {
                                // Adjust the column's Width to 0 to hide it
                                column.Width = columnItem.IsVisible ? Double.NaN : 0;
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("No columns found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("This tab isn't setup to choose columns.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                lock (lockObject)
                {
                    // Check if the file exists before attempting to delete it
                    if (filePath != null && File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        spells.Clear();
                        totals.Clear();
                        spells.Clear();
                        weapons.Clear();
                        heals.Clear();
                        armor.Clear();
                        kills.Clear();
                        lastReadPosition = 0;
                    }

                    if (copiedFilePath != null && File.Exists(copiedFilePath))
                    {
                        File.Delete(copiedFilePath);
                    }
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
        static DateTime ExtractTime(string timestamp)
        {
            // Extract the timestamp string between square brackets
            string timeString = timestamp.Substring(timestamp.IndexOf('[') + 1, 8);

            // Parse the timestamp string to DateTime
            DateTime time;
            if (DateTime.TryParseExact(timeString, "HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out time))
            {
                return time;
            }
            else
            {
                // Handle parsing error
                Console.WriteLine("Error parsing timestamp.");
                return DateTime.MinValue;
            }
        }

        private long lastReadPosition = 0;
        private void ReadAndParseFile(string filePath)
        {
            try
            {
                //using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
               // using (StreamReader streamReader = new StreamReader(fileStream))
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    //fs.Seek(lastReadPosition, SeekOrigin.Begin);
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string[] allLines = sr.ReadToEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                        spells.Clear();
                        totals.Clear();
                        spells.Clear();
                        weapons.Clear();
                        heals.Clear();
                        armor.Clear();
                        kills.Clear();

                        string spellName = null;
                        bool healingSpell = false;
                        string styleName = null;
                        string weaponName = null;
                        int styleGrowth = 0;
                        string bodyPart = null;
                        DateTime? startCastTime = null;
                        DateTime? endCastTime = null;
                        int secondsDifference = 0;

                        string startCastPattern = @"\[(\d{2}:\d{2}:\d{2})\] You begin casting a (.+?) spell!"; // 1: time 2: spellName

                        string spellPattern = @"\[(\d{2}:\d{2}:\d{2})\] You cast a (.+?) spell!"; // 1: time 2: spellName
                        string shotPattern = @"You fire a (.+?)!";
                        string damagePattern = @"You hit .+? for (\d+).+?damage!";
                        string critSpellPattern = @"You critically hit for an additional (\d+) damage!";
                        string critAttackPattern = @" You critically hit .+? for an additional (\d+) damage!";
                        string healPattern = @"You heal .+? for (\d+) hit points.";
                        string critHealPattern = @"Your heal criticals for an extra (\d+) amount of hit points!";
                        string dotDamagePattern = @"Your (.+?) attacks .+? and hits for (\d+).+?damage!"; // 1:spellName, 2:damageValue
                        string dotDamageHitPattern = @"Your (.+?) hits .+? for (\d+).+?damage!"; // 1:spellName, 2:damageValue
                        string dotCritPattern = @"Your (.+?) critically hits .+? for an additional (\d+) damage!"; // 1:spellName, 2:damageValue
                        string styleGrowthPattern = @"You perform your (.+?) perfectly!.+?(\d+),"; // 1:styleName, 2:growthValue
                        string attackPattern = @"You attack .+? with your (.+?) and hit for (\d+).+?damage!"; // 1:weaponName, 2:damage
                        string killPattern = @"You just killed (.+?)!";
                        string bodyDamagePattern = ".+? hits your (.+?) for (\\d+).+?damage!";

                        string resistPattern = @".+?resists the effect!.+?";
                        string interruptedPattern = @"(interrupt your spellcast)|(spell is interrupted)|(interrupt your focus)";
                        string overHealedPattern = @"fully healed";



                        lock (lockObject)
                        {
                            //string line;
                            foreach (string line in allLines)
                            {

                                // might work with this later on extra damage I think
                                if (line.Contains("@@"))
                                {
                                    continue;
                                }

                                Match startCastMatch = Regex.Match(line, startCastPattern);
                                Match spellMatch = Regex.Match(line, spellPattern);
                                Match shotMatch = Regex.Match(line, shotPattern);
                                Match spellDamageMatch = Regex.Match(line, damagePattern);
                                Match critSpellMatch = Regex.Match(line, critSpellPattern);
                                Match critAttackMatch = Regex.Match(line, critAttackPattern);
                                Match healMatch = Regex.Match(line, healPattern);
                                Match critHealMatch = Regex.Match(line, critHealPattern);
                                Match dotDamageMatch = Regex.Match(line, dotDamagePattern);
                                Match dotHitMatch = Regex.Match(line, dotDamageHitPattern);
                                Match dotCritMatch = Regex.Match(line, dotCritPattern);
                                Match styleGrowthMatch = Regex.Match(line, styleGrowthPattern);
                                Match attackDamageMatch = Regex.Match(line, attackPattern);
                                Match killMatch = Regex.Match(line, killPattern);
                                Match bodyDamageMatch = Regex.Match(line, bodyDamagePattern);
                                Match resistMatch = Regex.Match(line, resistPattern);
                                Match interruptedMatch = Regex.Match(line, interruptedPattern);
                                Match overHealedMatch = Regex.Match(line, overHealedPattern);

                                if (startCastMatch.Success)
                                {
                                    startCastTime = ExtractTime(startCastMatch.Groups[1].Value);
                                    //spellName = startCastMatch.Groups[2].Value;
                                }
                                else if (spellMatch.Success)
                                {
                                    endCastTime = ExtractTime(spellMatch.Groups[1].Value);
                                    if (startCastTime.HasValue && endCastTime.HasValue)
                                    {
                                        TimeSpan timeDifference = endCastTime.Value - startCastTime.Value;
                                        secondsDifference = (int)timeDifference.TotalSeconds;
                                    }


                                    spellName = spellMatch.Groups[2].Value;
                                    styleName = "";
                                    weaponName = "";
                                    bodyPart = "";

                                    Ability spell = spells.FirstOrDefault(s => s.SpellName == spellName);
                                    Ability healSpell = heals.FirstOrDefault(s => s.SpellName == spellName);
                                    if (healSpell != null)
                                    {
                                        healSpell.CastCount += 1;
                                    }
                                    if (spell != null)
                                    {
                                        spell.CastCount += 1;
                                    }

                                }
                                else if (shotMatch.Success)
                                {
                                    spellName = shotMatch.Groups[1].Value;
                                    styleName = "";
                                    weaponName = "";
                                    bodyPart = "";
                                    Ability spell = spells.FirstOrDefault(s => s.SpellName == spellName);
                                    if (spell == null)
                                    {
                                        Ability newSpell = new Ability
                                        {
                                            SpellName = spellName,
                                        };
                                        spells.Add(newSpell);
                                    }
                                }
                                else if (interruptedMatch.Success)
                                {
                                    Ability spell = spells.FirstOrDefault(s => s.SpellName == spellName);
                                    if (spell != null)
                                    {
                                        spell.Interrupted += 1;
                                    }

                                    spell = heals.FirstOrDefault(s => s.SpellName == spellName);
                                    if (spell != null)
                                    {
                                        spell.Interrupted += 1;
                                    }
                                }
                                else if (dotCritMatch.Success)
                                {
                                    string dotSpellName = dotCritMatch.Groups[1].Value;
                                    int damageValue = int.Parse(dotCritMatch.Groups[2].Value);


                                    Ability dnp = spells.FirstOrDefault(s => s.SpellName == dotSpellName);
                                    if (dnp != null)
                                    {
                                        dnp.Crit.Add(damageValue);
                                    }
                                    else
                                    {
                                        Ability newDnp = new Ability
                                        {
                                            SpellName = dotSpellName,
                                            CastCount = 1,
                                            Crit = { damageValue }
                                        };
                                        spells.Add(newDnp);
                                    }

                                    Totals tots = totals.FirstOrDefault(s => s.Type == "Damage");
                                    if (tots != null)
                                    {
                                        tots.Crit += damageValue;
                                    }
                                    else
                                    {
                                        Totals newTots = new Totals
                                        {
                                            Crit = damageValue,
                                            Type = "Damage"
                                        };
                                        totals.Add(newTots);
                                    }

                                    styleName = "";
                                }
                                else if (resistMatch.Success)
                                {
                                    if (spellName == "")
                                    {
                                        if (styleName != "")
                                        {
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
                                        else
                                        {
                                            spellName = "unknown";
                                        }
                                    }

                                    Ability dnp = spells.FirstOrDefault(s => s.SpellName == spellName);
                                    if (dnp != null)
                                    {
                                        dnp.Resists += 1;
                                    }
                                    else
                                    {
                                        Ability newDnp = new Ability
                                        {   SpellName = spellName,
                                            CastCount = 1,
                                            Resists = 1
                                        };
                                        spells.Add(newDnp);
                                    }

                                    styleName = "";
                                }

                                else if (dotDamageMatch.Success)
                                {
                                    string dotSpellName = dotDamageMatch.Groups[1].Value;
                                    int damageValue = int.Parse(dotDamageMatch.Groups[2].Value);

                                    if (damageValue > 0)
                                    {
                                        Ability dnp = spells.FirstOrDefault(s => s.SpellName == dotSpellName);
                                        if (dnp != null)
                                        {
                                            dnp.Output.Add(damageValue);
                                            if (secondsDifference > 0)
                                            {
                                                dnp.Times.Add(secondsDifference);
                                                secondsDifference = 0;
                                            }
                                        }
                                        else
                                        {
                                            Ability newDnp = new Ability
                                            {
                                                SpellName = dotSpellName,
                                                CastCount = 1,
                                                Output = { damageValue },
                                            };
                                            if (secondsDifference > 0)
                                            {
                                                newDnp.Times.Add(secondsDifference);
                                                secondsDifference = 0;
                                            }
                                            spells.Add(newDnp);
                                        }
                                    }

                                    Totals tots = totals.FirstOrDefault(s => s.Type == "Damage");
                                    if (tots != null)
                                    {
                                        tots.Output += damageValue;
                                    }
                                    else
                                    {
                                        Totals newTots = new Totals
                                        {
                                            Output = damageValue,
                                            Type = "Damage"
                                        };
                                        totals.Add(newTots);
                                    }

                                    styleName = "";
                                }
                                else if (dotHitMatch.Success)
                                {
                                    string dotSpellName = dotHitMatch.Groups[1].Value;
                                    int damageValue = int.Parse(dotHitMatch.Groups[2].Value);

                                    if (damageValue > 0)
                                    {
                                        Ability dnp = spells.FirstOrDefault(s => s.SpellName == dotSpellName);
                                        if (dnp != null)
                                        {
                                            dnp.Output.Add(damageValue);
                                            if (secondsDifference > 0)
                                            {
                                                dnp.Times.Add(secondsDifference);
                                                secondsDifference = 0;
                                            }
                                        }
                                        else
                                        {
                                            Ability newDnp = new Ability
                                            {
                                                SpellName = dotSpellName,
                                                CastCount = 1,
                                                Output = { damageValue },
                                            };
                                            if (secondsDifference > 0)
                                            {
                                                newDnp.Times.Add(secondsDifference);
                                                secondsDifference = 0;
                                            }
                                            spells.Add(newDnp);
                                        }
                                    }

                                    Totals tots = totals.FirstOrDefault(s => s.Type == "Damage");
                                    if (tots != null)
                                    {
                                        tots.Output += damageValue;
                                    }
                                    else
                                    {
                                        Totals newTots = new Totals
                                        {
                                            Output = damageValue,
                                            Type = "Damage"
                                        };
                                        totals.Add(newTots);
                                    }

                                    styleName = "";
                                }
                                else if (critAttackMatch.Success)
                                {
                                    int damageValue = int.Parse(dotHitMatch.Groups[1].Value);

                                    if (styleName == "")
                                    {
                                        styleName = "Base";
                                    }

                                    if (damageValue > 0)
                                    {
                                        Ability weap = weapons.FirstOrDefault(s => s.StyleName == styleName && s.WeaponName == weaponName);
                                        if (weap != null)
                                        {
                                            weap.Crit.Add(damageValue);
                                        }
                                        else
                                        {
                                            Ability newWeap = new Ability
                                            {
                                                StyleName = styleName,
                                                Crit = { damageValue },
                                                WeaponName = weaponName,
                                            };
                                            weapons.Add(newWeap);
                                        }
                                    }

                                    Totals tots = totals.FirstOrDefault(s => s.Type == "Damage");
                                    if (tots != null)
                                    {
                                        tots.Crit += damageValue;
                                    }
                                    else
                                    {
                                        Totals newTots = new Totals
                                        {
                                            Crit = damageValue,
                                            Type = "Damage"
                                        };
                                        totals.Add(newTots);
                                    }

                                    styleName = "";
                                }
                                else if (critHealMatch.Success)
                                {
                                    int damageValue = int.Parse(critHealMatch.Groups[1].Value);
                                    if (spellName == "")
                                    {
                                        if (styleName != "")
                                        {
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
                                    }
                                    if (damageValue > 0)
                                    {
                                        Ability spell = heals.FirstOrDefault(s => s.SpellName == spellName);
                                        if (spell != null)
                                        {
                                            spell.Crit.Add(damageValue);
                                        }
                                        else
                                        {
                                            Ability newSpell = new Ability
                                            {
                                                SpellName = spellName,
                                                CastCount = 1,
                                                Crit = { damageValue }
                                            };
                                            heals.Add(newSpell);
                                        }
                                    }

                                    Totals tots = totals.FirstOrDefault(s => s.Type == "Heal");
                                    if (tots != null)
                                    {
                                        tots.Crit += damageValue;
                                    }
                                    else
                                    {
                                        Totals newTots = new Totals
                                        {
                                            Crit = damageValue,
                                            Type = "Heal"
                                        };
                                        totals.Add(newTots);
                                    }

                                    styleName = "";
                                }
                                else if (critSpellMatch.Success)
                                {
                                    int damageValue = int.Parse(critSpellMatch.Groups[1].Value);
                                    if (spellName == "")
                                    {
                                        if (styleName != "")
                                        {
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
                                    }
                                    if (damageValue > 0)
                                    {
                                        Ability spell = spells.FirstOrDefault(s => s.SpellName == spellName);
                                        if (spell != null)
                                        {
                                            spell.Crit.Add(damageValue);
                                        }
                                        else
                                        {
                                            Ability newSpell = new Ability
                                            {
                                                SpellName = spellName,
                                                CastCount = 1,
                                                Crit = { damageValue }
                                            };

                                            spells.Add(newSpell);
                                        }

                                    }

                                    Totals tots = totals.FirstOrDefault(s => s.Type == "Damage");
                                    if (tots != null)
                                    {
                                        tots.Crit += damageValue;
                                    }
                                    else
                                    {
                                        Totals newTots = new Totals
                                        {
                                            Crit = damageValue,
                                            Type = "Damage"
                                        };
                                        totals.Add(newTots);
                                    }

                                    styleName = "";
                                }
                                else if (spellDamageMatch.Success)
                                {
                                    int damageValue = int.Parse(spellDamageMatch.Groups[1].Value);
                                    if (spellName == "")
                                    {
                                        if (styleName != "")
                                        {
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
                                    }
                                    if (damageValue > 0)
                                    {
                                        Ability spell = spells.FirstOrDefault(s => s.SpellName == spellName);
                                        if (spell != null)
                                        {
                                            spell.Output.Add(damageValue);
                                            if (secondsDifference > 0)
                                            {
                                                spell.Times.Add(secondsDifference);
                                                secondsDifference = 0;
                                            }
                                        }
                                        else
                                        {
                                            Ability newSpell = new Ability
                                            {
                                                SpellName = spellName,
                                                CastCount = 1,
                                                Output = { damageValue }
                                            };

                                            if (secondsDifference > 0)
                                            {
                                                newSpell.Times.Add(secondsDifference);
                                                secondsDifference = 0;
                                            }
                                            spells.Add(newSpell);
                                        }
                                    }

                                    Totals tots = totals.FirstOrDefault(s => s.Type == "Damage");
                                    if (tots != null)
                                    {
                                        tots.Output += damageValue;
                                    }
                                    else
                                    {
                                        Totals newTots = new Totals
                                        {
                                            Output = damageValue,
                                            Type = "Damage"
                                        };
                                        totals.Add(newTots);
                                    }

                                    styleName = "";
                                }
                                else if (healMatch.Success)
                                {
                                    int matchValue = int.Parse(healMatch.Groups[1].Value);
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
                                    Ability heal = heals.FirstOrDefault(s => s.SpellName == spellName);
                                    if (heal != null)
                                    {
                                        heal.Output.Add(matchValue);
                                        if (secondsDifference > 0)
                                        {
                                            heal.Times.Add(secondsDifference);
                                            secondsDifference = 0;
                                        }
                                    }
                                    else
                                    {
                                        Ability newHeal = new Ability
                                        {
                                            SpellName = spellName,
                                            CastCount = 1,
                                            Output = { matchValue }
                                        };
                                        if (secondsDifference > 0)
                                        {
                                            newHeal.Times.Add(secondsDifference);
                                            secondsDifference = 0;
                                        }
                                        heals.Add(newHeal);
                                    }


                                    Totals tots = totals.FirstOrDefault(s => s.Type == "Heal");
                                    if (tots != null)
                                    {
                                        tots.Output += matchValue;
                                    }
                                    else
                                    {
                                        Totals newTots = new Totals
                                        {
                                            Output = matchValue,
                                            Type = "Heal"
                                        };
                                        totals.Add(newTots);
                                    }
                                }
                                else if (killMatch.Success)
                                {
                                    string matchValue = killMatch.Groups[1].Value;
                                    Kills kill = kills.FirstOrDefault(s => s.Name == matchValue);
                                    if (kill != null)
                                    {
                                        kill.Output.Add(1);
                                    }
                                    else
                                    {
                                        Kills newKill = new Kills
                                        {
                                            Name = matchValue,
                                            Output = { 1 }
                                        };
                                        kills.Add(newKill);
                                    }
                                }
                                else if (bodyDamageMatch.Success)
                                {
                                    bodyPart = bodyDamageMatch.Groups[1].Value;
                                    int matchValue = int.Parse(bodyDamageMatch.Groups[2].Value);
                                    Armor part = armor.FirstOrDefault(s => s.Name == bodyPart);
                                    if (part != null)
                                    {
                                        part.Output.Add(matchValue);
                                    }
                                    else
                                    {
                                        Armor newPart = new Armor
                                        {
                                            Name = bodyPart,
                                            Output = { matchValue }
                                        };
                                        armor.Add(newPart);
                                    }
                                    spellName = "";
                                    styleName = "";
                                    weaponName = "";
                                }
                                else if (styleGrowthMatch.Success)
                                {
                                    styleName = styleGrowthMatch.Groups[1].Value;
                                    styleGrowth = int.Parse(styleGrowthMatch.Groups[2].Value);
                                }
                                else if (attackDamageMatch.Success)
                                {
                                    weaponName = attackDamageMatch.Groups[1].Value;
                                    int matchValue = int.Parse(attackDamageMatch.Groups[2].Value);
                                    if (styleName == "")
                                    {
                                        styleName = "Base";
                                    }

                                    Ability weap = weapons.FirstOrDefault(s => s.StyleName == styleName && s.WeaponName == weaponName);
                                    if (weap != null)
                                    {
                                        weap.Output.Add(matchValue);
                                        weap.Growths.Add(styleGrowth);
                                    }
                                    else
                                    {
                                        Ability newWeap = new Ability
                                        {
                                            StyleName = styleName,
                                            Output = { matchValue },
                                            WeaponName = weaponName,
                                            Growths = { styleGrowth }
                                        };
                                        weapons.Add(newWeap);
                                    }


                                    Totals tots = totals.FirstOrDefault(s => s.Type == "Damage");
                                    if (tots != null)
                                    {
                                        tots.Output += matchValue;
                                    }
                                    else
                                    {
                                        Totals newTots = new Totals
                                        {
                                            Output = matchValue,
                                            Type = "Damage"
                                        };
                                        totals.Add(newTots);
                                    }
                                    spellName = "";
                                    bodyPart = "";
                                    styleGrowth = 0;
                                }
                                // [15:08:20] You perform your Bash perfectly! (+35, Growth Rate: 0.87)
                                // [15:08:20] You attack the grimwood willow with your Basalt Buckler of Oblivion and hit for 125 damage! (Damage Modifier: 1869)

                            }
                        }
                    }
                    lastReadPosition = fs.Position;

                }
                lastParseTime = File.GetLastWriteTime(filePath);
            }
            catch (Exception)
            {
            }
        }

        public void PopulateTabsWithData()
        {
            TotalView.ItemsSource = totals;
            SpellView.ItemsSource = spells;
            StyleView.ItemsSource = weapons;
            HealView.ItemsSource = heals;
            ArmorView.ItemsSource = armor;
            KillView.ItemsSource = kills;
        }
        public void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabController.SelectedItem == SpellTab)
            {
                viewBase = SpellView;
            }
            if (TabController.SelectedItem == HealTab)
            {
                viewBase = HealView;
            }
            if (TabController.SelectedItem == StyleTab)
            {
                viewBase = StyleView;
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

    public class Totals
    {
        public int Output { get; set; } = new int();
        public int Crit { get; set; } = new int();
        public string Type { get; set; }
    }


    public class Ability
    {
        public string WeaponName { get; set; }
        public string StyleName { get; set; }
        public string SpellName { get; set; }

        public int Resists { get; set; }
        public int Interrupted { get; set; }
        public int Misses { get; set; }
        public int OverHealed { get; set; }

        public int CastCount { get; set; }

        public List<int> Output { get; set; } = new List<int>();

        public List<int> Times { get; set; } = new List<int>();
        public double AverageTime => Times.Count > 0 ? Math.Round(Times.Average(), 2) : 0;
        public int OutputCount => Output.Count;
        public int TotalOutput => Output.Count > 0 ? Output.Sum() : 0;
        public int MinOutput => Output.Count > 0 ? Output.Min() : 0;
        public int MaxOutput => Output.Count > 0 ? Output.Max() : 0;
        public double AverageOutput => Output.Count > 0 ? Math.Round(Output.Average(), 2) : 0;

        public List<int> Crit { get; set; } = new List<int>();
        public int CritCount => Crit.Count;
        public int TotalCrit => Crit.Count > 0 ? Crit.Sum() : 0;
        public int MinCrit => Crit.Count > 0 ? Crit.Min() : 0;
        public int MaxCrit => Crit.Count > 0 ? Crit.Max() : 0;
        public double AverageCrit => Crit.Count > 0 ? Math.Round(Crit.Average(), 2) : 0;

        public List<int> Growths { get; set; } = new List<int>();
        public int MinGrowth => Growths.Count > 0 ? Growths.Min() : 0;
        public int MaxGrowth => Growths.Count > 0 ? Growths.Max() : 0;
        public double AverageGrowth => Growths.Count > 0 ? Math.Round(Growths.Average(), 2) : 0;
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