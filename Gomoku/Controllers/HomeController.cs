using Gomoku.Models;
using Microsoft.AspNetCore.Mvc;
using System.Configuration;
using System.Diagnostics;

namespace Gomoku.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Index2()
        {
            return View();
        }
        public IActionResult Index3()
        {
            Random r = new Random();
            string[,] mine = new string[10, 10];
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    mine[i, j] = "0";
                }
            }
            for (int i = 0; i < 20; i++)
            {
                int Rrnd = r.Next(0, 10);
                int Crnd = r.Next(0, 10);
                mine[Rrnd, Crnd] = "X";
            }
            countMine(mine);
            TempData["mine"] = mine;
            return View();
        }
        public void countMine(string[,] mine)
        {
            (int, int)[] set = [
                (-1, -1),
                (-1, 0),
                (-1, 1),
                (0, -1),
                (0, 1),
                (1,-1 ),
                (1, 0),
                (1, 1)
            ];
            foreach (var i in set)
            {
                for (int r = 0; r < 10; r++)
                {
                    for (int c = 0; c < 10; c++)
                    {
                        var newRow = r + i.Item1;
                        var newCol = c + i.Item2;
                        if (newRow >= 0 && newRow < 10 && newCol >= 0 && newCol < 10 && mine[r, c]!="X" && mine[newRow, newCol] == "X")
                        {
                            int num = int.Parse(mine[r, c]) + 1;
                            mine[r, c] = num.ToString();
                        }
                    }
                }

            }

        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
