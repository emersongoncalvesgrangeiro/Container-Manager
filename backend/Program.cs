using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowNextJS", policy =>
  {
    policy.WithOrigins("http://localhost:3000")
          .AllowAnyHeader()
          .AllowAnyMethod();
  });
});

var app = builder.Build();

var containers = new Containers();

app.UseCors("AllowNextJS");

app.UseWebSockets();

//teste
app.MapGet("/", async () =>
{
  return "SystemOn!";
});
//edit container configs and dockerfile
app.MapPost("/editor", async (string path, string name) =>
{

});
//create a new container
app.MapPost("/createcontainer", async (ContainerRequest req) =>
{
  await containers.CreateContainer(req.Name, req.Image, req.UsePort, req.Port, req.UseVolume, req.Volume, req.IsEnvironment, req.Environment);
  return Results.Ok(new { message = "Processo de criação iniciado" });
});
//logs
app.MapGet("/container/logs/{name}", async (string name) =>
{
  var logs = await containers.GetLogs(name);
  return Results.Ok(new { container = name, logs = logs });
});
//start container
app.MapPost("/startcontainer", async (string name) =>
{
  await containers.StartContainer(name);
  return Results.Ok($"Container {name} iniciado");
});
//restart container
app.MapPost("/restartcontainer", async (string name) =>
{
  await containers.RestartContainer(name);
  return Results.Ok($"Container {name} reiniciado");
});
//estatisticas
app.MapGet("/container/stats/{name}", async (string name) =>
{
  var stats = await containers.GetStats(name);
  return Results.Ok(new { container = name, stats = stats });
});
//rota de manutenção
app.MapPost("/system/prune", async () =>
{
  await containers.CleanSystem();
  return Results.Ok("Limpeza do Docker concluída");
});
//remove containers
app.MapPost("/removecontainer", async (string name) =>
{
  if (string.IsNullOrEmpty(name))
  {
    return Results.BadRequest("Nome é obrigatório");
  }
  await containers.RemoveContainer(name);
  return Results.Ok($"Volume {name} removido");
});
//stop container
app.MapPost("/stopcontainer", async (string name) =>
{
  if (string.IsNullOrEmpty(name))
  {
    return Results.BadRequest("Nome é obrigatório");
  }
  await containers.StopContainer(name);
  return Results.Ok($"Stoped container: {name}");
});
//remove image
app.MapPost("/removeimage", async (string name) =>
{
  if (string.IsNullOrEmpty(name))
  {
    return Results.BadRequest("Nome é obrigatório");
  }
  await containers.RemoveImage(name);
  return Results.Ok($"Remove image: {name}");
});
//pull new image
app.MapPost("/pullimage", async (string name) =>
{
  if (string.IsNullOrEmpty(name))
  {
    return Results.BadRequest("Nome é obrigatório");
  }
  await containers.PullImage(name);
  return Results.Ok($"Imagem pega: {name} com sucess");
});
//get all images
app.MapGet("/images", async () =>
{
  return await containers.GetAllImages();
});
//start this container
app.MapPost("/createvolume", async (string name) =>
{
  if (string.IsNullOrEmpty(name))
  {
    return Results.BadRequest("Nome é obrigatório");
  }
  await containers.CreateVolume(name);
  return Results.Ok($"Volume {name} criado");
});
//remove volume
app.MapPost("/removevolume", async (string name) =>
{
  if (string.IsNullOrEmpty(name))
  {
    return Results.BadRequest("Nome é obrigatório");
  }
  await containers.RemoveVolume(name);
  return Results.Ok($"Volume {name} removido");
});
//list online containers
app.Map("/containers", async context =>
{
  if (context.WebSockets.IsWebSocketRequest)
  {
    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    while (webSocket.State == WebSocketState.Open)
    {
      var list = await containers.GetOnlineContainers();
      var response = new
      {
        UpdatedAt = DateTime.Now.ToString("HH:mm:ss"),
        count = list.Count,
        data = list
      };
      var json = JsonSerializer.Serialize(response);
      var bytes = Encoding.UTF8.GetBytes(json);
      await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
      await Task.Delay(5000);
    }
  }
  else
  {
    context.Response.StatusCode = StatusCodes.Status400BadRequest;
  }
});
//list all container
app.Map("/allcontainers", async context =>
{
  if (context.WebSockets.IsWebSocketRequest)
  {
    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    while (webSocket.State == WebSocketState.Open)
    {
      var list = await containers.GetAllContainers();
      var response = new
      {
        UpdatedAt = DateTime.Now.ToString("HH:mm:ss"),
        count = list.Count,
        data = list
      };
      var json = JsonSerializer.Serialize(response);
      var bytes = Encoding.UTF8.GetBytes(json);
      await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
      await Task.Delay(5000);
    }
  }
  else
  {
    context.Response.StatusCode = StatusCodes.Status400BadRequest;
  }
});
//acess terminal
app.Map("/terminal/{name}", async (string name, HttpContext context) =>
{
  if (context.WebSockets.IsWebSocketRequest)
  {
    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    while (webSocket.State == WebSocketState.Open)
    {
      var buffer = new byte[1024 * 4];
      var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
      if (result.MessageType == WebSocketMessageType.Close)
      {
        break;
      }
      var command = Encoding.UTF8.GetString(buffer, 0, result.Count);
      await containers.EnterDocker(name, command, async (output) =>
      {
        var outputBytes = Encoding.UTF8.GetBytes(output + "\n");
        await webSocket.SendAsync(new ArraySegment<byte>(outputBytes), WebSocketMessageType.Text, true, CancellationToken.None);
      });
    }
  }
  else
  {
    context.Response.StatusCode = 400;
  }
});

app.Run();
