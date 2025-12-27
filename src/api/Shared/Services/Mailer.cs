using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.Pkcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
//using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using System.Security.Cryptography.X509Certificates;
using Google.Apis.Services;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Google.Apis.Util.Store;
using System.IO;
using System.Net.Mail;
using Google.Apis.Storage.v1.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using Amazon.S3.Model;
using Newtonsoft.Json;
using MailKit.Security;
using MimeKit;

namespace Shared;

public static class MailerExtensions
{
    public static void AddMailer(this IServiceCollection services, IConfiguration configuration)
    {
        Console.WriteLine($"Mailer Provider: {configuration["Mailer:Provider"]}");

        if (configuration["Mailer:Provider"] == "SMTP")
        {
            services.AddSingleton<IMailer, SmtpMailer>();
        }

        if (configuration["Mailer:Provider"] == "MAILTRAP")
        {
            services.AddSingleton<IMailer, MailTrapMailer>();
        }
    }
}

public interface IMailer
{
    public void Send(string to, string subject, string content, string displayName = null);
    public void Send(string to, string subject, string content, Dictionary<string, object> bindingValues, string displayName = null);
}

public class SmtpMailer : IMailer
{
    protected IConfiguration Configuration { get; set; }
    private readonly MailSettings Settings;

    public SmtpMailer(IConfiguration configuration, IOptions<MailSettings> settings)
    {
        Configuration = configuration;
        Settings = settings.Value;
    }

    public void Send(string to, string subject, string content, Dictionary<string, object> bindingValues, string displayName = null)
    {

        foreach (var item in bindingValues)
        {
            var value = item.Value?.ToString();

            //formatting data type here (ex. number, date, etc)
            content = content.Replace("[" + item.Key + "]", value);
        }

        Send(to, subject, content, displayName);
    }

    public void Send(string to, string subject, string content, string displayName = null)
    {
        if (string.IsNullOrWhiteSpace(to))
            return;

#if DEBUG
        to = "momogie@yopmail.com";
#endif

        try
        {

            // Initialize a new instance of the MimeKit.MimeMessage class
            var mail = new MimeMessage();

            #region Sender / Receiver
            // Sender
            mail.From.Add(new MailboxAddress(Settings.DisplayName, Settings.Email));
            mail.Sender = new MailboxAddress(displayName ?? Settings.DisplayName, Settings.Email);

            // Receiver
            mail.To.Add(MailboxAddress.Parse(to));
            #endregion

            #region Content

            // Add Content to Mime Message
            var body = new BodyBuilder();
            mail.Subject = subject;
            body.HtmlBody = content;
            mail.Body = body.ToMessageBody();

            #endregion

            #region Send Mail

            using var smtp = new MailKit.Net.Smtp.SmtpClient();

            if (Settings.UseSSL)
            {
                smtp.Connect(Settings.Host, Settings.Port, SecureSocketOptions.SslOnConnect);
            }
            else if (Settings.UseStartTls)
            {
                smtp.Connect(Settings.Host, Settings.Port, SecureSocketOptions.StartTls);
            }
            else
            {
                smtp.Connect(Settings.Host, Settings.Port);
            }

            smtp.Authenticate(Settings.Email, Settings.Password);
            smtp.Send(mail);
            smtp.Disconnect(true);

            #endregion
        }
        catch// (Exception ex)
        {
            throw;
        }
    }
}

public class MailTrapMailer : IMailer
{
    protected IConfiguration Configuration { get; set; }

    protected HttpClient Client;

    protected ConcurrentQueue<EmailItem> QueueList { get; set; } = new ConcurrentQueue<EmailItem>();

    protected System.Threading.Thread Process { get; set; }

    public MailTrapMailer(IConfiguration configuration)
    {
        Configuration = configuration;
        Process = new System.Threading.Thread(Dequeue);

        Client = new HttpClient();
        Client.BaseAddress = new Uri($"{Configuration["Mailer:MailTrap:Host"]}/api/send");
        Client.DefaultRequestHeaders.Add("Accept", "application/json");
        Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Configuration["Mailer:MailTrap:Token"]}");
    }

    public void Send(string to, string subject, string templateName, Dictionary<string, object> bindingValues, string displayName = null)
    {
        string path = AppDomain.CurrentDomain.BaseDirectory;
        string filePath = Path.Combine(path, "Resources", "Templates", "Html", $"{templateName}.html");
        string templateContent = File.ReadAllText(filePath);

        foreach (var item in bindingValues)
        {
            var value = item.Value?.ToString();

            //formatting data type here (ex. number, date, etc)
            templateContent = templateContent.Replace("[" + item.Key + "]", value);
        }

        Send(to, subject, templateContent, displayName);
    }

    public void Send(string to, string subject, string content, string displayName = null)
    {
        if (string.IsNullOrWhiteSpace(to))
            return;

        if (!Process.IsAlive)
            Process.Start();

        var item = new EmailItem
        {
            To = to,
            Subject = subject,
            Payload = new
            {
                to = new List<object>
                {
                    new
                    {
                        email = to
                    }
                },
                from = new
                {
                    email = Configuration["Mailer:MailTrap:From"],
                    name = Configuration["Mailer:MailTrap:DisplayName"],
                },
                headers = new Dictionary<string, string>(),
                subject,
                html = content,
                category = "",
            }
        };

        QueueList.Enqueue(item);
        Console.WriteLine($"[{DateTime.UtcNow}] To:{to}, Subject:{subject}");
    }

    private void Dequeue()
    {
        while (true)
        {
            var limitPerInterval = Convert.ToInt32(Configuration["Mailer:MailTrap:LimitPerInterval"]);
            for (int i = 0; i < limitPerInterval; i++)
            {
                QueueList.TryDequeue(out EmailItem item);
                if (item != null)
                {
                    try
                    {
                        var data = JsonConvert.SerializeObject(item.Payload);
                        var httpContent = new StringContent(data, Encoding.UTF8, "application/json");
                        Client.PostAsync("/api/send", httpContent).Wait();

                        Console.WriteLine($"[{DateTime.UtcNow}] [Queue:({QueueList.Count})] Sending mail To {item.To}, Subject: {item.Subject}: OK");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.UtcNow}] Sending mail To {item?.To}, Subject: {item?.Subject} Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                    }
                }
            }

            System.Threading.Thread.Sleep(Convert.ToInt32(Configuration["Mailer:MailTrap:LimitInterval"]) * 1000);
        }
    }

    public class EmailItem
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public object Payload { get; set; }
    }
}

public class MailSettings
{
    public string DisplayName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public bool UseSSL { get; set; }
    public bool UseStartTls { get; set; }
}