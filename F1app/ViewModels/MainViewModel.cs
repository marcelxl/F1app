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

    public ObservableCollection<F1Weekend> Weekends { get; } = new();

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
            Weekends.Clear();
            var data = await _service.GetUpcomingWeekendsAsync();
            foreach (var item in data)
            {
                Weekends.Add(item);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}