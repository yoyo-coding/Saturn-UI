using SaturnUI.Models;

namespace SaturnUI.Tests;

public class MessageTests
{
    [Fact]
    public void StreamingContentIsFlushedOnComplete()
    {
        var message = new Message(MessageRole.Assistant, string.Empty)
        {
            IsStreaming = true
        };

        message.AppendContent("hello");
        message.AppendContent(" world");
        message.CompleteStreaming();

        Assert.Equal("hello world", message.Content);
        Assert.False(message.IsStreaming);
    }

    [Theory]
    [InlineData("image.png", true)]
    [InlineData("photo.WEBP", true)]
    [InlineData("notes.pdf", false)]
    public void AttachmentTypeDetectsImages(string fileName, bool expected)
    {
        var message = new Message
        {
            HasAttachment = true,
            AttachmentPath = Path.Combine("C:\\tmp", fileName)
        };

        Assert.Equal(expected, message.IsImageAttachment);
    }
}
