using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;

class LcuApi
{
    private HttpClient httpClient;
    // TODO: Add websocket
    
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
        }
    }

    public async Task<string> GetIngame(string path)
    {
        var response = await httpClient.GetAsync($"https://127.0.0.1:2999/liveclientdata/{path}");
        var content = await response.Content.ReadAsStringAsync();
        return content;
    }

    public async Task<string> Get(string path)
    {
        var response = await httpClient.GetAsync(path);
        var content = await response.Content.ReadAsStringAsync();
        return content;
    }

    public async Task<List<Item>> GetItems(Player player)
    {
        var response = await GetIngame($"playeritems?summonerName={player.SummonerName}");
        return JsonConvert.DeserializeObject<List<Item>>(response) ?? new List<Item>();
    }
    
    public async Task<List<Player>> GetPlayerList()
    {
        var response = await GetIngame("playerlist");
        return JsonConvert.DeserializeObject<List<Player>>(response) ?? new List<Player>();
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