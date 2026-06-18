using System.Text;
using CashFlow.DailyBalanceService.Api.Data;
using CashFlow.DailyBalanceService.Api.Middleware;
using CashFlow.DailyBalanceService.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CashFlow — Daily Balance Service",
        Version = "v1",
        Description = """
            Microserviço responsável pela **consulta do saldo diário consolidado**.

            ## Fluxo de uso
            1. Autentique-se em `POST /api/auth/login` com `admin` / `admin`
            2. Clique em **Authorize** e cole o `accessToken` retornado
            3. Use os endpoints de `/api/balance`

            ## Notas de negócio
            - O saldo é consolidado **assincronamente** via eventos do Launch Service
            - Pode haver um pequeno lag entre o registro de um lançamento e a atualização do saldo
            - `consolidatedBalance` = `totalCredits` − `totalDebits`
            """
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o JWT retornado pelo endpoint `/api/auth/login`."
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", doc), [] }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
});

builder.Services.AddDbContext<BalanceDbContext>(opts =>
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

builder.Services.AddScoped<DailyBalanceQueryService>();
builder.Services.AddScoped<TokenService>();

var app = builder.Build();

// O schema do daily_balance_db é gerenciado exclusivamente pelo DailyBalanceWorker.
// O serviço de leitura aguarda a tabela existir antes de aceitar requisições.

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Daily Balance Service v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
