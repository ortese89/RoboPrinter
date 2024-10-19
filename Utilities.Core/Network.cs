using log4net;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace Utilities.core;

public static class Network
{
    private static readonly ILog _Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

    public static bool MapNetworkDrive(string driveLetter, string networkPath)
    {
        ProcessStartInfo psi = new()
        {
            FileName = "cmd.exe",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        Process proc = new()
        {
            StartInfo = psi
        };
        proc.Start();
        proc.StandardInput.WriteLine($"net use {driveLetter} /delete /y");
        proc.StandardInput.WriteLine($"net use {driveLetter} {networkPath}");
        proc.StandardInput.WriteLine("exit");
        proc.WaitForExit();

        if (proc.ExitCode != 0)
        {
            _Log.Debug("MapNetworkDrive - Output: " + proc.StandardOutput.ReadToEnd());
            _Log.Debug("MapNetworkDrive - Error: " + proc.StandardError.ReadToEnd());
            _Log.Debug("MapNetworkDrive - Exit code: " + proc.ExitCode);
        }

        return proc.ExitCode == 0;
    }

    public static void SendEmailMessage(string from, string to, string subject, string body, string attachFilename = "", string mediaType = "image/jpeg")
    {
        var message = new MailMessage(from, to, subject, body);

        if (!string.IsNullOrEmpty(attachFilename))
        {
            using MemoryStream m = new();
            Attachment? inline = null;

            try
            {
                if (File.Exists(attachFilename))
                    inline = new Attachment(attachFilename);
            }
            catch (Exception ex)
            {
                _Log.Error(string.Format("Exception caught attaching file: {0}", ex.Message));
            }

            if (mediaType == "application/pdf")
                message.Body = body;
            else
            {
                if (inline != null)
                {
                    inline.ContentDisposition.Inline = mediaType != "application/pdf";
                    inline.ContentDisposition.DispositionType = mediaType != "application/pdf" ? DispositionTypeNames.Inline : DispositionTypeNames.Attachment;
                    inline.ContentId = "Attach";
                    inline.ContentType.MediaType = mediaType;
                    inline.ContentType.Name = Path.GetFileName(attachFilename);
                    message.Body = $"<h3>{body}</h3><img src=\"cid:{inline.ContentId}\" />";
                }
                else
                    message.Body = string.Format("<h3>{0}</h3>", body);
            }

            message.IsBodyHtml = true;

            if (inline != null)
                message.Attachments.Add(inline);
        }

        using SmtpClient client = new("smtp.gmail.com", 587);
        client.Credentials = new NetworkCredential("logcoditech@gmail.com", "gcpd zaud zegd bcgz");
        client.EnableSsl = true;

        try
        {
            client.Send(message);
            _Log.Debug("Email sent.");
        }
        catch (Exception ex)
        {
            _Log.Error($"Exception caught send email message: {ex.Message}");
        }

        message.Dispose();
    }


    public static async Task SendEmailMessageAsync(string from, string to, string subject, string body, string attachFilename = "", string mediaType = "image/jpeg")
    {
        try
        {
            using MailMessage message = new(from, to, subject, body);
            using MemoryStream m = new();
            Attachment? inline = null;

            if (!string.IsNullOrEmpty(attachFilename) && File.Exists(attachFilename))
                inline = new Attachment(attachFilename);

            if (mediaType == "application/pdf")
                message.Body = body;
            else
            {
                if (inline != null)
                {
                    inline.ContentDisposition.Inline = mediaType != "application/pdf";
                    inline.ContentDisposition.DispositionType = mediaType != "application/pdf" ? DispositionTypeNames.Inline : DispositionTypeNames.Attachment;
                    inline.ContentId = "Attach";
                    inline.ContentType.MediaType = mediaType;
                    inline.ContentType.Name = Path.GetFileName(attachFilename);
                    message.Body = string.Format("<h3>{0}</h3><img src=\"cid:{1}\" />", body, inline.ContentId);
                }
                else
                    message.Body = string.Format("<h3>{0}</h3>", body);
            }

            message.IsBodyHtml = true;

            if (inline != null)
                message.Attachments.Add(inline);

            using SmtpClient client = new("smtp.gmail.com", 587);
            client.Credentials = new NetworkCredential("logcoditech@gmail.com", "gcpd zaud zegd bcgz");
            client.EnableSsl = true;
            await client.SendMailAsync(message);
            _Log.Debug("Email sent.");
        }
        catch (Exception ex)
        {
            _Log.Error($"Exception caught send email message: {ex.Message}");
        }
    }

    //public static void SendErrorByEmail(string message)
    //{
    //    Configurator.Configuration config = new Configurator.Configuration();

    //    string from = "visiontech@coditech.it";
    //    string to = "g.ciarcelluti@coditech.it";
    //    string subject = string.Format("Error notice from {0}", config.System.Description);
    //    SendEmailMessage(from, to, subject, message);
    //}

    public static async Task UploadWebClientResource(string urlparams, string urlimage, string deviceId, NameValueCollection data)
    {
        try
        {
            using HttpClient client = new();
            var keyValues = data.AllKeys
                .SelectMany(key => data.GetValues(key) ?? Array.Empty<string>(),
                            (key, value) => new KeyValuePair<string, string>(key, value))
                .ToList();
            var content = new FormUrlEncodedContent(keyValues);
            await client.PostAsync(urlparams + deviceId, content);
        }
        catch (Exception ex)
        {
            _Log.Error($"Error Sending Sentinel params: {ex.Message}");
        }

        try
        {
            using var client = new HttpClient();
            using var content = new MultipartFormDataContent(); 
            using var fileStream = new FileStream(deviceId + ".jpg", FileMode.Open);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(fileContent, "file", deviceId + ".jpg");
            await client.PostAsync(urlimage, content);
        }
        catch (Exception ex)
        {
            _Log.Error($"Error Sending Sentinel image: {ex.Message}");
        }
    }
}
