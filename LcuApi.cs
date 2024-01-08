using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

class LcuApi
{
    private HttpClient httpClient;
    private WebSocket webSocket;
    public LcuApi()
    {
        var leagueProcess = Process.GetProcessesByName("LeagueClientUx").First();
        if (leagueProcess is null || leagueProcess.MainModule is null)
        {
            throw new Exception("League Client not found");
        }
        var lockfilePath = leagueProcess.MainModule.FileName.Replace("LeagueClientUx.exe", "lockfile");
        using (var fs = new FileStream(lockfilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var sr = new StreamReader(fs, Encoding.Default))
        {
            var lockfile = sr.ReadToEnd().Split(':');
            var port = lockfile[2];
            var password = lockfile[3];
            httpClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            });
            httpClient.BaseAddress = new Uri($"https://127.0.0.1:{port}");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes($"riot:{password}"))}");

            webSocket = new WebSocket($"wss://127.0.0.1:{port}/");
            webSocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            webSocket.SslConfiguration.ServerCertificateValidationCallback = (message, cert, chain, errors) => true;
            webSocket.OnMessage += HandleMessage;
            webSocket.SetCredentials("riot", password, true);
            webSocket.Connect();
        }
    }

    public void WSSend(string message)
    {
        webSocket.Send(message);
    }

    private Dictionary<string, Action<object>> wsEvents = new Dictionary<string, Action<object>>();

    private void HandleMessage(object? sender, MessageEventArgs e)
    {
        if (e.Data == "") return;
        var data = JsonConvert.DeserializeObject<JArray>(e.Data);
        if (data is null) return;
        var data2 = JsonConvert.DeserializeObject<WSEventData>(data[2].ToString());
        if (data2 is null) return;
        if (wsEvents.ContainsKey(data2.Uri))
        {
            wsEvents[data2.Uri](data2.Data);
        }
    }

    // TODO: Remove need for uri
    public void WSReceive(string eventName, string uri, Action<object> action)
    {
        Console.WriteLine($"[5, \"{eventName}\"]");
        webSocket.Send($"[5, \"{eventName}\"]");
        wsEvents.Add(uri, action);
    }

    public bool GetIngame(string path, out string content)
    {
        try 
        {
            var response = httpClient.GetAsync($"https://127.0.0.1:2999/liveclientdata/{path}").Result;
            content = response.Content.ReadAsStringAsync().Result;
            if (content.Contains("errorCode")) 
            {
                content = "";
                return true;
            }
            return true;
        }
        catch
        {
            content = "";
            return false;
        }
    }

    public async Task<string> Get(string path)
    {
        var response = await httpClient.GetAsync(path);
        var content = await response.Content.ReadAsStringAsync();
        return content;
    }

    public bool GetItems(Player player, out List<Item> items)
    {
        if (!GetIngame($"playeritems?summonerName={player.SummonerName}", out var content))
        {
            items = new List<Item>();
            return false;
        }
        items = JsonConvert.DeserializeObject<List<Item>>(content) ?? new List<Item>();
        return true;
    }
    
    public bool GetPlayerList(out List<Player> playerlist)
    {
        if(!GetIngame("playerlist", out var content))
        {
            playerlist = new List<Player>();
            return false;
        }
        playerlist = JsonConvert.DeserializeObject<List<Player>>(content) ?? new List<Player>();
        return true;
    }

    public async Task SetupItems()
    {
        var response = await Get("/lol-game-data/assets/v1/items.json");
        var items = JsonConvert.DeserializeObject<List<Item>>(response) ?? new List<Item>();
        
        Item.totalPrices = new Dictionary<int, int>();
        foreach (var item in items)
        {
            if (item.ID == 0) continue;
            Item.totalPrices.Add(item.ID, item.PriceTotal);
        }
    }
}