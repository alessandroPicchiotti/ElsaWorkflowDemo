using Elsa.Email.Options;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Extensions;
using ElsaWorkflowDemo.Api.Class;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.SwaggerGen;

// Rimuovi i duplicati di using Microsoft.OpenApi.Models e Swashbuckle.AspNetCore.Swagger

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// 1. Aggiungere il servizio Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Elsa Workflow Demo API",
        Version = "v1"
    });
    options.CustomSchemaIds(type => type.ToString()); // <-- aggiungi questa riga!
});
// Elsa DB (usa SQL Server qui, puoi cambiare provider)
var elsaConnectionString = builder.Configuration.GetConnectionString("Elsa") ?? throw new InvalidOperationException("La stringa di connessione 'Elsa' non è configurata.");

builder.Services.AddElsa(elsa =>
{
    // Configure Management layer to use EF Core.
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UseSqlServer(elsaConnectionString)));

    // Configure Runtime layer to use EF Core.
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UseSqlServer(elsaConnectionString)));

    elsa.UseHttp(http => http.ConfigureHttpOptions = options =>
    {
        options.BaseUrl = new Uri("https://localhost:7023");
        options.BasePath = "/workflows";
    });
    elsa.UseEmail();

    elsa.UseWorkflows();
    elsa.UseWorkflowRuntime();
    // Expose Elsa API endpoints.
    elsa.UseWorkflowsApi();
    // Setup a SignalR hub for real-time updates from the server.
    elsa.UseRealTimeWorkflows();

    // Enable JavaScript workflow expressions
    elsa.UseJavaScript(options => options.AllowClrAccess = true);

    // Enable HTTP activities.
    elsa.UseHttp(options => options.ConfigureHttpOptions = httpOptions => httpOptions.BaseUrl = new("https://localhost:5001"));

    // Register custom activities from the application, if any.
    elsa.AddActivitiesFrom<Program>();

    // Register custom workflows from the application, if any.
    elsa.AddWorkflowsFrom<Program>();
 
    //elsa.UseTemporal();
});






// Configurazione SMTP per email Elsa (usane una reale/test)
builder.Services.Configure<MapOptions>(builder.Configuration.GetSection("Elsa:Smtp"));

// Abilita CORS se client Blazor è su porta diversa
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();



if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // <<--- AGGIUNGI QUESTO per servire lo swagger.json
   // app.UseSwaggerUI();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Il Mio API V1");
        c.RoutePrefix = "swagger";
});
}

app.UseCors();
app.UseHttpsRedirection();

app.UseRouting(); // Required for SignalR.
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseWorkflowsApi(); // Use Elsa API endpoints.
app.UseWorkflows(); // Use Elsa middleware to handle HTTP requests mapped to HTTP Endpoint activities.
////app.UseWorkflowsSignalRHubs(); // Optional SignalR integration. Elsa Studio uses SignalR to receive real-time updates from the server. 

app.Run();