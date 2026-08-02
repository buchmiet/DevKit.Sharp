using DevKit.Screenshot.Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace DevKit.Screenshot.WinUi3.Sharp;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IScreenshot"/> using reflection-based main-window resolution.</summary>
    public static IServiceCollection AddScreenshot(this IServiceCollection services)
    {
        return services.AddSingleton<IScreenshot, WinUiScreenshot>();
    }

    /// <summary>Registers <see cref="IScreenshot"/> with an explicit main-window accessor (preferred).</summary>
    public static IServiceCollection AddScreenshot(
        this IServiceCollection services,
        Func<IServiceProvider, Window> windowAccessor)
    {
        ArgumentNullException.ThrowIfNull(windowAccessor);
        return services.AddSingleton<IScreenshot>(sp => new WinUiScreenshot(() => windowAccessor(sp)));
    }
}
