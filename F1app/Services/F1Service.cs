using System.Net.Http.Json;
using F1app.Models;

namespace F1app.Services;

public class F1Service
{
    private readonly HttpClient _httpClient = new();

    public async Task<List<F1Weekend>> GetCurrentSeasonWeekendsAsync()
    {
        var url = "https://api.jolpi.ca/ergast/f1/current.json";
        var response = await _httpClient.GetFromJsonAsync<JolpicaResponse>(url);

        if (response?.MRData?.RaceTable?.Races == null)
            return new List<F1Weekend>();

        var weekends = new List<F1Weekend>();

        foreach (var r in response.MRData.RaceTable.Races)
        {
            var raceStart = ParseUtc(r.Date, r.Time);
            var weekend = new F1Weekend
            {
                Round = r.Round,
                GrandPrixName = r.RaceName,
                CircuitName = r.Circuit?.CircuitName ?? string.Empty,
                RaceStartUtc = raceStart
            };

            AddSession(weekend.Sessions, "1e Vrije Training", r.FirstPractice);
            AddSession(weekend.Sessions, "2e Vrije Training", r.SecondPractice);
            AddSession(weekend.Sessions, "Sprint Kwalificatie", r.SprintQualifying);
            AddSession(weekend.Sessions, "Sprint Race", r.Sprint);
            AddSession(weekend.Sessions, "3e Vrije Training", r.ThirdPractice);
            AddSession(weekend.Sessions, "Kwalificatie", r.Qualifying);

            weekend.Sessions.Add(new F1Session
            {
                Name = "Grand Prix (Race)",
                StartTimeUtc = raceStart
            });

            // Sorteer sessies chronologisch
            weekend.Sessions = weekend.Sessions.OrderBy(s => s.StartTimeUtc).ToList();
            weekends.Add(weekend);
        }

        return weekends;
    }

    private static void AddSession(List<F1Session> list, string name, SessionDto? dto)
    {
        if (dto != null && !string.IsNullOrEmpty(dto.Date))
        {
            list.Add(new F1Session
            {
                Name = name,
                StartTimeUtc = ParseUtc(dto.Date, dto.Time)
            });
        }
    }

    private static DateTime ParseUtc(string date, string? time)
    {
        var cleanTime = string.IsNullOrEmpty(time) ? "12:00:00Z" : time;
        if (DateTimeOffset.TryParse($"{date}T{cleanTime}", out var parsed))
            return parsed.UtcDateTime;

        return DateTime.UtcNow;
    }
}