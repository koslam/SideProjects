using System;
using System.Windows;
using System.IO;
using System.Linq;
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
		private void CTAddReferencesButton_Click(object sender, RoutedEventArgs e)
		{
			// Update the cursor to the wait cursor.
			System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
			try
			{
				// Get the path where we will search for CSPROJ files.
				string ls_path = CTPathTextBox.Text;

				if (ls_path.Length < 1 || !Directory.Exists(ls_path))
					throw new Exception("Invalid path");

				// Get the file list and establish some local variables.
				string[] lsa_files = Directory.GetFiles(ls_path, "*.csproj", SearchOption.AllDirectories);
				
				string ls_error = "";
				string ls_temp;
				bool lb_core, lb_server, lb_client, lb_any_update;

				// Spin through the projects we are processing.
				foreach (string ls_filename in lsa_files)
				{
					ls_temp = Path.GetFileNameWithoutExtension(ls_filename);
					ls_temp = ls_temp.ToLower();
					lb_core = true;
					lb_client = (ls_temp.EndsWith("ui") || ls_temp.EndsWith("controls"));
					lb_server = !lb_client;
					lb_any_update = false;
					try
					{
						// Open the project file and find the ItemGroup with the Reference nodes.
						XmlDocument lxd_document = new XmlDocument();
						lxd_document.Load(ls_filename);
						
						XmlNodeList lxnl_nodes = lxd_document.GetElementsByTagName("ItemGroup");
						foreach (XmlNode lxn_node in lxnl_nodes)
						{
							if (lxn_node.HasChildNodes && lxn_node.FirstChild.Name == "Reference")
							{
								// Check to see if we already have the references we want to add.
								foreach (XmlNode lxn_ref in lxn_node.ChildNodes)
								{
									if (lxn_ref.Name == "Reference")
									{
										if (lxn_ref.Attributes["Include"].Value.Contains("ShadoFWCore"))
											lb_any_update = true;
										else if (lb_core && lxn_ref.Attributes["Include"].Value.Contains("ShadowFrameworkCore"))
											lb_core = false;
										else if (lb_server && lxn_ref.Attributes["Include"].Value.Contains("ShadowFrameworkServer"))
											lb_server = false;
										else if (lb_client && lxn_ref.Attributes["Include"].Value.Contains("ShadowFrameworkClient"))
											lb_client = false;
									}
								}
								
								if (!lb_any_update)
									break;

								// Add the reference nodes.
								for (int ll_index = 0; ll_index < 3; ll_index++)
								{
									if (ll_index == 0)
									{
										if (!lb_core) continue;
										ls_temp = "ShadowFrameworkCore";
									}
									else if (ll_index == 1)
									{
										if (!lb_server) continue;
										ls_temp = "ShadowFrameworkServer";
									}
									else if (ll_index == 2)
									{
										if (!lb_client) continue;
										ls_temp = "ShadowFrameworkClient";
									}
									else
									{
										break;
									}
									
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
								break;
							}
						}

						// Write the updated document to disk.
						lxd_document.Save(ls_filename);
						lxd_document = null;
					}
					catch (Exception ex)
					{
						ls_error += "[File: " + ls_filename + "][Error: " + ex.Message + "]\r\n";
					}
				}

				if (ls_error.Length > 0)
					throw new Exception(ls_error);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
		private void CTRemoveReferencesButton_Click(object sender, RoutedEventArgs e)
		{
			// Update the cursor to the wait cursor.
			System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
			try
			{
				// Get the path where we will search for CSPROJ files.
				string ls_path = CTPathTextBox.Text;

				if (ls_path.Length < 1 || !Directory.Exists(ls_path))
					throw new Exception("Invalid path");

				// Get the file list and establish some local variables.
				string[] lsa_files = Directory.GetFiles(ls_path, "*.csproj", SearchOption.AllDirectories);
				
				string ls_error = "";
				string ls_temp;

				// Spin through the projects we are processing.
				foreach (string ls_filename in lsa_files)
				{
					ls_temp = Path.GetFileNameWithoutExtension(ls_filename);
					ls_temp = ls_temp.ToLower();
					try
					{
						XDocument xDoc;
						XmlNamespaceManager namespaceManager;

						// Pull the current csproj into an xdocument
						using (XmlReader xmlReader = XmlReader.Create(ls_filename))
						{
							xDoc = XDocument.Load(xmlReader);
							namespaceManager = new XmlNamespaceManager(xmlReader.NameTable);
							namespaceManager.AddNamespace("prefix", "http://schemas.microsoft.com/developer/msbuild/2003");
						}
						
						// Get the list of nodes that are for "ShadopFW*" references
						XElement root = xDoc.Root;
						XElement[] referenceElements = root.XPathSelectElements("//prefix:Reference[starts-with(@Include,'ShadoFW')]", namespaceManager).ToArray();

						// Remove the elements
						foreach (XElement refElement in referenceElements)
						{
							refElement.Remove();
						}
						
						// Save the doc. We do this twice to get rid of blank lines that the remove causes
						xDoc.Save(ls_filename);

						xDoc = XDocument.Load(ls_filename);
						xDoc.Save(ls_filename);
					}
					catch (Exception ex)
					{
						ls_error += "[File: " + ls_filename + "][Error: " + ex.Message + "]\r\n";
					}
				}

				if (ls_error.Length > 0)
					throw new Exception(ls_error);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				System.Windows.Input.Mouse.OverrideCursor = null;
			}
		}
	}
}
