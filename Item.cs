using Newtonsoft.Json;

public class Item
{
    public static Dictionary<int, int> totalPrices = new Dictionary<int, int>();

    [JsonProperty("price")]
    public int Price { get; set; }
    [JsonProperty("priceTotal")]
    public int PriceTotal { get; set; }

    [JsonProperty("itemID")]
    public int ItemID { get; set; }

    [JsonProperty("id")]
    public int ID { get; set; }

    [JsonProperty("count")]
    public int Count { get; set; }

    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    public int totalGold => totalPrices.ContainsKey(ItemID) ? totalPrices[ItemID] * Count : 0;

    public Item()
    {
        Price = 0;
        PriceTotal = 0;
        ItemID = 0;
        ID = 0;
        DisplayName = "";
    }
}