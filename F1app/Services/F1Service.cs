using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using F1app.Models;

namespace F1app.Services;

public class F1Service
{
    private const string CacheFileName = "f1_schedule_cache.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(3)
    };

    public async Task<List<F1Weekend>> LoadCachedWeekendsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cachePath = Path.Combine(FileSystem.Current.AppDataDirectory, CacheFileName);
            var json = await File.ReadAllTextAsync(cachePath, cancellationToken).ConfigureAwait(false);
            return await Task.Run(
                () => JsonSerializer.Deserialize<List<F1Weekend>>(json, JsonOptions) ?? new List<F1Weekend>(),
                cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            return new List<F1Weekend>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Schedule cache read failed: {ex}");
            return new List<F1Weekend>();
        }
    }

    public async Task<List<F1Weekend>> FetchCurrentSeasonWeekendsAsync(CancellationToken cancellationToken = default)
    {
        var url = "https://api.jolpi.ca/ergast/f1/current.json";
        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(TimeSpan.FromSeconds(3));
        var requestTask = _httpClient.GetFromJsonAsync<JolpicaResponse>(url, timeoutCancellation.Token);
        var completedTask = await Task.WhenAny(
            requestTask,
            Task.Delay(TimeSpan.FromSeconds(3), cancellationToken)).ConfigureAwait(false);
        if (completedTask != requestTask)
        {
            timeoutCancellation.Cancel();
            throw new TimeoutException("Jolpica request exceeded the 3 second timeout.");
        }

        var response = await requestTask.ConfigureAwait(false)
            ?? throw new HttpRequestException("Jolpica returned an empty response.");

        if (response?.MRData?.RaceTable?.Races == null)
            throw new HttpRequestException("Jolpica returned no race data.");

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

        try
        {
            await SaveCacheAsync(weekends, cancellationToken);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Schedule cache write failed: {ex}");
        }
        return weekends;
    }

    private static async Task SaveCacheAsync(List<F1Weekend> weekends, CancellationToken cancellationToken)
    {
        var cachePath = Path.Combine(FileSystem.Current.AppDataDirectory, CacheFileName);
        var temporaryPath = $"{cachePath}.tmp";
        var json = JsonSerializer.Serialize(weekends, JsonOptions);

        try
        {
            await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
            File.Move(temporaryPath, cachePath, true);
        }
        catch
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (IOException cleanupException)
            {
                Debug.WriteLine($"Schedule cache cleanup failed: {cleanupException}");
            }

            throw;
        }
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