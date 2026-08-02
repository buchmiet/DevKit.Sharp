using DevKit.Screenshot.Sharp;
using Microsoft.Extensions.DependencyInjection;

namespace DevKit.Screenshot.Avalonia.Sharp;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScreenshot(this IServiceCollection services)
    {
        return services.AddSingleton<IScreenshot, AvaloniaScreenshot>();
    }
}
