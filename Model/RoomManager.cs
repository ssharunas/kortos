using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
public static class RoomManager
{
    private static ConcurrentDictionary<string, Room> _rooms = new();

    public static void Initialize(CancellationToken token)
    {
        CancellationToken = token;
        new Thread(Cleanup).Start();
    }

    public static CancellationToken CancellationToken { get; private set;}
    public static bool IsStopping => CancellationToken.IsCancellationRequested;

    public static Room GetOrAddRoom(string id)
    {
        return _rooms.GetOrAdd(id, (key) => new Room(key));
    }

    public static void Remove(Room room)
    {
        _rooms.TryRemove(room.Id, out _);
    }

    private static void Cleanup()
    {
        while(!IsStopping)
        {
            foreach(var room in _rooms.Values)
                room.Cleanup();
            
            if(!IsStopping)
                Thread.Sleep(3);
        }

        Console.WriteLine("Stopping service...");
    }
}