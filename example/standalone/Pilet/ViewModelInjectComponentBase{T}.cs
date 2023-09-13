#nullable enable

using Microsoft.AspNetCore.Components;
using System;
using System.ComponentModel;

namespace Pilet;

public abstract class ViewModelInjectComponentBase<T> : ComponentBase
where T : INotifyPropertyChanged
{
    private T _viewModel = default!;

    [Inject]
    public virtual T ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel is not null)
            {
                _viewModel.PropertyChanged -= OnPropertyChanged;
            }

            _viewModel = value;

            if (_viewModel is not null)
            {
                _viewModel.PropertyChanged += OnPropertyChanged;
            }

            ViewModelSetCallback?.Invoke();
        }
    }

    protected Action? ViewModelSetCallback { get; set; }

    protected Action<PropertyChangedEventArgs>? PropertyChangedCallback { get; set; }

    protected void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
            PropertyChangedCallback?.Invoke(e);
            InvokeAsync(StateHasChanged);
    }
}
