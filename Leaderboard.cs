using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Leaderboard", "Bazz3l", "1.0.4")]
    [Description("Display player PlayerStats kills deaths and so on.")]
    class Leaderboard : RustPlugin
    {
        #region Fields
        Dictionary<ulong, PlayerUI> _playersUI = new Dictionary<ulong, PlayerUI>();
        string _leaderboardPanel = "Leaderboard_Panel";

        string _statsPanel = "Stats_Panel";
        string _statsPanelHeader = "Stats_Panel_Header";
        string _statsPanelItem = "Stats_Panel_Item";

        // Row Amount
        double _rowAmount = 15;
        // Column Width
        float _columnWidth = 1f / 5;
        #endregion

        #region _data
        static StoredData _data;

        class StoredData
        {
            public Dictionary<ulong, PlayerData> PlayerStats = new Dictionary<ulong, PlayerData>();

            public StoredData()
            {
                //
            }
        }

        class PlayerData
        {
            public string Name;
            public int Kills;
            public int Deaths;
            public int Suicides;
            public double KDR { get { return (Deaths == 0) ? Kills : (Kills / Deaths); } }

            public static PlayerData GetPlayer(BasePlayer player)
            {
                PlayerData playerData;

                if (!_data.PlayerStats.TryGetValue(player.userID, out playerData))
                {
                    playerData = _data.PlayerStats[player.userID] = new PlayerData();
                }

                playerData.Name = player.displayName;

                return playerData;
            }

            public string GetInfo(string Title)
            {
                return $"<color=#DC143C>Leaderboard</color>: {Title}\nKills: {Kills.ToString()}\nDeaths: {Deaths.ToString()}\nSuicides: {Suicides.ToString()}\nKDR: {KDR.ToString()}";
            }
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject<StoredData>(Name, _data);
        }
        #endregion

        #region Oxide
        void Init()
        {
            _data = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
        }

        void Unload()
        {
            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player == null || !player.IsConnected)
                {
                    continue;
                }

                CloseUI(player);
            }
        }

        void OnServerSave() => SaveData();
        #endregion

        #region Core
        List<PlayerData> GetPlayerData(int page, double takeCount)
        {
            return _data.PlayerStats.Values.OrderByDescending(i => i.Kills)
            .Skip((page - 1) * (int)takeCount)
            .Take((int)takeCount)
            .ToList();
        }

        int GetCurrentPage(BasePlayer player)
        {
            PlayerUI playerUi;

            if (!_playersUI.TryGetValue(player.userID, out playerUi))
            {
                return 1;
            }

            return playerUi.page;
        }

        class PlayerUI
        {
            public CuiElementContainer container;
            public CuiPanel panel;
            public int page = 1;
        }

        BasePlayer FindTarget(string nameOrId)
        {
            foreach (BasePlayer player in BasePlayer.allPlayerList)
            {
                if (player.displayName.Contains(nameOrId, CompareOptions.IgnoreCase) || nameOrId == player.UserIDString)
                {
                    return player;
                }
            }

            return null;
        }
        #endregion

        #region UI
        CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool useCursor = false, string parent = "Overlay")
        {
            CuiElementContainer element = new CuiElementContainer()
            {
                {
                    new CuiPanel {
                        CursorEnabled = useCursor,
                        Image = {
                            Color    = color,
                            Material = "assets/content/ui/uibackgroundblur.mat"
                        },
                        RectTransform = {
                            AnchorMin = aMin, 
                            AnchorMax = aMax
                        }
                    },
                    new CuiElement().Parent = parent,
                    panelName
                }
            };

            return element;
        }

        CuiPanel CreatePanel(ref CuiElementContainer container, string panel, string name, string color, string aMin, string aMax, bool cursor = false)
        {
            CuiPanel cuiPanel = new CuiPanel
            {
                Image = { 
                    Color    = color,
                    Material = "assets/icons/iconmaterial.mat"
                },
                RectTransform = { 
                    AnchorMin = aMin, 
                    AnchorMax = aMax 
                },
                CursorEnabled = cursor
            };

            container.Add(cuiPanel, panel, name);

            return cuiPanel;
        }

        void LoadImagePNG(ref CuiElementContainer container, string panel, string png, string aMin, string aMax)
        {
            container.Add(new CuiElement
            {
                Name       = CuiHelper.GetGuid(),
                Parent     = panel,
                FadeOut    = 0.15f,
                Components = {
                    new CuiRawImageComponent { 
                        Png    = png, 
                        FadeIn = 0.3f 
                    },
                    new CuiRectTransformComponent { 
                        AnchorMin = aMin, 
                        AnchorMax = aMax 
                    }
                }
            });
        }

        void Label(ref CuiElementContainer container, string panelName, string color, string aMin, string aMax,int textSize, string text, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            container.Add(new CuiLabel
            {
                Text = {
                    FontSize = textSize,
                    Text     = text,
                    Color    = color,
                    Align    = anchor,
                },
                RectTransform = {
                    AnchorMin = aMin,
                    AnchorMax = aMax,
                }
            }, panelName);
        }

        void ButtonCommand(ref CuiElementContainer container, string panelName, string color,string aMin, string aMax, int textSize, string text, string command, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            container.Add(new CuiButton
            {
                Button = {
                    Command = command,
                    Color   = color,
                },
                Text = {
                    FontSize = textSize,
                    Text     = text,
                    Align    = anchor,
                },
                RectTransform = {
                    AnchorMin = aMin,
                    AnchorMax = aMax,
                }
            }, panelName);
        }

        void ButtonClose(ref CuiElementContainer container, string panelName, string color,string aMin, string aMax, int textSize, string text, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            container.Add(new CuiButton
            {
                Button = {
                    Close = panelName,
                    Color = color,
                },
                Text = {
                    FontSize = textSize,
                    Text     = text,
                    Align    = anchor,
                },
                RectTransform = {
                    AnchorMin = aMin,
                    AnchorMax = aMax,
                }
            }, panelName);
        }

        void OpenUI(BasePlayer player, int page = 1, double count = 15)
        {
            CuiElementContainer container;
            
            PlayerUI playerUI;

            if(!_playersUI.TryGetValue(player.userID, out playerUI))
            {
                playerUI = _playersUI[player.userID] = new PlayerUI();
            }

            playerUI.page = page;

            if (playerUI.container == null)
            {
                playerUI.container = container = CreateElementContainer(_leaderboardPanel, "0.1 0.1 0.1 0.98", "0 0", "1 1", true);

                Label(ref container, _leaderboardPanel, "255 255 255 1", "0.02 0.946", "0.136 0.969", 12, "LEADERBOARD");
                ButtonCommand(ref container, _leaderboardPanel, "1.2 1.2 1.2 0.24", "0.797 0.94225", "0.875 0.97675", 12, "Next", "stats.next", TextAnchor.MiddleCenter);
                ButtonCommand(ref container, _leaderboardPanel, "1.2 1.2 1.2 0.24", "0.716 0.94225", "0.794 0.97675", 12, "Prev", "stats.prev", TextAnchor.MiddleCenter);
                ButtonCommand(ref container, _leaderboardPanel, "1.4 1.4 0.4 0.24", "0.878 0.94225", "0.995 0.97675", 12, "Close", "stats.close", TextAnchor.MiddleCenter);

                CuiHelper.AddUi(player, container);
            }
            else
            {
                container = _playersUI[player.userID].container;
            }

            if (playerUI.panel != null)
            {
                CuiHelper.DestroyUi(player, _statsPanel);
            }

            playerUI.panel = CreatePanel(ref container, _leaderboardPanel, _statsPanel, "0.4 0.4 0.4 0.24", "0 0", "1 0.920");
            int panelIndex = container.Count - 1;

            CreatePanel(ref container, _statsPanel, _statsPanelHeader, "1.4 1.4 1.4 0.14", $"0.008 0.930", $"0.992 0.984");
            Label(ref container, _statsPanelHeader, "255 255 255 1", "0 0", $"{_columnWidth} 1", 10, "PLAYER");
            Label(ref container, _statsPanelHeader, "255 255 255 1", $"{_columnWidth} 0", $"{_columnWidth * 2} 1", 10, "KILLS");
            Label(ref container, _statsPanelHeader, "255 255 255 1", $"{_columnWidth * 2} 0", $"{_columnWidth * 3} 1", 10, "DEATHS");
            Label(ref container, _statsPanelHeader, "255 255 255 1", $"{_columnWidth * 3} 0", $"{_columnWidth * 4} 1", 10, "SUICIDES");

            int rowPos = 1;

            foreach(PlayerData item in GetPlayerData(page, count))
            {
                CreatePanel(ref container, _statsPanel, $"{_statsPanelItem}_{rowPos}", "0.4 0.4 0.4 0.24", $"0.008 {0.930 - (rowPos * (0.06))}", $"0.992 {0.986 - (rowPos * (0.06))}");
                Label(ref container, $"{_statsPanelItem}_{rowPos}", "255 255 255 1", "0 0", $"{_columnWidth} 1", 10, $"{item.Name}");
                Label(ref container, $"{_statsPanelItem}_{rowPos}", "255 255 255 1", $"{_columnWidth} 0", $"{_columnWidth * 2} 1", 10, $"{item.Kills}");
                Label(ref container, $"{_statsPanelItem}_{rowPos}", "255 255 255 1", $"{_columnWidth * 2} 0", $"{_columnWidth * 3} 1", 10, $"{item.Deaths}");
                Label(ref container, $"{_statsPanelItem}_{rowPos}", "255 255 255 1", $"{_columnWidth * 3} 0", $"{_columnWidth * 4} 1", 10, $"{item.Suicides}");
                rowPos++;
            }

            CuiHelper.AddUi(player, container.Skip(panelIndex).Take(container.Count - panelIndex).ToList());
            container.RemoveRange(panelIndex, container.Count - panelIndex);
        }

        void CloseUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, _leaderboardPanel);

            PlayerUI playerUi;

            if (!_playersUI.TryGetValue(player.userID, out playerUi))
            {
                return;
            }

            playerUi.container = null;
            playerUi.panel = null;
        }
        #endregion

        #region Commands
        [ConsoleCommand("stats.next")]
        void NextPage(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
            {
                return;
            }

            int currentPage = GetCurrentPage(player);

            currentPage++;

            if (currentPage < 1 || currentPage > System.Math.Ceiling(_data.PlayerStats.Count / _rowAmount))
            {
                return;
            }

            OpenUI(player, currentPage, _rowAmount);
        }

        [ConsoleCommand("stats.prev")]
        void PrevPage(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
            {
                return;
            }

            int currentPage = GetCurrentPage(player);

            currentPage--;

            if (currentPage < 1 || currentPage > System.Math.Ceiling(_data.PlayerStats.Count / _rowAmount))
            {
                return;
            }

            OpenUI(player, currentPage, _rowAmount);
        }

        [ConsoleCommand("stats.open")]
        void Open(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
            {
                return;
            }

            OpenUI(player, 1, _rowAmount);
        }

        [ConsoleCommand("stats.close")]
        void Close(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
            {
                return;
            }

            CloseUI(player);
        }

        [ChatCommand("leaderboard")]
        void LeaderboardCommand(BasePlayer player, string command, string[] args) => OpenUI(player, 1, _rowAmount);

        [ChatCommand("pinfo")]
        void StatsCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 1)
            {
                player.ChatMessage(PlayerData.GetPlayer(player).GetInfo("Your stats"));
                return;
            }

            BasePlayer target = FindTarget(string.Join(" ", args));
            if (target == null)
            {
                player.ChatMessage("No player found.");
                return;
            }

            player.ChatMessage(PlayerData.GetPlayer(target).GetInfo($"{target.displayName} stats"));
        }
        #endregion

        #region API
        [HookMethod("RecordKill")]
        public void RecordKill(BasePlayer player) => PlayerData.GetPlayer(player).Kills++;

        [HookMethod("RecordDeath")]
        public void RecordDeath(BasePlayer player) => PlayerData.GetPlayer(player).Deaths++;

        [HookMethod("RecordSuicide")]
        public void RecordSuicide(BasePlayer player) => PlayerData.GetPlayer(player).Suicides++;
        #endregion
    }
}