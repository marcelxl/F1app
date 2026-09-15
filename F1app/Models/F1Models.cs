using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace F1app.Models;

public class F1Session
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartTimeUtc { get; set; }
    public bool IsNext { get; set; }
    public DateTime LocalStartTime => StartTimeUtc.ToLocalTime();
    public bool IsLive => StartTimeUtc <= DateTime.UtcNow && DateTime.UtcNow <= StartTimeUtc.AddHours(2);
    public bool IsPast => StartTimeUtc.AddHours(2) < DateTime.UtcNow;
    public string FormattedDay => LocalStartTime.ToString("ddd d MMM", new System.Globalization.CultureInfo("nl-NL"));
    public string FormattedTime => LocalStartTime.ToString("HH:mm");
}

public class F1Weekend : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _isActiveSeasonRace;

    public string GrandPrixName { get; set; } = string.Empty;
    public string CircuitName { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string CountryFlag => CountryFlagResolver.Resolve(CountryName, CircuitName, GrandPrixName);
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public bool IsActiveSeasonRace
    {
        get => _isActiveSeasonRace;
        set
        {
            if (_isActiveSeasonRace == value) return;
            _isActiveSeasonRace = value;
            OnPropertyChanged();
        }
    }

    public string Round { get; set; } = string.Empty;
    public string Broadcaster => "Viaplay / F1 TV Pro";
    public DateTime RaceStartUtc { get; set; }
    public List<F1Session> Sessions { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

    [JsonPropertyName("Location")]
    public LocationDto? Location { get; set; }
}

public class LocationDto
{
    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;
}

public class SessionDto
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public string? Time { get; set; }
}