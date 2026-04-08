using System;
using System.Configuration;
using System.Timers;
using log4net;
using Meridian.ComplianceReporter.Reports;

namespace Meridian.ComplianceReporter
{
    public class ComplianceReporterService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ComplianceReporterService));
        private Timer dailyTimer;
        private Timer weeklyTimer;
        private Timer monthlyTimer;
        private readonly DailyComplianceReport dailyReport;
        private readonly WeeklyRiskReport weeklyReport;
        private readonly MonthlyAuditReport monthlyReport;

        public ComplianceReporterService()
        {
            dailyReport = new DailyComplianceReport();
            weeklyReport = new WeeklyRiskReport();
            monthlyReport = new MonthlyAuditReport();
        }

        public void Start()
        {
            log.Info("Compliance Reporter Service starting...");

            var dailyInterval = int.Parse(ConfigurationManager.AppSettings["DailyReportIntervalMinutes"] ?? "1440");
            dailyTimer = new Timer(dailyInterval * 60 * 1000);
            dailyTimer.Elapsed += OnDailyReportTimer;
            dailyTimer.Start();
            log.Info($"Daily report timer started (interval: {dailyInterval} minutes)");

            var weeklyInterval = int.Parse(ConfigurationManager.AppSettings["WeeklyReportIntervalMinutes"] ?? "10080");
            weeklyTimer = new Timer(weeklyInterval * 60 * 1000);
            weeklyTimer.Elapsed += OnWeeklyReportTimer;
            weeklyTimer.Start();
            log.Info($"Weekly report timer started (interval: {weeklyInterval} minutes)");

            var monthlyInterval = int.Parse(ConfigurationManager.AppSettings["MonthlyReportIntervalMinutes"] ?? "43200");
            monthlyTimer = new Timer(monthlyInterval * 60 * 1000);
            monthlyTimer.Elapsed += OnMonthlyReportTimer;
            monthlyTimer.Start();
            log.Info($"Monthly report timer started (interval: {monthlyInterval} minutes)");

            log.Info("Compliance Reporter Service started.");
        }

        public void Stop()
        {
            log.Info("Compliance Reporter Service stopping...");
            dailyTimer?.Stop();
            weeklyTimer?.Stop();
            monthlyTimer?.Stop();
            log.Info("Compliance Reporter Service stopped.");
        }

        private void OnDailyReportTimer(object sender, ElapsedEventArgs e)
        {
            log.Info("Daily compliance report timer triggered");
            try
            {
                dailyReport.GenerateReport();
            }
            catch (Exception ex)
            {
                log.Error("Error generating daily compliance report", ex);
            }
        }

        private void OnWeeklyReportTimer(object sender, ElapsedEventArgs e)
        {
            log.Info("Weekly risk report timer triggered");
            try
            {
                weeklyReport.GenerateReport();
            }
            catch (Exception ex)
            {
                log.Error("Error generating weekly risk report", ex);
            }
        }

        private void OnMonthlyReportTimer(object sender, ElapsedEventArgs e)
        {
            log.Info("Monthly audit report timer triggered");
            try
            {
                monthlyReport.GenerateReport();
            }
            catch (Exception ex)
            {
                log.Error("Error generating monthly audit report", ex);
            }
        }
    }
}
