using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Scoreboard", "Bazz3l", "1.0.3")]
    [Description("Display player stats kills deaths and so on.")]
    class Scoreboard : RustPlugin
    {
        #region Fields
        Dictionary<ulong, PlayerUI> PlayersUI = new Dictionary<ulong, PlayerUI>();
        string LeaderboardPanel = "LeaderboardPanel";
        string StatsPanel = "StatsPanel";
        string StatsPanelItem = "StatsPanelItem";
        string StatsPanelHeader = "StatsPanelHeader";
        // Row Amount
        double RowAmount = 15;
        // Column Width
        float ColumnWidth = 1f / 5;
        #endregion

        #region Storage
        static StoredData storage;

        class StoredData
        {
            public Dictionary<ulong, PlayerData> Stats = new Dictionary<ulong, PlayerData>();

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

                if (!storage.Stats.TryGetValue(player.userID, out playerData))
                {
                    playerData = storage.Stats[player.userID] = new PlayerData();
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
            Interface.Oxide.DataFileSystem.WriteObject<StoredData>(Name, storage);
        }
        #endregion

        #region Oxide
        void Init()
        {
            storage = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
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
            return storage.Stats.Values.OrderByDescending(i => i.Kills)
            .Skip((page - 1) * (int)takeCount)
            .Take((int)takeCount)
            .ToList();
        }

        int GetCurrentPage(BasePlayer player)
        {
            PlayerUI playerUi;

            if (!PlayersUI.TryGetValue(player.userID, out playerUi))
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

        void Label(ref CuiElementContainer container, string panelName, string aMin, string aMax,int textSize, string text, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            container.Add(new CuiLabel
            {
                Text = {
                    FontSize = textSize,
                    Text     = text,
                    Color    = "255 255 255 1",
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

            if(!PlayersUI.TryGetValue(player.userID, out playerUI))
            {
                playerUI = PlayersUI[player.userID] = new PlayerUI();
            }

            playerUI.page = page;

            if (playerUI.container == null)
            {
                playerUI.container = container = CreateElementContainer(LeaderboardPanel, "0.1 0.1 0.1 0.98", "0 0", "1 1", true);

                Label(ref container, LeaderboardPanel, "0.02 0.946", "0.136 0.969", 12, "LEADERBOARD");
                ButtonCommand(ref container, LeaderboardPanel, "1.2 1.2 1.2 0.24", "0.797 0.94225", "0.875 0.97675", 12, "Next", "stats.next", TextAnchor.MiddleCenter);
                ButtonCommand(ref container, LeaderboardPanel, "1.2 1.2 1.2 0.24", "0.716 0.94225", "0.794 0.97675", 12, "Prev", "stats.prev", TextAnchor.MiddleCenter);
                ButtonCommand(ref container, LeaderboardPanel, "1.4 1.4 0.4 0.24", "0.878 0.94225", "0.995 0.97675", 12, "Close", "stats.close", TextAnchor.MiddleCenter);

                CuiHelper.AddUi(player, container);
            }
            else
            {
                container = PlayersUI[player.userID].container;
            }

            if (playerUI.panel != null)
            {
                CuiHelper.DestroyUi(player, StatsPanel);
            }

            playerUI.panel = CreatePanel(ref container, LeaderboardPanel, StatsPanel, "0.4 0.4 0.4 0.24", "0 0", "1 0.920");
            int panelIndex = container.Count - 1;

            CreatePanel(ref container, StatsPanel, StatsPanelHeader, "1.4 1.4 1.4 0.14", $"0.008 0.930", $"0.992 0.984");
            Label(ref container, StatsPanelHeader, "0 0", $"{ColumnWidth} 1", 10, "PLAYER");
            Label(ref container, StatsPanelHeader, $"{ColumnWidth} 0", $"{ColumnWidth * 2} 1", 10, "KILLS");
            Label(ref container, StatsPanelHeader, $"{ColumnWidth * 2} 0", $"{ColumnWidth * 3} 1", 10, "DEATHS");
            Label(ref container, StatsPanelHeader, $"{ColumnWidth * 3} 0", $"{ColumnWidth * 4} 1", 10, "SUICIDES");

            int rowPos = 1;

            foreach(PlayerData item in GetPlayerData(page, count))
            {
                CreatePanel(ref container, StatsPanel, $"{StatsPanelItem}_{rowPos}", "0.4 0.4 0.4 0.24", $"0.008 {0.930 - (rowPos * (0.06))}", $"0.992 {0.986 - (rowPos * (0.06))}");
                Label(ref container, $"{StatsPanelItem}_{rowPos}", "0 0", $"{ColumnWidth} 1", 10, $"{item.Name}");
                Label(ref container, $"{StatsPanelItem}_{rowPos}", $"{ColumnWidth} 0", $"{ColumnWidth * 2} 1", 10, $"{item.Kills}");
                Label(ref container, $"{StatsPanelItem}_{rowPos}", $"{ColumnWidth * 2} 0", $"{ColumnWidth * 3} 1", 10, $"{item.Deaths}");
                Label(ref container, $"{StatsPanelItem}_{rowPos}", $"{ColumnWidth * 3} 0", $"{ColumnWidth * 4} 1", 10, $"{item.Suicides}");
                rowPos++;
            }

            CuiHelper.AddUi(player, container.Skip(panelIndex).Take(container.Count - panelIndex).ToList());
            container.RemoveRange(panelIndex, container.Count - panelIndex);
        }

        void CloseUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, LeaderboardPanel);

            PlayerUI playerUi;

            if (!PlayersUI.TryGetValue(player.userID, out playerUi))
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

            if (currentPage < 1 || currentPage > System.Math.Ceiling(storage.Stats.Count / RowAmount))
            {
                return;
            }

            OpenUI(player, currentPage, RowAmount);
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

            if (currentPage < 1 || currentPage > System.Math.Ceiling(storage.Stats.Count / RowAmount))
            {
                return;
            }

            OpenUI(player, currentPage, RowAmount);
        }

        [ConsoleCommand("stats.open")]
        void Open(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
            {
                return;
            }

            OpenUI(player, 1, RowAmount);
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
        void LeaderboardCommand(BasePlayer player, string command, string[] args) => OpenUI(player, 1, RowAmount);

        [ChatCommand("pinfo")]
        void StatsCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 1)
            {
                player.ChatMessage(PlayerData.GetPlayer(player).GetInfo("Your Stats"));
                return;
            }

            BasePlayer target = FindTarget(string.Join(" ", args));
            if (target == null)
            {
                player.ChatMessage("No player found.");
                return;
            }

            player.ChatMessage(PlayerData.GetPlayer(target).GetInfo($"{target.displayName} Stats"));
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