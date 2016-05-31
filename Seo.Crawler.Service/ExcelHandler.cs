using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.IO;

namespace Seo.Crawler.Selenium
{
    public class PageInfoToExcel
    {
        public string LogCount { get; set; }
        public string Error { get; set; }
        public string SourceURL { get; set; }
        public string NotFound { get; set; }
        
    
    }
    public class ExcelHandler
    {
        
        public static DataTable LoadDataTable(string filePath, string sql, string tableName)
        {
            OleDbConnection conn = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath + ";Extended Properties='Excel 12.0 Xml;HDR=YES'");
            OleDbDataAdapter da = new OleDbDataAdapter(sql, conn);
            DataTable dt = new DataTable();
            da.Fill(dt);
            dt.TableName = tableName;
            conn.Close();
            return dt;
        }

        public static void DataTableToExcel(string filePath, DataTable dtDataTable)
        {
            XLWorkbook wb = new XLWorkbook();
            wb.Worksheets.Add(dtDataTable, "Result" + DateTime.Now.ToString("yyyyMMddHHmmss"));
            wb.SaveAs(filePath);
        }
        public static void DataTableToExcel(string filePath, DataTable dtDataTable,string SheetName)
        {
            XLWorkbook wb = new XLWorkbook();
            wb.Worksheets.Add(dtDataTable, SheetName);
            wb.SaveAs(filePath);
        }

        public static DataTable InitTable(DataTable dtDataTable)
        {
            if (dtDataTable != null)
            {
                dtDataTable.Columns.Add("SourceURL", typeof(System.String));
                dtDataTable.Columns.Add("URL", typeof(System.String));
                dtDataTable.Columns.Add("NotFound", typeof(System.String));
                dtDataTable.Columns.Add("Error", typeof(System.String));
                dtDataTable.Columns.Add("LogCount", typeof(System.String));
            }
            return dtDataTable;
        }

        public static DataTable ConvertClassToTable(ConcurrentDictionary<Uri, PageInfoToExcel> pageNotFoundMapping)
        {
            DataTable dt = new DataTable();
            dt = InitTable(dt);

            foreach (var Keys in pageNotFoundMapping.Keys)
            {
                DataRow drRow = dt.NewRow();

                drRow["LogCount"] = pageNotFoundMapping[Keys].LogCount;
                drRow["NotFound"] = pageNotFoundMapping[Keys].NotFound;
                drRow["Error"] = pageNotFoundMapping[Keys].Error;
                drRow["SourceURL"] = pageNotFoundMapping[Keys].SourceURL;
                drRow["URL"] = Keys;
                dt.Rows.Add(drRow);
            }
            return dt;
        }

        public static DataTable LinkToDatatTable(ConcurrentDictionary<Uri,Uri> allLinks)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("URL", typeof(System.String));
            dt.Columns.Add("SourceURL", typeof(System.String));

            foreach (var Url in allLinks.Keys)
            {
                DataRow drRow = dt.NewRow();
                drRow["URL"] = Url.ToString();
                if (allLinks[Url] != null )
                    drRow["SourceURL"] = allLinks[Url].ToString();
                dt.Rows.Add(drRow);
            }
            return dt;
        }


        public static DataTable DictToDatatTable(Dictionary<string, string> allLinks)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("URL", typeof(System.String));
            dt.Columns.Add("Comment", typeof(System.String));

            foreach (var Url in allLinks.Keys)
            {
                DataRow drRow = dt.NewRow();
                drRow["URL"] = Url.ToString();
                drRow["Comment"] = allLinks[Url].ToString();
                dt.Rows.Add(drRow);
            }
            return dt;
        }

        public static string GetLastFileName(string path,string pattern)
        {

            var dirInfo = new DirectoryInfo(path);
            if (dirInfo.GetFiles(pattern).Length != 0)
            {
                var file = (from f in dirInfo.GetFiles(pattern) orderby f.LastWriteTime descending select f).First();
                if (file != null)
                    return file.Name;
                
            }
            return "";
        }

        public static Dictionary<string,Object> LoadFileToDictionary(string file)
        {
            DataTable dt = new DataTable();
            using (XLWorkbook workBook = new XLWorkbook(file))
            {
                //Read the first Sheet from Excel file.
                IXLWorksheet workSheet = workBook.Worksheet(1);

                //Create a new DataTable.
               

                //Loop through the Worksheet rows.
                bool firstRow = true;
                foreach (IXLRow row in workSheet.Rows())
                {
                    //Use the first row to add columns to DataTable.
                    if (firstRow)
                    {
                        foreach (IXLCell cell in row.Cells())
                        {
                            dt.Columns.Add(cell.Value.ToString());
                        }
                        firstRow = false;
                    }
                    else
                    {
                        //Add rows to DataTable.
                        dt.Rows.Add();
                        int i = 0;
                        foreach (IXLCell cell in row.Cells())
                        {
                            dt.Rows[dt.Rows.Count - 1][i] = cell.Value.ToString();
                            i++;
                        }
                    }

                }
            }

            return dt.AsEnumerable().ToDictionary<DataRow, string, object>(row => row.Field<string>(0),
                                row => row.Field<object>(1));

        
        }
        
    }
}
