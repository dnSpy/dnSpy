/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using dnlib.DotNet;
using dnSpy.BamlDecompiler.Baml;
using dnSpy.BamlDecompiler.Rewrite;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.BamlDecompiler {
	internal class XamlDecompiler {
		static readonly IRewritePass[] rewritePasses = new IRewritePass[] {
			new XClassRewritePass(),
			new MarkupExtensionRewritePass(),
			new AttributeRewritePass(),
			new ConnectionIdRewritePass(),
			new DocumentRewritePass(),
		};

		public XDocument Decompile(ModuleDef module, BamlDocument document, CancellationToken token, BamlDecompilerOptions bamlDecompilerOptions, List<string> assemblyReferences) {
			var ctx = XamlContext.Construct(module, document, token, bamlDecompilerOptions);

			var handler = HandlerMap.LookupHandler(ctx.RootNode.Type);
			var elem = handler.Translate(ctx, ctx.RootNode, null);

			var xaml = new XDocument();
			xaml.Add(elem.Xaml.Element);

			foreach (var pass in rewritePasses) {
				token.ThrowIfCancellationRequested();
				pass.Run(ctx, xaml);
			}

			if (assemblyReferences != null)
				assemblyReferences.AddRange(ctx.Baml.AssemblyIdMap.Select(a => a.Value.AssemblyFullName));

			return xaml;
		}
	}
}
