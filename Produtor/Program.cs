using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using Produtor.Services.Interfaces;
using Produtor.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddTransient<IMessagePublisher, RabbitMqMessagePublisher>();
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "API Teste",
        Description = "",
    });
});

#region [Database]
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});
#endregion

#region [Cors]
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
                       builder =>
                       {
                           builder.AllowAnyMethod()
                                  .AllowAnyHeader()
                                  .AllowAnyOrigin();
                       });
});
#endregion

var app = builder.Build();

app.Use((context, next) =>
{
    context.Request.Scheme = "https";
    return next(context);
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.PreSerializeFilters.Add((swagger, req) =>
        {
            swagger.Servers = new List<OpenApiServer>() { new OpenApiServer() { Url = $"https://{req.Host}" } };
        });
    });

    app.UseDeveloperExceptionPage();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    });
}

#region [Cors]
app.UseCors(c =>
{
    c.AllowAnyHeader();
    c.AllowAnyMethod();
    c.AllowAnyOrigin();

});
#endregion

//Recomendado para processar corretamente os cabeçalhos encaminhados
app.UseForwardedHeaders();

app.UseRouting();

//Configurar autorização na aplicação
app.UseAuthorization();
app.UseAuthentication();

app.MapControllers();

app.Run();
