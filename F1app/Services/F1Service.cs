using System.Diagnostics;
using System.Net.Http.Json;
using F1app.Models;

namespace F1app.Services;

public class F1Service
{
    private readonly HttpClient _httpClient = new();

    public async Task<List<F1Weekend>> GetCurrentSeasonWeekendsAsync()
    {
        var url = "https://api.jolpi.ca/ergast/f1/current.json";
        JolpicaResponse? response;
        try
        {
            response = await _httpClient.GetFromJsonAsync<JolpicaResponse>(url);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Jolpica request failed: {ex}");
            return new List<F1Weekend>();
        }

        if (response?.MRData?.RaceTable?.Races == null)
            return new List<F1Weekend>();

        var weekends = new List<F1Weekend>();

        foreach (var r in response.MRData.RaceTable.Races)
        {
            if (r == null || !TryParseUtc(r.Date, r.Time, out var raceStart))
            {
                continue;
            }
            var weekend = new F1Weekend
            {
                Round = r.Round ?? string.Empty,
                GrandPrixName = r.RaceName ?? string.Empty,
                CircuitName = r.Circuit?.CircuitName ?? string.Empty,
                CountryName = r.Circuit?.Location?.Country ?? string.Empty,
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

            weekend.Sessions = weekend.Sessions.OrderBy(s => s.StartTimeUtc).ToList();
            weekends.Add(weekend);
        }

        var nextSession = weekends
            .SelectMany(weekend => weekend.Sessions)
            .Where(session => session.StartTimeUtc > DateTime.UtcNow)
            .OrderBy(session => session.StartTimeUtc)
            .FirstOrDefault();
        if (nextSession != null)
        {
            nextSession.IsNext = true;
        }

        return weekends;
    }

    private static void AddSession(List<F1Session> list, string name, SessionDto? dto)
    {
        if (dto != null && TryParseUtc(dto.Date, dto.Time, out var startTimeUtc))
        {
            list.Add(new F1Session
            {
                Name = name,
                StartTimeUtc = startTimeUtc
            });
        }
    }

    private static bool TryParseUtc(string? date, string? time, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(date))
        {
            return false;
        }

        var cleanTime = string.IsNullOrWhiteSpace(time) ? "12:00:00Z" : time.Trim();
        if (!DateTimeOffset.TryParse($"{date.Trim()}T{cleanTime}", out var parsed))
        {
            return false;
        }

        result = parsed.UtcDateTime;
        return true;
    }
}