using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace XmlTagRemover
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
        /// <summary>
        /// Modes supported.
        /// </summary>
        private enum ReferenceChangeMode
        {
            Add,
            Remove
        }

		/// <summary>
		/// Constructor
		/// </summary>
		public MainWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Button event to add the references.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			// Update the cursor to the wait cursor.
			System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
			try
			{
                UpdateReferences(ReferenceChangeMode.Add);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error: " + ex.Message, "Error Adding References", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				System.Windows.Input.Mouse.OverrideCursor = null;
			}
		}
		/// <summary>
		/// Button event to remove old FW references.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RemoveButton_Click(object sender, RoutedEventArgs e)
		{
			// Update the cursor to the wait cursor.
			System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
			try
            {
                UpdateReferences(ReferenceChangeMode.Remove);
            }
			catch (Exception ex)
			{
				MessageBox.Show("Error: " + ex.Message, "Error Removing References", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				System.Windows.Input.Mouse.OverrideCursor = null;
			}
		}

        /// <summary>
        /// Parse the values in the Path and References text boxes to get the list we will use when adding or removing references.
        /// </summary>
        /// <param name="asa_FileList">List of CSProj files in the target directory</param>
        /// <param name="asa_ReferenceList">List of references to add or remove from the project files</param>
        private void GetScreen(out string[] asa_FileList, out string[] asa_ReferenceList)
        {
            // Get the path where we will search for CSPROJ files.
            if (PathTextBox.Text.Length == 0 || !Directory.Exists(PathTextBox.Text))
                throw new Exception("Invalid path");

            // Get the file list from the directory.
            asa_FileList = Directory.GetFiles(PathTextBox.Text, "*.csproj", SearchOption.AllDirectories);
            if (asa_FileList.Length == 0)
                throw new Exception("No files found");

            // Get the references, assuming each line is a reference.
            asa_ReferenceList = ReferencesTextBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (asa_ReferenceList.Length == 0)
                throw new Exception("No references provided");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ae_Mode"></param>
        private void UpdateReferences(ReferenceChangeMode ae_Mode)
        {
            // Get the file list and reference list
            GetScreen(out string[] lsa_FileList, out string[] lsa_ReferenceList);

            // Spin through the projects we are processing.
            StringBuilder lsb_Errors = new StringBuilder();
            foreach (string ls_FileName in lsa_FileList)
            {
                try
                {
                    // Pull the current csproj into an xdocument
                    XDocument lxd_Document;
                    XmlNamespaceManager lxnm_NamespaceMgr;
                    using (XmlReader xmlReader = XmlReader.Create(ls_FileName))
                    {
                        lxd_Document = XDocument.Load(xmlReader);
                        lxnm_NamespaceMgr = new XmlNamespaceManager(xmlReader.NameTable);
                        lxnm_NamespaceMgr.AddNamespace("prefix", "http://schemas.microsoft.com/developer/msbuild/2003");
                    }

                    // Based on the mode provided, we need to handle the contents of the file differently
                    if (ae_Mode == ReferenceChangeMode.Add)
                    {
                        // Get the list of nodes that contain the references and extract the Include tags
                        XElement[] lxea_References = lxd_Document.Root.XPathSelectElements("//prefix:Reference[@Include]/@Include", lxnm_NamespaceMgr).ToArray();

                        // For each of the references
                        XmlNode lxn_ref_node = lxd_document.CreateNode(XmlNodeType.Element, "Reference", "http://schemas.microsoft.com/developer/msbuild/2003");

                        XmlAttribute lxa_ref_include = lxd_document.CreateAttribute("Include");
                        lxa_ref_include.Value = ls_temp + ", Version=1.0.0.0, Culture=neutral, processorArchitecture=MSIL";
                        lxn_ref_node.Attributes.Append(lxa_ref_include);

                        XmlNode lxn_specific_version = lxd_document.CreateNode(XmlNodeType.Element, "SpecificVersion", "http://schemas.microsoft.com/developer/msbuild/2003");
                        lxn_specific_version.InnerText = "False";
                        lxn_ref_node.AppendChild(lxn_specific_version);

                        XmlNode lxn_private = lxd_document.CreateNode(XmlNodeType.Element, "Private", "http://schemas.microsoft.com/developer/msbuild/2003");
                        lxn_private.InnerText = "False";
                        lxn_ref_node.AppendChild(lxn_private);

                        lxn_node.AppendChild(lxn_ref_node);
                    }
                    else if (ae_Mode == ReferenceChangeMode.Remove)
                    {
                        // Get the list of nodes that contain the references
                        XElement[] lxea_References = lxd_Document.Root.XPathSelectElements("//prefix:Reference[@Include]", lxnm_NamespaceMgr).ToArray();

                        // Check each reference node to see if the Include points to one of the provided references and remove it if so
                        foreach (XElement lxe_Reference in lxea_References)
                        {
                            string ls_AttributeValue = lxe_Reference.Attribute("Include").Value;
                            if (lsa_ReferenceList.Any(ls_AttributeValue.StartsWith))
                                lxe_Reference.Remove();
                        }
                    }
                    else
                    {
                        break;
                    }
                    
                    // Save the doc. We do this twice to get rid of blank lines that the remove causes
                    lxd_Document.Save(ls_FileName);

                    lxd_Document = XDocument.Load(ls_FileName);
                    lxd_Document.Save(ls_FileName);
                }
                catch (Exception ex)
                {
                    lsb_Errors.AppendLine("[File: " + ls_FileName + "][Error: " + ex.Message + "]");
                }
            }

            if (lsb_Errors.Length > 0)
                throw new Exception(lsb_Errors.ToString());
        }

    }
}
