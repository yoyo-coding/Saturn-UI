using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using SaturnUI.Models;

namespace SaturnUI.Services;

/// <summary>
/// 本地数据访问服务 - 基于 LiteDB
/// 优化: 共享 LiteDatabase 实例,避免每次操作都打开/关闭文件
/// </summary>
public sealed class LocalStorageService : IDisposable
{
    private const string SessionsCollection = "sessions";
    private const string MessagesCollection = "messages";

    private readonly LiteDatabase _db;

    public LocalStorageService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "SaturnUI");
        Directory.CreateDirectory(dir);
        var dbPath = Path.Combine(dir, "SaturnUI.db");

        _db = new LiteDatabase(dbPath, CreateMapper());
    }

    private static BsonMapper CreateMapper()
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

        return mapper;
    }

    public List<Session> GetSessions()
    {
        return _db.GetCollection<Session>(SessionsCollection)
            .Query()
            .OrderByDescending(s => s.UpdatedAt)
            .ToList();
    }

    public Session? GetSession(string id)
    {
        var session = _db.GetCollection<Session>(SessionsCollection).FindById(id);
        if (session == null) return null;

        var messages = _db.GetCollection<Message>(MessagesCollection)
            .Find(m => m.SessionId == id)
            .OrderBy(m => m.Timestamp)
            .ToList();

        foreach (var msg in messages)
            session.Messages.Add(msg);

        return session;
    }

    public void SaveSession(Session session)
    {
        var sessions = _db.GetCollection<Session>(SessionsCollection);
        sessions.Upsert(session);

        var messages = _db.GetCollection<Message>(MessagesCollection);
        foreach (var msg in session.Messages)
        {
            msg.SessionId = session.Id;
            messages.Upsert(msg);
        }
    }

    public void DeleteSession(string id)
    {
        _db.GetCollection<Session>(SessionsCollection).Delete(id);
        _db.GetCollection<Message>(MessagesCollection)
            .DeleteMany(m => m.SessionId == id);
    }

    public void SaveMessage(Message message)
    {
        _db.GetCollection<Message>(MessagesCollection).Upsert(message);
    }

    public void UpdateSessionTitle(string id, string title)
    {
        var col = _db.GetCollection<Session>(SessionsCollection);
        var session = col.FindById(id);
        if (session == null) return;

        session.Title = title;
        session.UpdatedAt = DateTime.Now;
        col.Update(session);
    }

    public void Dispose() => _db.Dispose();
}
