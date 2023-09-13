using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Pilet;

public class ViewModel : ObservableObject, IViewModel
{
    private readonly IStateDelegate _stateDelegate;
    private readonly ILogger<ViewModel> _logger;
    private static int callCount;

    public ViewModel(IStateDelegate stateDelegate, ILogger<ViewModel> logger)
    {
        callCount++;
        Console.WriteLine($"ctor ViewModel Called: {callCount}");
        _logger = logger;
        _stateDelegate = stateDelegate;
        _stateDelegate.SaveSample.PropertyChanged += OnSaveSampleChanged;
    }

    private void OnSaveSampleChanged(object? sender, PropertyChangedEventArgs e)
    {
        _logger.LogInformation(e.PropertyName);
        OnPropertyChanged(nameof(CurrentCount));
        OnPropertyChanged(nameof(Name));
    }

    public int CurrentCount { get => _stateDelegate.SaveSample.CurrentCount; set => _stateDelegate.SaveSample.CurrentCount = value; }

    public string Name { get => _stateDelegate.SaveSample.Name; set => _stateDelegate.SaveSample.Name = value; }
}

public interface IViewModel : INotifyPropertyChanged
{
    int CurrentCount { get; set; }
    string Name { get; set; }
}
