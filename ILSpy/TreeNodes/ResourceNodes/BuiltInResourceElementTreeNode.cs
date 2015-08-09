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

using System.Collections.Generic;
using System.IO;
using dnlib.DotNet.Resources;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.NRefactory;

namespace ICSharpCode.ILSpy.TreeNodes {
	public sealed class BuiltInResourceElementTreeNode : ResourceElementTreeNode {
		public BuiltInResourceElementTreeNode(ResourceElement resElem)
			: base(resElem) {
		}

		public override string IconName {
			get {
				if (resElem.ResourceData.Code == ResourceTypeCode.String)
					return "Strings";
				else if (resElem.ResourceData.Code >= ResourceTypeCode.UserTypes)
					return "UserDefinedDataType";
				return ResourceUtils.GetIconName(Name, "Binary");
			}
		}

		public override bool View(DecompilerTextView textView) {
			if (resElem.ResourceData.Code == ResourceTypeCode.String) {
				var output = new AvalonEditTextOutput();
				output.Write((string)((BuiltInResourceData)resElem.ResourceData).Data, TextTokenType.Text);
				textView.ShowNode(output, this, null);
				return true;
			}
			if (resElem.ResourceData.Code == ResourceTypeCode.ByteArray || resElem.ResourceData.Code == ResourceTypeCode.Stream) {
				var data = (byte[])((BuiltInResourceData)resElem.ResourceData).Data;
				return ResourceTreeNode.View(this, textView, new MemoryStream(data), Name);
			}

			return base.View(textView);
		}

		public override string GetStringContents() {
			if (resElem.ResourceData.Code == ResourceTypeCode.ByteArray || resElem.ResourceData.Code == ResourceTypeCode.Stream) {
				var data = (byte[])((BuiltInResourceData)resElem.ResourceData).Data;
				return ResourceTreeNode.GetStringContents(new MemoryStream(data));
			}
			return null;
		}

		protected override IEnumerable<ResourceData> GetDeserialized() {
			if (resElem.ResourceData.Code == ResourceTypeCode.Null)
				yield return new ResourceData(resElem.Name, () => new MemoryStream());
			else if (resElem.ResourceData.Code == ResourceTypeCode.String)
				yield return new ResourceData(resElem.Name, () => ResourceUtils.StringToStream((string)((BuiltInResourceData)resElem.ResourceData).Data));
			else if (resElem.ResourceData.Code == ResourceTypeCode.ByteArray || resElem.ResourceData.Code == ResourceTypeCode.Stream)
				yield return new ResourceData(resElem.Name, () => new MemoryStream((byte[])((BuiltInResourceData)resElem.ResourceData).Data));
			else if (resElem.ResourceData.Code >= ResourceTypeCode.UserTypes)
				yield return new ResourceData(resElem.Name, () => new MemoryStream(((BinaryResourceData)resElem.ResourceData).Data));
			else
				yield return new ResourceData(resElem.Name, () => ResourceUtils.StringToStream(this.ValueString));
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("resbuiltin", UIUtils.CleanUpName(resElem.Name)); }
		}
	}
}
