using Gomoku.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CoreMVC_SignalR_Chat.Hubs
{
    public class ChatHub : Hub
    {
        // 用戶連線 ID 列表
        private static readonly int MaxConnections = 2;
        public static List<PlayerList> playerLists = new List<PlayerList>();
        public static string currentPlayerID = "A";
        public static string[,] Cells = new string[15, 15];
        public static bool ComputerPlay = false;
        public static int ComputerStep = 0;
        public static (int, int) ComputerLastStep = (0, 0);
        public static (int, int) PlayerLastStep = (0, 0);
        public static (int, int)[] ComputertStepLog = new (int, int)[112];

        /// <summary>
        /// 連線事件
        /// </summary>
        /// <returns></returns>

        public override async Task OnConnectedAsync()
        {
            var playerA = playerLists.Where(m => m.PlayerID.Contains("A")).FirstOrDefault();
            var player = new PlayerList();
            player.id = Context.ConnectionId;
            if (ComputerPlay)
            {
                ComputerToPlayer();
            }
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
                PlayerLastStep = (row, col);
                player = currentPlayerID;
                await Clients.All.SendAsync("Updpiece", name.Player + "：" + row + " " + col, row, col, player);

                if (CheckWinner(row, col))
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
                    foreach (var playerq in playerLists)
                    {
                        playerq.Ready = false;
                    }
                    await Clients.All.SendAsync("UpdList");
                    ComputerReset();
                }
                else
                {
                    SwitchPlayer();
                }
            }
            else
            {
                await Clients.Client(playerID).SendAsync("Error", "等待另一位玩家移動回合");
            }
            if (ComputerPlay && currentPlayerID == "C" && !CheckWinner(row, col))
            {
                ComputerSetPiece();
            }
        }

        public async Task NameCheck(string playerName)
        {
            var UserID = Context.ConnectionId;
            var name = playerLists.Where(m => m.Player == playerName).FirstOrDefault();
            var id = playerLists.Where(m => m.id == UserID).FirstOrDefault();

            if (name == null)
            {
                id.Player = playerName;
                id.Ready = true;
                string jsonString = JsonConvert.SerializeObject(playerLists);
                await Clients.All.SendAsync("UpdList", jsonString);
                await Clients.Client(UserID).SendAsync("NewConnect", id.PlayerID);
                await Clients.All.SendAsync("UpdContent", "已連線 ID: " + UserID);
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
            ComputerPlay = ComputerPlay ? true : false;
            ComputerStep = 0;
            ComputertStepLog = new (int, int)[112];
            await Clients.All.SendAsync("GameStart");
        }
        public bool checkPlayer()
        {
            var playerID = ComputerPlay ? "C" : "B";
            var playerA = playerLists.Where(m => m.PlayerID == "A").FirstOrDefault();
            var playerB = playerLists.Where(m => m.PlayerID == playerID).FirstOrDefault();

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
            if (ComputerPlay)
            {
                currentPlayerID = (currentPlayerID == "A") ? "C" : "A";
            }
            else
            {
                currentPlayerID = (currentPlayerID == "A") ? "B" : "A";
            }
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
        public async Task PlayerReady(string playerID)
        {
            var player = playerLists.Where(m => m.id == playerID).FirstOrDefault();
            if (player != null)
            {
                player.Ready = true;
            }
            var allReady = playerLists.Where(m => m.Ready == false).FirstOrDefault();
            string jsonString = JsonConvert.SerializeObject(playerLists);
            await Clients.All.SendAsync("UpdList", jsonString);
            if (allReady == null)
            {
                ResetGame();
            }
        }
        public void ComputerReset()
        {
            var computer = playerLists.Where(m => m.id == "Computer").FirstOrDefault();
            if (computer != null)
            {
                computer.Ready = true;
                PlayerReady(computer.id);
            }
        }
        public void playWithcomputer()
        {
            if (ComputerPlay)
            {

            }
            else
            {
                ComputerPlay = true;
                var computer = new PlayerList()
                {
                    id = "Computer",
                    PlayerID = "C",
                    Ready = true,
                    Player = "Computer"
                };
                playerLists.Add(computer);

                PlayerReady(computer.id);
            }
        }
        public async void ComputerSetPiece()
        {
            var place = Mode2();
            var row = place[0];
            var col = place[1];
            SetPiece(row, col, "Computer");
        }

        public int[] Mode1()
        {
            int[] ans = new int[2];
            int row, col;
            int CS = ComputerStep;
            int CSmax = 0;
            int CSbest = 0;
            int a = 0, b = 0, c = 0, d = 0;

            if (ComputerStep == 0)
            {
                Random r = new();
                row = r.Next(6, 8);
                col = r.Next(6, 8);
                while (Cells[row, col] != null)
                {
                    row = r.Next(6, 8);
                    col = r.Next(6, 8);
                }
                ans[0] = row;
                ans[1] = col;
            }
            else
            {
                int cls1;
                int cls2;
                var pls1 = PlayerLastStep.Item1;
                var pls2 = PlayerLastStep.Item2;

                currentPlayerID = "A";
                var pa = (checkLine(pls1, pls2, 1, 0) + checkLine(pls1, pls2, -1, 0));
                // 检查垂直方向
                var pb = (checkLine(pls1, pls2, 0, 1) + checkLine(pls1, pls2, 0, -1));
                // 检查左斜方向
                var pc = (checkLine(pls1, pls2, 1, 1) + checkLine(pls1, pls2, -1, -1));
                // 检查右斜方向
                var pd = (checkLine(pls1, pls2, 1, -1) + checkLine(pls1, pls2, -1, 1));
                currentPlayerID = "C";
                int pmax = Math.Max(Math.Max(pa, pb), Math.Max(pc, pd));

                while (CS >= 0)
                {
                    cls1 = ComputertStepLog[CS].Item1;
                    cls2 = ComputertStepLog[CS].Item2;
                    a = (checkLine(cls1, cls2, 1, 0) + checkLine(cls1, cls2, -1, 0));
                    // 检查垂直方向
                    b = (checkLine(cls1, cls2, 0, 1) + checkLine(cls1, cls2, 0, -1));
                    // 检查左斜方向
                    c = (checkLine(cls1, cls2, 1, 1) + checkLine(cls1, cls2, -1, -1));
                    // 检查右斜方向
                    d = (checkLine(cls1, cls2, 1, -1) + checkLine(cls1, cls2, -1, 1));
                    int max = Math.Max(Math.Max(a, b), Math.Max(c, d));
                    if (CSmax < max)
                    {
                        CSmax = max;
                        CSbest = CS;
                    }
                    CS--;
                }
                int walkmode;
                cls1 = ComputertStepLog[CSbest].Item1;
                cls2 = ComputertStepLog[CSbest].Item2;
                if ((pa >= 2 || pb >= 2 || pc >= 2 || pd >= 2) && !(a >= 2 || b >= 2 || c >= 2 || d >= 2))
                {
                    if (pmax == pa)
                    {
                        walkmode = 1;
                    }
                    else if (pmax == pb)
                    {
                        walkmode = 2;
                    }
                    else if (pmax == pc)
                    {
                        walkmode = 3;
                    }
                    else
                    {
                        walkmode = 4;
                    }
                    ans = walkMode(walkmode, pls1, pls2);
                }
                else
                {
                    if (CSmax == a)
                    {
                        walkmode = 1;
                    }
                    else if (CSmax == b)
                    {
                        walkmode = 2;
                    }
                    else if (CSmax == c)
                    {
                        walkmode = 3;
                    }
                    else
                    {
                        walkmode = 4;
                    }
                    ans = walkMode(walkmode, cls1, cls2);
                }



            }
            ComputertStepLog[ComputerStep] = (ans[0], ans[1]);
            ComputerStep++;
            ComputerLastStep = (ans[0], ans[1]);
            return ans;
        }
        public int[] CheckLL(int row, int col, string player)
        {
            int[] results =
                [
                    // 上
                    checkLLL (row, col, -1, 0, player),
                    // 下
                    checkLLL(row, col,  1,  0, player),
                    // 左
                    checkLLL(row, col,  0, -1, player),
                    // 右
                    checkLLL(row, col,  0,  1, player), 
                    // 左上
                    checkLLL(row, col, -1, -1, player),
                    // 左下
                    checkLLL(row, col,  1, -1, player),
                    // 右上
                    checkLLL(row, col, -1,  1, player),
                    // 右下
                    checkLLL(row, col,  1,  1, player),
                ];

            return results;
        }
        public int checkLLL(int row, int col, int rowDelta, int colDelta, string player)
        {
            var count = 0;
            var r = row + rowDelta;
            var c = col + colDelta;
            var num = 0;
            var player1 = player == "A" ? "A" : "C";
            var player2 = player == "C" ? "A" : "C";
            while (r >= 0 && r < 15 && c >= 0 && c < 15 && num < 5)
            {
                if (Cells[r, c] == player1)
                {
                    count++;
                }
                else if (Cells[r, c] == player2)
                {
                    return 0;
                }
                num++;
                r += rowDelta;
                c += colDelta;
            }
            if (count >= 3) count += 100;
            else if (count == 2) count += 50;
            return count;
        }
        public int[] Mode2()
        {
            int row, col;
            int[,] Grade = new int[15, 15];
            int[] ans = new int[2];
            if (ComputerStep == 0)
            {
                Random r = new();
                row = r.Next(6, 8);
                col = r.Next(6, 8);
                while (Cells[row, col] != null)
                {
                    row = r.Next(6, 8);
                    col = r.Next(6, 8);
                }
                ans[0] = row;
                ans[1] = col;
            }
            else
            {
                //檢查黑子白子附近
                for (int i = 0; i < 15; i++)
                {
                    for (int j = 0; j < 15; j++)
                    {
                        if (Cells[i, j] != null)
                        {
                            if (i - 1 >= 0 && j - 1 >= 0)
                                Grade[i - 1, j - 1] += 1;
                            if (i - 1 >= 0)
                                Grade[i - 1, j] += 1;
                            if (i - 1 >= 0 && j + 1 < 15)
                                Grade[i - 1, j + 1] += 1;
                            if (j - 1 >= 0)
                                Grade[i, j - 1] += 1;
                            if (j + 1 < 15)
                                Grade[i, j + 1] += 1;
                            if (i + 1 < 15 && j - 1 >= 0)
                                Grade[i + 1, j - 1] += 1;
                            if (i + 1 < 15)
                                Grade[i + 1, j] += 1;
                            if (i + 1 < 15 && j + 1 < 15)
                                Grade[i + 1, j + 1] += 1;
                        }
                    }
                }
                for (int i = 0; i < 15; i++)
                {
                    for (int j = 0; j < 15; j++)
                    {
                        string player = Cells[i, j];
                        //白子黑子連線加分
                        if (player == "C" || player == "A")
                        {
                            int[] results = CheckLL(i, j, player);
                            int offset = 1;
                            while (offset <= 5)
                            {
                                //上
                                if (i - offset >= 0)
                                    Grade[i - offset, j] += results[0];
                                //下
                                if (i + offset < 15)
                                    Grade[i + offset, j] += results[1];
                                //左
                                if (j - offset >= 0)
                                    Grade[i, j - offset] += results[2];
                                //右
                                if (j + offset < 15)
                                    Grade[i, j + offset] += results[3];
                                //左上
                                if (i - offset >= 0 && j - offset >= 0)
                                    Grade[i - offset, j - offset] += results[4];
                                //左下
                                if (i + offset < 15 && j - offset >= 0)
                                    Grade[i + offset, j - offset] += results[5];
                                //右上
                                if (i - offset >= 0 && j + offset < 15)
                                    Grade[i - offset, j + offset] += results[6];
                                //右下
                                if (i + offset < 15 && j + offset < 15)
                                    Grade[i + offset, j + offset] += results[7];
                                offset++;
                            }
                        }
                    }
                }
                int max = 0;
                int ans0 = 0, ans1 = 0;
                for (int i = 0; i < 15; i++)
                {
                    for (int j = 0; j < 15; j++)
                    {
                        if (Cells[i, j] != null) Grade[i, j] = 0;
                        if (Grade[i, j] > max)
                        {

                            max = Grade[i, j]; // 更新最大值
                            ans0 = i;
                            ans1 = j;
                        }
                    }
                }
                ans[0] = ans0;
                ans[1] = ans1;
            }
            SendGrade(Grade);
            ComputertStepLog[ComputerStep] = (ans[0], ans[1]);
            ComputerStep++;
            ComputerLastStep = (ans[0], ans[1]);
            return ans;
        }
        public async Task SendGrade(int[,] Grade)
        {
            string jsonString = JsonConvert.SerializeObject(Grade);
            await Clients.All.SendAsync("Test1", jsonString);
        }
        public void ComputerToPlayer()
        {
            var computer = playerLists.Where(m => m.id == "Computer").FirstOrDefault();
            playerLists.Remove(computer);
            ComputerPlay = false;
        }
        public bool CheckDirection(int row, int col)
        {
            return row >= 0 && row < 15 && col >= 0 && col < 15 && Cells[row, col] == null;
        }
        public int error = 0;
        public int[] walkMode(int workmode, int cls1, int cls2)
        {
            if (error >= 4)
            {
                ComputerStep--;
                error = 0;
                cls1 = ComputertStepLog[ComputerStep].Item1;
                cls2 = ComputertStepLog[ComputerStep].Item2;
            }
            int[] ans = new int[2];
            int row, col;
            switch (workmode)
            {
                case 1:
                    row = cls1 + 1;
                    col = cls2 + 0;
                    if (CheckDirection(row, col))
                    {
                        ans[0] = row;
                        ans[1] = col;
                    }
                    else
                    {
                        row = cls1 - 1;
                        col = cls2 + 0;
                        if (CheckDirection(row, col))
                        {
                            ans[0] = row;
                            ans[1] = col;
                        }
                        else
                        {
                            error++;
                            ans = walkMode(workmode + 1, cls1, cls2);
                        }
                    }
                    break;
                case 2:
                    row = cls1 + 0;
                    col = cls2 + 1;
                    if (CheckDirection(row, col))
                    {
                        ans[0] = row;
                        ans[1] = col;
                    }
                    else
                    {
                        row = cls1 + 0;
                        col = cls2 - 1;
                        if (CheckDirection(row, col))
                        {
                            ans[0] = row;
                            ans[1] = col;
                        }
                        else
                        {
                            error++;
                            ans = walkMode(workmode + 1, cls1, cls2);
                        }
                    }
                    break;
                case 3:
                    row = cls1 + 1;
                    col = cls2 + 1;
                    if (CheckDirection(row, col))
                    {
                        ans[0] = row;
                        ans[1] = col;
                    }
                    else
                    {
                        row = cls1 - 1;
                        col = cls2 - 1;
                        if (CheckDirection(row, col))
                        {
                            ans[0] = row;
                            ans[1] = col;
                        }
                        else
                        {
                            error++;
                            ans = walkMode(workmode + 1, cls1, cls2);
                        }
                    }
                    break;
                case 4:
                    row = cls1 + 1;
                    col = cls2 - 1;
                    if (CheckDirection(row, col))
                    {
                        ans[0] = row;
                        ans[1] = col;
                    }
                    else
                    {
                        row = cls1 - 1;
                        col = cls2 + 1;
                        if (CheckDirection(row, col))
                        {
                            ans[0] = row;
                            ans[1] = col;
                        }
                        else
                        {
                            error++;
                            ans = walkMode(workmode + 1, cls1, cls2);
                        }
                    }
                    break;
                default:
                    ans = walkMode(1, cls1, cls2);
                    break;
            }

            return ans;
        }
    }
}