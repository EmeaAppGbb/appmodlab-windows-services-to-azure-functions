using System;
using System.Configuration;
using System.IO;
using log4net;
using Meridian.DocumentProcessor.FileWatcher;
using Meridian.DocumentProcessor.Processing;
using Meridian.DocumentProcessor.Notifications;

namespace Meridian.DocumentProcessor
{
    public class DocumentProcessorService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DocumentProcessorService));
        private IncomingDocWatcher docWatcher;
        private readonly string watchPath;

        public DocumentProcessorService()
        {
            watchPath = ConfigurationManager.AppSettings["IncomingDocPath"] ?? @"C:\MeridianData\Incoming";
        }

        public void Start()
        {
            log.Info("Document Processor Service starting...");
            
            if (!Directory.Exists(watchPath))
            {
                Directory.CreateDirectory(watchPath);
                log.Info($"Created watch directory: {watchPath}");
            }

            docWatcher = new IncomingDocWatcher(watchPath);
            docWatcher.StartWatching();

            log.Info($"Document Processor Service started. Watching: {watchPath}");
        }

        public void Stop()
        {
            log.Info("Document Processor Service stopping...");
            docWatcher?.StopWatching();
            log.Info("Document Processor Service stopped.");
        }
    }
}
