using System;
using System.IO;
using log4net;
using Meridian.DocumentProcessor.Processing;
using Meridian.DocumentProcessor.Notifications;

namespace Meridian.DocumentProcessor.FileWatcher
{
    public class IncomingDocWatcher
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(IncomingDocWatcher));
        private readonly FileSystemWatcher watcher;
        private readonly DocumentClassifier classifier;
        private readonly DataExtractor extractor;
        private readonly ComplianceChecker complianceChecker;
        private readonly EmailNotifier notifier;

        public IncomingDocWatcher(string path)
        {
            watcher = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                Filter = "*.*",
                EnableRaisingEvents = false
            };

            watcher.Created += OnFileCreated;
            watcher.Error += OnError;

            classifier = new DocumentClassifier();
            extractor = new DataExtractor();
            complianceChecker = new ComplianceChecker();
            notifier = new EmailNotifier();
        }

        public void StartWatching()
        {
            watcher.EnableRaisingEvents = true;
            log.Info($"File watcher started on path: {watcher.Path}");
        }

        public void StopWatching()
        {
            watcher.EnableRaisingEvents = false;
            log.Info("File watcher stopped");
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                log.Info($"New file detected: {e.Name}");
                System.Threading.Thread.Sleep(500);

                var docType = classifier.ClassifyDocument(e.FullPath);
                log.Info($"Document classified as: {docType}");

                var extractedData = extractor.ExtractData(e.FullPath, docType);
                log.Info($"Data extracted from document: {extractedData.Length} characters");

                var complianceResult = complianceChecker.CheckCompliance(extractedData, docType);
                log.Info($"Compliance check result: {complianceResult}");

                notifier.SendProcessingNotification(e.Name, docType, complianceResult);
                log.Info($"Processing notification sent for: {e.Name}");
            }
            catch (Exception ex)
            {
                log.Error($"Error processing file {e.Name}", ex);
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            log.Error("FileSystemWatcher error", e.GetException());
        }
    }
}
