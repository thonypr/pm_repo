using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace pm_repo
{
    class CsvExport
    {
        #region csv export
        public static void writeDataToFile(List<Model.TestResultEntry> items)
        {
            var path = ConfigurationManager.AppSettings["resultsExportPath"].ToString();
            string xp = "\\results.csv";
            string filename = path + xp;

            var data = string.Empty;
            if (!File.Exists(filename))
            {
                // no file. Then it will be written from the scratch + headers
                // csv -> string with headers
                data = CsvExport.ToCsv(";", items);
            }
            else
            {
                // file exists. Append data without headers
                // csv -> string without headers
                if (File.ReadAllLines(filename).Count() == 0)
                {
                    //0 lines in the file, still need to write it with headers
                    data = CsvExport.ToCsv(";", items);
                }
                else
                {
                    // file exists and is not empty (has header)
                    data = CsvExport.ToCsvWithoutHeaders(";", items);
                }
            }
            //string -> file.csv
            File.AppendAllText(filename, data);
        }

        public static string ToCsvWithoutHeaders<T>(string separator, IEnumerable<T> objectlist)
        {
            Type t = typeof(T);
            PropertyInfo[] fields = t.GetProperties();

            //string header = String.Join(separator, fields.Select(f => f.Name).ToArray());

            StringBuilder csvdata = new StringBuilder();
            //csvdata.AppendLine(header);

            foreach (var o in objectlist)
                csvdata.AppendLine(ToCsvFields(separator, fields, o));

            return csvdata.ToString();
        }

        public static string ToCsvFields(string separator, PropertyInfo[] fields, object o)
        {
            StringBuilder linie = new StringBuilder();

            foreach (var f in fields)
            {
                if (linie.Length > 0)
                    linie.Append(separator);

                var x = f.GetValue(o);

                if (x != null)
                    linie.Append(x.ToString());
            }

            return linie.ToString();
        }

        public static string ToCsv<T>(string separator, IEnumerable<T> objectlist)
        {
            Type t = typeof(T);
            PropertyInfo[] fields = t.GetProperties();

            string header = String.Join(separator, fields.Select(f => f.Name).ToArray());

            StringBuilder csvdata = new StringBuilder();
            csvdata.AppendLine(header);

            foreach (var o in objectlist)
                csvdata.AppendLine(ToCsvFields(separator, fields, o));

            return csvdata.ToString();
        }
        #endregion
    }
}
