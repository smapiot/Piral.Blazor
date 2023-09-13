using System;
using System.Globalization;
using System.Linq;

namespace Piral.Blazor.Core;

public static class Localization
{
    public static event EventHandler LanguageChanged;

    public static string Language
    {
        get
        {
            return CultureInfo.CurrentCulture.Name;
        }
        set
        {
            var culture = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(c => c.Name == value);

            if (culture is not null)
            {
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }

            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
