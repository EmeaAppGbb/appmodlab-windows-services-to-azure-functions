using System;
using System.Data.SqlClient;
using log4net;
using Meridian.Shared.Data;

namespace Meridian.PortfolioValuation.Notifications
{
    public class AlertService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AlertService));

        public void SendAlert(int portfolioId, string alertType, string message)
        {
            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@PortfolioId", portfolioId),
                    new SqlParameter("@AlertType", alertType),
                    new SqlParameter("@Message", message),
                    new SqlParameter("@CreatedDate", DateTime.Now)
                };

                SqlHelper.ExecuteStoredProcedureNonQuery(StoredProcedures.InsertAlert, parameters);
                
                log.Warn($"ALERT [{alertType}] Portfolio {portfolioId}: {message}");
                
                SendEmailAlert(portfolioId, alertType, message);
            }
            catch (Exception ex)
            {
                log.Error($"Error sending alert for portfolio {portfolioId}", ex);
            }
        }

        private void SendEmailAlert(int portfolioId, string alertType, string message)
        {
            log.Info($"Email alert sent (simulated) - Portfolio: {portfolioId}, Type: {alertType}");
        }
    }
}
