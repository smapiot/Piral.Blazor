﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Piral.Blazor.Utils;

/// <summary>
/// Represents a set of helper functions to be used in Piral Blazor components.
/// </summary>
public interface IPiletService
{
    /// <summary>
    /// Converts a given local URL (e.g., /images/foo.png) to a URL
    /// within the currently running pilet (e.g.,
    /// https://current.cdn.com/pilets/my-pilet/1.0.0/images/foo.png).
    /// </summary>
    string GetUrl(string localPath);

    /// <summary>
    /// Loads the provided language. Usually, this is done under the hood
    /// and does not need to be invoked explicitly. You could use this
    /// for pre-loading some language under a certain condition.
    /// </summary>
    Task LoadLanguage(string language);

    /// <summary>
    /// Gets the configuration object for the current pilet.
    /// </summary>
    IConfiguration Config { get; }

    /// <summary>
    /// Gets the name of the current pilet.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the current pilet.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Event emitted when the language has been changed.
    /// </summary>
    event EventHandler LanguageChanged;

    /// <summary>
    /// Dispatches an event using the given type.
    /// </summary>
    void DispatchEvent<T>(string type, T args);

    /// <summary>
    /// Adds the provided event listener.
    /// </summary>
    void AddEventListener<T>(string type, Action<T> handler);

    /// <summary>
    /// Removes the provided event listener.
    /// </summary>
    void RemoveEventListener<T>(string type, Action<T> handler);

    /// <summary>
    /// Calls the specified method on the pilet's pilet API object.
    /// </summary>
    Task<T> Call<T>(string fn, params object[] args);
}
