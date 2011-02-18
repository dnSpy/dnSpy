// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;
using System.Windows;
using System.Windows.Baml2006;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xaml;
using System.Xml;

using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TextView;
using Microsoft.Win32;

namespace ICSharpCode.ILSpy.TreeNodes
{
	class ResourceEntryNode : ILSpyTreeNode
	{
		string key;
		Stream value;
		
		public override object Text {
			get { return key.ToString(); }
		}
		
		public override object Icon {
			get { return Images.Resource; }
		}
		
		public ResourceEntryNode(string key, Stream value)
		{
			this.key = key;
			this.value = value;
		}
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.WriteCommentLine(output, string.Format("{0} = {1}", key, value));
		}
		
		internal override bool View(DecompilerTextView textView)
		{
			AvalonEditTextOutput output = new AvalonEditTextOutput();
			try {
				if (LoadImage(output))
					textView.Show(output, null);
				else if (LoadBaml(output)) {
					textView.Show(output, HighlightingManager.Instance.GetDefinitionByExtension(".xml"));
				} else
					return false;
			} catch (Exception ex) {
				output.Write(ex.ToString());
				textView.Show(output, null);
			}
			return true;
		}
		
		bool LoadImage(AvalonEditTextOutput output)
		{
			try {
				value.Position = 0;
				BitmapImage image = new BitmapImage();
				image.BeginInit();
				image.StreamSource = value;
				image.EndInit();
				output.AddUIElement(() => new Image { Source = image });
				output.WriteLine();
				output.AddButton(Images.Save, "Save", delegate { Save(); });
			} catch (Exception) {
				return false;
			}
			return true;
		}
		
		bool LoadBaml(AvalonEditTextOutput output)
		{
			value.Position = 0;
			TextWriter w = new StringWriter();
			Baml2006Reader reader = new Baml2006Reader(value, new XamlReaderSettings() { ValuesMustBeString = true });
			XamlXmlWriter writer = new XamlXmlWriter(new XmlTextWriter(w) { Formatting = Formatting.Indented }, reader.SchemaContext);
			while (reader.Read()) {
				switch (reader.NodeType) {
					case XamlNodeType.None:
						
						break;
					case XamlNodeType.StartObject:
						writer.WriteStartObject(reader.Type);
						break;
					case XamlNodeType.GetObject:
						writer.WriteGetObject();
						break;
					case XamlNodeType.EndObject:
						writer.WriteEndObject();
						break;
					case XamlNodeType.StartMember:
						writer.WriteStartMember(reader.Member);
						break;
					case XamlNodeType.EndMember:
						writer.WriteEndMember();
						break;
					case XamlNodeType.Value:
						// requires XamlReaderSettings.ValuesMustBeString = true to work properly
						writer.WriteValue(reader.Value);
						break;
					case XamlNodeType.NamespaceDeclaration:
						writer.WriteNamespace(reader.Namespace);
						break;
					default:
						throw new Exception("Invalid value for XamlNodeType");
				}
			}
			output.Write(w.ToString());
			return true;
		}
		
		public override bool Save()
		{
			SaveFileDialog dlg = new SaveFileDialog();
			dlg.FileName = Path.GetFileName(DecompilerTextView.CleanUpName(key));
			if (dlg.ShowDialog() == true) {
				value.Position = 0;
				using (var fs = dlg.OpenFile()) {
					value.CopyTo(fs);
				}
			}
			return true;
		}
	}
}
