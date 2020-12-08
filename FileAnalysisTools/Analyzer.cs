using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace FileAnalysisTools
{
    public class Analyzer
    {
        /// <summary>
        /// Creating a structure to retireve and display data.
        /// </summary>
        public class FileList
        {
            public string name { get; set; }
            public string extension { get; set; }
            public string creationTime { get; set; }
            public string lastAccessTime { get; set; }
            public string lastWriteTime { get; set; }
            public string size { get; set; }
            public string fullPath { get; set; }
            public string pastRetention { get; set; }
            //public string owner { get; set; }
        }
        public class ExtensionList
        {
            public string extension { get; set; }
            public int count { get; set; }
        }
        public class OverviewList
        {
            public string reportStats { get; set; }
            public int count { get; set; }
            public string percent { get; set; }
        }

        List<FileList> fileList = new List<FileList>();
        
        //static string watchPath = ConfigurationManager.AppSettings["FilePath"];
        static string securedScanFolder = ConfigurationManager.AppSettings["SecuredFolderScan"];
        static string zzPreCleanseFolder = ConfigurationManager.AppSettings["ZZ_PreCleanseFolder"];
        static string zzdeleteFolder = ConfigurationManager.AppSettings["ZZ_DeleteFolder"];
        static string zzNewFolder = ConfigurationManager.AppSettings["ZZ_NewFolder"];
        static string zzSecuredFolder = ConfigurationManager.AppSettings["ZZ_Secured"];
        static bool OnlySecuredFolder = Convert.ToBoolean(ConfigurationManager.AppSettings["OnlySecuredFolder"].ToLower());

        

        #region Scan Drive

        public void ScanAndReportFileMetaData(string scanPath, out int fValue, out int dValue)
        {
            using (System.Security.Principal.WindowsIdentity.GetCurrent().Impersonate())
            {
                //string[] rootDirectoryList = System.IO.Directory.GetDirectories(scanPath);
                //int fV = 0;
                //int dV = 0;

               // Parallel.ForEach(rootDirectoryList, rootSubFolderPath =>
                //{
                    List<TheFasterWay.FileInformation> filesL = new List<TheFasterWay.FileInformation>();
                    List<TheFasterWay.DirectoryInformation> dirL = new List<TheFasterWay.DirectoryInformation>();
                    // Fetches meta-data using the TheFasterWay class

                    bool success = TheFasterWay.FindNextFilePInvokeRecursiveParalleled(scanPath, out filesL, out dirL);
                    //if (success)
                    //{
                    //    fV = fV + filesL.Count;
                    //    dV = dV + dirL.Count;
                    //}

                //});
                fValue = filesL.Count;
                dValue = dirL.Count;
            }
        }

        /// <summary>
        /// This method is used to fetch meta-data information from a network drive.
        /// </summary>
        public void ScanAndReportFileMetaData(string scanPath, string reportPath)
        {
            List<FileList> fD = new List<FileList>();
            StreamWriter sWriter = null;
            object fileListLock = new object();
            object writeListLock = new object();

            // Checks and creates report directory where the meta-data reports are saved, if it doesn't exists. ex: C:\FileWatcher_Reports
            string reportFolderPath = System.IO.Path.GetDirectoryName(reportPath);
            //if path consists of more then one sub-folder
            string[] pathArray = reportFolderPath.Split('\\');
            string rootPath = pathArray[0];
            
            for (int i = 1; i <= pathArray.Length - 1; i++)
            {
                rootPath = rootPath + "\\" + pathArray[i];
                if (!System.IO.Directory.Exists(rootPath))
                    System.IO.Directory.CreateDirectory(rootPath);
            }
           
            // Checks and creates report text file whose name is constructed using scanned folder name and current date time.
            if (!System.IO.File.Exists(reportPath))
            {
                sWriter = System.IO.File.CreateText(reportPath);
                sWriter.Close();
            }

            using (System.Security.Principal.WindowsIdentity.GetCurrent().Impersonate())
            {
                string[] rootDirectoryList = System.IO.Directory.GetDirectories(scanPath);
                //int fValue = 0;
                //int dValue = 0;
                //int dirCount = System.IO.Directory.GetDirectories(scanPath).Count();
                //string[][] chunk = rootDirectoryList
                //.Select((s, i) => new { Value = s, Index = i })
                //.GroupBy(x => x.Index / (dirCount / 2))
                //.Select(grp => grp.Select(x => x.Value).ToArray())
                //.ToArray();
                //Parallel.ForEach(chunk, x =>
                //{
                //foreach (string rootSubFolderPath in rootDirectoryList)
                Parallel.ForEach(rootDirectoryList, rootSubFolderPath =>
                {
                    List<TheFasterWay.FileInformation> filesL = new List<TheFasterWay.FileInformation>();
                    List<TheFasterWay.DirectoryInformation> dirL = new List<TheFasterWay.DirectoryInformation>();
                    // Fetches meta-data using the TheFasterWay class

                    bool success = TheFasterWay.FindNextFilePInvokeRecursiveParalleled(rootSubFolderPath, out filesL, out dirL);
                    //int filesCount = filesL.Count;
                    //int dirCount = dirL.Count;
                    if (success)
                    {
                        //lock (fileListLock)
                        //{
                            //Parallel.ForEach(filesL, f =>
                            foreach (TheFasterWay.FileInformation f in filesL)
                            {
                                if ((f != null) && !f.Name.StartsWith("~$") && !f.Name.Contains("Thumbs.db") && !f.Name.Contains(".tmp") && !f.Name.Contains(".temp"))
                                {
                                    // Converts the fetched file information to a list and writes it to a text file with '|' delimiter.
                                    fD = AddFileInformationToFileList(f);
                                    lock (writeListLock)
                                    {
                                        WriteToReportFile(reportPath, fD);
                                    }
                                }
                            }
                        //}
                    }

                //});
                
                });
            }
        }

        /// <summary>
        /// Converts the fetched meta-data to a list.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        private List<FileList> AddFileInformationToFileList(TheFasterWay.FileInformation f)
        {
            List<FileList> fD = new List<FileList>();
            fD.Add(new FileList()
            {
                name = f.Name,
                creationTime = f.CreationTime.ToString(),
                lastAccessTime = f.LastAccessTime.ToString(),
                lastWriteTime = f.LastWriteTime.ToString(),
                size = f.Length.ToString(), // SizeFormat(f.Length) TODO: Revert back to SizeFormat. this was done to get size in bytes for Scott's report
                fullPath = f.FullPath,
                //extension = f.Extension,
                //owner = f.Owner,
            });
            return fD;
        }

        private string GetExtensionFromFileName(string filename)
        {
           return filename.Split('.')[filename.Split('.').Length - 1];
        }
        /// <summary>
        /// Write meta data into the report file.
        /// </summary>
        /// <param name="sW"></param>
        /// <param name="Listfd"></param>
        private void WriteToReportFile(string path, List<FileList> Listfd)
        {
            StreamWriter sW = null;
            using (sW = System.IO.File.AppendText(path))
            {
                //Parallel.ForEach(Listfd, fD =>
                foreach (FileList fD in Listfd)
                {
                    sW.Write(fD.name +
                        "|" + fD.creationTime +
                        "|" + fD.lastAccessTime +
                        "|" + fD.lastWriteTime +
                        "|" + fD.size +
                        "|" + fD.fullPath +
                        "|" + GetExtensionFromFileName(fD.name) + ";\n"
                        // "|" + fD.owner + ";\n"
                        );
                }
                sW.Close();
            }
        }

        /// <summary>
        /// This method is used to convert file size from bytes to string format in 
        /// </summary>
        /// <param name="len"></param>
        /// <returns></returns>
        public string SizeFormat(double len)
        {
            try
            {
                string[] sizes = { "Bytes", "KB", "MB", "GB", "TB" };
                int order = 0;
                if (len < 0)
                    len = len * -1;

                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                return String.Format("{0:0.##} {1}", len, sizes[order]);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string ReverseSizeFormat(string strFileSize)
        {
            try
            {
                string[] sizes = { "Bytes", "KB", "MB", "GB", "TB" };
                string[] splitSize = strFileSize.Split(' ');
                double numberSize = Convert.ToDouble(splitSize[0]);
                string strSize = splitSize[1];
                int order = Array.IndexOf(sizes, strSize);
                double bytes = numberSize * (double)Math.Pow(1024, order);
                return Convert.ToString(bytes);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion Scan Drive

        #region Extract Report to DataTable
        /// <summary>
        /// Reads the '|' delimited text report and creates data table based on defined filters.
        /// </summary>
        public System.Data.DataSet ReadTextReport(string reportFolderPath, string retentionYear, bool csvExtract)
        {
            Common common = new Common();
            DataSet aboveAll = new DataSet();
            //int retentionYear = Convert.ToInt32(ConfigurationManager.AppSettings["RetentionYear"]);

            // Checks if a text report already exists in the folder. If it exists, current fullpath gets added to the export excel report.
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(reportFolderPath);
            var lastRunReportfInfo = dir.EnumerateFiles("*.txt").Where(y => y.Name.Contains(System.IO.Path.GetFileName(reportFolderPath)) & !y.Name.Contains("Log")).OrderByDescending(y => y.LastWriteTime).First();

            if (lastRunReportfInfo != null)
            {
                using (StreamReader sr = System.IO.File.OpenText(lastRunReportfInfo.FullName))
                {
                    string s;
                    while ((s = sr.ReadLine()) != null)
                    //Parallel.ForEach(File.ReadLines(lastRunReportfInfo.FullName), s =>
                    {
                        fileList.Add(new FileList()
                        {
                            name = s.Split('|')[0],
                            extension = s.Split('|')[6].Replace(';', ' ').Trim().ToUpper(),
                            creationTime = s.Split('|')[1],
                            lastAccessTime = s.Split('|')[2],
                            lastWriteTime = s.Split('|')[3],
                            size = s.Split('|')[4],
                            fullPath = s.Split('|')[5],
                            pastRetention = common.GetPastRetentionStatus(s.Split('|')[3], Convert.ToInt32(retentionYear)),
                            //owner = s.Split('|')[7],
                        });
                    }
                }
            }
            if (fileList.Count > 0)
            {
                if (!string.IsNullOrEmpty(securedScanFolder))
                {
                    if (OnlySecuredFolder)
                        fileList = fileList.Where(r => r.fullPath.Contains("\\" + securedScanFolder + "\\") || r.fullPath.Contains(zzSecuredFolder)).ToList();
                    else
                        fileList = fileList.Where(r => !r.fullPath.Contains("\\" + securedScanFolder + "\\") && !r.fullPath.Contains(zzSecuredFolder)).ToList();
                }
                // Creates a data table with data for Original sheet in the excel report. 
                // It consists of all the file meta-data recorded in the text report.
                //System.Data.DataTable exportDataTable = new System.Data.DataTable("Original");
                System.Data.DataTable dupDataTable = new System.Data.DataTable("Duplicates");
                System.Data.DataTable zeroDataTable = new System.Data.DataTable("ZeroBytes");
                System.Data.DataTable deDupDataTable = new System.Data.DataTable("Distinct");
                System.Data.DataTable extensionDataTable = new System.Data.DataTable("ExtensionsCount");
                //System.Data.DataTable extExceptionDataTable = new System.Data.DataTable("ExtensionExceptions");
                System.Data.DataTable pastRetentionDataTable = new System.Data.DataTable("PastRetention");
                //System.Data.DataTable overViewDataTable = new System.Data.DataTable("OverView");

                //var result = fileList.GroupBy(x => x.fullPath).Select(group => group.Last()).OrderByDescending(r => r.lastWriteTime).ToList();
                ////exportDataTable = new System.Data.DataTable("Original");
                //exportDataTable = ListToDataTable(result, exportDataTable);
                //int originalCount = exportDataTable.Rows.Count - 1; // -1 is to avoid header row from being included in the count.
                //result.Clear();
                //aboveAll.Tables.Add(exportDataTable);

                // When CSV file is extracted, the original data table is only required to create the csv file
                // For creating an excel report, all the data tables are required. The csvExtract parameter determines if its running for creating an excel report or CSV.
                if (!csvExtract)
                {
                    // Creates a data table with data for Distinct sheet in the excel report. 
                    // Its created by grouping files with same name and size, and from the group picking the recently modified file to create a list of distinct files.
                    var deDupResult = fileList.GroupBy(x => new { x.name, x.size }).Select(r => r.OrderByDescending(a => a.lastWriteTime).First()).ToList();
                    deDupDataTable = ListToDataTable(deDupResult, deDupDataTable);
                    int movedDistinctCount = deDupResult.Where(x => x.fullPath.Contains(zzPreCleanseFolder) || x.fullPath.Contains(zzNewFolder) || x.fullPath.Contains(zzdeleteFolder)).Count(); // -1 is to avoid header row from being included in the count.
                    aboveAll.Tables.Add(deDupDataTable);

                    // Creates a data table with data for Duplicates sheet in the excel report. 
                    // This report is created by excluding all the distinct records created above.
                    var dupResult = fileList.Except(deDupResult).ToList();
                    dupDataTable = ListToDataTable(dupResult, dupDataTable);
                    dupResult.Clear();
                    deDupResult.Clear();
                    aboveAll.Tables.Add(dupDataTable);

                    // Creates a data table with data for Zero Bytes sheet in the excel report.
                    // This report lists out all the files with size 0 Bytes.
                    var zeroByteResult = fileList.Where(x => x.size == "0").Select(g => g).ToList();
                    zeroDataTable = ListToDataTable(zeroByteResult, zeroDataTable);
                    int movedZeroBytes = zeroByteResult.Where(x => x.fullPath.Contains(zzPreCleanseFolder) || x.fullPath.Contains(zzNewFolder) || x.fullPath.Contains(zzdeleteFolder)).Count();
                    zeroByteResult.Clear();
                    aboveAll.Tables.Add(zeroDataTable);

                    // Creates a data table with data for Extension Counts sheet in the excel report.
                    // Since this report consists of only two columns, another class was implemented named ExtensionList. This consists of all the extensions and its count.
                    var extensionResult = fileList.GroupBy(n => n.extension).Select(n => new ExtensionList
                    {
                        extension = n.Key,
                        count = n.Count()
                    }).AsEnumerable().OrderByDescending(n => n.count).ToList();
                    extensionDataTable = ListToDataTable(extensionResult, extensionDataTable);
                    extensionResult.Clear();
                    aboveAll.Tables.Add(extensionDataTable);

                    // Creates a data table with data for Extension exceptions sheet in the excel report.
                    // This report consists of file file information whose extension is provided in the list below.
                    //var exceptionlist = new List<string> { "dm", "adf", "bil", "bt2", "pr", "blk", "gdb", "gi", "geosoft_voxel", "ts", "pl", "vs", "vo", "mig", "pen", "tab", "mif", "grd", "grc", "dtm", "mdl", "00t", "bmf" };
                    //var extExceptionResult = fileList.Where(p => exceptionlist.Any(l => p.extension == l.Trim().ToString())).ToList();
                    //extExceptionDataTable = ListToDataTable(extExceptionResult, extExceptionDataTable); 
                    //extExceptionResult.Clear();
                    //aboveAll.Tables.Add(extExceptionDataTable);

                    var pastRetentionResult = fileList.Where(x => x.pastRetention == "YES").Select(g => g).ToList();
                    pastRetentionDataTable = ListToDataTable(pastRetentionResult, pastRetentionDataTable);
                    int retentionCount = pastRetentionDataTable.Rows.Count - 1; // -1 is to avoid header row from being included in the count.
                    pastRetentionResult.Clear();
                    aboveAll.Tables.Add(pastRetentionDataTable);

                    //int movedDelete = fileList.Where(x => x.fullPath.Contains(zzdeleteFolder)).Count();
                    //int movedPrecleanse = fileList.Where(x => x.fullPath.Contains(zzPreCleanseFolder)).Count();
                    //int movedNewFolder = fileList.Where(x => x.fullPath.Contains(zzNewFolder)).Count();
                    //int totalFilesToBeMoved = originalCount - (movedDelete + movedPrecleanse + movedNewFolder);
                    //List<int> valueList = new List<int> { originalCount, retentionCount, movedDistinctCount, movedZeroBytes, movedDelete, movedPrecleanse, movedNewFolder, totalFilesToBeMoved };

                    //var overviewResult = CreateOverviewList(valueList);
                    //overViewDataTable = ListToDataTable(overviewResult, overViewDataTable);
                    //aboveAll.Tables.Add(overViewDataTable);
                }

                fileList.Clear();
            }
            return aboveAll;
        }

        /// <summary>
        /// Adds value to overview list.
        /// </summary>
        /// <param name="valueList"></param>
        /// <returns></returns>
        private List<OverviewList> CreateOverviewList(List<int> valueList)
        {
            int orignalCount = valueList[0];
            List<string> statsStr = new List<string>
            {
                "Total files on scan",
                "Records past retention",
                "Total distinct files moved",
                "Total zero byte files moved",
                "Total files moved to ZZ_Delete",
                "Total files moved to ZZ_PreCleanse",
                "Total files moved to  ZZ_New",
                "Total files still to be moved"
            };
            List<OverviewList> overView = new List<OverviewList>();
            int i = 0;
            foreach (string stat in statsStr)
            {
                overView.Add(new OverviewList()
                {
                    reportStats = stat,
                    count = valueList[i],
                    percent = stat == statsStr[0] ? "" : string.Format("{0:0.00}%", Convert.ToDouble((valueList[i] * 100) / orignalCount))
                });
                i++;
            }
            return overView;
        }

        /// <summary>
        /// Converts an overview list to data table with headers to move to excel or csv format.
        /// </summary>
        /// <param name="overViewList"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        private System.Data.DataTable ListToDataTable(List<OverviewList> overViewList, System.Data.DataTable dt)
        {
            dt.Columns.Add("reportStats");
            dt.Columns.Add("count");
            dt.Columns.Add("percent");

            var headerRow = dt.NewRow();
            headerRow["reportStats"] = "Report Statistics";
            headerRow["count"] = "Count";
            headerRow["percent"] = "Percentage";
            dt.Rows.Add(headerRow);
            foreach (var val in overViewList)
            {
                var row = dt.NewRow();
                row["reportStats"] = val.reportStats;
                row["count"] = val.count;
                row["percent"] = val.percent;
                dt.Rows.Add(row);
            }
            return dt;
        }

        /// <summary>
        /// Converts an extension list to data table with headers to move to excel or csv format.
        /// </summary>
        /// <param name="extList"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        private System.Data.DataTable ListToDataTable(List<ExtensionList> extList, System.Data.DataTable dt)
        {
            dt.Columns.Add("extension");
            dt.Columns.Add("count");

            var headerRow = dt.NewRow();
            headerRow["extension"] = "Extensions";
            headerRow["count"] = "Count";
            dt.Rows.Add(headerRow);
            foreach (var lst in extList)
            {
                var row = dt.NewRow();
                row["extension"] = lst.extension;
                row["count"] = lst.count;
                dt.Rows.Add(row);
            }
            return dt;
        }

        /// <summary>
        /// Converts a file information list to data table with headers to move to excel or csv format.
        /// </summary>
        /// <param name="fLst"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        private System.Data.DataTable ListToDataTable(List<FileList> fLst, System.Data.DataTable dt)
        {
            dt.Columns.Add("name");
            dt.Columns.Add("extension");
            dt.Columns.Add("creationTime");
            dt.Columns.Add("lastAccessTime");
            dt.Columns.Add("lastWriteTime");
            dt.Columns.Add("size");
            dt.Columns.Add("fullPath");
            dt.Columns.Add("pastRetention");
            var headerRow = dt.NewRow();

            headerRow["name"] = "Name";
            headerRow["extension"] = "Extensions";
            headerRow["creationTime"] = "Create Time";
            headerRow["lastAccessTime"] = "Last Accessed";
            headerRow["lastWriteTime"] = "Last Modified";
            headerRow["size"] = "File Size";
            headerRow["fullPath"] = "FullPath";
            headerRow["pastRetention"] = "Past Retention";
            dt.Rows.Add(headerRow);

            foreach (var file in fLst)
            {
                var row = dt.NewRow();
                row["name"] = file.name;
                row["extension"] = file.extension;
                row["creationTime"] = file.creationTime;
                row["lastAccessTime"] = file.lastAccessTime;
                row["lastWriteTime"] = file.lastWriteTime;
                row["size"] = file.size;
                row["fullPath"] = file.fullPath;
                row["pastRetention"] = file.pastRetention;
                dt.Rows.Add(row);
            }
            return dt;
        }

        #endregion Extract Report to DataTable

        #region DataTable to Excel

        /// <summary>
        /// Creates excel report from the extracted '|' delimited text report  
        /// </summary>
        public bool CreateExcelReport(string reportFolderPath, string retentionYear, string reportDate)
        {
            string exportFolderPath = Path.Combine(reportFolderPath, "Export");
            string scanFolder = Path.GetFileName(reportFolderPath);
            System.Data.DataSet aboveAll = ReadTextReport(reportFolderPath, retentionYear, false);
            if (aboveAll != null)
            {
                if (!System.IO.Directory.Exists(exportFolderPath))
                    System.IO.Directory.CreateDirectory(exportFolderPath);

                Microsoft.Office.Interop.Excel.Application oXL = new Microsoft.Office.Interop.Excel.Application();
                oXL.DefaultSaveFormat = XlFileFormat.xlOpenXMLWorkbook;
                oXL.Visible = false;
                Workbook oWB = oXL.Workbooks.Add(Type.Missing);

                // Loops through the data set which contains all the data tables, each datatable creates a worksheet in excel.
                foreach (System.Data.DataTable dt in aboveAll.Tables)
                {
                    Worksheet oSheet = oWB.Sheets.Add(Type.Missing, Type.Missing, 1, Type.Missing) as Worksheet;
                    oSheet.Name = dt.TableName;
                    if (dt.Rows.Count > 0)
                        WriteArray(dt, dt.Rows.Count, dt.Columns.Count, oSheet);
                }

                if (string.IsNullOrEmpty(reportDate))
                {
                    System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(reportFolderPath);
                    var lastRunReportfInfo = dir.EnumerateFiles("*.txt").Where(y => y.Name.Contains(System.IO.Path.GetFileName(reportFolderPath)) & !y.Name.Contains("Log")).OrderByDescending(y => y.LastWriteTime).First();
                    reportDate = Path.GetFileNameWithoutExtension(lastRunReportfInfo.FullName).Split('_')[1];
                }

                //string fileName = System.IO.Path.GetDirectoryName(ConfigurationManager.AppSettings["ExportFolderPath"]) + "\\Export\\" + scanFolder + "\\Export_" + reportDate + ".xlsx";
                string fileName = System.IO.Path.Combine(exportFolderPath, "Export_xL_" + scanFolder + "_" + reportDate + ".xlsx");
                oWB.SaveAs(fileName, XlFileFormat.xlOpenXMLWorkbook,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    XlSaveAsAccessMode.xlExclusive,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                oWB.Close(true, Type.Missing, Type.Missing);
                oXL.Quit();
                Marshal.ReleaseComObject(oXL);
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Writes data from  the data table to the worksheet in array.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="rows"></param>
        /// <param name="columns"></param>
        /// <param name="worksheet"></param>
        private void WriteArray(System.Data.DataTable dt, int rows, int columns, Worksheet worksheet)
        {
            var data = new object[rows, columns];
            for (var row = 1; row <= rows; row++)
            {
                for (var column = 1; column <= columns; column++)
                {
                    data[row - 1, column - 1] = dt.Rows[row - 1].ItemArray[column - 1].ToString();
                }
            }

            var startCell = (Range)worksheet.Cells[1, 1];
            var endCell = (Range)worksheet.Cells[rows, columns];
            var writeRange = worksheet.Range[startCell, endCell];

            writeRange.Value2 = data;
        }

        #endregion DataTable to Excel

        #region Move Files & Folder

        /// <summary>
        /// Creates a list of file paths from duplicates
        /// </summary>
        public void ExecutePreCleanse(string reportFolderPath, string reportDate)
        {
            string exportFolderPath = Path.Combine(reportFolderPath, "Export");
            using (System.Security.Principal.WindowsIdentity.GetCurrent().Impersonate())
            {
                //List<string> lstPath = CreateFolderStructure("Duplicates").Where(x => !x.Contains("Thumbs.db") && !x.Contains(".tmp")).Select(y => y).ToList();
                //List<string> lstZeroPath = CreateFolderStructure("ZeroBytes").Where(x => !x.Contains("Thumbs.db") && !x.Contains(".tmp")).Select(y => y).ToList();
                List<string> pastRetention = CreateFolderStructure("PastRetention", exportFolderPath, ConfigurationManager.AppSettings["FilePath"]).Where(x => !x.Contains("Thumbs.db") && !x.Contains(".tmp")).Select(y => y).ToList();
                //List<string> extExcep = CreateFolderStructure("ExtensionExceptions", exportFolderPath).Where(x => !x.Contains("Thumbs.db") && !x.Contains(".tmp")).Select(y => y).ToList();

                //lstPath.AddRange(lstZeroPath);
                //lstPath.AddRange(pastRetention);
                //lstPath.AddRange(extExcep);
                MoveFiles(pastRetention.ToList(), reportFolderPath, reportDate);
            }
        }
        public void CreateFoldersForLegalDelete(string reportFolderPath, string reportDate)
        {
            string exportFolderPath = Path.Combine(reportFolderPath, "Export");
            using (System.Security.Principal.WindowsIdentity.GetCurrent().Impersonate())
            {
                CreateFolderStructure("Original", exportFolderPath, ConfigurationManager.AppSettings["FilePath"]).Where(x => !x.Contains("Thumbs.db") && !x.Contains(".tmp")).Select(y => y).ToList();
            }
        }

        public void RevertMovePreCleanse(string exportFolderPath, string reportFolderPath, string reportDate, string xlSheetToRead, string watchPath)
        {
            List<string> fileNames = new List<string>();
            List<string> newFileNames = new List<string>();
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(exportFolderPath);
            var xlPath = dir.GetFiles().Where(x => !x.FullName.StartsWith("~$")).OrderByDescending(f => f.LastWriteTime).First();
            fileNames = GetFullPathFromExcel(xlPath.FullName, xlSheetToRead, watchPath).Where(x => x.Contains("ZZ_PRECLEANSE\\Government Relations\\ZZ_Delete")).ToList();

            zzPreCleanseFolder = System.IO.Path.Combine(watchPath, zzPreCleanseFolder);
            zzNewFolder = System.IO.Path.Combine(watchPath, zzNewFolder);
            zzdeleteFolder = System.IO.Path.Combine(watchPath, zzdeleteFolder);
            zzSecuredFolder = System.IO.Path.Combine(watchPath, zzSecuredFolder);

            using (System.Security.Principal.WindowsIdentity.GetCurrent().Impersonate())
            {
                foreach (string f in fileNames)
                {
                    
                    //newFileNames.Add(newString);
                    string path = zzPreCleanseFolder; //change to precleanse
                    string[] fn = f.Split('\\').Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    for (int i = 0; i < fn.Length - 1; i++)
                    {
                        path = path + "\\" + fn[i];
                        if (!Delimon.Win32.IO.Directory.Exists(path))
                            Delimon.Win32.IO.Directory.CreateDirectory(path);
                    }
                }
                NewMoveFiles(fileNames, reportFolderPath, reportDate);
            }
        }

        private void NewMoveFiles(List<string> listOfSourcePath, string reportFolderPath, string reportDate)
        {
            System.IO.FileInfo logInfo = new System.IO.FileInfo(System.IO.Path.Combine(reportFolderPath, "MoveLog_" + reportDate + ".txt"));
            StreamWriter sW;
            string rootFilePath = ConfigurationManager.AppSettings["FilePath"];

            if (!System.IO.File.Exists(logInfo.FullName))
            {
                sW = System.IO.File.CreateText(logInfo.FullName);
                sW.Close();
            }
            Delimon.Win32.IO.FileInfo fileinfo;
            foreach (string sourcePath in listOfSourcePath)
            {
                if (Delimon.Win32.IO.File.Exists(Delimon.Win32.IO.Path.Combine(rootFilePath, sourcePath)))
                {
                    fileinfo = new Delimon.Win32.IO.FileInfo(Delimon.Win32.IO.Path.Combine(rootFilePath, sourcePath));
                    string destination = sourcePath.Substring(45, sourcePath.Length - 45); //("\\ZZ_PRECLEANSE\\ZZ_PRECLEANSE\\Government Relations", ""); //\ZZ_PRECLEANSE\Government Relations\ZZ_Delete
                    try
                    {
                        fileinfo.MoveTo(Delimon.Win32.IO.Path.Combine(zzdeleteFolder, destination));//change to precleanse
                        using (sW = System.IO.File.AppendText(logInfo.FullName))
                        {
                            sW.WriteLine("Files moved to:" + System.IO.Path.Combine(zzdeleteFolder, sourcePath));//change to precleanse
                            sW.Close();
                        }
                    }
                    catch
                    {
                        using (sW = System.IO.File.AppendText(logInfo.FullName))
                        {
                            sW.WriteLine("Error while moving:" + System.IO.Path.Combine(rootFilePath, sourcePath));
                            sW.Close();
                        }
                    }
                }
                else
                {
                    using (sW = System.IO.File.AppendText(logInfo.FullName))
                    {
                        sW.WriteLine("Already moved or deleted:" + System.IO.Path.Combine(rootFilePath, sourcePath));
                        sW.Close();
                    }
                }
            }
        }

        private List<string> CreateFolderStructure(string xlSheetToRead, string excelReportPath, string watchPath)
        {
            List<string> fileNames = new List<string>();
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(excelReportPath);
            var xlPath = dir.GetFiles().Where(x => !x.FullName.StartsWith("~$")).OrderByDescending(f => f.LastWriteTime).First();

            fileNames = GetFullPathFromExcel(xlPath.FullName, xlSheetToRead, watchPath).Where(x => !x.Contains(zzPreCleanseFolder) && !x.Contains(zzdeleteFolder) && !x.Contains(zzNewFolder) && !x.Contains(zzSecuredFolder)).ToList();
            
            if (OnlySecuredFolder)
            {
                zzSecuredFolder = System.IO.Path.Combine(watchPath, zzSecuredFolder);
                zzPreCleanseFolder = System.IO.Path.Combine(zzSecuredFolder, zzPreCleanseFolder);
                zzNewFolder = System.IO.Path.Combine(zzSecuredFolder, zzNewFolder);
                zzdeleteFolder = System.IO.Path.Combine(zzSecuredFolder, zzdeleteFolder);

            }
            else
            {
                zzPreCleanseFolder = System.IO.Path.Combine(watchPath, zzPreCleanseFolder);
                zzNewFolder = System.IO.Path.Combine(watchPath, zzNewFolder);
                zzdeleteFolder = System.IO.Path.Combine(watchPath, zzdeleteFolder);
                zzSecuredFolder = System.IO.Path.Combine(watchPath, zzSecuredFolder);
            }

            if (!System.IO.Directory.Exists(zzPreCleanseFolder))
                System.IO.Directory.CreateDirectory(zzPreCleanseFolder);
            if (!System.IO.Directory.Exists(zzNewFolder))
                System.IO.Directory.CreateDirectory(zzNewFolder);
            if (!System.IO.Directory.Exists(zzdeleteFolder))
                System.IO.Directory.CreateDirectory(zzdeleteFolder);
            if (!System.IO.Directory.Exists(zzSecuredFolder))
                System.IO.Directory.CreateDirectory(zzSecuredFolder);
            string[] folderParam = new string[] { zzPreCleanseFolder, zzNewFolder, zzdeleteFolder };
            foreach (string folder in folderParam)
            {
                foreach (string f in fileNames)
                {
                    string path = folder; //change to precleanse
                    string[] fn = f.Split('\\').Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    for (int i = 0; i < fn.Length - 1; i++)
                    {
                        path = path + "\\" + fn[i];
                        if (!Delimon.Win32.IO.Directory.Exists(path))
                            Delimon.Win32.IO.Directory.CreateDirectory(path);
                    }
                }
            }
            return fileNames;
        }

        private void MoveFiles(List<string> listOfSourcePath, string reportFolderPath, string reportDate)
        {
            System.IO.FileInfo logInfo = new System.IO.FileInfo(System.IO.Path.Combine(reportFolderPath, "MoveLog_" + reportDate + ".txt"));
            StreamWriter sW;
            string rootFilePath = ConfigurationManager.AppSettings["FilePath"];

            if (!System.IO.File.Exists(logInfo.FullName))
            {
                sW = System.IO.File.CreateText(logInfo.FullName);
                sW.Close();
            }
            Delimon.Win32.IO.FileInfo fileinfo;
            foreach (string sourcePath in listOfSourcePath)
            {
                if (Delimon.Win32.IO.File.Exists(Delimon.Win32.IO.Path.Combine(rootFilePath, sourcePath)))
                {
                    fileinfo = new Delimon.Win32.IO.FileInfo(Delimon.Win32.IO.Path.Combine(rootFilePath, sourcePath));
                    try
                    {
                        fileinfo.MoveTo(Delimon.Win32.IO.Path.Combine(zzPreCleanseFolder, sourcePath));//change to precleanse
                        using (sW = System.IO.File.AppendText(logInfo.FullName))
                        {
                            sW.WriteLine("Files moved to:" + System.IO.Path.Combine(zzPreCleanseFolder, sourcePath));//change to precleanse
                            sW.Close();
                        }
                    }
                    catch
                    {
                        using (sW = System.IO.File.AppendText(logInfo.FullName))
                        {
                            sW.WriteLine("Error while moving:" + System.IO.Path.Combine(rootFilePath, sourcePath));
                            sW.Close();
                        }
                    }
                }
                else
                {
                    using (sW = System.IO.File.AppendText(logInfo.FullName))
                    {
                        sW.WriteLine("Already moved or deleted:" + System.IO.Path.Combine(rootFilePath, sourcePath));
                        sW.Close();
                    }
                }
            }
        }

        private List<string> GetFullPathFromExcel(string fullPathToExcel, string sheetToRead, string textToRemovefromPathList)
        {
            Microsoft.Office.Interop.Excel.Application xlsApp = new Microsoft.Office.Interop.Excel.Application();

            Workbook wb = xlsApp.Workbooks.Open(fullPathToExcel,
            0, true, 5, "", "", true, XlPlatform.xlWindows, "\t", false, false, 0, true);
            Worksheet sheets = (Worksheet)wb.Worksheets[sheetToRead];

            //Worksheet ws = (Worksheet)sheets.get_Item(3);

            Range fullPathColumn = sheets.UsedRange.Columns[7];
            System.Array myvalues = (System.Array)fullPathColumn.Cells.Value;
            return RemoveCommonTextFromFilePathList(myvalues, textToRemovefromPathList);
            //List<string> pathList = myvalues.OfType<object>().Select(o => o.ToString().Replace(ConfigurationManager.AppSettings["FilePath"], "")).ToList().Skip(1).ToList(); // skip is used to avoid header row.
            //return pathList;
        }

        private List<string> RemoveCommonTextFromFilePathList(Array filePathArray, string textToRemove)
        {
            return filePathArray.OfType<object>().Select(o => o.ToString().Replace(textToRemove, "")).ToList().Skip(1).ToList();
        }

        #endregion Move Files & Folder

        #region DataTable To CSV

        /// <summary>
        /// This method is called from to export into CSV format.
        /// </summary>
        /// <param name="reportFolderPath"></param>
        /// <returns></returns>
        public bool ExportToCSV(string reportFolderPath, System.Data.DataTable exportDataTable)
        {
            try
            {
                string exportFolderPath = Path.Combine(reportFolderPath, "Export");
                if (exportDataTable == null)
                    return false;

                if (!System.IO.Directory.Exists(exportFolderPath))
                    System.IO.Directory.CreateDirectory(exportFolderPath);

                StringBuilder sb = new StringBuilder();
                //IEnumerable<string> columnNames = exportDataTable.Columns.Cast<DataColumn>().
                //                      Select(column => column.ColumnName);

                //sb.AppendLine(string.Join(",", columnNames));
                //DataTable dt = GetData();
                foreach (DataRow dr in exportDataTable.Rows)
                {
                    foreach (DataColumn dc in exportDataTable.Columns)
                        sb.Append(DataTableToCSV(dr[dc.ColumnName].ToString()) + ",");
                    sb.Remove(sb.Length - 1, 1);
                    sb.AppendLine();
                }
                string exportPath = string.Format("{0}\\Export_CSV_{1}_{2}.csv", exportFolderPath,exportDataTable.TableName, DateTime.Now.ToString("yyyyMMddhhmmss"));
                System.IO.File.WriteAllText(exportPath, sb.ToString());
                //sb.Clear();
                exportDataTable.Clear();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// This method creates csv report from a data table.
        /// Since the reporting format has been changed to excel with separate sheets in the same file. This method is not in use anymore.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string DataTableToCSV(string input)
        {
            try
            {
                if (input == null)
                    return string.Empty;

                bool containsQuote = false;
                bool containsComma = false;
                int len = input.Length;
                for (int i = 0; i < len && (containsComma == false || containsQuote == false); i++)
                {
                    char ch = input[i];
                    if (ch == '"')
                        containsQuote = true;
                    else if (ch == ',')
                        containsComma = true;
                }

                if (containsQuote && containsComma)
                    input = input.Replace("\"", "\"\"");

                if (containsComma)
                    return "\"" + input + "\"";
                else
                    return input;
            }
            catch
            {
                throw;
            }
        }

        #endregion DataTable To CSV
    }
}
