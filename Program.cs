var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var compileTime = new DateTime(Builtin.CompileTime, DateTimeKind.Utc);

Console.WriteLine($"Starting Kortos version {typeof(RoomManager).Assembly.GetName().Version}, compiled at: {compileTime}.");

var app = builder.Build();
RoomManager.Initialize(app.Lifetime.ApplicationStopping);

app.UseWebSockets(new WebSocketOptions{
    KeepAliveInterval = TimeSpan.FromSeconds(3)
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();