namespace Entities;

public class ExternalDeviceEventArgs : System.EventArgs
{
    public string Content { get; set; }

    public ExternalDeviceEventArgs()
    {
    }

    public ExternalDeviceEventArgs(string content)
    {
        Content = content;
    }
}
