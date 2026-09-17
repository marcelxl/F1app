using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using F1app.Models;
using F1app.Services;
using Microsoft.Maui.Networking;

namespace F1app.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly F1Service _service = new();
    private bool _isBusy;
    private F1Weekend? _currentWeekend;
    private F1Weekend? _activeWeekend;
    private int _activeRaceIndex = -1;
    private bool _syncStarted;
    private bool _isOfflineMode;
    private int _refreshInProgress;

    public MainViewModel()
    {
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    private ObservableCollection<F1Weekend> _weekends = new();

    public ObservableCollection<F1Weekend> Weekends
    {
        get => _weekends;
        private set
        {
            _weekends = value;
            OnPropertyChanged();
        }
    }

    public bool IsOfflineMode
    {
        get => _isOfflineMode;
        private set
        {
            if (_isOfflineMode == value) return;
            _isOfflineMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOffline));
        }
    }

    public bool IsOffline
    {
        get => IsOfflineMode;
        private set => IsOfflineMode = value;
    }

    public F1Weekend? ActiveWeekend
    {
        get => _activeWeekend;
        private set
        {
            _activeWeekend = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsJumpToCurrentRaceVisible));
            OnPropertyChanged(nameof(ReturnButtonText));
        }
    }

    public int ActiveRaceIndex => _activeRaceIndex;

    public F1Weekend? CurrentWeekend
    {
        get => _currentWeekend;
        private set
        {
            _currentWeekend = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsJumpToCurrentRaceVisible));
            UpdateSelectedIndicator();
        }
    }

    public bool IsJumpToCurrentRaceVisible =>
        ActiveWeekend != null && CurrentWeekend != ActiveWeekend;

    public string ReturnButtonText => IsActiveWeekend
        ? "🎯 Naar huidige race"
        : "🎯 Naar eerstvolgende race";

    private bool IsActiveWeekend =>
        ActiveWeekend != null &&
        ActiveWeekend.Sessions.Any(session => session.StartTimeUtc <= DateTime.UtcNow) &&
        ActiveWeekend.RaceStartUtc.AddHours(4) >= DateTime.UtcNow;

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    public async Task InitializeAsync()
    {
        if (_syncStarted) return;

        _syncStarted = true;
        var cacheStopwatch = Stopwatch.StartNew();
        var cachedData = await _service.LoadCachedWeekendsAsync();
        if (cachedData.Count > 0)
        {
            ApplySchedule(cachedData);
            IsBusy = false;
        }
        else
        {
            IsBusy = true;
        }
        cacheStopwatch.Stop();
        Debug.WriteLine($"[F1PERF] Cache loaded and bound in {cacheStopwatch.ElapsedMilliseconds}ms");

        try
        {
            var networkAccess = Connectivity.Current.NetworkAccess;
            Debug.WriteLine($"[F1NET] Network status changed: {networkAccess}");
            var hasInternet = networkAccess == NetworkAccess.Internet;
            IsOfflineMode = !hasInternet && cachedData.Count > 0;

            if (!hasInternet)
            {
                IsBusy = false;
                return;
            }

            if (cachedData.Count > 0)
            {
                IsBusy = false;
                _ = SynchronizeInBackgroundAsync();
                return;
            }

            _ = SynchronizeInBackgroundAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Schedule initialization failed: {ex}");
            IsBusy = false;
        }
    }

    private async Task SynchronizeInBackgroundAsync()
    {
        if (Interlocked.Exchange(ref _refreshInProgress, 1) == 1)
        {
            return;
        }

        var refreshStopwatch = Stopwatch.StartNew();
        try
        {
            var latestData = await _service.FetchCurrentSeasonWeekendsAsync();
            refreshStopwatch.Stop();
            Debug.WriteLine($"[F1PERF] Network update received in {refreshStopwatch.ElapsedMilliseconds}ms");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!AreSchedulesEqual(latestData))
                {
                    ApplySchedule(latestData);
                }

                IsOffline = false;
                IsBusy = false;
                Debug.WriteLine($"[F1PERF] Network update processed in {refreshStopwatch.ElapsedMilliseconds}ms");
            });
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"Schedule refresh failed: {ex.Message}");
            SetOfflineAfterRefreshFailure();
        }
        catch (SocketException ex)
        {
            Debug.WriteLine($"Schedule network failure: {ex.Message}");
            SetOfflineAfterRefreshFailure();
        }
        catch (TaskCanceledException ex)
        {
            Debug.WriteLine($"Schedule refresh timed out: {ex.Message}");
            SetOfflineAfterRefreshFailure();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Schedule refresh failed: {ex}");
            SetOfflineAfterRefreshFailure();
        }
        finally
        {
            Volatile.Write(ref _refreshInProgress, 0);
        }
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        Debug.WriteLine($"[F1NET] Network status changed: {e.NetworkAccess}");
        var isOnline = e.NetworkAccess == NetworkAccess.Internet;
        MainThread.BeginInvokeOnMainThread(() => IsOffline = !isOnline);

        if (isOnline && _syncStarted)
        {
            _ = SynchronizeInBackgroundAsync();
        }
    }

    private void SetOfflineAfterRefreshFailure()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsOffline = true;
            IsBusy = false;
        });
    }

    private void ApplySchedule(IEnumerable<F1Weekend> schedule)
    {
        Weekends = new ObservableCollection<F1Weekend>(schedule.Where(item => item != null));

        var now = DateTime.UtcNow;
        foreach (var session in Weekends.SelectMany(weekend => weekend.Sessions))
        {
            session.IsNext = false;
        }

        var nextSession = Weekends
            .SelectMany(weekend => weekend.Sessions)
            .Where(session => session.StartTimeUtc > now)
            .OrderBy(session => session.StartTimeUtc)
            .FirstOrDefault();
        if (nextSession != null)
        {
            nextSession.IsNext = true;
        }

        _activeRaceIndex = Weekends
            .Select((weekend, index) => new { weekend, index })
            .FirstOrDefault(item => item.weekend.RaceStartUtc.AddHours(4) >= now)
            ?.index ?? -1;
        var activeWeekend = _activeRaceIndex >= 0 && _activeRaceIndex < Weekends.Count
            ? Weekends[_activeRaceIndex]
            : null;
        _activeWeekend = activeWeekend;
        _currentWeekend = activeWeekend;
        UpdateActiveIndicator();
        OnPropertyChanged(nameof(ActiveRaceIndex));
        OnPropertyChanged(nameof(ActiveWeekend));
        OnPropertyChanged(nameof(IsJumpToCurrentRaceVisible));
        OnPropertyChanged(nameof(ReturnButtonText));
        OnPropertyChanged(nameof(CurrentWeekend));
        UpdateSelectedIndicator();
    }

    private bool AreSchedulesEqual(IReadOnlyList<F1Weekend> latest)
    {
        if (latest.Count != Weekends.Count) return false;
        return latest.Select((weekend, index) => new { weekend, index }).All(item =>
            item.weekend.Round == Weekends[item.index].Round &&
            item.weekend.RaceStartUtc == Weekends[item.index].RaceStartUtc &&
            item.weekend.Sessions.Count == Weekends[item.index].Sessions.Count);
    }

    public void SelectWeekendAt(int index)
    {
        if (index >= 0 && index < Weekends.Count)
        {
            CurrentWeekend = Weekends[index];
        }
    }

    private void UpdateActiveIndicator()
    {
        for (var index = 0; index < Weekends.Count; index++)
        {
            Weekends[index].IsActiveSeasonRace = index == _activeRaceIndex &&
                                                  _activeRaceIndex >= 0 &&
                                                  _activeRaceIndex < Weekends.Count;
        }
    }

    private void UpdateSelectedIndicator()
    {
        for (var index = 0; index < Weekends.Count; index++)
        {
            Weekends[index].IsSelected = ReferenceEquals(Weekends[index], _currentWeekend);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}