using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.IO;

namespace FileAnalysisTools
{

    public class MyTreeViewItem
    {
        public int Level
        {
            get;
            set;
        }

        public string FullPath { get; set; }

        public List<MyTreeViewItem> SubItems
        {
            get;
            set;
        }
    }
    /// <summary>
    /// Interaction logic for TreeView_FolderStructure.xaml
    /// </summary>
    public partial class TreeView_FolderStructure : Window
    {
        public TreeView_FolderStructure()
        {
            InitializeComponent();
            //Loaded += FrmTreeView_Loaded;
        }

        private static void PopulateTreeView(TreeView treeView, List<string> paths, char pathSeparator)
        {
            List<MyTreeViewItem> sourceCollection = new List<MyTreeViewItem>();
            foreach (string path in paths)
            {
                string[] fileItems = path.Split(pathSeparator);
                if (fileItems.Any())
                {

                    MyTreeViewItem root = sourceCollection.FirstOrDefault(x => x.FullPath.Equals(fileItems[0]) && x.Level.Equals(1));
                    if (root == null)
                    {
                        root = new MyTreeViewItem()
                        {
                            Level = 1,
                            FullPath = fileItems[0],
                            SubItems = new List<MyTreeViewItem>()
                        };
                        sourceCollection.Add(root);
                    }

                    if (fileItems.Length > 1)
                    {

                        MyTreeViewItem parentItem = root;
                        int level = 2;
                        for (int i = 1; i < fileItems.Length; ++i)
                        {

                            MyTreeViewItem subItem = parentItem.SubItems.FirstOrDefault(x => x.FullPath.Equals(fileItems[i]) && x.Level.Equals(level));
                            if (subItem == null)
                            {
                                subItem = new MyTreeViewItem()
                                {
                                    FullPath = fileItems[i],
                                    Level = level,
                                    SubItems = new List<MyTreeViewItem>()
                                };
                                parentItem.SubItems.Add(subItem);
                            }

                            parentItem = subItem;
                            level++;
                        }
                    }
                }
            }

            treeView.ItemsSource = sourceCollection;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            List<string> lstPath = ReadInCSV(txtReportFilepath.Text);

            //treeView1.PathSeparator = @"\";
            PopulateTreeView(treeView, lstPath, '\\');
        }
        public List<string> ReadInCSV(string absolutePath)
        {
            var temp = File.ReadAllLines(absolutePath);
            List<string> myExtraction = new List<string>();
            foreach (string line in temp)
            {
                var delimitedLine = line.Split('|'); //set ur separator, in this case tab
                string fullpathWithoutFileName = Delimon.Win32.IO.Path.GetDirectoryName(delimitedLine[5]);
                myExtraction.Add(fullpathWithoutFileName);
            }
            return myExtraction;
        }
    }

}
