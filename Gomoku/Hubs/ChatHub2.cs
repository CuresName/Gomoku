using Gomoku.Models;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Numerics;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CoreMVC_SignalR_Chat.Hubs
{
    public class ChatHub2 : Hub
    {
        // 用戶連線 ID 列表
        private static readonly int MaxConnections = 2;
        public static List<PlayerList> playerLists = new List<PlayerList>();
        public static string currentPlayerID="A";
        public static string[,] Cells = new string[15, 15];
        /// <summary>
        /// 連線事件
        /// </summary>
        /// <returns></returns>
        public override async Task OnConnectedAsync()
        {
            var playerA = playerLists.Where(m => m.PlayerID.Contains("A")).FirstOrDefault();
            var player = new PlayerList();
            player.id = Context.ConnectionId;
            if (playerA != null)
            {
                player.PlayerID = "B";
            }
            else
            {
                player.PlayerID = "A";
            }
            if (playerLists.Count > MaxConnections)
            {
                Context.Abort();
            }
            else
            {
                if (playerLists.Where(p => p.id == Context.ConnectionId).FirstOrDefault() == null)
                {
                    playerLists.Add(player);
                }
            }
            await Clients.Client(Context.ConnectionId).SendAsync("UpdSelfID", Context.ConnectionId);


            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 離線事件
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception ex)
        {
            var id = playerLists.Where(p => p.id == Context.ConnectionId).FirstOrDefault();
            if (id != null)
            {
                playerLists.Remove(id);
            }
            // 更新連線 ID 列表
            string jsonString = JsonConvert.SerializeObject(playerLists);
            await Clients.All.SendAsync("UpdList", jsonString);

            // 更新聊天內容
            await Clients.All.SendAsync("UpdContent", "已離線 ID: " + Context.ConnectionId);

            await base.OnDisconnectedAsync(ex);
        }

        /// <summary>
        /// 傳遞訊息
        /// </summary>
        /// <param name="user"></param>
        /// <param name="message"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task SendMessage(string selfID, string message, string sendToID)
        {
            if (string.IsNullOrEmpty(sendToID))
            {
                await Clients.All.SendAsync("UpdContent", selfID + " 說: " + message);
            }
            else
            {
                // 接收人
                await Clients.Client(sendToID).SendAsync("UpdContent", selfID + " 私訊向你說: " + message);

                // 發送人
                await Clients.Client(Context.ConnectionId).SendAsync("UpdContent", "你向 " + sendToID + " 私訊說: " + message);
            }
        }
        public async Task SetPiece(int row, int col, string playerID)
        {
            var name = playerLists.Where(m => m.id == playerID).FirstOrDefault(); 
            var player = "";
            if (CanPlayerMove(name.PlayerID))
            {
                Cells[row, col] = name.PlayerID;
                player = currentPlayerID;
                await Clients.All.SendAsync("Updpiece", name.Player + "：" + row + " " + col, row, col, player);
                if(CheckWinner(row, col))
                {
                    var Winner = name.Player;
                    if (player == "A")
                    {
                        Winner += " 黑色棋子玩家";
                    }
                    else
                    {
                        Winner += " 白色棋子玩家";
                    }
                    await Clients.All.SendAsync("Winner", Winner);
                    ResetGame();
                }
                SwitchPlayer();
            }
            else
            {
                await Clients.Client(playerID).SendAsync("Error", "等待另一位玩家移動回合");
            }
        }

        public async Task NameCheck(string playerName)
        {
            var name = playerLists.Where(m => m.Player == playerName).FirstOrDefault();
            var id = playerLists.Where(m => m.id == Context.ConnectionId).FirstOrDefault();

            if (name == null)
            {
                id.Player = playerName;
                string jsonString = JsonConvert.SerializeObject(playerLists);
                await Clients.All.SendAsync("UpdList", jsonString);
                await Clients.Client(Context.ConnectionId).SendAsync("NewConnect");
                await Clients.All.SendAsync("UpdContent", "已連線 ID: " + Context.ConnectionId);
                if (checkPlayer())
                {
                    ResetGame();
                }
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("Error", "名稱已使用");
            }
        }
        public bool CanPlayerMove(string playerID)
        {
            return currentPlayerID == playerID;
        }
        public async Task ResetGame()
        {
            Cells = new string[15, 15];
            currentPlayerID = "A";
            await Clients.All.SendAsync("GameStart");
        }
            public bool checkPlayer()
        {
            var playerA = playerLists.Where(m => m.PlayerID == "A").FirstOrDefault();
            var playerB = playerLists.Where(m => m.PlayerID == "B").FirstOrDefault();
            if (playerLists.Count == 2)
            {
                if (playerA != null && playerB != null)
                {
                    if (playerA.Player != null && playerB.Player != null)
                    {
                        currentPlayerID = "A";
                        return true;
                    }
                }
            }
            return false;
     
        }
        public void SwitchPlayer()
        {
            currentPlayerID = (currentPlayerID == "A") ? "B" : "A";
        }

        public bool CheckWinner(int row, int col)
        {
            // 检查水平方向
            if (checkLine(row, col, 1, 0) + checkLine(row, col, -1, 0) >= 4) return true;
            // 检查垂直方向
            if (checkLine(row, col, 0, 1) + checkLine(row, col, 0, -1) >= 4) return true;
            // 检查左斜方向
            if (checkLine(row, col, 1, 1) + checkLine(row, col, -1, -1) >= 4) return true;
            // 检查右斜方向
            if (checkLine(row, col, 1, -1) + checkLine(row, col, -1, 1) >= 4) return true;
            return false;
        }
        public int checkLine(int row, int col, int rowDelta, int colDelta)
        {
            var count = 0;
            var r = row + rowDelta;
            var c = col + colDelta;

            while (r >= 0 && r < 15 && c >= 0 && c < 15 && Cells[r, c] == currentPlayerID)
            {
                count++;
                r += rowDelta;
                c += colDelta;
            }
            return count;
        }
    }
}