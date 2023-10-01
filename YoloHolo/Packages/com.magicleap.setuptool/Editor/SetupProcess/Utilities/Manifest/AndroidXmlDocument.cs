using System.Text;
using System.Xml;
using UnityEngine;

namespace MagicLeap.SetupTool.Editor.Utilities
{
	internal class AndroidXmlDocument : XmlDocument
	{
		private readonly string _path;
		protected readonly XmlNamespaceManager _xmlNamespaceManager;
		public readonly string AndroidXmlNamespace = "http://schemas.android.com/apk/res/android";

		public AndroidXmlDocument(string path)
		{
	
			_path = path;
			using (var reader = new XmlTextReader(_path))
			{
				reader.Read();
				Load(reader);
			}

			_xmlNamespaceManager = new XmlNamespaceManager(NameTable);
			_xmlNamespaceManager.AddNamespace("android", AndroidXmlNamespace);
		}

		public string Save()
		{
			return SaveAs(_path);
		}

		public string SaveAs(string path)
		{
			using var writer = new XmlTextWriter(path, new UTF8Encoding(false));
			writer.Formatting = Formatting.Indented;
			Save(writer);

			return path;
		}
	}
}
