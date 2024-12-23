using Microsoft.Net.Http.Headers;
using SimpleAPI.OIDC;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => options
    .AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .WithHeaders(HeaderNames.Authorization, HeaderNames.Accept, HeaderNames.ContentType)
        .WithExposedHeaders(HeaderNames.WWWAuthenticate, HeaderNames.ContentType)));
builder.Services.AddControllers();
builder.Services.AddHttpClient<HttpClient>();
builder.Services.AddSingleton<IntrospectionClient>();

var app = builder.Build();

app.UseCors();
app.UseAuthorization();

app.MapControllers();

app.Run();
