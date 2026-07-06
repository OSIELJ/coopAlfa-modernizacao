using CoopAlfa.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "CoopAlfa API",
        Version     = "v1",
        Description = "API de modernização do cadastro de clientes - Cooperativa Financeira Alfa"
    });
});

// Registra o serviço que faz P/Invoke para o COBOL
builder.Services.AddScoped<IClienteService, ClienteService>();

// Permite que a interface HTML seja servida pela própria API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoopAlfa API v1");
    c.RoutePrefix = "swagger";
});

app.UseStaticFiles();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Rota raiz redireciona para a interface do atendente
app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run();
