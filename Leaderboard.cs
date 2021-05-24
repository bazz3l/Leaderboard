using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Leaderboard", "Bazz3l", "1.0.5")]
    [Description("Display players kdr with leaderboard.")]
    class Leaderboard : RustPlugin
    {
        #region Fields
        
        private const string LEADERBOARD_PANEL = "Leaderboard_Panel";
        private const string LEADERBOARD_HEADER = "Leaderboard_Header";
        private const string STATS_PANEL_HEADER = "Stats_Panel_Header";
        private const string STATS_PANEL_ITEM = "Stats_Panel_Item";
        private const string STATS_PANEL = "Stats_Panel";
        private const float COLUMN_WIDTH = 1f / 5;
        
        #endregion

        #region Storage
        
        private static StoredData _data;

        private class StoredData
        {
            #region Fields

            public readonly Dictionary<ulong, PlayerData> PlayerStats = new Dictionary<ulong, PlayerData>();

            #endregion

            #region IO
            
            public static StoredData Load() 
                => Interface.Oxide.DataFileSystem.ReadObject<StoredData>("Leaderboard");
            
            public void Save() 
                => Interface.Oxide.DataFileSystem.WriteObject("Leaderboard", this);
            
            #endregion
        }

        private class PlayerData
        {
            public string Name;
            public int Kills;
            public int Deaths;
            public double KDR => Deaths > 0 ? Math.Round((double) Kills / Deaths, 2) : Kills;

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
        }

        #endregion

        #region Oxide
        
        private void Loaded()
        {
            _data = StoredData.Load();
        }

        private void Unload()
        {
            UI.RemoveAll();
            
            _data.Save();
        }

        private void OnServerSave()
        {
            _data.Save();
        }

        private void OnNewSave(string filename)
        {
            _data.PlayerStats.Clear();
            _data.Save();
        }

        #endregion

        #region Core
        
        private IEnumerable<PlayerData> GetPlayerData(int page, int takeCount)
        {
            return _data.PlayerStats.Values.OrderByDescending(i => i.Kills)
            .Skip((page - 1) * takeCount)
            .Take(takeCount);
        }

        private void OpenUI(BasePlayer player, int page = 1, int count = 15, bool isFirst = false)
        {
            CuiElementContainer container = new CuiElementContainer();
            
            if (isFirst)
            {
                CuiHelper.DestroyUi(player, LEADERBOARD_PANEL);
                
                container = UI.CreateElementContainer(LEADERBOARD_PANEL, "0.1 0.1 0.1 0.98", "0 0", "1 1", true);
            }
            
            CuiHelper.DestroyUi(player, LEADERBOARD_HEADER);
            CuiHelper.DestroyUi(player, STATS_PANEL);

            #region Header

            UI.Panel(container, LEADERBOARD_PANEL, LEADERBOARD_HEADER, "0 0 0 0", "0 0", "1 1");
            UI.Label(container, LEADERBOARD_HEADER, "255 255 255 1", "0.02 0.946", "0.136 0.969", 12, "LEADERBOARD");
            UI.Button(container, LEADERBOARD_HEADER, "1.2 1.2 1.2 0.24", "0.797 0.94225", "0.875 0.97675", 12, "►", $"stats.change {page + 1}");
            UI.Button(container, LEADERBOARD_HEADER, "1.2 1.2 1.2 0.24", "0.716 0.94225", "0.794 0.97675", 12, "◄", $"stats.change {page - 1}");
            UI.Button(container, LEADERBOARD_HEADER, "1.4 1.4 0.4 0.24", "0.878 0.94225", "0.995 0.97675", 12, "Close", "stats.close");            

            #endregion

            #region Stats
            
            int totalPages = 0;

            if (_data.PlayerStats.Values.Count > 0)
            {
                totalPages = _data.PlayerStats.Values.Count / count;
            }

            if (page < 0 || page > totalPages)
            {
                page = 0;
            }
            
            UI.Panel(container, LEADERBOARD_PANEL, STATS_PANEL, "0.4 0.4 0.4 0.24", "0 0", "1 0.920");
            UI.Panel(container, STATS_PANEL, STATS_PANEL_HEADER, "1.4 1.4 1.4 0.14", $"0.008 0.930", $"0.992 0.984");
            UI.Label(container, STATS_PANEL_HEADER, "255 255 255 1", "0 0", $"{COLUMN_WIDTH} 1", 10, "PLAYER");
            UI.Label(container, STATS_PANEL_HEADER, "255 255 255 1", $"{COLUMN_WIDTH} 0", $"{COLUMN_WIDTH * 2} 1", 10, "KILLS");
            UI.Label(container, STATS_PANEL_HEADER, "255 255 255 1", $"{COLUMN_WIDTH * 2} 0", $"{COLUMN_WIDTH * 3} 1", 10, "DEATHS");
            UI.Label(container, STATS_PANEL_HEADER, "255 255 255 1", $"{COLUMN_WIDTH * 3} 0", $"{COLUMN_WIDTH * 4} 1", 10, "KDR");

            int rowPos = 1;

            foreach(PlayerData item in GetPlayerData(page, count))
            {
                UI.Panel(container, STATS_PANEL, $"{STATS_PANEL_ITEM}_{rowPos}", "0.4 0.4 0.4 0.24", $"0.008 {0.930 - (rowPos * (0.06))}", $"0.992 {0.986 - (rowPos * (0.06))}");
                UI.Label(container, $"{STATS_PANEL_ITEM}_{rowPos}", "255 255 255 1", "0 0", $"{COLUMN_WIDTH} 1", 10, $"{item.Name}");
                UI.Label(container, $"{STATS_PANEL_ITEM}_{rowPos}", "255 255 255 1", $"{COLUMN_WIDTH} 0", $"{COLUMN_WIDTH * 2} 1", 10, $"{item.Kills}");
                UI.Label(container, $"{STATS_PANEL_ITEM}_{rowPos}", "255 255 255 1", $"{COLUMN_WIDTH * 2} 0", $"{COLUMN_WIDTH * 3} 1", 10, $"{item.Deaths}");
                UI.Label(container, $"{STATS_PANEL_ITEM}_{rowPos}", "255 255 255 1", $"{COLUMN_WIDTH * 3} 0", $"{COLUMN_WIDTH * 4} 1", 10, $"{item.KDR}");
                rowPos++;
            }

            #endregion

            CuiHelper.AddUi(player, container);
        }

        private void CloseUI(BasePlayer player)
        {
            UI.RemoveUI(player);
        }
        
        #endregion

        #region UI

        private static class UI
        {
            public static CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool useCursor = false, string parent = "Overlay")
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

            public static void Panel(CuiElementContainer container, string parentName, string panelName, string color, string aMin, string aMax, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = cursor,
                    Image = { 
                        Color    = color,
                        Material = "assets/icons/iconmaterial.mat"
                    },
                    RectTransform = { 
                        AnchorMin = aMin, 
                        AnchorMax = aMax 
                    }
                }, parentName, panelName);
            }
            
            public static void Label(CuiElementContainer container, string parentName, string color, string aMin, string aMax,int textSize, string text, TextAnchor anchor = TextAnchor.MiddleCenter)
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
                }, parentName);
            }

            public static void Button(CuiElementContainer container, string parentName, string color,string aMin, string aMax, int textSize, string text, string command, TextAnchor anchor = TextAnchor.MiddleCenter)
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
                }, parentName);
            }
            
            public static void RemoveUI(BasePlayer player) => CuiHelper.DestroyUi(player, LEADERBOARD_PANEL);
                
            public static void RemoveAll()
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    RemoveUI(player);
                }
            }
        }

        #endregion

        #region Commands

        #region Chat

        [ChatCommand("leaderboard")]
        private void LeaderboardCommand(BasePlayer player, string command, string[] args)
            => OpenUI(player, 1, 10, true);

        #endregion

        #region Console

        [ConsoleCommand("stats.change")]
        private void PrevPage(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            OpenUI(player, arg.GetInt(0), 10);
        }

        [ConsoleCommand("stats.close")]
        private void Close(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            CloseUI(player);
        }        

        #endregion

        #endregion

        #region API
        
        public void RecordKill(BasePlayer player)
        {
            PlayerData.GetPlayer(player).Kills++;
        }
        
        public void RecordDeath(BasePlayer player)
        {
            PlayerData.GetPlayer(player).Deaths++;
        }

        #endregion
    }
}