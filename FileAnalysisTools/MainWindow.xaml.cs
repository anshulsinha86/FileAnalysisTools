using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using System.Collections.ObjectModel;
using System.Data;
using System.Security.Cryptography;
using Delimon.Win32.IO;
using Microsoft.Office.Interop.Excel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Data.OleDb;
using System.Diagnostics;


namespace FileAnalysisTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        Analyzer fileAnalyzer = new Analyzer();
        static string reportDate;
        static string watchPath = ConfigurationManager.AppSettings["FilePath"];
        static string scanFolder;
        static string reportFolderPath;
        //static string exportFolderPath = System.IO.Path.Combine(reportFolderPath, "Export");

        //static string reportFolderPath = System.IO.Path.Combine(ConfigurationManager.AppSettings["ReportFolderPath"], scanFolder);
        //static string reportPath = System.IO.Path.Combine(reportFolderPath, scanFolder + "_" + reportDate + ".txt");

        static string retentionYear = ConfigurationManager.AppSettings["RetentionYear"];
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Event
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string[] multipleWatchPath = watchPath.Split(',');
                foreach (string mPath in multipleWatchPath)
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    reportDate = DateTime.Now.ToString("yyyyMMddhhmmss");
                    scanFolder = System.IO.Path.GetFileName(mPath);
                    if (string.IsNullOrEmpty(scanFolder))
                    {
                        DriveInfo drive = new DriveInfo(mPath);
                        scanFolder = drive.ToString();
                        scanFolder = scanFolder.Substring(0, 1);
                    }
                    reportFolderPath = System.IO.Path.Combine(ConfigurationManager.AppSettings["ReportFolderPath"], scanFolder);
                    string reportPath = System.IO.Path.Combine(reportFolderPath, scanFolder + "_" + reportDate + ".txt");
                    string logPath = System.IO.Path.Combine(reportFolderPath, scanFolder + "_Log.txt");
                    fileAnalyzer.ScanAndReportFileMetaData(mPath, reportPath);
                    stopwatch.Stop();
                    //MessageBox.Show(this, "Scanned meta-data in =" + (stopwatch.ElapsedMilliseconds / 1000) + "seconds");
                    TimeSpan elapsedTime = TimeSpan.FromSeconds(stopwatch.ElapsedMilliseconds / 1000);
                    StreamWriter sW = null;
                    using (sW = System.IO.File.AppendText(logPath))
                    {
                        
                        string strTime = elapsedTime.ToString(@"hh\:mm\:ss\:fff");
                        string logText = string.Format("Scanned path: {0} on Date Time: {1}, scan time: {2} ", mPath, reportDate, strTime);
                        sW.Write(logText+"\n");
                    }
                }
                MessageBox.Show("done!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Stay calm, and contact the developer with the error: {0}", ex.ToString()));
            }
        }

        private void BtnCreateExcelReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Analyzer fileAnalyzer = new Analyzer();
                string[] multipleWatchPath = watchPath.Split(',');
                foreach (string mPath in multipleWatchPath)
                {
                    scanFolder = System.IO.Path.GetFileName(mPath);
                    if (string.IsNullOrEmpty(scanFolder))
                    {
                        DriveInfo drive = new DriveInfo(mPath);
                        scanFolder = drive.ToString();
                        scanFolder = scanFolder.Substring(0, 1);
                    }
                                            
                    if(string.IsNullOrEmpty(reportFolderPath))
                        reportFolderPath = System.IO.Path.Combine(ConfigurationManager.AppSettings["ReportFolderPath"], scanFolder);

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    bool success = fileAnalyzer.CreateExcelReport(reportFolderPath, retentionYear, reportDate);
                    stopwatch.Stop();
                    if (success)
                        MessageBox.Show(this, "Created excel report in =" + (stopwatch.ElapsedMilliseconds / 1000) + "seconds");
                    else
                        MessageBox.Show(this, "No Excel report created.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error while creating excel report: {0}", ex.ToString()));
            }
        }

        private void BtnMove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Analyzer fileAnalyzer = new Analyzer();
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                fileAnalyzer.ExecutePreCleanse(reportFolderPath, reportDate);
                stopwatch.Stop();
                MessageBox.Show(this, "Move duplicates/zero byte files in=" + (stopwatch.ElapsedMilliseconds / 1000) + "seconds");
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error while moving files: {0}", ex.ToString()));
            }
        }

        #endregion Event

        #region useless
        //private void Duplicates()
        //{
        //    List<string> filePathList = new List<string>();
        //    if (string.IsNullOrEmpty(path))
        //        return;
        //    foreach (TheFasterWay.FileInformation files in filesList)
        //    {
        //        if (files.Length != 0 && files.Extension != "SYS")
        //            filePathList.Add(files.FullPath);
        //    }
        //    var dupFiles = filePathList.Select(f => new
        //    {
        //        FileName = f,
        //        FileHash = Encoding.UTF8.GetString(new SHA1Managed().ComputeHash(new FileStream(f, FileMode.Open, FileAccess.Read)))
        //    })
        //    .GroupBy(f => f.FileHash)
        //    .Select(g => new { FileHash = g.Key, Files = g.Select(z => z.FileName).ToList() })
        //    .SelectMany(f => f.Files.Skip(1))
        //    .ToList();
        //    //.ForEach(File.Delete);

        //    foreach (var file in dupFiles)
        //    {
        //        using (StreamWriter sW = System.IO.File.AppendText(System.IO.Path.Combine(reportFolderPath, "DuplicateFileNames.txt")))
        //        {
        //            sW.Write(string.Format("{0} has duplicates\n", file));
        //        }
        //    }
        //}
        //private void DuplicateList()
        //{
        //    System.IO.DirectoryInfo directoryInfo = new System.IO.DirectoryInfo(watchPath);
        //    var files = directoryInfo.GetFiles();
        //    var duplicateGroups = files.GroupBy(file => file.Name )
        //                   .Where(group => group.Count() > 1);

        //    // Replace with what you want to do
        //    foreach (var group in duplicateGroups)
        //    {
        //        Console.WriteLine("Files with name {0}", group.Key);
        //        foreach (var file in group)
        //        {
        //            using (StreamWriter sW = System.IO.File.AppendText(System.IO.Path.Combine(reportFolderPath, "DuplicateFileNames.txt")))
        //            {
        //                sW.Write(string.Format("{0} has duplicates\n", file.FullName));
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// The method reads the text report and displays in the datagrid.
        /// </summary>
        //private void SearchReport()
        //{
        //    try
        //    {

        //        int total = 0;
        //        grdMain.ItemsSource = null;
        //        Common common = new Common();
        //        EeekSoft.Text.StringSearch search = new StringSearch();
        //        List<string> lstFullPath = new List<string>();
        //        List<FileList> fList = new List<FileList>();
        //        //IDictionary<string, string> orgList = new Dictionary<string, string>();
        //        //int MAX = (int)Math.Floor((double)(Int32.MaxValue / 5000));
        //        //int a = 0;
        //        int a = 0;

        //        string reportFileName = DateTime.Today.ToString("ddMMyyyy") + ".txt";
        //        //if (reportFileName == lstOfReportFiles[i])
        //        if (!System.IO.File.Exists(System.IO.Path.Combine(reportFolderPath, reportFileName)))
        //            reportFileName = DateTime.Today.AddDays(-1).ToString("ddMMyyyy") + ".txt";
        //        //using (StreamReader sr1 = File.OpenText(System.IO.Path.Combine(reportFolderPath, "FirstRunReport.txt")))
        //        //{
        //        //    string s1;
        //        //    while ((s1 = sr1.ReadLine()) != null)
        //        //    {
        //        //        orgList.Add(s1.Split('|')[5], s1.Split('|')[4]);
        //        //    }
        //        //}
        //        using (StreamReader sr = System.IO.File.OpenText(System.IO.Path.Combine(reportFolderPath, reportFileName)))
        //        {
        //            string s;
        //            while ((s = sr.ReadLine()) != null)
        //            {
        //                lstFullPath.Add(s.Split('|')[5]);
        //            }
        //        }
        //        //string s;
        //        //allLines = new string[MAX];
        //        //while (!sr.EndOfStream)
        //        //{
        //        // allLines.Add(sr.ReadLine());
        //        //using (StreamReader sr1 = File.OpenText(System.IO.Path.Combine(reportFolderPath, "FirstRunReport.txt")))
        //        //{
        //        //    string s1;

        //        //}

        //        StringSearchResult row;
        //        List<string> commonFromFirstRun = new List<string>();
        //        IStringSearchAlgorithm searchAlgo = new StringSearch();
        //        searchAlgo.Keywords = lstFullPath.ToArray();
        //        using (StreamReader sx = System.IO.File.OpenText(System.IO.Path.Combine(reportFolderPath, "FirstRunGovRelationsReport.txt")))
        //        {

        //            string sd;
        //            while ((sd = sx.ReadLine()) != null)
        //            {
        //                row = searchAlgo.FindFirst(sd);
        //                if (!string.IsNullOrEmpty(row.Keyword))
        //                    commonFromFirstRun.Add(sd.Split('|')[4] + "|" + row.Keyword);
        //            }
        //        }

        //        //StreamReader ss = new StreamReader((System.IO.Path.Combine(reportFolderPath, "FirstRunReport.txt")));
        //        //string rows = ss.ReadToEnd();
        //        using (StreamReader sr = System.IO.File.OpenText(System.IO.Path.Combine(reportFolderPath, reportFileName)))
        //        {
        //            string s;
        //            StringSearchResult[] resultss;
        //            string oldSize = string.Empty;
        //            while ((s = sr.ReadLine()) != null)
        //            {
        //                string orgSize = string.Empty;
        //                //allLines.Add(s);
        //                string filePath = s.Split('|')[5];
        //                string currentSize = s.Split('|')[4];
        //                foreach (var matched in commonFromFirstRun)
        //                {
        //                    if (matched.Contains(filePath))
        //                        oldSize = matched.Split('|')[0];
        //                    else
        //                        oldSize = "0 Bytes";



        //                    //string orignalFileSize = sr1.ReadLine().Split('|')[5].Where(x => x == filePath).FistOrDefault();
        //                    //if (orgList.ContainsKey(filePath))
        //                    //{
        //                    //    orgSize = orgList[filePath];
        //                    //}
        //                    //var row = File.ReadLines(System.IO.Path.Combine(reportFolderPath, "FirstRunReport.txt")).FirstOrDefault(x => x.Contains(filePath));
        //                    //string row = rows.FirstOrDefault(x => x.Contains(filePath));
        //                    //if (row == null)
        //                    //    orgSize = "0 Bytes";
        //                    //else
        //                    //    orgSize = row.Split('|')[4];
        //                    //string[] filePathArray = new string[] { filePath };
        //                    //List<int> asd = common.FindAllStates(s, lstFullPath.ToArray());
        //                    // using (Delimon.Win32.IO.File.OpenText(System.IO.Path.Combine(reportFolderPath, "FirstRunReport.txt")))
        //                    //using (var mmf = MemoryMappedFile.CreateFromFile(System.IO.Path.Combine(reportFolderPath, "FirstRunReport.txt")))
        //                    //{
        //                    //    using (var stream = mmf.CreateViewStream())
        //                    //    {
        //                    //        using (StreamReader binReader = new StreamReader(stream))
        //                    //        {
        //                    //            resultss = searchAlgo.FindAll(binReader.ReadToEnd());
        //                    //        }
        //                    //    }
        //                    //}

        //                    //using (StreamReader sx = System.IO.File.OpenText(System.IO.Path.Combine(reportFolderPath, "FirstRunReport.txt")))
        //                    //{

        //                    fileList.Add(new FileList()
        //                    {
        //                        name = s.Split('|')[0],
        //                        extension = s.Split('|')[6],
        //                        //count = File.ReadLines((System.IO.Path.Combine(reportFolderPath, reportFileName))).Count(x => x == filePath),
        //                        count = lstFullPath.Count(x => x == filePath),
        //                        //count = common.FindAllStates(s, lstFullPath.ToArray()).Count,
        //                        //count = lstFullPath.Where(x => x == filePath).Count(),//FileCount(common.FetchValues(line[5])),
        //                        //count = searchAlgo.FindAll(sr.ReadToEnd())
        //                        lastWriteTime = s.Split('|')[3],
        //                        size = currentSize,
        //                        delta = General.Common.CalculateDelta(currentSize, oldSize),
        //                        fullPath = filePath,
        //                        moveToSharepoint = common.GetSharePointStatus(s.Split('|')[3])
        //                    });
        //                }
        //                //a = a+1;
        //                //}
        //                //}
        //                //string s1;
        //                //while ((s1 = sr.ReadLine()) != null)
        //                //{
        //                //    extList.Add(s1.Split('|')[6]);
        //            }

        //        }
        //        //Parallel.For(0, allLines.Count(), x =>
        //        //{
        //        //Execute(a);
        //        //ds.Tables.Add(dt);
        //        //dt.Rows.Add(fileList);
        //        //}
        //        //});
        //        //string[] line = sr.ReadLine().Split('|');
        //        //if (line[0].Contains(txtSearch.Text))
        //        //{
        //        //    fileList.Add(new FileList()
        //        //    {
        //        //        name = common.FetchValues(line[0]),
        //        //        extension = common.DisplayTextForExtension(common.FetchValues(line[6])),
        //        //        count = FileCount(common.FetchValues(line[5])),
        //        //        size = common.FetchValues(line[4]),
        //        //        fullPath = common.FetchValues(line[5]),
        //        //        moveToSharepoint = common.GetSharePointStatus(Convert.ToDateTime(common.FetchValues(line[3])))
        //        //    });
        //        //    //ds.Tables.Add(dt);
        //        //    //dt.Rows.Add(fileList);
        //        //}

        //        var result = fileList.GroupBy(x => x.fullPath).Select(group => group.Last()).OrderByDescending(r => r.lastWriteTime).ToList();
        //        // Assign search result to data table for export.
        //        exportDataTable = ListToDataTable(result, exportDataTable);

        //        ListCollectionView displayFileList = new ListCollectionView(result);
        //        displayFileList.GroupDescriptions.Add(new PropertyGroupDescription("name"));

        //        grdMain.ItemsSource = displayFileList;
        //        displayFileList.Refresh();
        //        result.Clear();
        //        fileList.Clear();


        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //private void SubDirectoryCount()
        //{
        //    int count = 0;
        //    List<string> dirList = new List<string>();
        //    List<string> result = new List<string>();
        //    using (StreamReader sx = System.IO.File.OpenText(System.IO.Path.Combine(reportFolderPath, "FilePath_Legal_Repo.txt")))
        //    {

        //        string s;
        //        string folderName = string.Empty;
        //        while ((s = sx.ReadLine()) != null)
        //        {
        //            int subFolderLength = s.Split('\\').Count() - 1;
        //            string[] ss = s.Split('\\');
        //            for (int y = 0; y < subFolderLength; y++)
        //            {
        //                folderName = s.Split('\\')[y];
        //                if (y  != subFolderLength && !string.IsNullOrEmpty(s.Split('\\')[y + 1]))
        //                {
        //                    count = System.IO.File.ReadAllLines(System.IO.Path.Combine(reportFolderPath, "FilePath_Legal_Repo.txt")).Select(x => x.Split('\\')[y + 1]).Distinct().Count();
        //                    result.Add(string.Format("{0} has {1} subdirectories", folderName, count.ToString()));
        //                    //foreach (string r in result)
        //                    //{
        //                    //ount = extList.Where(x => x == ext).Count();
        //                    using (StreamWriter sW = System.IO.File.AppendText(System.IO.Path.Combine(reportFolderPath, "Legal_subFolder_Count_Repo.txt")))
        //                    {
        //                        sW.Write(string.Format("{0} has {1} subdirectories\n", folderName, count.ToString()));
        //                    }
        //                }
        //                else return;
        //                //{

        //                //    if (c + 1 == subFolderLength)
        //                //        break;
        //                //    else
        //                //        dirList.Add(s.Split('\\')[c + 1]);
        //                //}
        //                //= dirList.Distinct().Count();

        //            }
        //        }

        //    }

        //    //string sd;
        //    //while ((sd = sx.ReadLine()) != null)
        //    //{
        //    //    string subFilePath = sd.Substring(19, sd.Length);
        //    //    string[] subDirectories = subFilePath.Split('\\');
        //    //    for (int i = 0; i <=subDirectories.Length; i++)
        //    //    {

        //    //    }
        //    //    //row = searchAlgo.FindAll(legal);
        //    //    sd.Replace("\"", "");
        //    //    if (sd.Length > 260)
        //    //    {

        //    //    }
        //    //}
        //    MessageBox.Show("Done!");
        //}

        //private void CheckFilePathLength()
        //{
        //    List<string> extList = new List<string>();
        //    using (StreamReader sx = System.IO.File.OpenText(System.IO.Path.Combine(reportFolderPath, "Legal_Repo.txt")))
        //    {

        //        string sd;
        //        while ((sd = sx.ReadLine()) != null)
        //        {
        //            //row = searchAlgo.FindAll(legal);
        //            sd.Replace("\"", "");
        //            if (sd.Length > 260)
        //            {
        //                extList.Add(sd);

        //            }
        //        }
        //    }
        //    foreach (string ext in extList)
        //    {
        //        //ount = extList.Where(x => x == ext).Count();
        //        using (StreamWriter sW = System.IO.File.AppendText(System.IO.Path.Combine(reportFolderPath, "FilePath_Legal_Repo.txt")))
        //        {
        //            sW.Write(ext + "\n");
        //        }

        //    }
        //    MessageBox.Show("Done!");
        //}

        //private int FileCount(string filePath)
        //{
        //    string reportFileName = DateTime.Today.ToString("ddMMYYYY") + ".txt";
        //    //if (reportFileName == lstOfReportFiles[i])
        //    if (!File.Exists(System.IO.Path.Combine(reportFolderPath, reportFileName)))
        //        reportFileName = DateTime.Today.AddDays(-1).ToString("ddMMYYYY") + ".txt";
        //    StreamReader sr = new StreamReader("C:\\FileWatcher_Reports\\Reports.txt");
        //    int total = 0;
        //    while (!sr.EndOfStream)
        //    {
        //        var lines = sr.ReadLine().Split('|');
        //        string fullpath = FetchValues(lines[5]);
        //        if (fullpath == filePath)
        //            total = total + 1;
        //    }
        //    return total;

        //}

        //private void SearchLegalFolder()
        //{
        //    StringSearchResult[] row;
        //    List<string> extList = new List<string>();
        //    string reportFileName = string.Empty;
        //    StreamWriter sW = null;
        //    string legal = "Legal";
        //    StreamReader sr = new StreamReader(System.IO.Path.Combine(reportFolderPath, "FirstRunReport.txt"));
        //    IStringSearchAlgorithm searchAlgo = new StringSearch();
        //    //searchAlgo.Keywords = legal;
        //    //int total = 0;
        //    string s;
        //    while ((s = sr.ReadLine()) != null)
        //    {
        //        using (StreamReader sx = System.IO.File.OpenText(System.IO.Path.Combine(reportFolderPath, "FirstRunReport.txt")))
        //        {

        //            string sd;
        //            while ((sd = sx.ReadLine()) != null)
        //            {
        //                //row = searchAlgo.FindAll(legal);
        //                if (sd.Contains("\\\\brspfp01\\shared\\Legal"))
        //                {
        //                    extList.Add(sd);

        //                }
        //            }
        //        }
        //        foreach (string ext in extList)
        //        {
        //            //ount = extList.Where(x => x == ext).Count();
        //            using (sW = System.IO.File.AppendText(System.IO.Path.Combine(reportFolderPath, "Legal_Repo.txt")))
        //            {
        //                sW.Write(ext + "\n");
        //            }

        //        }
        //    }
        //    //using (sW = System.IO.File.AppendText(System.IO.Path.Combine(reportFolderPath, "Legal_Repo.txt")))
        //    //{
        //    //    sW.Write(s + "\n");
        //    //}
        //}

        //private void GetFilepath()
        //{
        //    StringSearchResult[] row;
        //    List<string> extList = new List<string>();
        //    string reportFileName = string.Empty;
        //    StreamWriter sW = null;
        //    string legal = "Legal";
        //    StreamReader sr = new StreamReader(System.IO.Path.Combine(reportFolderPath, "GovRelations_Repo.txt"));
        //    IStringSearchAlgorithm searchAlgo = new StringSearch();
        //    //searchAlgo.Keywords = legal;
        //    //int total = 0;
        //    string s;
        //    //while ((s = sr.ReadLine()) != null)
        //    //{
        //        //using (StreamReader sx = System.IO.File.OpenText(System.IO.Path.Combine(reportFolderPath, "FirstRunReport.txt")))
        //        //{

        //            string sd;
        //    while ((sd = sr.ReadLine()) != null)
        //    {
        //        string[] filePath = sd.Split('|');
        //        string fp = filePath[5];
        //        //row = searchAlgo.FindAll(legal);
        //        //if (sd.Contains("\\\\brspfp01\\shared\\Government Relations"))
        //        //{
        //        extList.Add(fp);

        //        //}
        //    }
        //    // }
        //    foreach (string ext in extList)
        //    {
        //        //ount = extList.Where(x => x == ext).Count();
        //        using (sW = System.IO.File.AppendText(System.IO.Path.Combine(reportFolderPath, "GovRelations_filepath_Repo.txt")))
        //        {
        //            sW.Write(ext + "\n");
        //        }

        //    }

        //}

        //private void SearchGovRelationFolder()
        //{
        //    StringSearchResult[] row;
        //    List<string> extList = new List<string>();
        //    string reportFileName = string.Empty;
        //    StreamWriter sW = null;
        //    string legal = "Legal";
        //    StreamReader sr = new StreamReader(System.IO.Path.Combine(reportFolderPath, "FirstRunReport.txt"));
        //    IStringSearchAlgorithm searchAlgo = new StringSearch();
        //    //searchAlgo.Keywords = legal;
        //    //int total = 0;
        //    string s;
        //    while ((s = sr.ReadLine()) != null)
        //    {
        //        using (StreamReader sx = System.IO.File.OpenText(System.IO.Path.Combine(reportFolderPath, "FirstRunReport.txt")))
        //        {

        //            string sd;
        //            while ((sd = sx.ReadLine()) != null)
        //            {
        //                //row = searchAlgo.FindAll(legal);
        //                if (sd.Contains("\\\\brspfp01\\shared\\Government Relations"))
        //                {
        //                    extList.Add(sd);

        //                }
        //            }
        //        }
        //        foreach (string ext in extList)
        //        {
        //            //ount = extList.Where(x => x == ext).Count();
        //            using (sW = System.IO.File.AppendText(System.IO.Path.Combine(reportFolderPath, "GovRelations_Repo.txt")))
        //            {
        //                sW.Write(ext + "\n");
        //            }

        //        }
        //    }
        //    //using (sW = System.IO.File.AppendText(System.IO.Path.Combine(reportFolderPath, "Legal_Repo.txt")))
        //    //{
        //    //    sW.Write(s + "\n");
        //    //}
        //}

        //private void TheSecondLogic()
        //{
        //    try
        //    {
        //        List<string> tempsearchList = new List<string>();
        //        List<string> lstCurrentFullPath = new List<string>();
        //        string reportFileName = string.Empty;
        //        StringSearchResult row;
        //        List<string> commonFromFirstRun = new List<string>();
        //        IStringSearchAlgorithm searchAlgo = new StringSearch();

        //        reportFileName = DateTime.Today.ToString("ddMMyyyy") + ".txt";
        //        //if (reportFileName == lstOfReportFiles[i])
        //        //if (!System.IO.File.Exists(System.IO.Path.Combine(reportFolderPath, reportFileName)))
        //        //    reportFileName = DateTime.Today.AddDays(-1).ToString("ddMMyyyy") + ".txt";
        //        for (int i = 1; i <= 7; i++)
        //        {
        //            reportFileName = DateTime.Today.AddDays(-i).ToString("ddMMyyyy") + ".txt";
        //            if (System.IO.File.Exists(System.IO.Path.Combine(reportFolderPath, reportFileName)))
        //            {
        //                using (StreamReader sr = System.IO.File.OpenText(System.IO.Path.Combine(reportFolderPath, reportFileName)))
        //                {

        //                    string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(reportFolderPath, reportFileName));
        //                    //string line;
        //                    //while ((line = sr.ReadLine()) != null)
        //                    //{
        //                    //    //row = searchAlgo.FindFirst(line);
        //                    //    //if (!string.IsNullOrEmpty(row.Keyword))
        //                    //        //commonFromFirstRun.Add(line.Split('|')[4] + "|" + row.Keyword);

        //                    //}
        //                }

        //                searchAlgo.Keywords = lstCurrentFullPath.ToArray();

        //                using (StreamReader sr = System.IO.File.OpenText(System.IO.Path.Combine(reportFolderPath, reportFileName)))
        //                {
        //                    string line;
        //                    while ((line = sr.ReadLine()) != null)
        //                    {
        //                        row = searchAlgo.FindFirst(line);
        //                        if (!string.IsNullOrEmpty(row.Keyword))
        //                            commonFromFirstRun.Add(line.Split('|')[4] + "|" + row.Keyword);
        //                    }
        //                }



        //                using (StreamReader sr = System.IO.File.OpenText(System.IO.Path.Combine(reportFolderPath, reportFileName)))
        //                {
        //                    string line;
        //                    while ((line = sr.ReadLine()) != null)
        //                    {
        //                        row = searchAlgo.FindFirst(line);
        //                        if (!string.IsNullOrEmpty(row.Keyword))
        //                            commonFromFirstRun.Add(line.Split('|')[4] + "|" + row.Keyword);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}
        //private void ParallelExcecution(string s)
        //{
        //    Common common = new Common();
        //    string[] line = s.Split('|');
        //    //if (line[0].Contains(txtSearch.Text))
        //    //{
        //    fileList.Add(new FileList()
        //    {
        //        name = common.FetchValues(line[0]),
        //        extension = common.DisplayTextForExtension(common.FetchValues(line[6])),
        //        //count = FileCount(common.FetchValues(line[5])),
        //        size = common.FetchValues(line[4]),
        //        fullPath = common.FetchValues(line[5]),
        //        //moveToSharepoint = common.GetSharePointStatus(Convert.ToDateTime(common.FetchValues(line[3])))
        //    });
        //}

        //private void Execute(int limit)
        //{
        //    try
        //    {
        //        //RegexStringValidator regex = new RegexStringValidator(allfilePath);


        //        Common common = new Common();
        //        List<string> lstFullPath = new List<string>();
        //        int total = 0;
        //        //allLines = allLines.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key).ToArray();

        //        //allLines.Where(x => x.Contains("|"++"|"));

        //        //for (int j = 0; j < limit; j++)
        //        //{
        //        //    lstFullPath.Add(allLines[j].Split('|')[5]);
        //        //    //var query = allLines.GroupBy(x => x.Contains(lstFullPath[j])).Where(g => g.Count() > 1).Select(y => new { Element = y.Key, Counter = y.Count() }).ToList();
        //        //}

        //        //fOR ARCHIVE SEARCH
        //        //string[] lstOfReportFiles = Directory.GetFiles(reportFolderPath);//.Where(x => x.Name == reportFileName + ".txt");
        //        //for (int i = 0; i <= lstOfReportFiles.Length; i++)
        //        //{

        //        //string reportFileName = DateTime.Today.ToString("ddMMYYYY") + ".txt";
        //        ////if (reportFileName == lstOfReportFiles[i])
        //        //if (!File.Exists(System.IO.Path.Combine(reportFolderPath, reportFileName)))
        //        //    reportFileName = DateTime.Today.AddDays(-1).ToString("ddMMYYYY") + ".txt";




        //        //}



        //        var q = from x in allLines
        //                    //where allLines.Intersect(lstFullPath)
        //                group x by x into g
        //                let count = g.Count()
        //                orderby count descending
        //                select new { Value = g.Key, Count = count };

        //        foreach (var a in q)
        //        {

        //            fileList.Add(new FileList()
        //            {
        //                name = a.Value.Split('|')[0],
        //                extension = a.Value.Split('|')[6],//common.DisplayTextForExtension(common.FetchValues(line[6])),

        //                ////count = lstFullPath.FindAll(s => s.Equals(common.FetchValues(line[5]))).Count(),
        //                //count = a.Count, // FileCount(common.FetchValues(line[5])),
        //                size = a.Value.Split('|')[4],//common.FetchValues(line[4]),
        //                fullPath = a.Value.Split('|')[5],//common.FetchValues(line[5]),
        //                //moveToSharepoint = common.GetSharePointStatus(Convert.ToDateTime(a.Value.Split('|')[3]))
        //            });
        //        }

        //        // common.FetchValues()

        //        //for (int i = 0; i < limit; i++)
        //        //{

        //        //    string[] line = allLines[i].Split('|');

        //        //    //if (line[0].Contains(txtSearch.Text))
        //        //    //{
        //        //    //foreach (string pathL in lstFullPath)
        //        //    //{
        //        //    //    if (common.FetchValues(line[5]) == pathL)
        //        //    //        total = total + 1;
        //        //    //}
        //        //    //total = lstFullPath.Where(x => x == common.FetchValues(line[5])).Count();
        //        //    //lstFullPath.Remove(common.FetchValues(line[5]));
        //        //    //allLines = allLines.GroupBy(x => x).Where(g => g.Count() > 1 && g.Key.Contains(common.FetchValues(line[5]))).Select(g => g.Key).ToArray();
        //        //    //total = lstFullPath.GroupBy(x => x).Where(g => g.Count() > 1 && g.Key.Contains(common.FetchValues(line[5]))).Select(g => g.Key).ToList().Count();
        //        //    total = allLines.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => new { Element = y.Key, Counter = y.Count() }).ToList().Count;
        //        //    fileList.Add(new FileList()
        //        //    {
        //        //        name = common.FetchValues(line[0]),
        //        //        extension = common.DisplayTextForExtension(common.FetchValues(line[6])),

        //        //        //count = lstFullPath.FindAll(s => s.Equals(common.FetchValues(line[5]))).Count(),
        //        //        count = total, // FileCount(common.FetchValues(line[5])),
        //        //        size = common.FetchValues(line[4]),
        //        //        fullPath = common.FetchValues(line[5]),
        //        //        moveToSharepoint = common.GetSharePointStatus(Convert.ToDateTime(common.FetchValues(line[3])))
        //        //    });
        //        //}
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //private void CompareDataTables(System.Data.DataTable dt1, System.Data.DataTable dt2)
        //{
        //    //dt1.PrimaryKey = dt1.Select(x => dt1.Columns[x]).ToArray();
        //    //dt2.PrimaryKey = primaryKeyColumnNames.Select(x => dt2.Columns[x]).ToArray();
        //    //var matches = (from System.Data.DataRow RowA in dt1.Rows
        //    //               where dt2.Rows.Contains(RowA.ItemArray.Where((x, y) => primaryKeyColumnNames.Contains(dt1.Columns[y].ColumnName)).ToArray())
        //    //               select RowA).ToList();
        //}

        //private void AddExcelSheet(System.Data.DataTable dt, Workbook wb)
        //{
        //    Microsoft.Office.Interop.Excel.Sheets sheets = wb.Sheets;
        //    Microsoft.Office.Interop.Excel.Worksheet newSheet = sheets.Add();
        //    int iCol = 0;
        //    foreach (DataColumn c in dt.Columns)
        //    {
        //        iCol++;
        //        newSheet.Cells[1, iCol] = c.ColumnName;
        //    }

        //    int iRow = 0;
        //    foreach (DataRow r in dt.Rows)
        //    {
        //        iRow++;
        //        // add each row's cell data...
        //        iCol = 0;
        //        foreach (DataColumn c in dt.Columns)
        //        {
        //            iCol++;
        //            newSheet.Cells[iRow + 1, iCol] = r[c.ColumnName];
        //        }
        //    }
        //}

        //private void ExtensionCount()
        //{
        //    List<string> extList = new List<string>();
        //    string reportFileName = string.Empty;
        //    StreamWriter sW = null;
        //    int count;
        //    StreamReader sr = new StreamReader(System.IO.Path.Combine(reportFolderPath, "FirstRunReport.txt"));
        //    //int total = 0;
        //    string s;
        //    while ((s = sr.ReadLine()) != null)
        //    {
        //        extList.Add(s.Split('|')[6]);
        //    }
        //    var list = new List<string> { ".dm", ".adf", ".bil", ".bt2", "bpr", ".blk", ".gdb", ".gi", ".geosoft_voxel", ".ts", ".pl", ".vs", ".vo", ".mig", ".pen", ".tab", ".mif", ".grd", ".grc", ".dtm", ".mdl", ".00t", ".bmf" };
        //    //var q = from x in extList.Intersect(list)
        //    //        group x by x into g
        //    //        let count = g.Count()
        //    //        orderby count descending
        //    //        select new { Value = g.Key, Count = count };
        //    //int count = extList.Intersect(list).Count();
        //    foreach (string ext in list)
        //    {
        //        count = extList.Where(x => x == ext).Count();
        //        using (sW = System.IO.File.AppendText(System.IO.Path.Combine(reportFolderPath, "Ext_Repo.txt")))
        //        {
        //            sW.Write(ext + ":" + count + "\n");
        //        }

        //    }
        //    //foreach (var x in q)
        //    //{

        //    //    using (sW = File.AppendText(System.IO.Path.Combine(reportFolderPath, "Ext_Repo.txt")))
        //    //    {
        //    //        sW.Write(x.Value + ":" + x.Count + "\n");
        //    //    }
        //    //    //while (!sr.EndOfStream)
        //    //    //{
        //    //    //    var lines = sr.ReadLine().Split('|');
        //    //    //    string ext = lines[6];
        //    //    //    if (ext == filePath)
        //    //    //        total = total + 1;
        //    //    //}
        //    //    //return total;
        //    //}
        //    MessageBox.Show("done");
        //}

        //private void CheckForExistingExportReport()
        //{
        //    //System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(System.IO.Path.Combine(exportFolderPath,scanFolder));
        //    //var myFile = dir.EnumerateFiles().OrderByDescending(f => f.LastWriteTime).First();

        //    //path = GetFullPathFromExcel(myFile.FullName, "Original");
        //    //Microsoft.Office.Interop.Excel.Application xlsApp = new Microsoft.Office.Interop.Excel.Application();

        //    //Workbook wb = xlsApp.Workbooks.Open(fullPathToExcel,
        //    //0, true, 5, "", "", true, XlPlatform.xlWindows, "\t", false, false, 0, true);
        //    //Worksheet sheets = (Worksheet)wb.Worksheets[sheetToRead];

        //    ////Worksheet ws = (Worksheet)sheets.get_Item(3);

        //    //Range DuplicatesColumn = sheets.UsedRange.Columns[7];
        //    //System.Array myvalues = (System.Array)DuplicatesColumn.Cells.Value;
        //    //List<string> pathList = myvalues.OfType<object>().Select(o => o.ToString().Replace(watchPath, "")).ToList();
        //    //return pathList;
        //}

        /// <summary>
        /// The report consists of data  using equal sign as a delimiter, using which data is extracted.
        /// </summary>
        /// <param name="stringWithEqualSign"></param>
        /// <returns></returns>
        //private string FetchValues(string stringWithEqualSign)
        //{
        //    try
        //    {
        //        string[] value = stringWithEqualSign.Split('=');
        //        string returnValue = value[1];
        //        return returnValue;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        /// <summary>
        /// Passes the filepath to Delta form to dispaly the details for the selected file.
        /// </summary>
        //private void DataForDeltaForm()
        //{
        //    try
        //    {
        //        Delta_details deltaForm = new Delta_details();

        //        var result = grdMain.SelectedItem as FileList;
        //        if (result == null)
        //            return;
        //        searchList.Add(result.fullPath);
        //        deltaForm.searchList = searchList;
        //        deltaForm.searchpath = reportSearchpath;
        //        deltaForm.Show();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}


        //private void GrdMain_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //}

        //private void MainForm_Loaded(object sender, RoutedEventArgs e)
        //{
        //    //txtSearch.Text = string.Empty;
        //    //chkArchiveSearch..checked;
        //}

        //private void CreateLegalFolderStructure()
        //{
        //    List<string> filenames = new List<string>();
        //    //StreamReader sr = null;
        //    //string s;
        //    //while ((s = sr.ReadLine()) != null)
        //    //{
        //        using (StreamReader sx = System.IO.File.OpenText(System.IO.Path.Combine(reportFolderPath, "Legal_Repo.txt")))
        //        {

        //            string sd;
        //            while ((sd = sx.ReadLine()) != null)
        //            {
        //                //row = searchAlgo.FindAll(legal);
        //                //if (sd.Contains("\\\\brspfp01\\shared\\Legal"))
        //                //{
        //                filenames.Add(sd);

        //                //}
        //            }
        //        //}
        //        foreach (string f in filenames)
        //        {
        //            string path = "C:\\A";
        //            string[] fn = f.Split('\\');
        //            for (int i = 0; i < fn.Length-1; i++)
        //            {
        //                path = path + "\\" + fn[i];
        //                if (!Delimon.Win32.IO.Directory.Exists(path))
        //                    Delimon.Win32.IO.Directory.CreateDirectory(path);

        //                //string previousPath = path;
        //            }

        //        }
        //    }
        //}

        //private void CreateGoveRelationFolderStructure()
        //{
        //    List<string> filenames = new List<string>();
        //    //StreamReader sr = null;
        //    //string s;
        //    //while ((s = sr.ReadLine()) != null)
        //    //{
        //    using (StreamReader sx = System.IO.File.OpenText(System.IO.Path.Combine(reportFolderPath, "GovRelations_filepath_Repo.txt")))
        //    {

        //        string sd;
        //        while ((sd = sx.ReadLine()) != null)
        //        {
        //            //row = searchAlgo.FindAll(legal);
        //            //if (sd.Contains("\\\\brspfp01\\shared\\Legal"))
        //            //{
        //            filenames.Add(sd);

        //            //}
        //        }
        //        //}
        //        foreach (string f in filenames)
        //        {
        //            string path = "C:\\B";
        //            string[] fn = f.Split('\\');
        //            for (int i = 0; i < fn.Length - 1; i++)
        //            {
        //                path = path + "\\" + fn[i];
        //                if (!Delimon.Win32.IO.Directory.Exists(path))
        //                    Delimon.Win32.IO.Directory.CreateDirectory(path);

        //                //string previousPath = path;
        //            }

        //        }
        //    }
        //}

        //private void BtnExport_Click(object sender, RoutedEventArgs e)
        //{
        //    if (exportDataTable == null)
        //        return;
        //    if (!System.IO.Directory.Exists(ConfigurationManager.AppSettings["ExportFolderPath"]))
        //    {
        //        System.IO.Directory.CreateDirectory(ConfigurationManager.AppSettings["ExportFolderPath"]);
        //    }
        //    StringBuilder sb = new StringBuilder();
        //    IEnumerable<string> columnNames = exportDataTable.Columns.Cast<DataColumn>().
        //                          Select(column => column.ColumnName);

        //    sb.AppendLine(string.Join(",", columnNames));
        //    //DataTable dt = GetData();
        //    foreach (DataRow dr in exportDataTable.Rows)
        //    {
        //        foreach (DataColumn dc in exportDataTable.Columns)
        //            sb.Append(DataTableToCSV(dr[dc.ColumnName].ToString()) + ",");
        //        sb.Remove(sb.Length - 1, 1);
        //        sb.AppendLine();
        //    }
        //    string exportPath = string.Format("{0}\\Export_{1}.csv", ConfigurationManager.AppSettings["ExportFolderPath"], DateTime.Now.ToString("yyyyMMddhhmmss"));
        //    System.IO.File.WriteAllText(exportPath, sb.ToString());
        //    MessageBox.Show("Export completed!");
        //    sb.Clear();
        //    exportDataTable.Clear();

        //}

        #region Excel To DataTable

        //private System.Data.DataTable ExcelToDataTable(string fullPathToExcel, string sheetToRead)
        //{
        //    System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(exportFolderPath);
        //    var lastExportedReportfInfo = dir.EnumerateFiles().OrderByDescending(x => x.LastWriteTime).First();
        //    using (OleDbConnection conn = new OleDbConnection())
        //    {
        //        System.Data.DataTable dt = new System.Data.DataTable();
        //        string Import_FileName = lastExportedReportfInfo.FullName;
        //        string fileExtension = System.IO.Path.GetExtension(Import_FileName);
        //        if (fileExtension == ".xls")
        //            conn.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + Import_FileName + ";" + "Extended Properties='Excel 8.0;HDR=YES;'";
        //        if (fileExtension == ".xlsx")
        //            conn.ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + Import_FileName + ";" + "Extended Properties='Excel 12.0 Xml;Persist Security Info=False;'";
        //        using (OleDbCommand comm = new OleDbCommand())
        //        {
        //            comm.CommandText = "Select * from [" + sheetToRead + "$]";
        //            comm.Connection = conn;
        //            using (OleDbDataAdapter da = new OleDbDataAdapter())
        //            {
        //                da.SelectCommand = comm;
        //                da.Fill(dt);

        //            }
        //        }
        //        return dt;
        //    }

        //}

        #endregion Excel To DataTable

        #region DataTable Joiner

        //private System.Data.DataTable DataTableJoiner(System.Data.DataTable dt1, System.Data.DataTable dt2)
        //{
        //    var commonColumns = dt1.Columns.OfType<DataColumn>().Intersect(dt2.Columns.OfType<DataColumn>(), new DataColumnComparer());

        //    var result = new System.Data.DataTable();
        //    result.Columns.AddRange(
        //        dt1.Columns.OfType<DataColumn>()
        //        .Union(dt2.Columns.OfType<DataColumn>(), new DataColumnComparer())
        //        .Select(c => new DataColumn(c.Caption, c.DataType, c.Expression, c.ColumnMapping))
        //        .ToArray());

        //    var rowData = dt1.AsEnumerable().Join(
        //        dt2.AsEnumerable(),
        //        row => commonColumns.Select(col => row[col.Caption]).ToArray(),
        //        row => commonColumns.Select(col => row[col.Caption]).ToArray(),
        //        (row1, row2) =>
        //        {
        //            var row = result.NewRow();
        //            row.ItemArray = result.Columns.OfType<DataColumn>().Select(col => row1.Table.Columns.Contains(col.Caption) ? row1[col.Caption] : row2[col.Caption]).ToArray();
        //            return row;
        //        },
        //        new ObjectArrayComparer());

        //    foreach (var row in rowData)
        //        result.Rows.Add(row);

        //    return result;
        //}

        //private class DataColumnComparer : IEqualityComparer<DataColumn>
        //{

        //    #region IEqualityComparer<DataColumn> Members

        //    public bool Equals(DataColumn x, DataColumn y)
        //    {
        //        return x.Caption == y.Caption;
        //    }

        //    public int GetHashCode(DataColumn obj)
        //    {
        //        return obj.Caption.GetHashCode();
        //    }

        //    #endregion
        //}

        //private class ObjectArrayComparer : IEqualityComparer<object[]>
        //{
        //    #region IEqualityComparer<object[]> Members

        //    public bool Equals(object[] x, object[] y)
        //    {
        //        for (var i = 0; i < x.Length; i++)
        //        {
        //            if (!object.Equals(x[i], y[i]))
        //                return false;
        //        }

        //        return true;
        //    }

        //    public int GetHashCode(object[] obj)
        //    {
        //        return obj.Sum(item => item.GetHashCode());
        //    }

        //    #endregion
        //}

        #endregion DataTable Joiner

        #endregion  useless

        private void Btn_tempMoveFix_Click(object sender, RoutedEventArgs e)
        {
            Analyzer anzy = new Analyzer();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            //anzy.RevertMovePreCleanse(exportFolderPath, reportFolderPath, reportDate, "Original", watchPath);
            stopwatch.Stop();
            MessageBox.Show(this, "reverted move in=" + (stopwatch.ElapsedMilliseconds / 1000) + "seconds");
        }

        private void Btn_CreateFolder_Click(object sender, RoutedEventArgs e)
        {
            Analyzer anzy = new Analyzer();
            Stopwatch stopwatch = new Stopwatch();
            anzy.CreateFoldersForLegalDelete(reportFolderPath, reportDate);
            stopwatch.Stop();
            MessageBox.Show(this, "Created empty folder structures in=" + (stopwatch.ElapsedMilliseconds / 1000) + "seconds");
        }

        private void BtnCount_Click(object sender, RoutedEventArgs e)
        {
            int fValue;
            int dValue;
            Analyzer analyzer = new Analyzer();
            reportDate = DateTime.Now.ToString("yyyyMMddhhmmss");
            string reportPath = System.IO.Path.Combine(reportFolderPath, scanFolder + "_" + reportDate + ".txt");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            analyzer.ScanAndReportFileMetaData(watchPath, out fValue, out dValue);
            MessageBox.Show(string.Format("{0} files, {1} directories", fValue.ToString(), dValue.ToString()));
            stopwatch.Stop();
            MessageBox.Show(this, "Count of files and folders in=" + (stopwatch.ElapsedMilliseconds / 1000) + "seconds");
        }

        private void BtnCSVExport_Click(object sender, RoutedEventArgs e)
        {
            Analyzer anz = new Analyzer();
            string[] multipleWatchPath = watchPath.Split(',');
            foreach (string mPath in multipleWatchPath)
            {
                scanFolder = System.IO.Path.GetFileName(mPath);
                if (string.IsNullOrEmpty(scanFolder))
                {
                    DriveInfo drive = new DriveInfo(mPath);
                    scanFolder = drive.ToString();
                    scanFolder = scanFolder.Substring(0, 1);
                }
                //if (string.IsNullOrEmpty(reportFolderPath))
                    reportFolderPath = System.IO.Path.Combine(ConfigurationManager.AppSettings["ReportFolderPath"], scanFolder);

                System.Data.DataSet aboveAll = anz.ReadTextReport(reportFolderPath, retentionYear, false);
                foreach (System.Data.DataTable dt in aboveAll.Tables)
                {
                    anz.ExportToCSV(reportFolderPath, dt);
                }
            }
        }

        private void BtnTreeView_Click(object sender, RoutedEventArgs e)
        {
            TreeView_FolderStructure treeviewForm = new TreeView_FolderStructure();
            treeviewForm.Show();
        }
    }
}
