using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using F1app.Models;
using F1app.Services;

namespace F1app.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly F1Service _service = new();
    private bool _isBusy;
    private F1Weekend? _currentWeekend;
    private F1Weekend? _activeWeekend;
    private int _activeRaceIndex = -1;

    public ObservableCollection<F1Weekend> Weekends { get; } = new();

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
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            ActiveWeekend = null;
            CurrentWeekend = null;
            Weekends.Clear();
            var data = await _service.GetCurrentSeasonWeekendsAsync();
            foreach (var item in data)
            {
                if (item != null)
                {
                    Weekends.Add(item);
                }
            }

            var now = DateTime.UtcNow;
            _activeRaceIndex = -1;
            if (Weekends.Count > 0)
            {
                if (now < Weekends[0].RaceStartUtc)
                {
                    _activeRaceIndex = 0;
                }
                else
                {
                    _activeRaceIndex = Weekends
                        .Select((weekend, index) => new { weekend, index })
                        .FirstOrDefault(item => item.weekend.RaceStartUtc.AddHours(4) >= now)
                        ?.index ?? -1;
                }
            }

            ActiveWeekend = _activeRaceIndex >= 0 && _activeRaceIndex < Weekends.Count
                ? Weekends[_activeRaceIndex]
                : null;
            UpdateActiveIndicator();
            OnPropertyChanged(nameof(ActiveRaceIndex));
            CurrentWeekend = ActiveWeekend;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Race data initialization failed: {ex}");
            ActiveWeekend = null;
            CurrentWeekend = null;
            _activeRaceIndex = -1;
            OnPropertyChanged(nameof(ActiveRaceIndex));
        }
        finally
        {
            IsBusy = false;
        }
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