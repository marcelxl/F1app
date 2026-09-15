using System.Collections.ObjectModel;
using System.ComponentModel;
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
        }
    }

    public bool IsJumpToCurrentRaceVisible =>
        ActiveWeekend != null && CurrentWeekend != ActiveWeekend;

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
                Weekends.Add(item);
            }

            var now = DateTime.UtcNow;
            ActiveWeekend = Weekends.FirstOrDefault(weekend =>
                weekend.RaceStartUtc.AddHours(4) >= now)
                ?? Weekends.LastOrDefault();
            _activeRaceIndex = ActiveWeekend == null ? -1 : Weekends.IndexOf(ActiveWeekend);
            OnPropertyChanged(nameof(ActiveRaceIndex));
            CurrentWeekend = ActiveWeekend;
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

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}