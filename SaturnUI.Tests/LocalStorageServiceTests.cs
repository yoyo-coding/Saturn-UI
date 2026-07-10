using SaturnUI.Models;
using SaturnUI.Services;

namespace SaturnUI.Tests;

public class LocalStorageServiceTests
{
    [Fact]
    public void SessionRoundTripKeepsMessagesAttachmentsAndOrder()
    {
        using var temp = new TempDirectory();
        using var storage = new LocalStorageService(temp.Path);

        var session = new Session("demo") { UpdatedAt = DateTime.UtcNow };
        session.Messages.Add(new Message(MessageRole.User, "first")
        {
            Id = "m1",
            Timestamp = new DateTime(2026, 1, 1, 10, 0, 0),
            AttachmentPath = "C:/tmp/a.png",
            AttachmentName = "a.png",
            HasAttachment = true
        });
        session.Messages.Add(new Message(MessageRole.Assistant, "second")
        {
            Id = "m2",
            Timestamp = new DateTime(2026, 1, 1, 10, 0, 1)
        });

        storage.SaveSession(session);
        var loaded = storage.GetSession(session.Id);

        Assert.NotNull(loaded);
        Assert.Equal("demo", loaded!.Title);
        Assert.Equal(2, loaded.Messages.Count);
        Assert.Equal("first", loaded.Messages[0].Content);
        Assert.Equal("second", loaded.Messages[1].Content);
        Assert.True(loaded.Messages[0].HasAttachment);
        Assert.Equal("a.png", loaded.Messages[0].AttachmentName);
        Assert.True(loaded.Messages[0].IsImageAttachment);
    }
}
