using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
public class Participant
{
    private WebSocket _socket;
    private Room _room;

    public Participant(WebSocket socket, Room room)
    {
        _room = room;
        _socket = socket;
        Id = Guid.NewGuid();
        Name = string.Empty;
        Vote = string.Empty;
    }

    public Guid Id{ get; }
    public string Name{ get; set; }
    public string Vote { get ; set; }
    public bool IsClosed => _socket.CloseStatus.HasValue || RoomManager.IsStopping;

    public void SetName(string name)
    {
        Name = name;
        _room.SendPlayerListChanged();
    }

    public Task SendPlayers()
    {
        var players = new List<object>();
        foreach(var participant in _room.Participants)
        {
            if(!string.IsNullOrEmpty(participant.Name))
            {
                players.Add(new { participant.Id, participant.Name });
            }
        }

        return SendEvent(SocketEvent.SET_PLAYERS, players);
    }

    public Task SendStartVoting()
    {
        foreach(var participant in this._room.Participants)
            participant.Vote = string.Empty;

        return SendEvent(SocketEvent.START_VOTING, _room.VotingId);
    }
    public Task SetVote(Vote? vote)
    {
        if(vote is not null && _room.VotingId == vote.Id && !string.IsNullOrEmpty(_room.VotingId))
        {
            Vote = vote.Result ?? string.Empty;

            foreach(var participant in _room.Participants)
            {
                participant.SendRoomVotingStatus();
            }
        }

        return Task.FromResult(0);
    }

    public Task SendRoomVotingStatus()
    {
        var votes = new List<object>();
        foreach(var participant in _room.Participants)
        {
            if(!string.IsNullOrEmpty(participant.Vote))
            {
                votes.Add(new 
                {
                    playerId = participant.Id,
                    playerName = participant.Name,
                    vote = participant.Vote,
                });
            }
        }

        return SendEvent(SocketEvent.SET_VOTING, new { 
            id = _room.VotingId, 
            status = _room.VotingStatus,
            votes 
        });
    }

    public Task SendChoices()
    {
        return SendEvent(SocketEvent.SET_CHOICES, _room.Choices);
    }

    public Task SendEvent(string name, object? data)
    {
        string? serialized = data as string;

        if(serialized is null)
            serialized = JsonSerializer.Serialize(data);

        return SendEvent(new SocketEvent(name, serialized));
    }

    public Task SendEvent(SocketEvent @event)
    {
        return Send(JsonSerializer.Serialize(@event));
    }

    public Task Send(string text)
    {
        if(!IsClosed)
        {
            Console.WriteLine($"Sending to {Id} message: " + text);
            return _socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, RoomManager.CancellationToken);
        }

        Leave();
        return Task.FromResult(0);
    }

    public async Task<SocketEvent?> Receive()
    {
        if(IsClosed)
        {
            Leave();
            return null;
        }

        var data = new List<byte>();
        var buffer = new ArraySegment<byte>(new byte[1024 * 100]);
        try
        {
            WebSocketReceiveResult result;
            do
            {
                result = await _socket.ReceiveAsync(buffer, RoomManager.CancellationToken);


                if(buffer.Array?.Length > 0)
                {
                    Console.WriteLine("READ" + result.Count);
                    data.AddRange(buffer.Slice(0, result.Count));
                    Console.WriteLine("LENGTH" + data.Count);
                }
            }
            while(!result.EndOfMessage && !result.CloseStatus.HasValue);
        }
        catch(OperationCanceledException) { /* Server is shutting down. */ }

        var text = Encoding.UTF8.GetString(data.ToArray());
        Console.WriteLine($"Participant {Id} reveived: '{text ?? "<null>"}'");

        try
        {
            if(!string.IsNullOrEmpty(text))
                return JsonSerializer.Deserialize<SocketEvent>(text);
        }
        catch(Exception ex)
        {
            Console.WriteLine("ERROR: invalid json: " + ex.Message);
        }

        return null;
    }


    public async Task Join()
    {
        await SendEvent(SocketEvent.SET_ID, Id);
        await SendChoices();
        await SendPlayers();
        await SendRoomVotingStatus();

        while(!IsClosed)
        {
            var evt = await Receive();

            if(evt is null || IsClosed)
                continue;

            try
            {
                switch(evt.Name)
                {
                    case SocketEvent.SET_NAME:
                        SetName(evt.GetData<string>() ?? string.Empty);
                        break;

                    case SocketEvent.GET_PLAYERS:
                        await SendPlayers();
                        break;
                    
                    case SocketEvent.START_VOTING:
                        _room.StartVoting();
                        break;
                    
                    case SocketEvent.SET_VOTE:
                        await SetVote(evt.GetData<Vote>());
                        break;
                        
                    case SocketEvent.END_VOTING:
                        _room.EndVoting();
                        break;
                    
                    case SocketEvent.SET_CHOICES:
                        _room.SetChoices(evt.GetData<List<string>>());
                        break;

                    default:
                        await SendEvent(SocketEvent.NO_COPRENDE, string.Empty);
                        break;
                }
            }
            catch(Exception ex)
            {
                await SendEvent(SocketEvent.NO_COPRENDE, "Klaida: " + ex.Message);
            }
        }

        if(_socket.State == WebSocketState.Open || 
            _socket.State == WebSocketState.CloseSent ||
            _socket.State == WebSocketState.CloseReceived
        )
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "pabaiga", RoomManager.CancellationToken);
        }

        Leave();
    }

    public void Leave()
    {
        Console.WriteLine($"Participant {Id} disconnected");
        _room.Remove(this);
    }
}