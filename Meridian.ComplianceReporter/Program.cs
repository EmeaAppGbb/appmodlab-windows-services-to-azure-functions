using System;
using log4net;
using log4net.Config;
using Topshelf;

namespace Meridian.ComplianceReporter
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            var log = LogManager.GetLogger(typeof(Program));
            log.Info("Meridian Compliance Reporter starting...");

            HostFactory.Run(x =>
            {
                x.Service<ComplianceReporterService>(s =>
                {
                    s.ConstructUsing(name => new ComplianceReporterService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });

                x.RunAsLocalSystem();
                x.SetDescription("Meridian Capital Compliance Reporting Service");
                x.SetDisplayName("Meridian Compliance Reporter");
                x.SetServiceName("MeridianComplianceReporter");
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
