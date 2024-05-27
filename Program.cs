using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MyBGList.Constants;
using MyBGList.GraphQL;
using MyBGList.Models;
using MyBGList.Swagger;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders().AddSimpleConsole().AddDebug();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(cfg =>
    {
        cfg.WithOrigins(builder.Configuration["AllowedOrigins"]!);
        cfg.AllowAnyHeader();
        cfg.AllowAnyMethod();
    });
    opts.AddPolicy(
        name: "AnyOrigin",
        cfg =>
        {
            cfg.AllowAnyOrigin();
            cfg.AllowAnyHeader();
            cfg.AllowAnyMethod();
        }
    );
});

builder.Services.AddControllers(opts =>
{
    opts.ModelBindingMessageProvider.SetValueIsInvalidAccessor(x => $"The value '{x}' is invalid.");
    opts.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(x =>
        $"The value '{x} must be a number'"
    );
    opts.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor(
        (x, y) => $"The value '{x} is not valid for {y}.'"
    );
    opts.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(() => "A value is required;");

    opts.CacheProfiles.Add("NoCache", new CacheProfile() { NoStore = true });
    opts.CacheProfiles.Add(
        "Any-60",
        new CacheProfile() { Location = ResponseCacheLocation.Any, Duration = 60 }
    );
    opts.CacheProfiles.Add(
        "Client-120",
        new CacheProfile() { Location = ResponseCacheLocation.Client, Duration = 120 }
    );
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.ParameterFilter<SortColumnFilter>();
    opts.ParameterFilter<SortOrderFilter>();
    opts.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "bearer"
        }
    );
    opts.AddSecurityRequirement(
        new OpenApiSecurityRequirement
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
                Array.Empty<string>()
            }
        }
    );
});

var connString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseMySql(connString, ServerVersion.AutoDetect(connString))
);

builder.Services.AddGraphQLServer()
  .AddAuthorization()
  .AddQueryType<Query>()
  .AddMutationType<Mutation>()
  .AddProjections()
  .AddFiltering()
  .AddSorting();

builder
    .Services.AddIdentity<ApiUser, IdentityRole>(opts =>
    {
        opts.Password.RequireDigit = true;
        opts.Password.RequireLowercase = true;
        opts.Password.RequireUppercase = true;
        opts.Password.RequireNonAlphanumeric = true;
        opts.Password.RequiredLength = 12;
    })
    .AddEntityFrameworkStores<AppDbContext>();

builder
    .Services.AddAuthentication(opts =>
    {
        opts.DefaultAuthenticateScheme =
            opts.DefaultChallengeScheme =
            opts.DefaultForbidScheme =
            opts.DefaultScheme =
            opts.DefaultSignInScheme =
            opts.DefaultSignOutScheme =
                JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JWT:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JWT:SigningKey"])
            )
        };
    });

/* builder.Services.Configure<ApiBehaviorOptions>(opts => */
/* { */
/*     opts.SuppressModelStateInvalidFilter = true; */
/* }); */

builder.Services.AddResponseCaching(opts =>
{
    opts.MaximumBodySize = 32 * 1024 * 1024;
    opts.SizeLimit = 50 * 1024 * 1024;
});

builder.Services.AddMemoryCache();
builder.Services.AddStackExchangeRedisCache(opts =>
{
    opts.Configuration = builder.Configuration["Redis:ConnectionString"];
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Configuration.GetValue<bool>("UseDeveloperExceptionPage"))
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler(action =>
    {
        action.Run(async context =>
        {
            var exceptionHandler = context.Features.Get<IExceptionHandlerPathFeature>();

            //TODO logging, sending notis and more

            var details = new ProblemDetails();
            details.Detail = exceptionHandler?.Error.Message;
            details.Extensions["traceId"] =
                System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;
            details.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
            details.Status = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(details));
        });
    });

app.UseHttpsRedirection();
app.UseCors();
app.UseResponseCaching();

app.UseAuthentication();
app.UseAuthorization();
app.Use(
    (context, next) =>
    {
        context.Response.GetTypedHeaders().CacheControl =
            new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
            {
                NoCache = true,
                NoStore = true
            };
        return next.Invoke();
    }
);

app.MapGraphQL();

app.MapGet(
    "/error",
    [EnableCors("AnyOrigin")]
[ResponseCache(NoStore = true)]
(HttpContext context) =>
    {
        IExceptionHandlerPathFeature? exceptionHandler =
                context.Features.Get<IExceptionHandlerPathFeature>();
        ProblemDetails details = new ProblemDetails();

        details.Detail = exceptionHandler?.Error.Message;
        details.Extensions["traceId"] =
            System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;
        details.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
        details.Status = StatusCodes.Status500InternalServerError;

        app.Logger.LogError(
            CustomLogEvents.Error_Get,
            exceptionHandler?.Error,
            "An unhandled error occurred."
        );

        return Results.Problem(details);
    }
);

app.MapGet(
    "/test",
    [EnableCors("AnyOrigin")]
[ResponseCache(NoStore = true)]
() =>
    {
        throw new Exception("error");
    }
);

app.MapGet(
    "auth/test/2",
    [Authorize(Roles = RoleNames.Moderator)]
[EnableCors("AnyOrigin")]
[ResponseCache(NoStore = true)]
() =>
    {
        return Results.Ok("Auth");
    }
);
app.MapGet(
    "auth/test/3",
    [Authorize(Roles = RoleNames.Administrator)]
[EnableCors("AnyOrigin")]
[ResponseCache(NoStore = true)]
() =>
    {
        return Results.Ok("Auth");
    }
);

app.MapControllers().RequireCors("AnyOrigin");

app.Run();
