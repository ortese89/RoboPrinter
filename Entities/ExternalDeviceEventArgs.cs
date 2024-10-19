namespace Entities;

public class ExternalDeviceEventArgs : System.EventArgs
{
    public int? ProductId { get; set; }
    public string? ProductDescription { get; set; }
    public string Content { get; set; }

    public ExternalDeviceEventArgs()
    {
    }

    public ExternalDeviceEventArgs(string content)
    {
        Content = content;
    }

    public ExternalDeviceEventArgs(int productId, string content)
    {
        ProductId = productId;
        Content = content;
    }

    public ExternalDeviceEventArgs(string productDescription, string content)
    {
        ProductDescription = productDescription;
        Content = content;
    }
}
