// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using dnSpy.AsmEditor;
using dnSpy.Controls;

namespace ICSharpCode.ILSpy.Options
{
	/// <summary>
	/// Interaction logic for OptionsDialog.xaml
	/// </summary>
	public partial class OptionsDialog : MetroWindow
	{
		sealed class MefState
		{
			public static readonly MefState Instance = new MefState();

			MefState()
			{
				App.CompositionContainer.ComposeParts(this);
			}

			[ImportMany(typeof(IOptionPageCreator))]
			public Lazy<IOptionPageCreator, IOptionPageCreatorMetadata>[] optionPages = null;
		}

		readonly OptionPage[] optionPages;
		public RefreshFlags RefreshFlags { get; private set; }

		bool HasError {
			get { return optionPages.Any(a => a.HasError); }
		}

		void HasErrorUpdated()
		{
			this.okButton.IsEnabled = !HasError;
		}

		public OptionsDialog()
		{
			InitializeComponent();
			ILSpySettings settings = ILSpySettings.Load();
			var creators = MefState.Instance.optionPages.OrderBy(p => p.Metadata.Order).ToArray();
			optionPages = creators.Select(p => p.Value.Create()).ToArray();
			for (int i = 0; i < creators.Length; i++) {
				TabItem tabItem = new TabItem();
				tabItem.Header = creators[i].Metadata.Title;
				tabItem.Content = new ScrollViewer {
					HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
					VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
					Content = optionPages[i],
				};
				tabControl.Items.Add(tabItem);
				
				optionPages[i].Load(settings);
			}

			foreach (var page in optionPages)
				page.PropertyChanged += (s, e) => HasErrorUpdated();
			HasErrorUpdated();
		}
		
		void OKButton_Click(object sender, RoutedEventArgs e)
		{
			RefreshFlags = RefreshFlags.None;
			ILSpySettings.Update(
				delegate (XElement root) {
					foreach (var optionPage in optionPages)
						RefreshFlags |= optionPage.Save(root);
				});
			this.DialogResult = true;
			Close();
		}
	}

	public interface IOptionPageCreator
	{
		OptionPage Create();
	}
	
	public interface IOptionPageCreatorMetadata
	{
		string Title { get; }
		int Order { get; }
	}
	
	public abstract class OptionPage : ViewModelBase
	{
		public abstract void Load(ILSpySettings settings);
		public abstract RefreshFlags Save(XElement root);
	}

	[Flags]
	public enum RefreshFlags
	{
		/// <summary>
		/// Nothing needs to be refreshed
		/// </summary>
		None				= 0,

		/// <summary>
		/// Tree view nodes need to be updated
		/// </summary>
		TreeViewNodes		= 0x00000001,

		/// <summary>
		/// Text editor needs to re-disassemble IL
		/// </summary>
		IL					= 0x00000002,

		/// <summary>
		/// Text editor needs to re-decompile ILAst, C# and VB code
		/// </summary>
		ILAst				= 0x00000004,

		/// <summary>
		/// Text editor needs to re-decompile C# and VB code
		/// </summary>
		CSharp				= 0x00000008,

		/// <summary>
		/// Text editor needs to re-decompile VB code
		/// </summary>
		VB					= 0x00000010,

		/// <summary>
		/// Disable memory mapped I/O
		/// </summary>
		DisableMmap			= 0x00000020,

		DecompileAll = IL | ILAst | CSharp | VB,
	}
	
	[MetadataAttribute]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
	public class ExportOptionPageAttribute : ExportAttribute, IOptionPageCreatorMetadata
	{
		public ExportOptionPageAttribute() : base(typeof(IOptionPageCreator))
		{ }
		
		public string Title { get; set; }
		
		public int Order { get; set; }
	}

	[ExportMainMenuCommand(Menu = "_View", MenuHeader = "_Options…", MenuIcon = "Settings", MenuCategory = "Options", MenuOrder = 3999)]
	sealed class ShowOptionsCommand : SimpleCommand
	{
		public override void Execute(object parameter)
		{
			OptionsDialog dlg = new OptionsDialog();
			dlg.Owner = MainWindow.Instance;
			if (dlg.ShowDialog() == true) {
				var inst = MainWindow.Instance;
				bool disassembleIL = (dlg.RefreshFlags & RefreshFlags.IL) != 0;
				bool decompileILAst = (dlg.RefreshFlags & RefreshFlags.ILAst) != 0;
				bool decompileCSharp = (dlg.RefreshFlags & RefreshFlags.CSharp) != 0;
				bool decompileVB = (dlg.RefreshFlags & RefreshFlags.VB) != 0;
				bool disableMmap = (dlg.RefreshFlags & RefreshFlags.DisableMmap) != 0;
				if (disableMmap) {
					disassembleIL = true;
					decompileILAst = true;
					decompileCSharp = true;
					decompileVB = true;
				}
				if (disableMmap)
					inst.DisableMemoryMappedIO();
				inst.RefreshCodeCSharp(disassembleIL, decompileILAst, decompileCSharp, decompileVB);
				if ((dlg.RefreshFlags & RefreshFlags.TreeViewNodes) != 0)
					inst.RefreshTreeViewNodes();
			}
		}
	}
}