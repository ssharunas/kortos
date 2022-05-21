using System.Text.Json;
public class SocketEvent
{
    public const string GET_PLAYERS = "get-players";
    public const string SET_PLAYERS = "set-players";
    public const string SET_NAME = "set-name";
    public const string SET_ID = "set-id";
    public const string START_VOTING = "start-voting";
    public const string END_VOTING = "end-voting";
    public const string SET_VOTING = "set-voting";
    public const string SET_VOTE = "set-vote";
    public const string NO_COPRENDE = "no-coprende";
    public const string SET_CHOICES = "set-choices";

    /*
    1. JOIN:
        #> set-id
        #> set-choices
        #> set-players
        #> set-voting
    2. SET NAME
        < set-name "name"
        *> set-players [ {id: '', name: ''}, ... ]
    3. GET PLAYERS
        < get-players
        #> set-players [ {id: '', name: ''}, ... ]
    4. VOTING
        4.1 START VOTING
            < start-voting
            *> start-voting 'voting id'
        4.2 VOTE
            < set-vote { Id: 'voting id', Result: '' }
            *> set-voting { id: 'voting id', status: 'in_progress', votes: [ { playerId: '', playerName: '', vote: '' } ] }
        4.3 END VOTING
            < end-voting
            *> set-voting { id: 'voting id', status: 'ended', votes: [ { playerId: '', playerName: '', vote: '' } ] }
    5. SET CHOICES
        < set-choices ['', ...]
        *> set-choices ['', ...]
    */

    public SocketEvent(string name, string data)
    {
        Name = name;
        Data = data;
    }

    public string Name { get; }
    public string Data { get; }

    public T? GetData<T>()
    {
        if(Data is null)
            return default(T);

       return JsonSerializer.Deserialize<T>((string)Data);
    }
}