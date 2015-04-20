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
using System.ComponentModel;
using System.Text;
using System.Xml.Linq;

namespace ICSharpCode.ILSpy
{
	[Flags]
	enum VisibleMembersFlags
	{
		AssemblyDef		= 0x00000001,
		ModuleDef		= 0x00000002,
		Namespace		= 0x00000004,
		TypeDef			= 0x00000008,
		FieldDef		= 0x00000010,
		MethodDef		= 0x00000020,
		PropertyDef		= 0x00000040,
		EventDef		= 0x00000080,
		AssemblyRef		= 0x00000100,
		BaseTypes		= 0x00000200,
		DerivedTypes	= 0x00000400,
		ModuleRef		= 0x00000800,
		Resources		= 0x00001000,
		NonNetFile		= 0x00002000,
		GenericTypeDef	= 0x00004000,
		NonGenericTypeDef=0x00008000,
		EnumTypeDef		= 0x00010000,
		InterfaceTypeDef= 0x00020000,
		ClassTypeDef	= 0x00040000,
		ValueTypeDef	= 0x00080000,
		TypeDefOther	= GenericTypeDef | NonGenericTypeDef | EnumTypeDef | InterfaceTypeDef | ClassTypeDef | ValueTypeDef,
		AnyTypeDef		= TypeDef | TypeDefOther,
		All				= AssemblyDef | ModuleDef | Namespace | TypeDef |
						  FieldDef | MethodDef | PropertyDef | EventDef |
						  AssemblyRef | BaseTypes | DerivedTypes | ModuleRef |
						  Resources | NonNetFile,
	}

	static class VisibleMembersFlagsExtensions
	{
		public static string GetListString(this VisibleMembersFlags flags)
		{
			int count;
			return flags.GetListString(out count);
		}

		public static string GetListString(this VisibleMembersFlags flags, out int count)
		{
			var sb = new StringBuilder();
			count = 0;

			if ((flags & VisibleMembersFlags.AssemblyDef) != 0) AddString(sb, "AssemblyDef", ref count);
			if ((flags & VisibleMembersFlags.ModuleDef) != 0) AddString(sb, "ModuleDef", ref count);
			if ((flags & VisibleMembersFlags.Namespace) != 0) AddString(sb, "Namespace", ref count);
			if ((flags & VisibleMembersFlags.TypeDef) != 0) AddString(sb, "TypeDef", ref count);
			if ((flags & VisibleMembersFlags.FieldDef) != 0) AddString(sb, "FieldDef", ref count);
			if ((flags & VisibleMembersFlags.MethodDef) != 0) AddString(sb, "MethodDef", ref count);
			if ((flags & VisibleMembersFlags.PropertyDef) != 0) AddString(sb, "PropertyDef", ref count);
			if ((flags & VisibleMembersFlags.EventDef) != 0) AddString(sb, "EventDef", ref count);
			if ((flags & VisibleMembersFlags.AssemblyRef) != 0) AddString(sb, "AssemblyRef", ref count);
			if ((flags & VisibleMembersFlags.BaseTypes) != 0) AddString(sb, "BaseTypes", ref count);
			if ((flags & VisibleMembersFlags.DerivedTypes) != 0) AddString(sb, "DerivedTypes", ref count);
			if ((flags & VisibleMembersFlags.ModuleRef) != 0) AddString(sb, "ModuleRef", ref count);
			if ((flags & VisibleMembersFlags.Resources) != 0) AddString(sb, "Resources", ref count);
			if ((flags & VisibleMembersFlags.NonNetFile) != 0) AddString(sb, "NonNetFile", ref count);
			if ((flags & VisibleMembersFlags.GenericTypeDef) != 0) AddString(sb, "Generic TypeDef", ref count);
			if ((flags & VisibleMembersFlags.NonGenericTypeDef) != 0) AddString(sb, "Non-Generic TypeDef", ref count);
			if ((flags & VisibleMembersFlags.EnumTypeDef) != 0) AddString(sb, "Enum TypeDef", ref count);
			if ((flags & VisibleMembersFlags.InterfaceTypeDef) != 0) AddString(sb, "Interface TypeDef", ref count);
			if ((flags & VisibleMembersFlags.ClassTypeDef) != 0) AddString(sb, "Class TypeDef", ref count);
			if ((flags & VisibleMembersFlags.ValueTypeDef) != 0) AddString(sb, "Value Type TypeDef", ref count);

			return sb.ToString();
		}

		static void AddString(StringBuilder sb, string text, ref int count)
		{
			if (count++ != 0)
				sb.Append(", ");
			sb.Append(text);
		}
	}

	/// <summary>
	/// Represents the filters applied to the tree view.
	/// </summary>
	/// <remarks>
	/// This class is mutable; but the ILSpyTreeNode filtering assumes that filter settings are immutable.
	/// Thus, the main window will use one mutable instance (for data-binding), and will assign a new
	/// clone to the ILSpyTreeNodes whenever the main mutable instance changes.
	/// </remarks>
	public class FilterSettings : INotifyPropertyChanged
	{
		internal FilterSettings(VisibleMembersFlags flags, Language language, bool showInternalApi)
		{
			this.ShowInternalApi = showInternalApi;
			this.Language = language ?? Languages.GetLanguage("C#");
			this.Flags = flags;
		}

		public FilterSettings(XElement element)
		{
			this.ShowInternalApi = (bool?)element.Element("ShowInternalAPI") ?? true;
			this.Language = Languages.GetLanguage("C#");
			this.Flags = VisibleMembersFlags.All;
		}
		
		public XElement SaveAsXml()
		{
			return new XElement(
				"FilterSettings",
				new XElement("ShowInternalAPI", this.ShowInternalApi)
			);
		}

		VisibleMembersFlags flags;

		internal VisibleMembersFlags Flags {
			get { return flags; }
			set {
				if (flags != value) {
					flags = value;
					OnPropertyChanged("Flags");
				}
			}
		}
		
		string searchTerm;
		
		/// <summary>
		/// Gets/Sets the search term.
		/// Only tree nodes containing the search term will be shown.
		/// </summary>
		public string SearchTerm {
			get { return searchTerm; }
			set {
				if (searchTerm != value) {
					searchTerm = value;
					OnPropertyChanged("SearchTerm");
				}
			}
		}
		
		/// <summary>
		/// Gets whether a node with the specified text is matched by the current search term.
		/// </summary>
		public bool SearchTermMatches(string text)
		{
			if (string.IsNullOrEmpty(searchTerm))
				return true;
			return text.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
		}
		
		bool showInternalApi;
		
		/// <summary>
		/// Gets/Sets whether internal API members should be shown.
		/// </summary>
		public bool ShowInternalApi {
			get { return showInternalApi; }
			set {
				if (showInternalApi != value) {
					showInternalApi = value;
					OnPropertyChanged("ShowInternalAPI");
				}
			}
		}
		
		Language language;
		
		/// <summary>
		/// Gets/Sets the current language.
		/// </summary>
		/// <remarks>
		/// While this isn't related to filtering, having it as part of the FilterSettings
		/// makes it easy to pass it down into all tree nodes.
		/// </remarks>
		public Language Language {
			get { return language; }
			set {
				if (language != value) {
					language = value;
					OnPropertyChanged("Language");
				}
			}
		}
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null) {
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		
		public FilterSettings Clone()
		{
			FilterSettings f = (FilterSettings)MemberwiseClone();
			f.PropertyChanged = null;
			return f;
		}
	}
}
