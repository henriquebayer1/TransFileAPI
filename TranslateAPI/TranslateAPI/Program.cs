using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TranslateAPI.Interface;
using TranslateAPI.Repository;
using TranslateAPI.Services;
using TranslateAPI.Services.AzureTranslator;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();

// Dependence Injection class MongoDbService
builder.Services.AddSingleton<MongoDbService>();

// Adiciona serviço de repositório
builder.Services.AddSingleton<IAzureTranslatorInterface, AzureTranslatorRepository>();

// Registra a implementação do IFileRepository
builder.Services.AddSingleton<IFileRepository, FileRepository>();  // Adicionada linha para registrar o IFileRepository

// Adiciona serviço de Jwt Bearer (forma de autenticação)
builder.Services.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = "JwtBearer";
    options.DefaultAuthenticateScheme = "JwtBearer";
})
.AddJwtBearer("JwtBearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Valida quem está solicitando
        ValidateIssuer = true,

        // Valida quem está recebendo
        ValidateAudience = true,

        // Define se o tempo de expiração será validado
        ValidateLifetime = true,

        // Forma de criptografia e valida a chave de autenticação
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("transfile-webapi-chave-symmetricsecuritykey")),

        // Valida o tempo de expiração do token
        ClockSkew = TimeSpan.FromMinutes(30),

        // Nome do issuer (de onde está vindo)
        ValidIssuer = "TransFile-WebAPI",

        // Nome do audience (para onde está indo)
        ValidAudience = "TransFile-WebAPI"
    };
});

// Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "API TransFile",
        Description = "Backend API",
        Contact = new OpenApiContact
        {
            Name = "Grupo 5 LabWare"
        }
    });

    // Usando a autenticação no Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Value: Bearer TokenJWT ",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

var app = builder.Build();

app.UseSwagger();
// Swagger
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

// Use Services
app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();