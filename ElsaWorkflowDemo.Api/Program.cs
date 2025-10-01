using Elsa;
using Elsa.Activities;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Email.Options;
using Elsa.Activities.Signaling;
using Elsa.Activities.Temporal;
using Elsa.Activities.UserTask.Activities;
using Elsa.Activities.UserTask.Bookmarks;
using Elsa.Activities.UserTask.Contracts;
using Elsa.Activities.UserTask.Extensions;
using Elsa.Activities.UserTask.Models;
using Elsa.Activities.UserTask.Services;
using Elsa.Email.Options;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Extensions;
using ElsaWorkflowDemo.Api.Class;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Parlot.Fluent;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Serialization;
using System.Threading;



// Rimuovi il using duplicato Elsa.Activities.UserTask.Activities

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
Elsa.EndpointSecurityOptions.DisableSecurity();

builder.Services.AddElsa(elsa =>
{
    // Configure Management layer to use EF Core.
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UseSqlServer(elsaConnectionString)));
    
    // Configure Runtime layer to use EF Core.
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UseSqlServer(elsaConnectionString)));
    //elsa.UseWorkflowRuntime(run => run?.AddActivity<SignalReceived>()  // ⭐ REGISTRA MANUALMENTE
      //  );
    elsa.UseLiquid();
    elsa.UseCSharp();
    elsa.UseHttp(http => http.ConfigureHttpOptions = options =>
    {
        options.BaseUrl = new Uri("https://localhost:7023");
        options.BasePath = "/workflows";
    });

    // Default Identity features for authentication/authorization.
    elsa.UseIdentity(identity =>
    {
        identity.TokenOptions = options => options.SigningKey = "sufficiently-large-secret-signing-key"; // This key needs to be at least 256 bits long.
        identity.UseAdminUserProvider();
    });

    
    // Configure ASP.NET authentication/authorization.
    //elsa.UseDefaultAuthentication(auth => auth.UseAdminApiKey());
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
    //elsa.UseHttp(options => options.ConfigureHttpOptions = httpOptions => httpOptions.BaseUrl = new("https://localhost:7023"));
    elsa.UseWorkflowsApi();


    //elsa.UseTemporal();
    //elsa.UseTemporal();
    //elsa.UseTemporal();
    //elsa.UseSignals(); // <--- ABILITA SIGNALS!; // <--- ABILITA SIGNALS!
     elsa.UseScheduling();//scheduling => scheduling.UseQuartzScheduler());
    elsa.UseQuartz();
    // Registra le attività UserTask tramite assembly scanning
    //elsa.AddActivitiesFrom<Elsa.
    //elsa.use
    //elsa.AddActivity<Elsa.Activities.UserTask.Activities.UserTask>();
    // Register custom activities from the application, if any.
    elsa.AddActivitiesFrom<Program>();
    //elsa.AddActivities<Elsa.Activities.UserTask.Activities.UserTask>();

    // Register custom workflows from the application, if any.
    elsa.AddWorkflowsFrom<Program>();
    elsa.UseRealTimeWorkflows();
    // elsa.AddWorkflowsCore();
    //elsa.AddActivitiesFrom<Elsa.Workflows.Activities.Start>(); // Qualsiasi activity di Elsa
    elsa.AddActivitiesFrom<Elsa.Workflows.Activities.Start>(); // Qualsiasi activity di Elsa

    //elsa.AddActivitiesFrom(typeof(Elsa.Workflows.Activities.Start).Assembly);
    //elsa.AddActivitiesFrom(typeof(Elsa.Workflows.Activities.Start).Assembly);
    //elsa.WithActivitiesFrom(typeof(Elsa.Workflows.Activities.Start));
    elsa.AddActivitiesFrom<Start>();
    elsa.AddActivitiesFrom<SignalReceived>();
    elsa.AddActivitiesFrom<WriteLine>();

    // Opzione A: Carica attività specifiche
    elsa.AddActivitiesFrom<Start>();
    elsa.AddActivitiesFrom<SignalReceived>();
    elsa.AddActivitiesFrom<WriteLine>();
    //elsa.AddActivitiesFrom<If>();
    //elsa.AddActivitiesFrom<Fork>();

    // Opzione B: Oppure solo questa (SignalReceived dovrebbe essere incluso)
    elsa.AddActivitiesFrom<Start>();
    

    
    // ✅ 3. SCHEDULING (CRITICO per SignalReceived)
    elsa.UseScheduling(scheduling =>
    {
        scheduling.UseQuartzScheduler(); // Usa Quartz invece del scheduler built-in
    });


    // 1. DATABASE
    //elsa.UseWorkflowManagement(management =>
    //{
    //    management.UseMemoryStore();
    //});

    // 2. RUNTIME
    elsa.UseWorkflowRuntime();

    // 3. SCHEDULING CON QUARTZ
    elsa.UseScheduling(scheduling => scheduling.UseQuartzScheduler());

    // 4. ATTIVITÀ
    elsa.UseJavaScript();
    elsa.UseHttp();

    // 5. API
    elsa.UseWorkflowsApi();

    // 6. REAL-TIME
    elsa.UseRealTimeWorkflows();

    // ⭐⭐ CARICA ESPLICITAMENTE LE ATTIVITÀ CORE DI ELSA
    elsa.AddActivitiesFrom<Start>();
    elsa.AddActivitiesFrom<Sequence>();
    //elsa.AddActivitiesFrom<Parallel>();
   // elsa.AddActivitiesFrom<If>();
   // elsa.AddActivitiesFrom<Switch>();
   // elsa.AddActivitiesFrom<While>();
   // elsa.AddActivitiesFrom<For>();
   // elsa.AddActivitiesFrom<ForEach>();
    //elsa.AddActivitiesFrom<Fork>();
    elsa.AddActivitiesFrom<Join>();

    // ⭐⭐ PROVA A CARICARE SIGNALRECEIVED ESPLICITAMENTE
    // Se questa riga dà errore, SignalReceived non è nel namespace atteso
    // Prova diversi namespace possibili per SignalReceived
    try
    {
        // Opzione 1
        elsa.AddActivitiesFrom<SignalReceived>();
    }
    catch
    {
        try
        {
            // Opzione 2 - namespace alternativo
            var signalType = Type.GetType("Elsa.Workflows.Activities.SignalReceived, Elsa.Workflows.Core");
            if (signalType != null)
            {
                var method = typeof(ElsaServiceCollectionExtensions).GetMethod("AddActivitiesFrom");
                var genericMethod = method.MakeGenericMethod(signalType);
                genericMethod.Invoke(null, new object[] { elsa });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Impossibile caricare SignalReceived: {ex.Message}");
        }
    }
});


// Configurazione SMTP per email Elsa (usane una reale/test)
builder.Services.Configure<MapOptions>(builder.Configuration.GetSection("Elsa:Smtp"));

// Abilita CORS se client Blazor è su porta diversa
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("https://localhost:7231", "http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
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
//app.UseElsaFeatures(); // Adds Elsa features to the request pipeline.


app.UseRouting(); // Required for SignalR.
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseWorkflows(); // Use Elsa middleware to handle HTTP requests mapped to HTTP Endpoint activities.
app.UseWorkflowsApi(); // Use Elsa API endpoints.
app.UseWorkflowsSignalRHubs(); // Optional SignalR integration. Elsa Studio uses SignalR to receive real-time updates from the server. 
app.MapGet("/debug", () => "Server funziona");
app.MapGet("/elsa/debug", () => "Elsa base funziona");
app.Run();