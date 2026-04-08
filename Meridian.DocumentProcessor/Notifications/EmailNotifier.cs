using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using log4net;

namespace Meridian.DocumentProcessor.Notifications
{
    public class EmailNotifier
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(EmailNotifier));
        private readonly string smtpServer;
        private readonly int smtpPort;
        private readonly string fromAddress;
        private readonly string toAddress;

        public EmailNotifier()
        {
            smtpServer = ConfigurationManager.AppSettings["SmtpServer"] ?? "smtp.office365.com";
            smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
            fromAddress = ConfigurationManager.AppSettings["FromEmail"] ?? "noreply@meridiancapital.com";
            toAddress = ConfigurationManager.AppSettings["ToEmail"] ?? "compliance@meridiancapital.com";
        }

        public void SendProcessingNotification(string fileName, string documentType, string complianceResult)
        {
            try
            {
                var subject = $"Document Processed: {fileName}";
                var body = $@"
Document Processing Complete

File Name: {fileName}
Document Type: {documentType}
Compliance Result: {complianceResult}
Processed Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

This is an automated notification from Meridian Capital Document Processor.
";

                log.Info($"Sending email notification for {fileName} (simulated)");
                log.Debug($"Subject: {subject}");
                log.Debug($"Body: {body}");
                
            }
            catch (Exception ex)
            {
                log.Error($"Error sending email notification for {fileName}", ex);
            }
        }
    }
}
