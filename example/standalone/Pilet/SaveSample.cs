using CommunityToolkit.Mvvm.ComponentModel;

namespace Pilet;

public class SaveSample : ObservableObject
{
    private string _name = string.Empty;
    private int currentCount;

    public int CurrentCount { get => currentCount; set => SetProperty(ref currentCount, value); }

    public string Name { get => _name; set => SetProperty(ref _name, value); }
}
