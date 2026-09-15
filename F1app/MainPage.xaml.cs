using F1app.ViewModels;

namespace F1app;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _vm;

    public MainPage()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
    }

    private void OnJumpToCurrentRaceClicked(object? sender, EventArgs e)
    {
        if (_vm.ActiveRaceIndex >= 0)
        {
            RaceCarousel.ScrollTo(_vm.ActiveRaceIndex, animate: true);
        }
    }

    private void OnCarouselPositionChanged(object? sender, PositionChangedEventArgs e)
    {
        _vm.SelectWeekendAt(e.CurrentPosition);
    }
}