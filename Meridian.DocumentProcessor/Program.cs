using System;
using log4net;
using log4net.Config;
using Topshelf;

namespace Meridian.DocumentProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            var log = LogManager.GetLogger(typeof(Program));
            log.Info("Meridian Document Processor starting...");

            HostFactory.Run(x =>
            {
                x.Service<DocumentProcessorService>(s =>
                {
                    s.ConstructUsing(name => new DocumentProcessorService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });

                x.RunAsLocalSystem();
                x.SetDescription("Meridian Capital Document Processing Service");
                x.SetDisplayName("Meridian Document Processor");
                x.SetServiceName("MeridianDocProcessor");
                x.StartAutomatically();

                x.EnableServiceRecovery(rc =>
                {
                    rc.RestartService(1);
                    rc.SetResetPeriod(1);
                });
            });
        }
    }
}
