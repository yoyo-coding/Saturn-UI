using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using SaturnUI.Models;

namespace SaturnUI.Services;

/// <summary>
/// ???????? - ?? LiteDB?
/// ????????????????????????????????????????????
/// </summary>
public sealed class LocalStorageService : IDisposable
{
    private const string SessionsCollection = "sessions";
    private const string MessagesCollection = "messages";

    private readonly LiteDatabase _db;

    public LocalStorageService(string? dataDirectory = null)
    {
        var dir = AppDataPaths.ResolveDataDirectory(dataDirectory);
        var dbPath = Path.Combine(dir, "SaturnUI.db");

        _db = new LiteDatabase(dbPath, CreateMapper());
        EnsureIndexes();
    }

    private static BsonMapper CreateMapper()
    {
        var mapper = new BsonMapper();

        mapper.Entity<Session>()
            .Id(x => x.Id)
            .Field(x => x.Title, "title")
            .Field(x => x.CreatedAt, "created_at")
            .Field(x => x.UpdatedAt, "updated_at")
            .Ignore(x => x.Messages)
            .Ignore(x => x.IsSelected);

        mapper.Entity<Message>()
            .Id(x => x.Id)
            .Field(x => x.SessionId, "session_id")
            .Field(x => x.Role, "role")
            .Field(x => x.Content, "content")
            .Field(x => x.Timestamp, "timestamp")
            .Field(x => x.IsStreaming, "is_streaming")
            .Field(x => x.IsError, "is_error")
            .Field(x => x.ErrorMessage, "error_message")
            .Field(x => x.AttachmentPath, "attachment_path")
            .Field(x => x.AttachmentName, "attachment_name")
            .Field(x => x.HasAttachment, "has_attachment");

        return mapper;
    }

    private void EnsureIndexes()
    {
        _db.GetCollection<Session>(SessionsCollection)
            .EnsureIndex(x => x.UpdatedAt);

        var messages = _db.GetCollection<Message>(MessagesCollection);
        messages.EnsureIndex(x => x.SessionId);
        messages.EnsureIndex(x => x.Timestamp);
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
            .Query()
            .Where(m => m.SessionId == id)
            .OrderBy(m => m.Timestamp)
            .ToList();

        foreach (var msg in messages)
        {
            msg.IsStreaming = false;
            session.Messages.Add(msg);
        }

        return session;
    }

    public void SaveSession(Session session)
    {
        session.UpdatedAt = session.UpdatedAt == default ? DateTime.Now : session.UpdatedAt;
        _db.GetCollection<Session>(SessionsCollection).Upsert(session);

        var messages = _db.GetCollection<Message>(MessagesCollection);
        foreach (var msg in session.Messages)
        {
            msg.SessionId = session.Id;
            msg.FlushContentBuffer();
            messages.Upsert(msg);
        }
    }

    public void DeleteSession(string id)
    {
        _db.GetCollection<Session>(SessionsCollection).Delete(id);
        _db.GetCollection<Message>(MessagesCollection).DeleteMany(m => m.SessionId == id);
    }

    public void SaveMessage(Message message)
    {
        message.FlushContentBuffer();
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
