using System.Text.Json.Serialization;

namespace F1app.Models;

public class F1Session
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartTimeUtc { get; set; }
    public DateTime LocalStartTime => StartTimeUtc.ToLocalTime();
    public string FormattedDay => LocalStartTime.ToString("ddd d MMM", new System.Globalization.CultureInfo("nl-NL"));
    public string FormattedTime => LocalStartTime.ToString("HH:mm");
}

public class F1Weekend
{
    public string GrandPrixName { get; set; } = string.Empty;
    public string CircuitName { get; set; } = string.Empty;
    public string Round { get; set; } = string.Empty;
    public string Broadcaster => "Viaplay / F1 TV Pro";
    public DateTime RaceStartUtc { get; set; }
    public List<F1Session> Sessions { get; set; } = new();
}

// DTO-klassen voor deserialisatie van Jolpica API
public class JolpicaResponse
{
    [JsonPropertyName("MRData")]
    public MRData? MRData { get; set; }
}

public class MRData
{
    [JsonPropertyName("RaceTable")]
    public RaceTable? RaceTable { get; set; }
}

public class RaceTable
{
    [JsonPropertyName("Races")]
    public List<RaceDto>? Races { get; set; }
}

public class RaceDto
{
    [JsonPropertyName("round")]
    public string Round { get; set; } = string.Empty;

    [JsonPropertyName("raceName")]
    public string RaceName { get; set; } = string.Empty;

    [JsonPropertyName("Circuit")]
    public CircuitDto? Circuit { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public string? Time { get; set; }

    [JsonPropertyName("FirstPractice")]
    public SessionDto? FirstPractice { get; set; }

    [JsonPropertyName("SecondPractice")]
    public SessionDto? SecondPractice { get; set; }

    [JsonPropertyName("ThirdPractice")]
    public SessionDto? ThirdPractice { get; set; }

    [JsonPropertyName("Qualifying")]
    public SessionDto? Qualifying { get; set; }

    [JsonPropertyName("SprintQualifying")]
    public SessionDto? SprintQualifying { get; set; }

    [JsonPropertyName("Sprint")]
    public SessionDto? Sprint { get; set; }
}

public class CircuitDto
{
    [JsonPropertyName("circuitName")]
    public string CircuitName { get; set; } = string.Empty;
}

public class SessionDto
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public string? Time { get; set; }
}