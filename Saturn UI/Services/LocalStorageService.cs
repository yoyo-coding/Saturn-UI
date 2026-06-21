using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using SaturnUI.Models;

namespace SaturnUI.Services;

public class LocalStorageService
{
    private readonly string _dbPath;

    public LocalStorageService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "SaturnUI");
        System.IO.Directory.CreateDirectory(dir);
        _dbPath = Path.Combine(dir, "SaturnUI.db");
    }

    private LiteDatabase OpenDb()
    {
        var mapper = new BsonMapper();
        mapper.Entity<Session>()
            .Id(x => x.Id)
            .Field(x => x.Title, "title")
            .Field(x => x.CreatedAt, "created_at")
            .Field(x => x.UpdatedAt, "updated_at");

        mapper.Entity<Message>()
            .Id(x => x.Id)
            .Field(x => x.SessionId, "session_id")
            .Field(x => x.Role, "role")
            .Field(x => x.Content, "content")
            .Field(x => x.Timestamp, "timestamp")
            .Field(x => x.IsStreaming, "is_streaming")
            .Field(x => x.IsError, "is_error")
            .Field(x => x.ErrorMessage, "error_message");

        return new LiteDatabase(_dbPath, mapper);
    }

    public List<Session> GetSessions()
    {
        using var db = OpenDb();
        var col = db.GetCollection<Session>("sessions");
        return col.Query().OrderByDescending(s => s.UpdatedAt).ToList();
    }

    public Session? GetSession(string id)
    {
        using var db = OpenDb();
        var session = db.GetCollection<Session>("sessions").FindById(id);
        if (session == null) return null;

        var messages = db.GetCollection<Message>("messages")
            .Find(m => m.SessionId == id)
            .OrderBy(m => m.Timestamp)
            .ToList();

        foreach (var msg in messages)
            session.Messages.Add(msg);

        return session;
    }

    public void SaveSession(Session session)
    {
        using var db = OpenDb();
        var sessions = db.GetCollection<Session>("sessions");
        sessions.Upsert(session);

        var messages = db.GetCollection<Message>("messages");
        foreach (var msg in session.Messages)
        {
            msg.SessionId = session.Id;
            messages.Upsert(msg);
        }
    }

    public void DeleteSession(string id)
    {
        using var db = OpenDb();
        db.GetCollection<Session>("sessions").Delete(id);
        db.GetCollection<Message>("messages").DeleteMany(m => m.SessionId == id);
    }

    public void SaveMessage(Message message)
    {
        using var db = OpenDb();
        db.GetCollection<Message>("messages").Upsert(message);
    }

    public void UpdateSessionTitle(string id, string title)
    {
        using var db = OpenDb();
        var col = db.GetCollection<Session>("sessions");
        var session = col.FindById(id);
        if (session != null)
        {
            session.Title = title;
            session.UpdatedAt = DateTime.Now;
            col.Update(session);
        }
    }
}
