using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;

public class WebSocketController : ControllerBase
{
    [HttpGet("/ws")]
    public async Task Get(string id)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var participant = RoomManager.GetOrAddRoom(id).AddParticipant(webSocket);
            await participant.Join();
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}