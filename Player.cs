using Newtonsoft.Json;

public class Player
{
    [JsonProperty("summonerName")]
    public string SummonerName { get; set; }

    [JsonProperty("position")]
    public string Position { get; set; }

    [JsonProperty("team")]
    public string Team { get; set; }

    // console position
    public int left { get; set;}
    public int top { get; set; }

    public Player? rival { get; set; }
    public int totalGold { get; set; }

    public Player()
    {
        SummonerName = "";
        Position = "";
        Team = "";
        left = 0;
        top = 0;
        totalGold = 0;
    }
}