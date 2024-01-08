﻿using System.Runtime.InteropServices;
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

if(await lcuApi.Get("/lol-gameflow/v1/gameflow-phase") == "\"InProgress\"") await StartThing();

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

    Console.SetCursorPosition(1, 1);
    Console.Write("ORDER");

    Console.SetCursorPosition(33, 1);
    Console.Write("CHAOS");
    Dictionary<string, List<int>> teamPositions = new Dictionary<string, List<int>>();
    foreach (var player in playerList)
    {
        switch (player.Position)
        {
            case "TOP":
                player.top = 4;
                break;
            case "JUNGLE":
                player.top = 6;
                break;
            case "MIDDLE":
                player.top = 8;
                break;
            case "BOTTOM":
                player.top = 10;
                break;
            case "UTILITY":
                player.top = 12;
                break;
            default:
                var allPlayerTopValues = new int[] { 4, 6, 8, 10, 12 };
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
                player.left = 1;
                break;
            case "CHAOS":
                player.left = 33;
                break;
            default:
                player.left = 1;
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
            var left = player.Team == "ORDER" ? 18 : 26;
            Console.SetCursorPosition(left, player.top);
            if (player.rival != null)
            {
                Console.ForegroundColor = player.totalGold > player.rival.totalGold ? ConsoleColor.Green : ConsoleColor.Red;
                Console.ForegroundColor = player.totalGold == player.rival.totalGold ? ConsoleColor.Yellow : Console.ForegroundColor;
            }
            Console.Write(player.totalGold);
            Console.ForegroundColor = ConsoleColor.White;
        }

        Console.SetCursorPosition(18, 1);
        Console.ForegroundColor = order_gold > chaos_gold ? ConsoleColor.Green : ConsoleColor.Red;
        Console.ForegroundColor = order_gold == chaos_gold ? ConsoleColor.Yellow : Console.ForegroundColor;
        Console.Write(order_gold);

        Console.SetCursorPosition(26, 1);
        Console.ForegroundColor = chaos_gold > order_gold ? ConsoleColor.Green : ConsoleColor.Red;
        Console.ForegroundColor = chaos_gold == order_gold ? ConsoleColor.Yellow : Console.ForegroundColor;
        Console.Write(chaos_gold);

        Console.ForegroundColor = ConsoleColor.White;

        await Task.Delay(1000);
    }

    Console.Clear();
    Console.WriteLine("Waiting for game to start...");
}