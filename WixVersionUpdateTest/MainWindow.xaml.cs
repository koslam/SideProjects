using System;
using System.Windows;
using System.IO;
using System.Xml;

namespace WixVersionUpdateTest
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

		private void button_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string ls_file_path = textBox.Text;
				string ls_version = textBox1.Text;
				string ls_product_name = "Shadow Framework Client";

				XmlDocument lxd_wix_script = new XmlDocument();
				lxd_wix_script.Load(ls_file_path);
				
				XmlNamespaceManager lxns_manager = new XmlNamespaceManager(lxd_wix_script.NameTable);
				lxns_manager.AddNamespace("Wix", "http://schemas.microsoft.com/wix/2006/wi");

				XmlNode lxn_to_update = lxd_wix_script.SelectSingleNode("descendant::Wix:Module", lxns_manager);
				if (lxn_to_update != null && lxn_to_update.Attributes != null)
				{
					if (lxn_to_update.Attributes["Version"] != null) lxn_to_update.Attributes["Version"].Value = ls_version;
				}

				lxn_to_update = lxd_wix_script.SelectSingleNode("descendant::Wix:Product", lxns_manager);
				if (lxn_to_update != null && lxn_to_update.Attributes != null)
				{
					if (lxn_to_update.Attributes["Id"] != null) lxn_to_update.Attributes["Id"].Value = "{" + Guid.NewGuid() + "}";
					if (lxn_to_update.Attributes["Name"] != null) lxn_to_update.Attributes["Name"].Value = ls_product_name + " " + ls_version;
					if (lxn_to_update.Attributes["Version"] != null) lxn_to_update.Attributes["Version"].Value = ls_version;
				}

				File.SetAttributes(ls_file_path, FileAttributes.Normal);
				lxd_wix_script.Save(ls_file_path);
				File.SetAttributes(ls_file_path, FileAttributes.ReadOnly);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
	}
}
