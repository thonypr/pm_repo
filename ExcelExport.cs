using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;


namespace pm_repo.ExcelExport
{
    class ExcelExport
    {
        public static void exportDataToExcel(DataSet entries)
        {
            String fileNamePostfix = DateTime.Now.Hour.ToString() + DateTime.Now.Day.ToString() + DateTime.Now.Month.ToString();
            Microsoft.Office.Interop.Excel.Application excelApp = new Microsoft.Office.Interop.Excel.Application();
            Microsoft.Office.Interop.Excel.Workbook excelWorkbook = excelApp.Workbooks.Add();
            foreach (DataTable table in entries.Tables)
            {
                Microsoft.Office.Interop.Excel.Worksheet excelWorksheet = excelWorkbook.Sheets.Add();
                excelWorksheet.Name = table.TableName;
                for (int i = 1; i < table.Columns.Count + 1; i++)
                {
                    excelWorksheet.Cells[1, i] = table.Columns[i - 1].ColumnName;
                }

                for (int j = 0; j < table.Rows.Count; j++)
                {
                    for (int k = 0; k < table.Columns.Count; k++)
                    {
                        excelWorksheet.Cells[j + 2, k + 1] = table.Rows[j].ItemArray[k].ToString();
                    }
                }
            }
            var path = ConfigurationManager.AppSettings["resultsExportPath"].ToString();
            string xp = String.Format("\\results{0}.xlsx", fileNamePostfix);
            string pathf = path + xp;
            excelApp.DisplayAlerts = false;
            excelWorkbook.SaveAs(@pathf);
            excelWorkbook.Close();
            excelApp.Quit();
        }

        public static DataTable entriesHeader()
        {
            DataTable entries = new DataTable("TestResults");
            entries.Columns.Add("Project"); 
            entries.Columns.Add("ProjectId");
            entries.Columns.Add("Milestone");
            entries.Columns.Add("MilestoneId");
            entries.Columns.Add("TestName");
            entries.Columns.Add("TestId");
            entries.Columns.Add("CaseId");
            entries.Columns.Add("TestResultId");
            entries.Columns.Add("CreatedBy");
            entries.Columns.Add("Elapsed");
            entries.Columns.Add("Estimate");
            entries.Columns.Add("Period");
            return entries;
        }

        public static DataSet entriesHeaderWithValues(List<int> results)
        {
            DataTable entries = entriesHeader();
            DataSet entriesValues = new DataSet("TestResults");
            foreach (var item in results)
            {
                //entries.Rows.Add(item.id, item.runName, item.test_id, item.caseId, item.status, item.testName, item.created_by, item.createdOn, item.assignedto_id, item.defects, item.runCreatedOn);
            }
            entriesValues.Tables.Add(entries);
            return entriesValues;
        }
    }
}
