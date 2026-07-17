using DevTools.ScreenShot.Sharp;
using Microsoft.Extensions.DependencyInjection;

namespace DevTools.ScreenShot.Avalonia.Sharp;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScreenShot(this IServiceCollection services)
    {
        return services.AddSingleton<IScreenShot, ScreenShot>();
    }
}
