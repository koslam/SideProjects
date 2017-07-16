using System;
using System.Windows;
using System.IO;
using System.Xml;

namespace XmlTagRemover
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void CTRemoveButton_Click(object sender, RoutedEventArgs e)
		{
			System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
			try
			{
				string ls_path = CTPathTextBox.Text;
				string ls_search = CTSearchTextBox.Text;
				bool? lb_recurse = CTRecursiveCheck.IsChecked;
				string ls_error = "";
				
				string[] lsa_tags = CTTagsTextBox.Text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

				if (ls_path.Length < 1 || !Directory.Exists(ls_path))
					throw new Exception("Invalid path");

				if (ls_search.Length < 1)
					throw new Exception("Invalid mask");

				if (lsa_tags.Length < 1)
					throw new Exception("No tags provided");

				string[] lsa_files = Directory.GetFiles(ls_path, ls_search, (lb_recurse != null && lb_recurse == true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));

				foreach (string ls_filename in lsa_files)
				{
					try
					{
						XmlDocument lxd_document = new XmlDocument();
						lxd_document.Load(ls_filename);

						foreach (string ls_tag in lsa_tags)
						{
							XmlNodeList lxnl_nodes = lxd_document.GetElementsByTagName(ls_tag);
							for (int i = lxnl_nodes.Count - 1; i >= 0; i--)
							{
								lxnl_nodes[i].ParentNode.RemoveChild(lxnl_nodes[i]);
							}
						}

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
	}
}
