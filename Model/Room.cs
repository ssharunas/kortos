using System.Net.WebSockets;
using System.Collections.Concurrent;

public class Room
{
    private const string VOTING_STATUS_IN_ENDED = "ended";
    private const string VOTING_STATUS_IN_PROGRESS = "in_progress";

    private ConcurrentDictionary<Guid, Participant> _participants = new();

    public Room(string id)
    {
        Id = id;
        VotingStatus= VOTING_STATUS_IN_ENDED;
    }

    public string Id{ get; }
    public string? VotingId { get; set; }
    public string VotingStatus { get; set; }
    public IList<string>? Choices { get; set; }

    public IEnumerable<Participant> Participants => _participants.Values;

    public Participant AddParticipant(WebSocket socket)
    {
        var participant = new Participant(socket, this);
        _participants.TryAdd(participant.Id, participant);
        return participant;
    }

    public void SetChoices(IList<string>? choices)
    {
        Choices = choices;

        foreach(var participant in _participants.Values)
            participant.SendChoices();
    }

    public void StartVoting()
    {
        VotingId = Guid.NewGuid().ToString();
        VotingStatus = VOTING_STATUS_IN_PROGRESS;
        foreach(var participant in _participants.Values)
            participant.SendStartVoting();
    }

    public void EndVoting()
    {
        VotingStatus = VOTING_STATUS_IN_ENDED;

        foreach(var participant in _participants.Values)
            participant.SendRoomVotingStatus();

        VotingId = Guid.Empty.ToString();
    }

    public void SendPlayerListChanged()
    {
        foreach(var participant in _participants.Values)
            participant.SendPlayers();
    }

    public void Remove(Participant participant)
    {
        _participants.TryRemove(participant.Id, out _);
        SendPlayerListChanged();
    }

    public void Cleanup()
    {

    }
}