using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

const string basicLayout = @"╔════════════════╦══════╦╦══════╦════════════════╗ 
║                ║      ║║      ║                ║
╠╦╦╦╦╦╦╦╦╦╦╦╦╦╦╦╦╬╦╦╦╦╦╦╬╬╦╦╦╦╦╦╬╦╦╦╦╦╦╦╦╦╦╦╦╦╦╦╦╣
╠╩╩╩╩╩╩╩╩╩╩╩╩╩╩╩╩╬╩╩╩╩╩╩╬╬╩╩╩╩╩╩╬╩╩╩╩╩╩╩╩╩╩╩╩╩╩╩╩╣
║                ║      ║║      ║                ║
╠════════════════╬══════╬╬══════╬════════════════╣
║                ║      ║║      ║                ║
╠════════════════╬══════╬╬══════╬════════════════╣
║                ║      ║║      ║                ║
╠════════════════╬══════╬╬══════╬════════════════╣
║                ║      ║║      ║                ║
╠════════════════╬══════╬╬══════╬════════════════╣
║                ║      ║║      ║                ║
╚════════════════╩══════╩╩══════╩════════════════╝";

/*

top values:
team = 1
top = 4
jungle = 6
mid = 8
adc = 10
support = 12

left values:
order_name = 1
order_gold = 18

chaos_name = 33
chaos_gold = 26

*/

var top_team = 1;
var top_top = 4;
var top_jungle = 6;
var top_mid = 8;
var top_adc = 10;
var top_support = 12;

var left_order_name = 1;
var left_order_gold = 18;

var left_chaos_name = 33;
var left_chaos_gold = 26;

LcuApi lcuApi = new LcuApi();
await lcuApi.SetupItems();

lcuApi.WSReceive("OnJsonApiEvent_lol-gameflow_v1_gameflow-phase", "/lol-gameflow/v1/gameflow-phase", async (data) =>
{
    var str = data as string;
    if (str == "InProgress")
    {
        await StartThing();
    }
});

// --ForceStart can be used for replays :)
if(await lcuApi.Get("/lol-gameflow/v1/gameflow-phase") == "\"InProgress\"" || args.Contains("--ForceStart")) await StartThing();

Console.Clear();
Console.WriteLine("Waiting for game to start...");

while(true) await Task.Delay(1000);

async Task StartThing()
{
    Console.Clear();
    Console.WriteLine(basicLayout);
    if (!lcuApi.GetPlayerList(out var playerList)) return; // Exit("No player list found");

    while (playerList.Count <= 0)
    {
        await Task.Delay(1000);
        if (!lcuApi.GetPlayerList(out playerList)) return; // Exit("No player list found");
    }
    Console.WriteLine("Player list found, amount: " + playerList.Count);

    foreach (var player in playerList)
    {
        Console.WriteLine($"Player: {player.SummonerName} - {player.Position} - {player.Team}");
    }

    Console.SetCursorPosition(left_order_name, top_team);
    Console.Write("ORDER");

    Console.SetCursorPosition(left_chaos_name, top_team);
    Console.Write("CHAOS");
    Dictionary<string, List<int>> teamPositions = new Dictionary<string, List<int>>();
    foreach (var player in playerList)
    {
        switch (player.Position)
        {
            case "TOP":
                player.top = top_top;
                break;
            case "JUNGLE":
                player.top = top_jungle;
                break;
            case "MIDDLE":
                player.top = top_mid;
                break;
            case "BOTTOM":
                player.top = top_adc;
                break;
            case "UTILITY":
                player.top = top_support;
                break;
            default:
                var allPlayerTopValues = new int[] { top_top, top_jungle, top_mid, top_adc, top_support };
                var usedTopValues = teamPositions.ContainsKey(player.Team) ? teamPositions[player.Team] : new List<int>();
                var availableTopValues = allPlayerTopValues.Except(usedTopValues);
                player.top = availableTopValues.First();
                break;
        }

        if (!teamPositions.ContainsKey(player.Team))
        {
            teamPositions.Add(player.Team, new List<int>());
        }
        teamPositions[player.Team].Add(player.top);

        switch (player.Team)
        {
            case "ORDER":
                player.left = left_order_name;
                break;
            case "CHAOS":
                player.left = left_chaos_name;
                break;
            default:
                player.left = 0;
                break;
        }

        Console.SetCursorPosition(player.left, player.top);
        Console.Write(player.SummonerName);
    }

    foreach (var player in playerList)
    {
        player.rival = playerList.Find(p => p.top == player.top && p.Team != player.Team);
    }

    bool shouldExit = false;

    while (!shouldExit)
    {
        foreach (var player in playerList)
        {
            if(!lcuApi.GetItems(player, out var items)) shouldExit = true; // Exit("No items found");
            player.totalGold = items.Sum(item => item.totalGold);
        }

        var chaos_gold = playerList.Where(p => p.Team == "CHAOS").Sum(p => p.totalGold);
        var order_gold = playerList.Where(p => p.Team == "ORDER").Sum(p => p.totalGold);

        foreach (var player in playerList)
        {
            var left = player.Team == "ORDER" ? left_order_gold : left_chaos_gold;
            Console.SetCursorPosition(left, player.top);
            if (player.rival != null)
            {
                Console.ForegroundColor = player.totalGold > player.rival.totalGold ? ConsoleColor.Green : ConsoleColor.Red;
                Console.ForegroundColor = player.totalGold == player.rival.totalGold ? ConsoleColor.Yellow : Console.ForegroundColor;
            }
            Console.Write(player.totalGold);
            Console.ForegroundColor = ConsoleColor.White;
        }

        Console.SetCursorPosition(left_order_gold, top_team);
        Console.ForegroundColor = order_gold > chaos_gold ? ConsoleColor.Green : ConsoleColor.Red;
        Console.ForegroundColor = order_gold == chaos_gold ? ConsoleColor.Yellow : Console.ForegroundColor;
        Console.Write(order_gold);

        Console.SetCursorPosition(left_chaos_gold, top_team);
        Console.ForegroundColor = chaos_gold > order_gold ? ConsoleColor.Green : ConsoleColor.Red;
        Console.ForegroundColor = chaos_gold == order_gold ? ConsoleColor.Yellow : Console.ForegroundColor;
        Console.Write(chaos_gold);

        Console.ForegroundColor = ConsoleColor.White;

        await Task.Delay(1000);
    }

    Console.Clear();
    Console.WriteLine("Waiting for game to start...");
}