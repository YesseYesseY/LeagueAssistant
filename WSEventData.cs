using Newtonsoft.Json;

public class WSEventData
{
    [JsonProperty("uri")]
    public string Uri { get; set; }

    [JsonProperty("eventType")]
    public string EventType { get; set; }

    [JsonProperty("data")]
    public object Data { get; set; }

    WSEventData()
    {
        Uri = "";
        EventType = "";
        Data = "";
    }
}