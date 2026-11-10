using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OficinaMecanica.Contracts.IntegrationEvents;
using PecasInsumos.Adapters.Controllers;
using PecasInsumos.Adapters.DataSources;
using PecasInsumos.Adapters.Gateways;
using PecasInsumos.Application.Commands.AdicionarPecaInsumo;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Application.IntegrationEventHandlers;
using PecasInsumos.Application.Queries.ListarPecasInsumos;
using PecasInsumos.Infrastructure.Messaging;
using PecasInsumos.Infrastructure.Persistence;
using PecasInsumos.Infrastructure.Queries;
using SharedKernel.Application;

namespace PecasInsumos.Infrastructure;

public static class PecasInsumosModule
{
    public static IServiceCollection AddPecasInsumosModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<PecasInsumosDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PecasInsumosDbContext>());

        #region DataSources (Repositórios EF)

        services.AddScoped<IPecaInsumoRepository, PecaInsumoRepository>();

        #endregion

        #region Gateways (Interface Adapters)

        services.AddScoped<IPecaInsumoGateway, PecaInsumoGateway>();

        #endregion

        #region Controllers CA

        services.AddScoped<PecaInsumoController>();

        #endregion

        #region Queries de leitura

        services.AddScoped<IListarPecasInsumosQuery, ListarPecasInsumosQueryImpl>();

        #endregion

        #region Integration Events

        services.AddScoped<IIntegrationEventHandler<OrcamentoGeradoIntegrationEvent>, DecrementarEstoqueQuandoOrcamentoGerado>();
        services.AddScoped<IIntegrationEventHandler<OrcamentoRejeitadoIntegrationEvent>, IncrementarEstoqueQuandoOrcamentoRejeitado>();

        #endregion

        #region Mensageria (MassTransit — consumers e IIntegrationEventBus)

        services.AddPecasInsumosMensageria(configuration);

        #endregion

        var applicationAssembly = typeof(AdicionarPecaInsumoCommand).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(applicationAssembly);
            cfg.AddOpenBehavior(typeof(SharedKernel.Application.Behaviors.LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(SharedKernel.Application.Behaviors.ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(SharedKernel.Application.Behaviors.TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(applicationAssembly);

        return services;
    }
}
