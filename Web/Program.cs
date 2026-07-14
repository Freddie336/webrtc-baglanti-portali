using WebRTCSignalingServer;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30),
});

var signalingHub = new SignalingHub(app.Logger);

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "webrtc-signaling",
    connections = signalingHub.ConnectionCount,
}));

app.Map("/ws", signalingHub.HandleAsync);

app.Logger.LogInformation("WebRTC demo: http://localhost:8080");
app.Logger.LogInformation("Sağlık kontrolü: http://localhost:8080/health");

await app.RunAsync("http://0.0.0.0:8080");
