using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using CashFlow.Bff.Api.Data;
using CashFlow.Bff.Api.Middleware;
using CashFlow.Bff.Api.Services;
using CashFlow.Observability;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddCashFlowObservability("bff");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CashFlow — BFF (Web Channel)",
        Version = "v1",
        Description = """
            **Backend for Frontend** — ponto único de entrada para o canal web.

            ## Fluxo de uso
            1. Autentique-se em `POST /api/auth/login` com e-mail e senha
            2. Clique em **Authorize** e cole o `accessToken` retornado
            3. Use os endpoints de `/api/launches`, `/api/balance` e `/api/users` (admin)

            ## Usuário padrão (criado automaticamente na primeira subida)
            - **E-mail:** admin@admin.com
            - **Senha:** Master@123
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

builder.Services.AddDbContext<BffDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000"])
              .AllowAnyHeader()
              .AllowAnyMethod());
});

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.Headers["Retry-After"] = "60";
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                title = "Too many requests",
                detail = "Rate limit exceeded. Try again later.",
                status = 429
            }, token);
        };

        options.AddPolicy("login", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1)
                }));

        options.AddPolicy("api", httpContext =>
        {
            var key = httpContext.User.Identity?.IsAuthenticated == true
                ? httpContext.User.FindFirstValue(ClaimTypes.Email)
                  ?? httpContext.Connection.RemoteIpAddress?.ToString()
                  ?? "unknown"
                : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            });
        });
    });
}

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
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserAppService>();
builder.Services.AddScoped<DownstreamProxy>();

builder.Services.AddHttpClient("launch", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Launch"]!);
});

builder.Services.AddHttpClient("balance", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Balance"]!);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BffDbContext>();
    db.Database.Migrate();
    await UserSeeder.SeedAsync(db);
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BFF v1");
    c.RoutePrefix = "swagger";
});

app.UseCors();
if (!app.Environment.IsEnvironment("Testing"))
    app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsEnvironment("Testing"))
    app.MapControllers();
else
    app.MapControllers().RequireRateLimiting("api");

app.UseCashFlowObservability();

app.Run();

public partial class Program { }
