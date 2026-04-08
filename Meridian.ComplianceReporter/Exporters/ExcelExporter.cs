using System;
using System.Data;
using System.IO;
using log4net;
using OfficeOpenXml;

namespace Meridian.ComplianceReporter.Exporters
{
    public class ExcelExporter
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ExcelExporter));

        public void ExportToExcel(DataTable data, string filePath, string sheetName)
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add(sheetName);

                    for (int col = 0; col < data.Columns.Count; col++)
                    {
                        worksheet.Cells[1, col + 1].Value = data.Columns[col].ColumnName;
                        worksheet.Cells[1, col + 1].Style.Font.Bold = true;
                    }

                    for (int row = 0; row < data.Rows.Count; row++)
                    {
                        for (int col = 0; col < data.Columns.Count; col++)
                        {
                            worksheet.Cells[row + 2, col + 1].Value = data.Rows[row][col];
                        }
                    }

                    worksheet.Cells.AutoFitColumns();
                    
                    var fileInfo = new FileInfo(filePath);
                    package.SaveAs(fileInfo);
                }
                log.Info($"Excel exported successfully: {filePath}");
            }
            catch (Exception ex)
            {
                log.Error($"Error exporting Excel to {filePath}", ex);
                throw;
            }
        }
    }
}
