using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using log4net;

namespace Meridian.DocumentProcessor.FileWatcher
{
    public class CsvImporter
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(CsvImporter));

        public string ImportCsv(string csvPath)
        {
            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                };

                using (var reader = new StreamReader(csvPath))
                using (var csv = new CsvReader(reader, config))
                {
                    var records = csv.GetRecords<dynamic>().ToList();
                    log.Info($"Imported {records.Count} records from CSV: {csvPath}");
                    
                    var summary = new StringBuilder();
                    summary.AppendLine($"Records: {records.Count}");
                    if (records.Count > 0)
                    {
                        var firstRecord = records[0] as IDictionary<string, object>;
                        summary.AppendLine($"Columns: {string.Join(", ", firstRecord.Keys)}");
                    }
                    return summary.ToString();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error importing CSV: {csvPath}", ex);
                return $"Error: {ex.Message}";
            }
        }
    }
}
