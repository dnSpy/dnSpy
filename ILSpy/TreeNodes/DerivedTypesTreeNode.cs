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

using System.Collections.Generic;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Utils;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Lists the super types of a class.
	/// </summary>
	sealed class DerivedTypesTreeNode : ILSpyTreeNode
	{
		readonly AssemblyList list;
		readonly TypeDef type;
		readonly ThreadingSupport threading;

		public DerivedTypesTreeNode(AssemblyList list, TypeDef type)
		{
			this.list = list;
			this.type = type;
			this.LazyLoading = true;
			this.threading = new ThreadingSupport();
		}

		protected override void Write(ITextOutput output, Language language)
		{
			output.Write("Derived Types", TextTokenType.Text);
		}

		public override object Icon
		{
			get { return ImageCache.Instance.GetImage("SubTypes", BackgroundType.TreeNode); }
		}

		public override object ExpandedIcon
		{
			get { return ImageCache.Instance.GetImage("SubTypesOpen", BackgroundType.TreeNode); }
		}

		public override FilterResult Filter(FilterSettings settings)
		{
			var res = settings.Filter.GetFilterResult(this);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			return base.Filter(settings);
		}

		protected override void LoadChildren()
		{
			threading.LoadChildren(this, FetchChildren);
		}

		IEnumerable<ILSpyTreeNode> FetchChildren(CancellationToken cancellationToken)
		{
			// FetchChildren() runs on the main thread; but the enumerator will be consumed on a background thread
			return FindDerivedTypes(type, list.GetAllModules(), cancellationToken);
		}

		internal static IEnumerable<DerivedTypesEntryNode> FindDerivedTypes(TypeDef type, ModuleDef[] modules, CancellationToken cancellationToken)
		{
			foreach (ModuleDef module in modules) {
				foreach (TypeDef td in TreeTraversal.PreOrder(module.Types, t => t.NestedTypes)) {
					cancellationToken.ThrowIfCancellationRequested();
					if (type.IsInterface && td.HasInterfaces) {
						foreach (var typeRef in td.Interfaces) {
							if (IsSameType(typeRef.Interface, type))
								yield return new DerivedTypesEntryNode(td, modules);
						}
					} else if (!type.IsInterface && td.BaseType != null && IsSameType(td.BaseType, type)) {
						yield return new DerivedTypesEntryNode(td, modules);
					}
				}
			}
		}

		static bool IsSameType(ITypeDefOrRef typeRef, TypeDef type)
		{
			return new SigComparer().Equals(typeRef, type);
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			threading.Decompile(language, output, options, EnsureChildrenFiltered);
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("dtt", type.FullName); }
		}
	}
}