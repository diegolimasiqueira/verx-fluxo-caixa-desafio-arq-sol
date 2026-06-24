using System.Text;
using CashFlow.LaunchService.Api.Data;
using CashFlow.LaunchService.Api.Domain.Events;
using CashFlow.LaunchService.Api.Middleware;
using CashFlow.LaunchService.Api.Services;
using CashFlow.Observability;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddCashFlowObservability("launch-service");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CashFlow — Launch Service",
        Version = "v1",
        Description = """
            Microserviço responsável pelo **registro e consulta de lançamentos financeiros** (débitos e créditos).

            ## Autenticação
            - Este serviço **não possui login próprio**
            - Obtenha o JWT no **BFF** (`http://localhost:5000/swagger`) via `POST /api/auth/login`
            - Clique em **Authorize** e cole o token para usar `/api/launches`

            ## Notas de negócio
            - Lançamentos são **imutáveis** após registro
            - Após cada registro, um evento `LaunchRegistered` é publicado no RabbitMQ para atualização assíncrona do saldo consolidado
            """
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o JWT obtido no BFF (`POST /api/auth/login` em :5000)."
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", doc), [] }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
});

builder.Services.AddDbContext<LaunchDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddMassTransit(x =>
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        x.UsingInMemory((_, _) => { });
    }
    else
    {
        x.UsingRabbitMq((_, cfg) =>
        {
            cfg.Host(builder.Configuration["RabbitMQ:Host"], h =>
            {
                h.Username(builder.Configuration["RabbitMQ:Username"]!);
                h.Password(builder.Configuration["RabbitMQ:Password"]!);
            });

            cfg.Message<LaunchRegisteredEvent>(m =>
                m.SetEntityName("launch.registered"));
        });
    }
});

builder.Services.AddScoped<LaunchAppService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LaunchDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Launch Service v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseCashFlowObservability();

app.Run();

public partial class Program { }
