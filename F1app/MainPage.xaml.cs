using System.Diagnostics;
using F1app.Models;
using F1app.ViewModels;

namespace F1app;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _vm;

    public MainPage()
    {
        _vm = new MainViewModel();
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            await Task.Yield();
            _ = InitializeViewModelAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MainPage appearing failed: {ex}");
        }
    }

    private async Task InitializeViewModelAsync()
    {
        try
        {
            await _vm.InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MainPage data initialization failed: {ex}");
        }
    }

    private void OnJumpToCurrentRaceClicked(object? sender, EventArgs e)
    {
        try
        {
            if (_vm.ActiveRaceIndex >= 0)
            {
                RaceCarousel.ScrollTo(_vm.ActiveRaceIndex, animate: true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Race navigation failed: {ex}");
        }
    }

    private void OnCarouselPositionChanged(object? sender, PositionChangedEventArgs e)
    {
        try
        {
            _vm.SelectWeekendAt(e.CurrentPosition);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Carousel position update failed: {ex}");
        }
    }

    private void OnRaceIndicatorTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            if (sender is TapGestureRecognizer gesture &&
                gesture.BindingContext is F1Weekend weekend)
            {
                var index = _vm.Weekends.IndexOf(weekend);
                if (index >= 0 && index < _vm.Weekends.Count)
                {
                    RaceCarousel.ScrollTo(index, animate: true);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Race indicator navigation failed: {ex}");
        }
    }
}