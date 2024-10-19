using System;

namespace Entities
{
    public class ExternalDeviceEventArgs : EventArgs
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
}
