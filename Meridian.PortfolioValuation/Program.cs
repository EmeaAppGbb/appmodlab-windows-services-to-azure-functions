using System;
using log4net;
using log4net.Config;
using Topshelf;

namespace Meridian.PortfolioValuation
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            var log = LogManager.GetLogger(typeof(Program));
            log.Info("Meridian Portfolio Valuation starting...");

            HostFactory.Run(x =>
            {
                x.Service<ValuationService>(s =>
                {
                    s.ConstructUsing(name => new ValuationService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });

                x.RunAsLocalSystem();
                x.SetDescription("Meridian Capital Portfolio Valuation Service");
                x.SetDisplayName("Meridian Portfolio Valuation");
                x.SetServiceName("MeridianPortfolioValuation");
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
