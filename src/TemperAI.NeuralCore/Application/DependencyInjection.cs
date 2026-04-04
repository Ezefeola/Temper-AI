using Microsoft.Extensions.DependencyInjection;

namespace TemperAI.NeuralCore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddUseCases();

        return services;
    }

    private static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddSessionUseCases();
        services.AddObservationUseCases();

        return services;
    }

    private static IServiceCollection AddSessionUseCases(this IServiceCollection services)
    {
        return services;
    }

    private static IServiceCollection AddObservationUseCases(this IServiceCollection services)
    {
        return services;
    }
}
