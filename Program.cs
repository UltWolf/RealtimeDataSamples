using Microsoft.OpenApi.Models;
using RealtimeDataSamples.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<ItemService>();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("test", f =>
    {
        f.AllowAnyHeader()
                                     .AllowAnyOrigin()
                                     .AllowAnyMethod()
                                     ;
    });
});
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Your API Name", Version = "v1" });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Your API Name v1"));

}

app.UseCors("test");
app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers(); // This maps all controllers in the application
});
app.Run();




