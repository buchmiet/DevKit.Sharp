using DevTools.Screenshot.Sharp;
using Microsoft.Extensions.DependencyInjection;

namespace DevTools.Screenshot.WinUi3.Sharp;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScreenshot(this IServiceCollection services)
    {
        return services.AddSingleton<IScreenshot, WinUiScreenshot>();
    }
}
