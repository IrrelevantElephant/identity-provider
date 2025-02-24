using Database;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Server;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var databaseSettings = builder.Configuration.Get<DatabaseSettings>();
ArgumentNullException.ThrowIfNull(databaseSettings);

builder.Services.AddDbContext<ApplicationContext>(options =>
{
    options.UseNpgsql(databaseSettings.ConnectionString);
    options.UseOpenIddict();
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationContext>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("connect/token");
        options.AllowClientCredentialsFlow();
        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
            .EnableTokenEndpointPassthrough();
    });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();

//app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapPost("/connect/token", async (HttpContext context, [FromServices]IOpenIddictApplicationManager applicationManager) => {
    var request = context.GetOpenIddictServerRequest();

    ArgumentNullException.ThrowIfNull(request);

    if (!request.IsClientCredentialsGrantType())
    {
        throw new NotImplementedException("The specified grant is not implemented.");
    }

    var application = await applicationManager.FindByClientIdAsync(request.ClientId) ?? throw new InvalidOperationException("The application cannot be found.");
    // Create a new ClaimsIdentity containing the claims that
    // will be used to create an id_token, a token or a code.
    var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

    // Use the client_id as the subject identifier.
    identity.SetClaim(Claims.Subject, await applicationManager.GetClientIdAsync(application));
    identity.SetClaim(Claims.Name, await applicationManager.GetDisplayNameAsync(application));

    identity.SetDestinations(static claim => claim.Type switch
    {
        // Allow the "name" claim to be stored in both the access and identity tokens
        // when the "profile" scope was granted (by calling principal.SetScopes(...)).
        Claims.Name when claim.Subject.HasScope(Scopes.Profile)
            => [Destinations.AccessToken, Destinations.IdentityToken],

        // Otherwise, only store the claim in the access tokens.
        _ => [Destinations.AccessToken]
    });

    return Results.SignIn(new ClaimsPrincipal(identity), null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
