/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using System.IO;
using dnlib.DotNet;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy.TreeNodes {
	public sealed class UnknownResourceTreeNode : ResourceTreeNode {
		public UnknownResourceTreeNode(Resource resource)
			: base(resource) {
		}

		public override void Decompile(Language language, ITextOutput output) {
			base.Decompile(language, output);
			var so = output as ISmartTextOutput;
			if (so != null) {
				so.AddButton(null, "Save", (s, e) => Save());
				so.WriteLine();
				so.WriteLine();
			}
		}

		public override bool View(TextView.DecompilerTextView textView) {
			EmbeddedResource er = r as EmbeddedResource;
			if (er != null)
				return View(this, textView, new MemoryStream(er.GetResourceData()), er.Name);
			return false;
		}

		public override string GetStringContents() {
			EmbeddedResource er = r as EmbeddedResource;
			if (er != null)
				return GetStringContents(new MemoryStream(er.GetResourceData()));
			return null;
		}
	}
}
