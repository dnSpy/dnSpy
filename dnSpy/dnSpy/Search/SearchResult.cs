/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Diagnostics;
using System.Windows.Media;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.TreeView;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Highlighting;
using dnSpy.Shared.MVVM;
using dnSpy.Shared.Search;

namespace dnSpy.Search {
	class SearchResult : ViewModelBase, ISearchResult, IMDTokenNode, IComparable<ISearchResult> {
		IMDTokenProvider IMDTokenNode.Reference {
			get { return Reference2; }
		}
		IMDTokenProvider Reference2 {
			get { return Object as IMDTokenProvider; }
		}

		public object Reference {
			get {
				var ns = Object as string;
				if (ns != null)
					return new NamespaceRef(DnSpyFile, ns);
				var node = Object as IFileTreeNodeData;
				if (node != null)
					return node;
				return Reference2;
			}
		}

		public SearchResultContext Context { get; set; }

		public object Object { get; set; }
		public object NameObject { get; set; }
		public ImageReference ObjectImageReference { get; set; }
		public object LocationObject { get; set; }
		public ImageReference LocationImageReference { get; set; }
		public IDnSpyFile DnSpyFile { get; set; }
		public object ObjectInfo { get; set; }

		public void RefreshUI() {
			OnPropertyChanged("Image");
			OnPropertyChanged("LocationImage");
			OnPropertyChanged("NameUI");
			OnPropertyChanged("LocationUI");
			OnPropertyChanged("ToolTip");
		}

		ImageSource GetImage(ImageReference imgRef) {
			if (imgRef.Assembly == null)
				return null;
			return Context.ImageManager.GetImage(imgRef.Assembly, imgRef.Name, Context.BackgroundType);
		}

		public ImageSource Image {
			get { return GetImage(ObjectImageReference); }
		}

		public ImageSource LocationImage {
			get { return GetImage(LocationImageReference); }
		}

		public string ToolTip {
			get {
				var dnSpyFile = DnSpyFile;
				if (dnSpyFile == null)
					return null;
				var module = dnSpyFile.ModuleDef;
				if (module == null)
					return dnSpyFile.Filename;
				if (!string.IsNullOrWhiteSpace(module.Location))
					return module.Location;
				if (!string.IsNullOrWhiteSpace(module.Name))
					return module.Name;
				if (module.Assembly != null && !string.IsNullOrWhiteSpace(module.Assembly.Name))
					return module.Assembly.Name;
				return null;
			}
		}

		public object NameUI {
			get { return CreateUI(NameObject, false); }
		}

		public object LocationUI {
			get { return CreateUI(LocationObject, true); }
		}

		public override string ToString() {
			var output = new NoSyntaxHighlightOutput();
			CreateUI(output, NameObject, false);
			return output.ToString();
		}

		object CreateUI(object o, bool includeNamespace) {
			var gen = UISyntaxHighlighter.Create(Context.SyntaxHighlight);
			var output = gen.Output;
			CreateUI(gen.Output, o, includeNamespace);
			return gen.CreateResult();
		}

		void CreateUI(ISyntaxHighlightOutput output, object o, bool includeNamespace) {
			var ns = o as NamespaceSearchResult;
			if (ns != null) {
				output.WriteNamespace(ns.Namespace);
				return;
			}

			var td = o as TypeDef;
			if (td != null) {
				Debug.Assert(Context.Language != null);
				Context.Language.WriteType(output, td, includeNamespace);
				return;
			}

			var md = o as MethodDef;
			if (md != null) {
				output.Write(IdentifierEscaper.Escape(md.Name), TextTokenKindUtils.GetTextTokenKind(md));
				return;
			}

			var fd = o as FieldDef;
			if (fd != null) {
				output.Write(IdentifierEscaper.Escape(fd.Name), TextTokenKindUtils.GetTextTokenKind(fd));
				return;
			}

			var pd = o as PropertyDef;
			if (pd != null) {
				output.Write(IdentifierEscaper.Escape(pd.Name), TextTokenKindUtils.GetTextTokenKind(pd));
				return;
			}

			var ed = o as EventDef;
			if (ed != null) {
				output.Write(IdentifierEscaper.Escape(ed.Name), TextTokenKindUtils.GetTextTokenKind(ed));
				return;
			}

			var asm = o as AssemblyDef;
			if (asm != null) {
				output.Write(asm);
				return;
			}

			var mod = o as ModuleDef;
			if (mod != null) {
				output.WriteModule(mod.FullName);
				return;
			}

			var asmRef = o as AssemblyRef;
			if (asmRef != null) {
				output.Write(asmRef);
				return;
			}

			var modRef = o as ModuleRef;
			if (modRef != null) {
				output.WriteModule(modRef.FullName);
				return;
			}

			// non-.NET file
			var file = o as IDnSpyFile;
			if (file != null) {
				output.Write(file.GetShortName(), BoxedTextTokenKind.Text);
				return;
			}

			var resNode = o as IResourceNode;
			if (resNode != null) {
				output.WriteFilename(resNode.Name);
				return;
			}

			var resElNode = o as IResourceElementNode;
			if (resElNode != null) {
				output.WriteFilename(resElNode.Name);
				return;
			}

			var em = o as ErrorMessage;
			if (em != null) {
				output.Write(em.Text, em.Color);
				return;
			}

			Debug.Assert(o == null);
		}

		public static SearchResult CreateMessage(SearchResultContext context, string msg, object color, bool first) =>
			new MessageSearchResult(msg, color, first) { Context = context };

		public int CompareTo(ISearchResult other) {
			if (other == null)
				return -1;
			int o1 = GetOrder(this);
			int o2 = GetOrder(other);
			int d = o1.CompareTo(o2);
			if (d != 0)
				return d;
			var sr = other as SearchResult;
			return StringComparer.CurrentCultureIgnoreCase.Compare(GetCompareString(), sr == null ? other.ToString() : sr.GetCompareString());
		}

		static int GetOrder(ISearchResult other) {
			var mr = other as MessageSearchResult;
			return mr == null ? 0 : mr.Order;
		}

		string GetCompareString() {
			return compareString ?? (compareString = ToString());
		}
		string compareString = null;
	}

	sealed class ErrorMessage {
		public string Text {
			get { return msg; }
		}
		readonly string msg;

		public object Color {
			get { return color; }
		}
		readonly object color;

		public ErrorMessage(string msg, object color) {
			this.msg = msg;
			this.color = color;
		}
	}

	sealed class MessageSearchResult : SearchResult {
		readonly string msg;
		public int Order {
			get { return order; }
		}
		readonly int order;

		public MessageSearchResult(string msg, object color, bool first) {
			this.msg = msg;
			this.NameObject = new ErrorMessage(msg, color);
			this.order = first ? int.MinValue : int.MaxValue;
		}

		public override string ToString() {
			return msg;
		}
	}
}
