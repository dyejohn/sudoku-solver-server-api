var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
        builder.SetIsOriginAllowed(x =>  true);
        
    });
    
    options.AddPolicy("defaultPolicy", policyBuilder =>
    {
        policyBuilder.AllowAnyHeader()
            .AllowAnyOrigin()
            .AllowAnyHeader();
    });
});
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("defaultPolicy");
app.UseHttpsRedirection();

/*app.Use(async (context, next) =>
{
    
    // Call the next delegate/middleware in the pipeline.
    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
    context.Response.StatusCode = 200; 
    await next(context);
});*/


app.UseAuthorization();

app.MapControllers();

app.Run();