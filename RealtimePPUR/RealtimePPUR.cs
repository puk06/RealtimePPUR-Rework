﻿using DiscordRPC;
using osu.Game.IO;
using osu.Game.Rulesets.Scoring;
using OsuMemoryDataProvider;
using OsuMemoryDataProvider.OsuMemoryModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RealtimePPUR
{
    public sealed partial class RealtimePpur : Form
    {
        private const string CurrentVersion = "v1.0.7-Rework";

        private Label _currentPp, _sr, _iffc, _good, _ok, _miss, _avgoffset, _ur, _avgoffsethelp;

        private readonly PrivateFontCollection _fontCollection;
        private readonly string _ingameoverlayPriority;

        private Point _mousePoint;
        private string _displayFormat;
        private int _mode, _x, _y;
        private bool _isosumode;
        private bool _nowPlaying;
        private int _currentBackgroundImage = 1;
        private bool _isDirectoryLoaded;
        private string _osuDirectory;
        private string _songsPath;
        private string _preTitle;
        private PpCalculator _calculator;
        private bool _isplaying;
        private bool _isResultScreen;
        private double _avgOffset;
        private double _avgOffsethelp;
        private int _urValue;
        private const bool IsNoClassicMod = true;
        private int _currentBeatmapGamemode;
        private int _currentOsuGamemode;
        private int _currentGamemode;
        private int _preOsuGamemode;
        private BeatmapData _calculatedObject;
        private OsuMemoryStatus _currentStatus;
        private static DiscordRpcClient _client;
        private readonly Stopwatch _stopwatch = new();
        private HitsResult _previousHits = new();
        private string _prevErrorMessage;

        private readonly Dictionary<string, string> _configDictionary = new();
        private readonly StructuredOsuMemoryReader _sreader = new();
        private readonly OsuBaseAddresses _baseAddresses = new();
        private readonly string _customSongsFolder;
        public static readonly Dictionary<int, string> OsuMods = new()
        {
            { 0, "NM" },
            { 1, "NF" },
            { 2, "EZ" },
            { 4, "TD" },
            { 8, "HD" },
            { 16, "HR" },
            { 32, "SD" },
            { 64, "DT" },
            { 128, "RX" },
            { 256, "HT" },
            { 512, "NC" },
            { 1024, "FL" },
            { 2048, "AT" },
            { 4096, "SO" },
            { 8192, "RX2" },
            { 16384, "PF" },
            { 32768, "4K" },
            { 65536, "5K" },
            { 131072, "6K" },
            { 262144, "7K" },
            { 524288, "8K" },
            { 1048576, "FI" },
            { 2097152, "RD" },
            { 4194304, "CM" },
            { 8388608, "TP" },
            { 16777216, "9K" },
            { 33554432, "CP" },
            { 67108864, "1K" },
            { 134217728, "3K" },
            { 268435456, "2K" },
            { 536870912, "SV2" },
            { 1073741824, "MR" }
        };
        private readonly Dictionary<string, int> _osuModeValue = new()
        {
            { "left", 0 },
            { "top", 0 }
        };

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left, Top, Right, Bottom;
        }

        public RealtimePpur()
        {
            _fontCollection = new PrivateFontCollection();
            _fontCollection.AddFontFile("./src/Fonts/MPLUSRounded1c-ExtraBold.ttf");
            _fontCollection.AddFontFile("./src/Fonts/Nexa Light.otf");
            InitializeComponent();

            if (File.Exists("Error.log")) File.Delete("Error.log");

            if (!File.Exists("Config.cfg"))
            {
                MessageBox.Show("Config.cfgがフォルダ内に存在しないため、すべての項目がOffとして設定されます。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
                sRToolStripMenuItem.Checked = false;
                sSPPToolStripMenuItem.Checked = false;
                currentPPToolStripMenuItem.Checked = false;
                currentACCToolStripMenuItem.Checked = false;
                hitsToolStripMenuItem.Checked = false;
                uRToolStripMenuItem.Checked = false;
                offsetHelpToolStripMenuItem.Checked = false;
                avgOffsetToolStripMenuItem.Checked = false;
                progressToolStripMenuItem.Checked = false;
                ifFCPPToolStripMenuItem.Checked = false;
                ifFCHitsToolStripMenuItem.Checked = false;
                expectedManiaScoreToolStripMenuItem.Checked = false;
                healthPercentageToolStripMenuItem.Checked = false;
                currentPositionToolStripMenuItem.Checked = false;
                higherScoreToolStripMenuItem.Checked = false;
                highestScoreToolStripMenuItem.Checked = false;
                userScoreToolStripMenuItem.Checked = false;
                discordRichPresenceToolStripMenuItem.Checked = false;
                pPLossModeToolStripMenuItem.Checked = false;
                _ingameoverlayPriority = "1/2/3/4/5/6/7/8/9/10/11/12/13/14/15/16";
                inGameValue.Font = new Font(_fontCollection.Families[0], 19F);
                _customSongsFolder = "";
            }
            else
            {
                string[] lines = File.ReadAllLines("Config.cfg");
                foreach (string line in lines)
                {
                    string[] parts = line.Split('=');

                    if (parts.Length != 2) continue;
                    string name = parts[0].Trim();
                    string value = parts[1].Trim();
                    _configDictionary[name] = value;
                }

                var defaultmodeTest = _configDictionary.TryGetValue("DEFAULTMODE", out string defaultmodestring);
                if (defaultmodeTest)
                {
                    var defaultModeResult = int.TryParse(defaultmodestring, out int defaultmode);
                    if (!defaultModeResult || defaultmode is not (0 or 1 or 2))
                    {
                        MessageBox.Show("Config.cfgのDEFAULTMODEの値が不正であったため、初期値の0が適用されます。0、1、2のどれかを入力してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        switch (defaultmode)
                        {
                            case 1:
                                ClientSize = new Size(316, 65);
                                BackgroundImage = Properties.Resources.PP;
                                _currentBackgroundImage = 2;
                                RoundCorners();
                                _mode = 1;
                                break;

                            case 2:
                                ClientSize = new Size(316, 65);
                                BackgroundImage = Properties.Resources.UR;
                                _currentBackgroundImage = 3;
                                RoundCorners();
                                foreach (Control control in Controls)
                                {
                                    if (control.Name == "inGameValue") continue;
                                    control.Location = control.Location with { Y = control.Location.Y - 65 };
                                }
                                _mode = 2;
                                break;
                        }
                    }
                }

                sRToolStripMenuItem.Checked = _configDictionary.TryGetValue("SR", out string test) && test.ToLower() == "true";
                sSPPToolStripMenuItem.Checked = _configDictionary.TryGetValue("SSPP", out string test2) && test2.ToLower() == "true";
                currentPPToolStripMenuItem.Checked = _configDictionary.TryGetValue("CURRENTPP", out string test3) && test3.ToLower() == "true";
                currentACCToolStripMenuItem.Checked = _configDictionary.TryGetValue("CURRENTACC", out string test4) && test4.ToLower() == "true";
                hitsToolStripMenuItem.Checked = _configDictionary.TryGetValue("HITS", out string test5) && test5.ToLower() == "true";
                uRToolStripMenuItem.Checked = _configDictionary.TryGetValue("UR", out string test6) && test6.ToLower() == "true";
                offsetHelpToolStripMenuItem.Checked = _configDictionary.TryGetValue("OFFSETHELP", out string test7) && test7.ToLower() == "true";
                avgOffsetToolStripMenuItem.Checked = _configDictionary.TryGetValue("AVGOFFSET", out string test8) && test8.ToLower() == "true";
                progressToolStripMenuItem.Checked = _configDictionary.TryGetValue("PROGRESS", out string test9) && test9.ToLower() == "true";
                ifFCPPToolStripMenuItem.Checked = _configDictionary.TryGetValue("IFFCPP", out string test13) && test13.ToLower() == "true";
                ifFCHitsToolStripMenuItem.Checked = _configDictionary.TryGetValue("IFFCHITS", out string test14) && test14.ToLower() == "true";
                expectedManiaScoreToolStripMenuItem.Checked = _configDictionary.TryGetValue("EXPECTEDMANIASCORE", out string test15) && test15.ToLower() == "true";
                healthPercentageToolStripMenuItem.Checked = _configDictionary.TryGetValue("HEALTHPERCENTAGE", out string test17) && test17.ToLower() == "true";
                currentPositionToolStripMenuItem.Checked = _configDictionary.TryGetValue("CURRENTPOSITION", out string test18) && test18.ToLower() == "true";
                higherScoreToolStripMenuItem.Checked = _configDictionary.TryGetValue("HIGHERSCOREDIFF", out string test19) && test19.ToLower() == "true";
                highestScoreToolStripMenuItem.Checked = _configDictionary.TryGetValue("HIGHESTSCOREDIFF", out string test20) && test20.ToLower() == "true";
                userScoreToolStripMenuItem.Checked = _configDictionary.TryGetValue("USERSCORE", out string test21) && test21.ToLower() == "true";
                pPLossModeToolStripMenuItem.Checked = _configDictionary.TryGetValue("PPLOSSMODE", out string test22) && test22.ToLower() == "true";
                discordRichPresenceToolStripMenuItem.Checked = _configDictionary.TryGetValue("DISCORDRICHPRESENCE", out string test23) && test23.ToLower() == "true";
                _ingameoverlayPriority = _configDictionary.TryGetValue("INGAMEOVERLAYPRIORITY", out string test16) ? test16 : "1/2/3/4/5/6/7/8/9/10/11/12/13/14/15/16";
                if (_configDictionary.TryGetValue("CUSTOMSONGSFOLDER", out string test24) && test24.ToLower() != "songs")
                {
                    _customSongsFolder = test24;
                }
                else
                {
                    _customSongsFolder = "";
                }


                if (_configDictionary.TryGetValue("USECUSTOMFONT", out string test12) && test12 == "true")
                {
                    if (File.Exists("Font"))
                    {
                        var fontDictionary = new Dictionary<string, string>();
                        string[] fontInfo = File.ReadAllLines("Font");
                        foreach (string line in fontInfo)
                        {
                            string[] parts = line.Split('=');
                            if (parts.Length != 2) continue;
                            string name = parts[0].Trim();
                            string value = parts[1].Trim();
                            fontDictionary[name] = value;
                        }

                        var fontName = fontDictionary.TryGetValue("FONTNAME", out string fontNameValue);
                        var fontSize = fontDictionary.TryGetValue("FONTSIZE", out string fontSizeValue);
                        var fontStyle = fontDictionary.TryGetValue("FONTSTYLE", out string fontStyleValue);

                        if (fontDictionary.Count == 3 && fontName && fontNameValue != "" && fontSize && fontSizeValue != "" && fontStyle && fontStyleValue != "")
                        {
                            try
                            {
                                inGameValue.Font = new Font(fontNameValue, float.Parse(fontSizeValue),
                                    (FontStyle)Enum.Parse(typeof(FontStyle), fontStyleValue));
                            }
                            catch
                            {
                                MessageBox.Show("Fontファイルのフォント情報が不正であったため、デフォルトのフォントが適用されます。一度Fontファイルを削除してみることをお勧めします。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                var fontsizeResult = _configDictionary.TryGetValue("FONTSIZE", out string fontsizeValue);
                                if (!fontsizeResult)
                                {
                                    MessageBox.Show("Config.cfgにFONTSIZEの値がなかったため、初期値の19が適用されます。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    inGameValue.Font = new Font(_fontCollection.Families[0], 19F);
                                }
                                else
                                {
                                    var result = float.TryParse(fontsizeValue, out float fontsize);
                                    if (!result)
                                    {
                                        MessageBox.Show("Config.cfgのFONTSIZEの値が不正であったため、初期値の19が適用されます。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        inGameValue.Font = new Font(_fontCollection.Families[0], 19F);
                                    }
                                    else
                                    {
                                        inGameValue.Font = new Font(_fontCollection.Families[0], fontsize);
                                    }
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("Fontファイルのフォント情報が不正であったため、デフォルトのフォントが適用されます。一度Fontファイルを削除してみることをお勧めします。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            var fontsizeResult = _configDictionary.TryGetValue("FONTSIZE", out string fontsizeValue);
                            if (!fontsizeResult)
                            {
                                MessageBox.Show("Config.cfgにFONTSIZEの値がなかったため、初期値の19が適用されます。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                inGameValue.Font = new Font(_fontCollection.Families[0], 19F);
                            }
                            else
                            {
                                var result = float.TryParse(fontsizeValue, out float fontsize);
                                if (!result)
                                {
                                    MessageBox.Show("Config.cfgのFONTSIZEの値が不正であったため、初期値の19が適用されます。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    inGameValue.Font = new Font(_fontCollection.Families[0], 19F);
                                }
                                else
                                {
                                    inGameValue.Font = new Font(_fontCollection.Families[0], fontsize);
                                }
                            }
                        }
                    }
                    else
                    {
                        var fontsizeResult = _configDictionary.TryGetValue("FONTSIZE", out string fontsizeValue);
                        if (!fontsizeResult)
                        {
                            MessageBox.Show("Config.cfgにFONTSIZEの値がなかったため、初期値の19が適用されます。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            inGameValue.Font = new Font(_fontCollection.Families[0], 19F);
                        }
                        else
                        {
                            var result = float.TryParse(fontsizeValue, out float fontsize);
                            if (!result)
                            {
                                MessageBox.Show("Config.cfgのFONTSIZEの値が不正であったため、初期値の19が適用されます。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                inGameValue.Font = new Font(_fontCollection.Families[0], 19F);
                            }
                            else
                            {
                                inGameValue.Font = new Font(_fontCollection.Families[0], fontsize);
                            }
                        }
                    }
                }
                else
                {
                    var fontsizeResult = _configDictionary.TryGetValue("FONTSIZE", out string fontsizeValue);
                    if (!fontsizeResult)
                    {
                        MessageBox.Show("Config.cfgにFONTSIZEの値がなかったため、初期値の19が適用されます。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        inGameValue.Font = new Font(_fontCollection.Families[0], 19F);
                    }
                    else
                    {
                        var result = float.TryParse(fontsizeValue, out float fontsize);
                        if (!result)
                        {
                            MessageBox.Show("Config.cfgのFONTSIZEの値が不正であったため、初期値の19が適用されます。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            inGameValue.Font = new Font(_fontCollection.Families[0], 19F);
                        }
                        else
                        {
                            inGameValue.Font = new Font(_fontCollection.Families[0], fontsize);
                        }
                    }
                }
            }
        }

        private void RealtimePpur_Shown(object sender, EventArgs e)
        {
            TopMost = true;
            Thread updateMemoryThread = new(UpdateMemoryData) { IsBackground = true };
            Thread updatePpDataThread = new(UpdatePpData) { IsBackground = true };
            Thread updateDiscordRichPresenceThread = new(UpdateDiscordRichPresence) { IsBackground = true };
            updateMemoryThread.Start();
            updatePpDataThread.Start();
            updateDiscordRichPresenceThread.Start();
            UpdateLoop();
        }

        private async void UpdateLoop()
        {
            while (true)
            {
                await Task.Delay(15);
                try
                {
                    if (Process.GetProcessesByName("osu!").Length == 0) throw new Exception("osu! is not running.");
                    bool isplaying = _isplaying;
                    bool isResultScreen = _isResultScreen;
                    int currentGamemode = _currentGamemode;
                    OsuMemoryStatus status = _currentStatus;

                    HitsResult hits = new()
                    {
                        HitGeki = status switch
                        {
                            OsuMemoryStatus.Playing => _baseAddresses.Player.HitGeki,
                            OsuMemoryStatus.ResultsScreen => _baseAddresses.ResultsScreen.HitGeki,
                            _ => 0
                        },
                        Hit300 = status switch
                        {
                            OsuMemoryStatus.Playing => _baseAddresses.Player.Hit300,
                            OsuMemoryStatus.ResultsScreen => _baseAddresses.ResultsScreen.Hit300,
                            _ => 0
                        },
                        HitKatu = status switch
                        {
                            OsuMemoryStatus.Playing => _baseAddresses.Player.HitKatu,
                            OsuMemoryStatus.ResultsScreen => _baseAddresses.ResultsScreen.HitKatu,
                            _ => 0
                        },
                        Hit100 = status switch
                        {
                            OsuMemoryStatus.Playing => _baseAddresses.Player.Hit100,
                            OsuMemoryStatus.ResultsScreen => _baseAddresses.ResultsScreen.Hit100,
                            _ => 0
                        },
                        Hit50 = status switch
                        {
                            OsuMemoryStatus.Playing => _baseAddresses.Player.Hit50,
                            OsuMemoryStatus.ResultsScreen => _baseAddresses.ResultsScreen.Hit50,
                            _ => 0
                        },
                        HitMiss = status switch
                        {
                            OsuMemoryStatus.Playing => _baseAddresses.Player.HitMiss,
                            OsuMemoryStatus.ResultsScreen => _baseAddresses.ResultsScreen.HitMiss,
                            _ => 0
                        },
                        Combo = status switch
                        {
                            OsuMemoryStatus.Playing => _baseAddresses.Player.MaxCombo,
                            OsuMemoryStatus.ResultsScreen => _baseAddresses.ResultsScreen.MaxCombo,
                            _ => 0
                        },
                        Score = status switch
                        {
                            OsuMemoryStatus.Playing => _baseAddresses.Player.Score,
                            OsuMemoryStatus.ResultsScreen => _baseAddresses.ResultsScreen.Score,
                            _ => 0
                        }
                    };

                    if (isplaying)
                    {
                        hits.HitGeki = _baseAddresses.Player.HitGeki;
                        hits.Hit300 = _baseAddresses.Player.Hit300;
                        hits.HitKatu = _baseAddresses.Player.HitKatu;
                        hits.Hit100 = _baseAddresses.Player.Hit100;
                        hits.Hit50 = _baseAddresses.Player.Hit50;
                        hits.HitMiss = _baseAddresses.Player.HitMiss;
                        hits.Combo = _baseAddresses.Player.MaxCombo;
                        hits.Score = _baseAddresses.Player.Score;
                    }

                    if (_calculatedObject == null) continue;

                    var leaderBoardData = GetLeaderBoard(_baseAddresses.LeaderBoard, _baseAddresses.Player.Score);
                    double sr = IsNaNWithNum(Math.Round(_calculatedObject.CurrentDifficultyAttributes.StarRating, 2));
                    double fullSr = IsNaNWithNum(Math.Round(_calculatedObject.DifficultyAttributes.StarRating, 2));
                    double sspp = IsNaNWithNum(_calculatedObject.PerformanceAttributes.Total);
                    double currentPp = IsNaNWithNum(_calculatedObject.CurrentPerformanceAttributes.Total);
                    double ifFcpp = IsNaNWithNum(_calculatedObject.PerformanceAttributesIffc.Total);

                    int geki = hits.HitGeki;
                    int good = hits.Hit300;
                    int katu = hits.HitKatu;
                    int ok = hits.Hit100;
                    int bad = hits.Hit50;
                    int miss = hits.HitMiss;

                    double healthPercentage = IsNaNWithNum(Math.Round(_baseAddresses.Player.HP / 2, 1));
                    int userScore = hits.Score;

                    int currentPosition = leaderBoardData["currentPosition"];
                    int higherScore = leaderBoardData["higherScore"];
                    int highestScore = leaderBoardData["highestScore"];

                    _avgoffset.Text = Math.Round(_avgOffset, 2) + "ms";
                    _avgoffset.Width = TextRenderer.MeasureText(_avgoffset.Text, _avgoffset.Font).Width;

                    _ur.Text = _urValue.ToString();
                    _ur.Width = TextRenderer.MeasureText(_ur.Text, _ur.Font).Width;

                    _avgoffsethelp.Text = _avgOffsethelp.ToString();
                    _avgoffsethelp.Width = TextRenderer.MeasureText(_avgoffsethelp.Text, _avgoffsethelp.Font).Width;

                    _sr.Text = sr.ToString();
                    _sr.Width = TextRenderer.MeasureText(_sr.Text, _sr.Font).Width;

                    _iffc.Text = (isplaying || isResultScreen) && currentGamemode != 3 ? Math.Round(ifFcpp) + " / " + Math.Round(sspp) : Math.Round(sspp).ToString();
                    _iffc.Width = TextRenderer.MeasureText(_iffc.Text, _iffc.Font).Width;

                    _currentPp.Text = Math.Round(currentPp).ToString();
                    _currentPp.Width = TextRenderer.MeasureText(_currentPp.Text, _currentPp.Font).Width;
                    _currentPp.Left = ClientSize.Width - _currentPp.Width - 35;

                    switch (_currentGamemode)
                    {
                        case 0:
                            _good.Text = good.ToString();
                            _good.Width = TextRenderer.MeasureText(_good.Text, _good.Font).Width;
                            _good.Left = (ClientSize.Width - _good.Width) / 2 - 120;

                            _ok.Text = (ok + bad).ToString();
                            _ok.Width = TextRenderer.MeasureText(_ok.Text, _ok.Font).Width;
                            _ok.Left = (ClientSize.Width - _ok.Width) / 2 - 61;

                            _miss.Text = miss.ToString();
                            _miss.Width = TextRenderer.MeasureText(_miss.Text, _miss.Font).Width;
                            _miss.Left = (ClientSize.Width - _miss.Width) / 2 - 3;
                            break;

                        case 1:
                            _good.Text = good.ToString();
                            _good.Width = TextRenderer.MeasureText(_good.Text, _good.Font).Width;
                            _good.Left = (ClientSize.Width - _good.Width) / 2 - 120;

                            _ok.Text = ok.ToString();
                            _ok.Width = TextRenderer.MeasureText(_ok.Text, _ok.Font).Width;
                            _ok.Left = (ClientSize.Width - _ok.Width) / 2 - 61;

                            _miss.Text = miss.ToString();
                            _miss.Width = TextRenderer.MeasureText(_miss.Text, _miss.Font).Width;
                            _miss.Left = (ClientSize.Width - _miss.Width) / 2 - 3;
                            break;

                        case 2:
                            _good.Text = good.ToString();
                            _good.Width = TextRenderer.MeasureText(_good.Text, _good.Font).Width;
                            _good.Left = (ClientSize.Width - _good.Width) / 2 - 120;

                            _ok.Text = (ok + bad).ToString();
                            _ok.Width = TextRenderer.MeasureText(_ok.Text, _ok.Font).Width;
                            _ok.Left = (ClientSize.Width - _ok.Width) / 2 - 61;

                            _miss.Text = miss.ToString();
                            _miss.Width = TextRenderer.MeasureText(_miss.Text, _miss.Font).Width;
                            _miss.Left = (ClientSize.Width - _miss.Width) / 2 - 3;
                            break;

                        case 3:
                            _good.Text = (good + geki).ToString();
                            _good.Width = TextRenderer.MeasureText(_good.Text, _good.Font).Width;
                            _good.Left = (ClientSize.Width - _good.Width) / 2 - 120;

                            _ok.Text = (katu + ok + bad).ToString();
                            _ok.Width = TextRenderer.MeasureText(_ok.Text, _ok.Font).Width;
                            _ok.Left = (ClientSize.Width - _ok.Width) / 2 - 61;

                            _miss.Text = miss.ToString();
                            _miss.Width = TextRenderer.MeasureText(_miss.Text, _miss.Font).Width;
                            _miss.Left = (ClientSize.Width - _miss.Width) / 2 - 3;
                            break;
                    }

                    _displayFormat = "";
                    var ingameoverlayPriorityArray = _ingameoverlayPriority.Replace(" ", "").Split('/');
                    foreach (var priorityValue in ingameoverlayPriorityArray)
                    {
                        var priorityValueResult = int.TryParse(priorityValue, out int priorityValueInt);
                        if (!priorityValueResult) continue;
                        switch (priorityValueInt)
                        {
                            case 1:
                                if (sRToolStripMenuItem.Checked)
                                {
                                    if (pPLossModeToolStripMenuItem.Checked && _currentGamemode is 1 or 3)
                                    {
                                        _displayFormat += "SR: " + sr + "\n";
                                    }
                                    else
                                    {
                                        _displayFormat += "SR: " + sr + " / " + fullSr + "\n";
                                    }
                                }

                                break;

                            case 2:
                                if (sSPPToolStripMenuItem.Checked)
                                {
                                    _displayFormat += "SSPP: " + Math.Round(sspp) + "pp\n";
                                }

                                break;

                            case 3:
                                if (currentPPToolStripMenuItem.Checked)
                                {
                                    _displayFormat += ifFCPPToolStripMenuItem.Checked switch
                                    {
                                        true when currentGamemode != 3 => "PP: " + Math.Round(currentPp) + " / " + Math.Round(ifFcpp) + "pp\n",
                                        true => "PP: " + Math.Round(currentPp) + " / " + Math.Round(sspp) + "pp\n",
                                        _ => "PP: " + Math.Round(currentPp) + "pp\n"
                                    };
                                }

                                break;

                            case 4:
                                if (currentACCToolStripMenuItem.Checked)
                                {
                                    _displayFormat += "ACC: " + Math.Round(_baseAddresses.Player.Accuracy, 2) + "%\n";
                                }

                                break;

                            case 5:
                                if (hitsToolStripMenuItem.Checked)
                                {
                                    switch (currentGamemode)
                                    {
                                        case 0:
                                            _displayFormat += $"Hits: {good}/{ok}/{bad}/{miss}\n";
                                            break;

                                        case 1:
                                            _displayFormat += $"Hits: {good}/{ok}/{miss}\n";
                                            break;

                                        case 2:
                                            _displayFormat += $"Hits: {good}/{ok}/{bad}/{miss}\n";
                                            break;

                                        case 3:
                                            _displayFormat += $"Hits: {geki}/{good}/{katu}/{ok}/{bad}/{miss}\n";
                                            break;
                                    }
                                }

                                break;

                            case 6:
                                if (ifFCHitsToolStripMenuItem.Checked)
                                {
                                    int ifFcGood = _calculatedObject.IfFcHitResult[HitResult.Great];
                                    int ifFcOk = currentGamemode == 2
                                        ? _calculatedObject.IfFcHitResult[HitResult.LargeTickHit]
                                        : _calculatedObject.IfFcHitResult[HitResult.Ok];
                                    int ifFcBad = currentGamemode switch
                                    {
                                        0 => _calculatedObject.IfFcHitResult[HitResult.Meh],
                                        1 => 0,
                                        2 => _calculatedObject.IfFcHitResult[HitResult.SmallTickHit],
                                        _ => 0
                                    };
                                    const int ifFcMiss = 0;

                                    switch (currentGamemode)
                                    {
                                        case 0:
                                            _displayFormat += $"IFFCHits: {ifFcGood}/{ifFcOk}/{ifFcBad}/{ifFcMiss}\n";
                                            break;

                                        case 1:
                                            _displayFormat += $"IFFCHits: {ifFcGood}/{ifFcOk}/{ifFcMiss}\n";
                                            break;

                                        case 2:
                                            _displayFormat += $"IFFCHits: {ifFcGood}/{ifFcOk}/{ifFcBad}/{ifFcMiss}\n";
                                            break;
                                    }
                                }

                                break;

                            case 7:
                                if (uRToolStripMenuItem.Checked)
                                {
                                    _displayFormat += "UR: " + _urValue + "\n";
                                }

                                break;

                            case 8:
                                if (offsetHelpToolStripMenuItem.Checked)
                                {
                                    _displayFormat += "OffsetHelp: " + Math.Round(_avgOffsethelp) + "\n";
                                }

                                break;

                            case 9:
                                if (expectedManiaScoreToolStripMenuItem.Checked && currentGamemode == 3)
                                {
                                    _displayFormat += "ManiaScore: " + _calculatedObject.ExpectedManiaScore + "\n";
                                }

                                break;

                            case 10:
                                if (avgOffsetToolStripMenuItem.Checked)
                                {
                                    _displayFormat += "AvgOffset: " + _avgOffset + "\n";
                                }

                                break;

                            case 11:
                                if (progressToolStripMenuItem.Checked)
                                {
                                    _displayFormat += "Progress: " + Math.Round(_baseAddresses.GeneralData.AudioTime /
                                        _baseAddresses.GeneralData.TotalAudioTime * 100) + "%\n";
                                }

                                break;

                            case 12:
                                if (healthPercentageToolStripMenuItem.Checked)
                                {
                                    _displayFormat += "HP: " + healthPercentage + "%\n";
                                }

                                break;

                            case 13:
                                if (currentPositionToolStripMenuItem.Checked && currentPosition != 0)
                                {
                                    if (currentPosition > 50)
                                    {
                                        _displayFormat += "Position: >#50" + "\n";
                                    }
                                    else
                                    {
                                        _displayFormat += "Position: #" + currentPosition + "\n";
                                    }
                                }

                                break;

                            case 14:
                                if (higherScoreToolStripMenuItem.Checked && higherScore != 0)
                                {
                                    _displayFormat += "HigherDiff: " + (higherScore - userScore) + "\n";
                                }

                                break;

                            case 15:
                                switch (highestScoreToolStripMenuItem.Checked)
                                {
                                    case true when highestScore != 0 && currentPosition == 1:
                                        _displayFormat += "HighestDiff: You're Top!!" + "\n";
                                        break;

                                    case true when highestScore != 0:
                                        _displayFormat += "HighestDiff: " + (highestScore - userScore) + "\n";
                                        break;
                                }

                                break;

                            case 16:
                                if (userScoreToolStripMenuItem.Checked)
                                {
                                    _displayFormat += "Score: " + userScore + "\n";
                                }

                                break;
                        }
                    }

                    inGameValue.Text = _displayFormat;

                    if (_isosumode)
                    {
                        var processes = Process.GetProcessesByName("osu!");
                        if (processes.Length > 0)
                        {
                            Process osuProcess = processes[0];
                            IntPtr osuMainWindowHandle = osuProcess.MainWindowHandle;
                            if (GetWindowRect(osuMainWindowHandle, out Rect rect) &&
                                _baseAddresses.GeneralData.OsuStatus == OsuMemoryStatus.Playing &&
                                GetForegroundWindow() == osuMainWindowHandle && osuMainWindowHandle != IntPtr.Zero)
                            {
                                if (!_nowPlaying)
                                {
                                    _x = Location.X;
                                    _y = Location.Y;
                                    _nowPlaying = true;
                                }

                                BackgroundImage = null;
                                _currentBackgroundImage = 0;
                                inGameValue.Visible = true;
                                _avgoffsethelp.Visible = false;
                                _sr.Visible = false;
                                _iffc.Visible = false;
                                _currentPp.Visible = false;
                                _good.Visible = false;
                                _ok.Visible = false;
                                _miss.Visible = false;
                                _avgoffset.Visible = false;
                                _ur.Visible = false;
                                Region = null;
                                Size = new Size(inGameValue.Width, inGameValue.Height);
                                Location = new Point(rect.Left + _osuModeValue["left"] + 2,
                                    rect.Top + _osuModeValue["top"]);
                            }
                            else if (_nowPlaying)
                            {
                                switch (_mode)
                                {
                                    case 0:
                                        if (_currentBackgroundImage != 1)
                                        {
                                            ClientSize = new Size(316, 130);
                                            RoundCorners();
                                            BackgroundImage = Properties.Resources.PPUR;
                                            _currentBackgroundImage = 1;
                                        }

                                        break;

                                    case 1:
                                        if (_currentBackgroundImage != 2)
                                        {
                                            ClientSize = new Size(316, 65);
                                            RoundCorners();
                                            BackgroundImage = Properties.Resources.PP;
                                            _currentBackgroundImage = 2;
                                        }

                                        break;

                                    case 2:
                                        if (_currentBackgroundImage != 3)
                                        {
                                            ClientSize = new Size(316, 65);
                                            RoundCorners();
                                            BackgroundImage = Properties.Resources.UR;
                                            _currentBackgroundImage = 3;
                                        }

                                        break;
                                }

                                if (_nowPlaying)
                                {
                                    Location = new Point(_x, _y);
                                    _nowPlaying = false;
                                }

                                inGameValue.Visible = false;
                                _sr.Visible = true;
                                _iffc.Visible = true;
                                _currentPp.Visible = true;
                                _good.Visible = true;
                                _ok.Visible = true;
                                _miss.Visible = true;
                                _avgoffset.Visible = true;
                                _ur.Visible = true;
                                _avgoffsethelp.Visible = true;
                            }
                        }
                        else if (_nowPlaying)
                        {
                            switch (_mode)
                            {
                                case 0:
                                    if (_currentBackgroundImage != 1)
                                    {
                                        ClientSize = new Size(316, 130);
                                        RoundCorners();
                                        BackgroundImage = Properties.Resources.PPUR;
                                        _currentBackgroundImage = 1;
                                    }

                                    break;

                                case 1:
                                    if (_currentBackgroundImage != 2)
                                    {
                                        ClientSize = new Size(316, 65);
                                        RoundCorners();
                                        BackgroundImage = Properties.Resources.PP;
                                        _currentBackgroundImage = 2;
                                    }

                                    break;

                                case 2:
                                    if (_currentBackgroundImage != 3)
                                    {
                                        ClientSize = new Size(316, 65);
                                        RoundCorners();
                                        BackgroundImage = Properties.Resources.UR;
                                        _currentBackgroundImage = 3;
                                    }

                                    break;
                            }

                            if (_nowPlaying)
                            {
                                Location = new Point(_x, _y);
                                _nowPlaying = false;
                            }

                            inGameValue.Visible = false;
                            _sr.Visible = true;
                            _iffc.Visible = true;
                            _currentPp.Visible = true;
                            _good.Visible = true;
                            _ok.Visible = true;
                            _miss.Visible = true;
                            _avgoffset.Visible = true;
                            _ur.Visible = true;
                            _avgoffsethelp.Visible = true;
                        }
                    }
                    else if (_nowPlaying)
                    {
                        switch (_mode)
                        {
                            case 0:
                                if (_currentBackgroundImage != 1)
                                {
                                    ClientSize = new Size(316, 130);
                                    RoundCorners();
                                    BackgroundImage = Properties.Resources.PPUR;
                                    _currentBackgroundImage = 1;
                                }

                                break;

                            case 1:
                                if (_currentBackgroundImage != 2)
                                {
                                    ClientSize = new Size(316, 65);
                                    RoundCorners();
                                    BackgroundImage = Properties.Resources.PP;
                                    _currentBackgroundImage = 2;
                                }

                                break;

                            case 2:
                                if (_currentBackgroundImage != 3)
                                {
                                    ClientSize = new Size(316, 65);
                                    RoundCorners();
                                    BackgroundImage = Properties.Resources.UR;
                                    _currentBackgroundImage = 3;
                                }

                                break;
                        }

                        if (_nowPlaying)
                        {
                            Location = new Point(_x, _y);
                            _nowPlaying = false;
                        }

                        inGameValue.Visible = false;
                        _sr.Visible = true;
                        _iffc.Visible = true;
                        _currentPp.Visible = true;
                        _good.Visible = true;
                        _ok.Visible = true;
                        _miss.Visible = true;
                        _avgoffset.Visible = true;
                        _ur.Visible = true;
                        _avgoffsethelp.Visible = true;
                    }
                }
                catch (Exception e)
                {
                    ErrorLogger(e);
                    if (!_nowPlaying) inGameValue.Text = "";
                    _sr.Text = "0";
                    _iffc.Text = "0";
                    _currentPp.Text = "0";
                    _good.Text = "0";
                    _ok.Text = "0";
                    _miss.Text = "0";
                    _avgoffset.Text = "0ms";
                    _ur.Text = "0";
                    _avgoffsethelp.Text = "0";
                }
            }
        }

        private void UpdateMemoryData()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(15);
                    if (Process.GetProcessesByName("osu!").Length == 0) throw new Exception("osu! is not running.");
                    if (!_isDirectoryLoaded)
                    {
                        Process osuProcess = Process.GetProcessesByName("osu!")[0];
                        string tempOsuDirectory = Path.GetDirectoryName(osuProcess.MainModule.FileName);

                        if (string.IsNullOrEmpty(tempOsuDirectory) || !Directory.Exists(tempOsuDirectory)) throw new Exception("osu! directory not found.");

                        _osuDirectory = tempOsuDirectory;
                        _songsPath = GetSongsFolderLocation(_osuDirectory);
                        _isDirectoryLoaded = true;
                    }

                    if (!_isDirectoryLoaded) throw new Exception("osu! directory not found.");

                    if (!_sreader.CanRead) throw new Exception("Memory reader is not initialized.");

                    _sreader.TryRead(_baseAddresses.Beatmap);
                    _sreader.TryRead(_baseAddresses.Player);
                    _sreader.TryRead(_baseAddresses.GeneralData);
                    _sreader.TryRead(_baseAddresses.LeaderBoard);
                    _sreader.TryRead(_baseAddresses.ResultsScreen);
                    _sreader.TryRead(_baseAddresses.BanchoUser);

                    _currentStatus = _baseAddresses.GeneralData.OsuStatus;

                    if (_currentStatus == OsuMemoryStatus.Playing) _isplaying = true;
                    else if (_currentStatus != OsuMemoryStatus.ResultsScreen) _isplaying = false;

                    if (_currentStatus == OsuMemoryStatus.Playing && !_baseAddresses.Player.IsReplay) _stopwatch.Start();
                    else _stopwatch.Reset();
                    _isResultScreen = _currentStatus == OsuMemoryStatus.ResultsScreen;
                    _currentOsuGamemode = _currentStatus switch
                    {
                        OsuMemoryStatus.Playing => _baseAddresses.Player.Mode,
                        OsuMemoryStatus.ResultsScreen => _baseAddresses.ResultsScreen.Mode,
                        _ => _baseAddresses.GeneralData.GameMode
                    };
                }
                catch (Exception e)
                {
                    ErrorLogger(e);
                }
            }
        }

        private async void UpdatePpData()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(15);
                    if (Process.GetProcessesByName("osu!").Length == 0) throw new Exception("osu! is not running.");
                    bool isplaying = _isplaying;
                    bool isResultScreen = _isResultScreen;
                    string currentMapString = _baseAddresses.Beatmap.MapString;
                    string currentOsuFileName = _baseAddresses.Beatmap.OsuFileName;
                    OsuMemoryStatus status = _currentStatus;

                    if (status == OsuMemoryStatus.Playing)
                    {
                        double currentUr = _baseAddresses.Player.HitErrors == null || _baseAddresses.Player.HitErrors.Count == 0 ? 0 : CalculateUnstableRate(_baseAddresses.Player.HitErrors);
                        double currentAvgOffset = CalculateAverage(_baseAddresses.Player.HitErrors);
                        if (!double.IsNaN(currentUr)) _urValue = (int)Math.Round(currentUr);
                        if (!double.IsNaN(currentAvgOffset)) _avgOffset = _baseAddresses.Player.HitErrors == null || _baseAddresses.Player.HitErrors.Count == 0 ? 0 : -Math.Round(currentAvgOffset, 2);
                        _avgOffsethelp = (int)Math.Round(-_avgOffset);
                    }

                    if (_preTitle != currentMapString)
                    {
                        string osuBeatmapPath = Path.Combine(_songsPath ?? "", _baseAddresses.Beatmap.FolderName ?? "",
                            currentOsuFileName ?? "");
                        if (!File.Exists(osuBeatmapPath)) throw new Exception("Beatmap file not found.");

                        int currentBeatmapGamemodeTemp = await GetMapMode(osuBeatmapPath);
                        if (currentBeatmapGamemodeTemp is -1 or not (0 or 1 or 2 or 3)) throw new Exception("Invalid gamemode.");

                        _currentBeatmapGamemode = currentBeatmapGamemodeTemp;
                        _currentGamemode = _currentBeatmapGamemode == 0 ? _currentOsuGamemode : _currentBeatmapGamemode;

                        if (_calculator == null)
                        {
                            _calculator = new PpCalculator(osuBeatmapPath, _currentGamemode);
                        }
                        else
                        {
                            _calculator.SetMap(osuBeatmapPath, _currentGamemode);
                        }

                        _preTitle = currentMapString;
                    }

                    if (_currentOsuGamemode != _preOsuGamemode)
                    {
                        if (_calculator == null) continue;
                        if (_currentBeatmapGamemode == 0 && _currentOsuGamemode is 0 or 1 or 2 or 3)
                        {
                            _calculator.SetMode(_currentOsuGamemode);
                            _currentGamemode = _currentOsuGamemode;
                        }

                        _preOsuGamemode = _currentOsuGamemode;
                    }

                    if (status == OsuMemoryStatus.EditingMap) _currentGamemode = _currentBeatmapGamemode;

                    HitsResult hits = new()
                    {
                        HitGeki = status switch
                        {
                            OsuMemoryStatus.Playing => _baseAddresses.Player.HitGeki,
                            OsuMemoryStatus.ResultsScreen => _baseAddresses.ResultsScreen.HitGeki,
                            _ => 0
                        },
                        Hit300 = status switch
                        {
                            OsuMemoryStatus.Playing => _baseAddresses.Player.Hit300,
                            OsuMemoryStatus.ResultsScreen => _baseAddresses.ResultsScreen.Hit300,
                            _ => 0
                        },
                        HitKatu = status switch
                        {
                            OsuMemoryStatus.Playing => _baseAddresses.Player.HitKatu,
                            OsuMemoryStatus.ResultsScreen => _baseAddresses.ResultsScreen.HitKatu,
                            _ => 0
                        },
                        Hit100 = status switch
                        {
                            OsuMemoryStatus.Playing => _baseAddresses.Player.Hit100,
                            OsuMemoryStatus.ResultsScreen => _baseAddresses.ResultsScreen.Hit100,
                            _ => 0
                        },
                        Hit50 = status switch
                        {
                            OsuMemoryStatus.Playing => _baseAddresses.Player.Hit50,
                            OsuMemoryStatus.ResultsScreen => _baseAddresses.ResultsScreen.Hit50,
                            _ => 0
                        },
                        HitMiss = status switch
                        {
                            OsuMemoryStatus.Playing => _baseAddresses.Player.HitMiss,
                            OsuMemoryStatus.ResultsScreen => _baseAddresses.ResultsScreen.HitMiss,
                            _ => 0
                        },
                        Combo = status switch
                        {
                            OsuMemoryStatus.Playing => _baseAddresses.Player.MaxCombo,
                            OsuMemoryStatus.ResultsScreen => _baseAddresses.ResultsScreen.MaxCombo,
                            _ => 0
                        },
                        Score = status switch
                        {
                            OsuMemoryStatus.Playing => _baseAddresses.Player.Score,
                            OsuMemoryStatus.ResultsScreen => _baseAddresses.ResultsScreen.Score,
                            _ => 0
                        }
                    };

                    if (isplaying)
                    {
                        hits.HitGeki = _baseAddresses.Player.HitGeki;
                        hits.Hit300 = _baseAddresses.Player.Hit300;
                        hits.HitKatu = _baseAddresses.Player.HitKatu;
                        hits.Hit100 = _baseAddresses.Player.Hit100;
                        hits.Hit50 = _baseAddresses.Player.Hit50;
                        hits.HitMiss = _baseAddresses.Player.HitMiss;
                        hits.Combo = _baseAddresses.Player.MaxCombo;
                        hits.Score = _baseAddresses.Player.Score;
                    }

                    if (hits.Equals(_previousHits) && status is OsuMemoryStatus.Playing && !hits.IsEmpty()) continue;
                    if (status is OsuMemoryStatus.Playing) _previousHits = hits.Clone();

                    string[] mods = status switch
                    {
                        OsuMemoryStatus.Playing => ParseMods(_baseAddresses.Player.Mods.Value).Calculate,
                        OsuMemoryStatus.ResultsScreen => ParseMods(_baseAddresses.ResultsScreen.Mods.Value).Calculate,
                        OsuMemoryStatus.MainMenu => ParseMods(_baseAddresses.GeneralData.Mods).Calculate,
                        _ => ParseMods(_baseAddresses.GeneralData.Mods).Calculate
                    };

                    if (isplaying) mods = ParseMods(_baseAddresses.Player.Mods.Value).Calculate;

                    double acc = CalculateAcc(hits, _currentGamemode);

                    var calcArgs = new CalculateArgs
                    {
                        Accuracy = acc,
                        Combo = hits.Combo,
                        Score = hits.Score,
                        NoClassicMod = IsNoClassicMod,
                        Mods = mods,
                        Time = _baseAddresses.GeneralData.AudioTime,
                        PplossMode = pPLossModeToolStripMenuItem.Checked
                    };
                    var result = _calculator?.Calculate(calcArgs, isplaying,
                        isResultScreen && !isplaying, hits);
                    if (result?.DifficultyAttributes == null || result.PerformanceAttributes == null ||
                        result.CurrentDifficultyAttributes == null ||
                        result.CurrentPerformanceAttributes == null) continue;
                    _calculatedObject = result;
                }
                catch (Exception e)
                {
                    ErrorLogger(e);
                }
            }
        }

        private void UpdateDiscordRichPresence()
        {
            bool isConnectedToDiscord = false;
            bool configDialog = false;

            while (!isConnectedToDiscord)
            {
                try
                {
                    _client = new DiscordRpcClient("1237279508239749211");
                    _client.Initialize();
                    isConnectedToDiscord = true;
                }
                catch (Exception e)
                {
                    Thread.Sleep(5000);
                    ErrorLogger(e);
                }
            }

            while (true)
            {
                try
                {
                    Thread.Sleep(2000);

                    if (Process.GetProcessesByName("osu!").Length == 0)
                    {
                        _client.ClearPresence();
                        continue;
                    }

                    if (!string.IsNullOrEmpty(_osuDirectory) && !configDialog && discordRichPresenceToolStripMenuItem.Checked)
                    {
                        try
                        {
                            bool configChecked = CheckConfigValue(_osuDirectory, "DiscordRichPresence", "1");
                            if (configChecked)
                            {
                                MessageBox.Show(
                                    "osu!の設定で、DiscordRichPresenceがオンになっています。\nこれにより、RealtimePPURのRichPresenceが上書きされる可能性があります。osu!の設定で無効化することができます。",
                                    "RealtimePPUR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }

                            configDialog = true;
                        }
                        catch (Exception e)
                        {
                            ErrorLogger(e);
                        }
                    }

                    if (!discordRichPresenceToolStripMenuItem.Checked)
                    {
                        _client.ClearPresence();
                        continue;
                    }

                    HitsResult hits = new()
                    {
                        HitGeki = _baseAddresses.Player.HitGeki,
                        Hit300 = _baseAddresses.Player.Hit300,
                        HitKatu = _baseAddresses.Player.HitKatu,
                        Hit100 = _baseAddresses.Player.Hit100,
                        Hit50 = _baseAddresses.Player.Hit50,
                        HitMiss = _baseAddresses.Player.HitMiss,
                        Combo = _baseAddresses.Player.MaxCombo,
                        Score = _baseAddresses.Player.Score
                    };

                    switch (_baseAddresses.GeneralData.OsuStatus)
                    {
                        case OsuMemoryStatus.Playing when !_baseAddresses.Player.IsReplay:
                            _client.SetPresence(new RichPresence
                            {
                                Details = RichPresenceStringChecker(_baseAddresses.BanchoUser.Username + ConvertStatus(_baseAddresses.GeneralData.OsuStatus)),
                                State = RichPresenceStringChecker(_baseAddresses.Beatmap.MapString),
                                Timestamps = new Timestamps()
                                {
                                    Start = DateTime.UtcNow - _stopwatch.Elapsed
                                },
                                Assets = new Assets()
                                {
                                    LargeImageKey = "osu_icon",
                                    LargeImageText =
                                        $"RealtimePPUR ({CurrentVersion})",
                                    SmallImageKey = "osu_playing",
                                    SmallImageText =
                                        $"{Math.Round(IsNaNWithNum(_calculatedObject.CurrentPerformanceAttributes.Total), 2)}pp  +{string.Join("", ParseMods(_baseAddresses.Player.Mods.Value).Show)}  {_baseAddresses.Player.Combo}x  {ConvertHits(_baseAddresses.Player.Mode, hits)}"
                                }
                            });

                            break;

                        case OsuMemoryStatus.Playing when
                            _baseAddresses.Player.IsReplay:
                            _client.SetPresence(new RichPresence
                            {
                                Details = RichPresenceStringChecker($"{_baseAddresses.BanchoUser.Username} is Watching {_baseAddresses.Player.Username}'s play"),
                                State = RichPresenceStringChecker(_baseAddresses.Beatmap.MapString),
                                Assets = new Assets()
                                {
                                    LargeImageKey = "osu_icon",
                                    LargeImageText =
                                        $"RealtimePPUR ({CurrentVersion})",
                                    SmallImageKey = "osu_playing",
                                    SmallImageText =
                                        $"{Math.Round(IsNaNWithNum(_calculatedObject.CurrentPerformanceAttributes.Total), 2)}pp  +{string.Join("", ParseMods(_baseAddresses.Player.Mods.Value).Show)}  {_baseAddresses.Player.Combo}x  {ConvertHits(_baseAddresses.Player.Mode, hits)}"
                                }
                            });

                            break;

                        default:
                            _client.SetPresence(new RichPresence
                            {
                                Details = RichPresenceStringChecker(_baseAddresses.BanchoUser.Username + ConvertStatus(_baseAddresses.GeneralData.OsuStatus)),
                                State = RichPresenceStringChecker(_baseAddresses.Beatmap.MapString),
                                Assets = new Assets()
                                {
                                    LargeImageKey = "osu_icon",
                                    LargeImageText =
                                        $"RealtimePPUR ({CurrentVersion})"
                                }
                            });

                            break;
                    }
                }
                catch (Exception e)
                {
                    ErrorLogger(e);
                }
            }
        }

        private static string RichPresenceStringChecker(string value)
        {
            if (value == null) return "Unknown";
            if (value.Length > 128) value = value[..128];
            return value;
        }

        private static string ConvertStatus(OsuMemoryStatus status)
        {
            return status switch
            {
                OsuMemoryStatus.EditingMap => " is Editing Map",
                OsuMemoryStatus.GameShutdownAnimation => " is Shutting Down osu!",
                OsuMemoryStatus.GameStartupAnimation => " is Starting Up osu!",
                OsuMemoryStatus.MainMenu => " is in Main Menu",
                OsuMemoryStatus.MultiplayerRoom => " is in Multiplayer Room",
                OsuMemoryStatus.MultiplayerResultsscreen => " is in Multiplayer Results",
                OsuMemoryStatus.MultiplayerSongSelect => " is in Multiplayer Song Select",
                OsuMemoryStatus.NotRunning => " is Not Running osu!",
                OsuMemoryStatus.OsuDirect => " is Searching Maps",
                OsuMemoryStatus.Playing => " is Playing Map",
                OsuMemoryStatus.ResultsScreen => " in Results",
                OsuMemoryStatus.SongSelect => " is Selecting Songs",
                OsuMemoryStatus.Unknown => " is Unknown",
                _ => " is Unknown"
            };
        }

        private static string ConvertHits(int mode, HitsResult hits)
        {
            return mode switch
            {
                0 => $"[{hits.Hit300}/{hits.Hit100}/{hits.Hit50}/{hits.HitMiss}]",
                1 => $"[{hits.Hit300}/{hits.Hit100}/{hits.HitMiss}]",
                2 => $"[{hits.Hit300}/{hits.Hit100}/{hits.Hit50}/{hits.HitMiss}]",
                3 => $"[{hits.HitGeki}/{hits.Hit300}/{hits.HitKatu}/{hits.Hit100}/{hits.Hit50}/{hits.HitMiss}]",
                _ => $"[{hits.Hit300}/{hits.Hit100}/{hits.Hit50}/{hits.HitMiss}]"
            };
        }

        private static Mods ParseMods(int mods)
        {
            List<string> activeModsCalc = new();
            List<string> activeModsShow = new();

            for (int i = 0; i < 32; i++)
            {
                int bit = 1 << i;
                if ((mods & bit) != bit) continue;
                activeModsCalc.Add(OsuMods[bit].ToLower());
                activeModsShow.Add(OsuMods[bit]);
            }

            if (activeModsCalc.Contains("nc") && activeModsCalc.Contains("dt")) activeModsCalc.Remove("nc");
            if (activeModsShow.Contains("NC") && activeModsShow.Contains("DT")) activeModsShow.Remove("DT");
            if (activeModsShow.Count == 0) activeModsShow.Add("NM");

            return new Mods()
            {
                Calculate = activeModsCalc.ToArray(),
                Show = activeModsShow.ToArray()
            };
        }

        private static double CalculateAcc(HitsResult hits, int mode)
        {
            return mode switch
            {
                0 => (double)(100 * (6 * hits.Hit300 + 2 * hits.Hit100 + hits.Hit50)) /
                     (6 * (hits.Hit50 + hits.Hit100 + hits.Hit300 + hits.HitMiss)),
                1 => (double)(100 * (2 * hits.Hit300 + hits.Hit100)) / (2 * (hits.Hit300 + hits.Hit100 + hits.HitMiss)),
                2 => (double)(100 * (hits.Hit300 + hits.Hit100 + hits.Hit50)) /
                     (hits.Hit300 + hits.Hit100 + hits.Hit50 + hits.HitKatu + hits.HitMiss),
                3 => (double)(100 * (6 * hits.HitGeki + 6 * hits.Hit300 + 4 * hits.HitKatu + 2 * hits.Hit100 + hits.Hit50)) /
                     (6 * (hits.Hit50 + hits.Hit100 + hits.Hit300 + hits.HitMiss + hits.HitGeki + hits.HitKatu)),
                _ => throw new ArgumentException("Invalid mode provided.")
            };
        }

        private static double IsNaNWithNum(double number) => double.IsNaN(number) ? 0 : number;

        public static Task<int> GetMapMode(string file)
        {
            using var stream = File.OpenRead(file);
            using var reader = new LineBufferedReader(stream);
            int count = 0;
            while (reader.ReadLine() is { } line)
            {
                if (count > 20) return Task.FromResult(0);
                if (line.StartsWith("Mode")) return Task.FromResult(int.Parse(line.Split(':')[1].Trim()));
                count++;
            }

            return Task.FromResult(-1);
        }

        private static double CalculateAverage(IReadOnlyCollection<int> array)
        {
            if (array == null || array.Count == 0) return 0;
            var sortedArray = array.OrderBy(x => x).ToArray();
            int count = sortedArray.Length;
            double q1 = sortedArray[(int)(count * 0.25)];
            double q3 = sortedArray[(int)(count * 0.75)];
            double iqr = q3 - q1;
            var filteredArray = sortedArray.Where(x => x >= q1 - 1.5 * iqr && x <= q3 + 1.5 * iqr);
            return filteredArray.Average();
        }

        private static double GetPercentile(IReadOnlyList<int> sortedData, double percentile)
        {
            int N = sortedData.Count;
            double n = (N - 1) * percentile + 1;
            if (n == 1d) return sortedData[0];
            if (n == N) return sortedData[N - 1];
            int k = (int)n;
            double d = n - k;
            return sortedData[k - 1] + d * (sortedData[k] - sortedData[k - 1]);
        }

        private static Dictionary<string, int> GetLeaderBoard(OsuMemoryDataProvider.OsuMemoryModels.Direct.LeaderBoard leaderBoard, int score)
        {
            var currentPositionArray = leaderBoard.Players.ToArray();
            var currentPosition = currentPositionArray.Length + 1;
            if (currentPosition == 1 || !leaderBoard.HasLeaderBoard)
            {
                return new Dictionary<string, int>
                {
                    { "currentPosition", 0 },
                    { "higherScore", 0 },
                    { "highestScore", 0 }
                };
            }

            foreach (var _ in leaderBoard.Players.Where(player => player.Score <= score)) currentPosition--;
            int higherScore = currentPosition - 2 <= 0
                ? leaderBoard.Players[0].Score
                : leaderBoard.Players[currentPosition - 2].Score;
            int highestScore = leaderBoard.Players[0].Score;
            return new Dictionary<string, int>
            {
                { "currentPosition", currentPosition },
                { "higherScore", higherScore },
                { "highestScore", highestScore }
            };
        }

        private string GetSongsFolderLocation(string osuDirectory)
        {
            string userName = Environment.UserName;
            string file = Path.Combine(osuDirectory, $"osu!.{userName}.cfg");
            if (!File.Exists(file))
            {
                MessageBox.Show("osu!.Username.cfgが見つからなかったため、Songsフォルダを自動検出できませんでした。\nConfigファイルのSongsFolderを参照します(もし設定されてなかったらデフォルトのSongsフォルダが参照されます。)。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.IsNullOrEmpty(_customSongsFolder) ? Path.Combine(osuDirectory, "Songs") : _customSongsFolder;
            }

            foreach (string readLine in File.ReadLines(file))
            {
                if (!readLine.StartsWith("BeatmapDirectory")) continue;
                string path = readLine.Split('=')[1].Trim(' ');
                return path == "Songs" ? Path.Combine(osuDirectory, "Songs") : path;
            }

            MessageBox.Show("BeatmapDirectoryが見つからなかったため、Songsフォルダを自動検出できませんでした。\nConfigファイルのSongsFolderを参照します(もし設定されてなかったらデフォルトのSongsフォルダが参照されます。)。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return string.IsNullOrEmpty(_customSongsFolder) ? Path.Combine(osuDirectory, "Songs") : _customSongsFolder;
        }

        private static bool CheckConfigValue(string osuDirectory, string parameter, string value)
        {
            string userName = Environment.UserName;
            string file = Path.Combine(osuDirectory, $"osu!.{userName}.cfg");
            if (!File.Exists(file)) throw new Exception("Configuration file not found.");
            foreach (string readLine in File.ReadLines(file))
            {
                if (!readLine.StartsWith(parameter)) continue;
                string configValue = readLine.Split('=')[1].Trim(' ');
                return configValue == value;
            }
            throw new Exception("Parameter not found.");
        }

        private static double CalculateUnstableRate(IReadOnlyCollection<int> hitErrors)
        {
            if (hitErrors == null || hitErrors.Count == 0) return 0;
            double totalAll = hitErrors.Sum(hit => (long)hit);
            double average = totalAll / hitErrors.Count;
            double variance = hitErrors.Sum(hit => Math.Pow(hit - average, 2)) / hitErrors.Count;
            double unstableRate = Math.Sqrt(variance) * 10;
            return unstableRate > 10000 ? double.NaN : unstableRate;
        }

        private void ErrorLogger(Exception error)
        {
            try
            {
                if (error.Message == _prevErrorMessage) return;
                _prevErrorMessage = error.Message;
                const string filePath = "Error.log";
                StreamWriter sw = File.Exists(filePath) ? File.AppendText(filePath) : File.CreateText(filePath);
                sw.WriteLine("[" + DateTime.Now + "]");
                sw.WriteLine(error);
                sw.WriteLine();
                sw.Close();
            }
            catch
            {
                Console.WriteLine("エラーログの書き込みに失敗しました");
            }
        }

        private void RoundCorners()
        {
            const int radius = 11;
            const int diameter = radius * 2;
            GraphicsPath gp = new GraphicsPath();
            gp.AddPie(0, 0, diameter, diameter, 180, 90);
            gp.AddPie(Width - diameter, 0, diameter, diameter, 270, 90);
            gp.AddPie(0, Height - diameter, diameter, diameter, 90, 90);
            gp.AddPie(Width - diameter, Height - diameter, diameter, diameter, 0, 90);
            gp.AddRectangle(new Rectangle(radius, 0, Width - diameter, Height));
            gp.AddRectangle(new Rectangle(0, radius, radius, Height - diameter));
            gp.AddRectangle(new Rectangle(Width - radius, radius, radius, Height - diameter));
            Region = new Region(gp);
        }

        private void realtimePPURToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_mode == 0) return;
            ClientSize = new Size(316, 130);
            BackgroundImage = Properties.Resources.PPUR;
            _currentBackgroundImage = 1;
            RoundCorners();
            if (_mode == 2)
            {
                foreach (Control control in Controls)
                {
                    if (control.Name == "inGameValue") continue;
                    control.Location = control.Location with { Y = control.Location.Y + 65 };
                }
            }
            _mode = 0;
        }

        private void realtimePPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_mode == 1) return;
            ClientSize = new Size(316, 65);
            BackgroundImage = Properties.Resources.PP;
            _currentBackgroundImage = 2;
            RoundCorners();
            if (_mode == 2)
            {
                foreach (Control control in Controls)
                {
                    if (control.Name == "inGameValue") continue;
                    control.Location = control.Location with { Y = control.Location.Y + 65 };
                }
            }
            _mode = 1;
        }

        private void offsetHelperToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_mode == 2) return;
            ClientSize = new Size(316, 65);
            BackgroundImage = Properties.Resources.UR;
            _currentBackgroundImage = 3;
            RoundCorners();
            if (_mode is 0 or 1)
            {
                foreach (Control control in Controls)
                {
                    if (control.Name == "inGameValue") continue;
                    control.Location = control.Location with { Y = control.Location.Y - 65 };
                }
            }
            _mode = 2;
        }

        private void changeFontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FontDialog font = new FontDialog();
            try
            {
                if (font.ShowDialog() == DialogResult.Cancel) return;

                inGameValue.Font = font.Font;
                DialogResult fontfDialogResult = MessageBox.Show("このフォントを保存しますか？", "情報", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (fontfDialogResult == DialogResult.No) return;

                try
                {
                    if (File.Exists("Font"))
                    {
                        const string filePath = "Font";
                        StreamWriter sw = new(filePath, false);
                        string fontInfo =
                            $"※絶対にこのファイルを自分で編集しないでください！\n※フォント名などを編集してしまうとフォントが見つからず、Windows標準のフォントが割り当てられてしまいます。\n※もし編集してしまった場合はこのファイルを削除することをお勧めします。\nFONTNAME={font.Font.Name}\nFONTSIZE={font.Font.Size}\nFONTSTYLE={font.Font.Style}";
                        sw.WriteLine(fontInfo);
                        sw.Close();
                        MessageBox.Show(
                            "フォントの保存に成功しました。Config.cfgのUSECUSTOMFONTをtrueにすることで起動時から保存されたフォントを使用できます。右クリック→Load Fontからでも読み込むことが可能です！",
                            "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        FileStream fs = File.Create("Font");
                        string fontInfo =
                            $"※絶対にこのファイルを自分で編集しないでください！\n※フォント名などを編集してしまうとフォントが見つからず、Windows標準のフォントが割り当てられてしまいます。\n※もし編集してしまった場合はこのファイルを削除することをお勧めします。\nFONTNAME={font.Font.Name}\nFONTSIZE={font.Font.Size}\nFONTSTYLE={font.Font.Style}";
                        byte[] fontInfoByte = System.Text.Encoding.UTF8.GetBytes(fontInfo);
                        fs.Write(fontInfoByte, 0, fontInfoByte.Length);
                        fs.Close();
                        MessageBox.Show(
                            "フォントの保存に成功しました。Config.cfgのUSECUSTOMFONTをtrueにすることで起動時から保存されたフォントを使用できます。右クリック→Load Fontからでも読み込むことが可能です！",
                            "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch
                {
                    MessageBox.Show("フォントの保存に失敗しました。もしFontファイルが作成されていたら削除することをお勧めします。", "エラー", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch
            {
                MessageBox.Show("フォントの変更に失敗しました。対応していないフォントです。", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void loadFontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (File.Exists("Font"))
            {
                var fontDictionaryLoad = new Dictionary<string, string>();
                string[] fontInfo = File.ReadAllLines("Font");
                foreach (string line in fontInfo)
                {
                    string[] parts = line.Split('=');
                    if (parts.Length != 2) continue;
                    string name = parts[0].Trim();
                    string value = parts[1].Trim();
                    fontDictionaryLoad[name] = value;
                }

                var fontName = fontDictionaryLoad.TryGetValue("FONTNAME", out string fontNameValue);
                var fontSize = fontDictionaryLoad.TryGetValue("FONTSIZE", out string fontSizeValue);
                var fontStyle = fontDictionaryLoad.TryGetValue("FONTSTYLE", out string fontStyleValue);

                if (fontDictionaryLoad.Count == 3 && fontName && fontNameValue != "" && fontSize && fontSizeValue != "" && fontStyle && fontStyleValue != "")
                {
                    try
                    {
                        inGameValue.Font = new Font(fontNameValue, float.Parse(fontSizeValue), (FontStyle)Enum.Parse(typeof(FontStyle), fontStyleValue));
                        MessageBox.Show($"フォントの読み込みに成功しました。\n\nフォント名: {fontNameValue}\nサイズ: {fontSizeValue}\nスタイル: {fontStyleValue}", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch
                    {
                        MessageBox.Show("Fontファイルのフォント情報が不正、もしくは非対応であったため読み込まれませんでした。一度Fontファイルを削除してみることをお勧めします。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Fontファイルのフォント情報が不正であったため、読み込まれませんでした。一度Fontファイルを削除してみることをお勧めします。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Fontファイルが存在しません。一度Change Fontでフォントを保存してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void resetFontToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            var fontsizeResult = _configDictionary.TryGetValue("FONTSIZE", out string fontsizeValue);
            if (!fontsizeResult)
            {
                MessageBox.Show("Config.cfgにFONTSIZEの値がなかったため、初期値の19が適用されます。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                inGameValue.Font = new Font(_fontCollection.Families[0], 19F);
                MessageBox.Show("フォントのリセットが完了しました！", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var result = float.TryParse(fontsizeValue, out float fontsize);
                if (!result)
                {
                    MessageBox.Show("Config.cfgのFONTSIZEの値が不正であったため、初期値の19が適用されます。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    inGameValue.Font = new Font(_fontCollection.Families[0], 19F);
                    MessageBox.Show("フォントのリセットが完了しました！", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    inGameValue.Font = new Font(_fontCollection.Families[0], fontsize);
                    MessageBox.Show("フォントのリセットが完了しました！", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void RealtimePPUR_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left) _mousePoint = new Point(e.X, e.Y);
        }

        private void RealtimePPUR_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) != MouseButtons.Left) return;
            Left += e.X - _mousePoint.X;
            Top += e.Y - _mousePoint.Y;
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e) => Close();

        private void osuModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var lefttest = _configDictionary.TryGetValue("LEFT", out string leftvalue);
            var toptest = _configDictionary.TryGetValue("TOP", out string topvalue);
            if (!lefttest || !toptest)
            {
                MessageBox.Show("Config.cfgにLEFTまたはTOPの値が存在しなかったため、osu! Modeの起動に失敗しました。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var leftResult = int.TryParse(leftvalue, out int left);
            var topResult = int.TryParse(topvalue, out int top);
            if ((!leftResult || !topResult) && !_isosumode)
            {
                MessageBox.Show("Config.cfgのLEFT、またはTOPの値が不正であったため、osu! Modeの起動に失敗しました。LEFT、TOPには数値以外入力しないでください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _osuModeValue["left"] = left;
            _osuModeValue["top"] = top;
            _isosumode = !_isosumode;
            osuModeToolStripMenuItem.Checked = _isosumode;
        }

        private void RealtimePPUR_Closed(object sender, EventArgs e) => System.Windows.Forms.Application.Exit();

        private void changePriorityToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form priorityForm = new ChangePriorityForm();
            priorityForm.Show();
        }

        private void sRToolStripMenuItem_Click(object sender, EventArgs e) => sRToolStripMenuItem.Checked = !sRToolStripMenuItem.Checked;

        private void ifFCPPToolStripMenuItem_Click(object sender, EventArgs e) => ifFCPPToolStripMenuItem.Checked = !ifFCPPToolStripMenuItem.Checked;

        private void ifFCHitsToolStripMenuItem_Click(object sender, EventArgs e) => ifFCHitsToolStripMenuItem.Checked = !ifFCHitsToolStripMenuItem.Checked;

        private void expectedManiaScoreToolStripMenuItem_Click(object sender, EventArgs e) => expectedManiaScoreToolStripMenuItem.Checked = !expectedManiaScoreToolStripMenuItem.Checked;

        private void currentPPToolStripMenuItem_Click(object sender, EventArgs e) => currentPPToolStripMenuItem.Checked = !currentPPToolStripMenuItem.Checked;

        private void currentPositionToolStripMenuItem_Click(object sender, EventArgs e) => currentPositionToolStripMenuItem.Checked = !currentPositionToolStripMenuItem.Checked;

        private void higherScoreToolStripMenuItem_Click(object sender, EventArgs e) => higherScoreToolStripMenuItem.Checked = !higherScoreToolStripMenuItem.Checked;

        private void highestScoreToolStripMenuItem_Click(object sender, EventArgs e) => highestScoreToolStripMenuItem.Checked = !highestScoreToolStripMenuItem.Checked;

        private void userScoreToolStripMenuItem_Click(object sender, EventArgs e) => userScoreToolStripMenuItem.Checked = !userScoreToolStripMenuItem.Checked;

        private void sSPPToolStripMenuItem_Click(object sender, EventArgs e) => sSPPToolStripMenuItem.Checked = !sSPPToolStripMenuItem.Checked;

        private void hitsToolStripMenuItem_Click(object sender, EventArgs e) => hitsToolStripMenuItem.Checked = !hitsToolStripMenuItem.Checked;

        private void uRToolStripMenuItem_Click(object sender, EventArgs e) => uRToolStripMenuItem.Checked = !uRToolStripMenuItem.Checked;

        private void offsetHelpToolStripMenuItem_Click(object sender, EventArgs e) => offsetHelpToolStripMenuItem.Checked = !offsetHelpToolStripMenuItem.Checked;

        private void currentACCToolStripMenuItem_Click(object sender, EventArgs e) => currentACCToolStripMenuItem.Checked = !currentACCToolStripMenuItem.Checked;

        private void progressToolStripMenuItem_Click(object sender, EventArgs e) => progressToolStripMenuItem.Checked = !progressToolStripMenuItem.Checked;

        private void avgOffsetToolStripMenuItem_Click(object sender, EventArgs e) => avgOffsetToolStripMenuItem.Checked = !avgOffsetToolStripMenuItem.Checked;

        private void healthPercentageToolStripMenuItem_Click(object sender, EventArgs e) => healthPercentageToolStripMenuItem.Checked = !healthPercentageToolStripMenuItem.Checked;

        private void discordRichPresenceToolStripMenuItem_Click(object sender, EventArgs e) => discordRichPresenceToolStripMenuItem.Checked = !discordRichPresenceToolStripMenuItem.Checked;

        private void pPLossModeToolStripMenuItem_Click(object sender, EventArgs e) => pPLossModeToolStripMenuItem.Checked = !pPLossModeToolStripMenuItem.Checked;

        private void saveConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                const string filePath = "Config.cfg";
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("Config.cfgが見つかりませんでした。RealtimePPURをダウンロードし直してください。", "エラー", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                StreamReader sr = new(filePath);
                string[] lines = File.ReadAllLines(filePath);
                sr.Close();

                for (var i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("SR="))
                    {
                        lines[i] = $"SR={(sRToolStripMenuItem.Checked ? "true" : "false")}";
                    }
                    else if (lines[i].StartsWith("SSPP="))
                    {
                        lines[i] = $"SSPP={(sSPPToolStripMenuItem.Checked ? "true" : "false")}";
                    }
                    else if (lines[i].StartsWith("CURRENTPP="))
                    {
                        lines[i] = $"CURRENTPP={(currentPPToolStripMenuItem.Checked ? "true" : "false")}";
                    }
                    else if (lines[i].StartsWith("CURRENTACC="))
                    {
                        lines[i] = $"CURRENTACC={(currentACCToolStripMenuItem.Checked ? "true" : "false")}";
                    }
                    else if (lines[i].StartsWith("HITS="))
                    {
                        lines[i] = $"HITS={(hitsToolStripMenuItem.Checked ? "true" : "false")}";
                    }
                    else if (lines[i].StartsWith("IFFCHITS="))
                    {
                        lines[i] = $"IFFCHITS={(ifFCHitsToolStripMenuItem.Checked ? "true" : "false")}";
                    }
                    else if (lines[i].StartsWith("UR="))
                    {
                        lines[i] = $"UR={(uRToolStripMenuItem.Checked ? "true" : "false")}";
                    }
                    else if (lines[i].StartsWith("OFFSETHELP="))
                    {
                        lines[i] = $"OFFSETHELP={(offsetHelpToolStripMenuItem.Checked ? "true" : "false")}";
                    }
                    else if (lines[i].StartsWith("EXPECTEDMANIASCORE="))
                    {
                        lines[i] =
                            $"EXPECTEDMANIASCORE={(expectedManiaScoreToolStripMenuItem.Checked ? "true" : "false")}";
                    }
                    else if (lines[i].StartsWith("AVGOFFSET="))
                    {
                        lines[i] = $"AVGOFFSET={(avgOffsetToolStripMenuItem.Checked ? "true" : "false")}";
                    }
                    else if (lines[i].StartsWith("PROGRESS="))
                    {
                        lines[i] = $"PROGRESS={(progressToolStripMenuItem.Checked ? "true" : "false")}";
                    }
                    else if (lines[i].StartsWith("IFFCPP="))
                    {
                        lines[i] = $"IFFCPP={(ifFCPPToolStripMenuItem.Checked ? "true" : "false")}";
                    }
                    else if (lines[i].StartsWith("HEALTHPERCENTAGE="))
                    {
                        lines[i] = $"HEALTHPERCENTAGE={(healthPercentageToolStripMenuItem.Checked ? "true" : "false")}";
                    }
                    else if (lines[i].StartsWith("CURRENTPOSITION="))
                    {
                        lines[i] = $"CURRENTPOSITION={(currentPositionToolStripMenuItem.Checked ? "true" : "false")}";
                    }
                    else if (lines[i].StartsWith("HIGHERSCOREDIFF="))
                    {
                        lines[i] = $"HIGHERSCOREDIFF={(higherScoreToolStripMenuItem.Checked ? "true" : "false")}";
                    }
                    else if (lines[i].StartsWith("USERSCORE="))
                    {
                        lines[i] = $"USERSCORE={(userScoreToolStripMenuItem.Checked ? "true" : "false")}";
                    }
                }

                StreamWriter sw = new(filePath, false);
                foreach (var line in lines)
                {
                    sw.WriteLine(line);
                }

                sw.Close();
                MessageBox.Show("Config.cfgの保存が完了しました！", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception error)
            {
                ErrorLogger(error);
                MessageBox.Show("Config.cfgの保存に失敗しました。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class Mods
    {
        public string[] Calculate { get; set; }
        public string[] Show { get; set; }
    }
}
