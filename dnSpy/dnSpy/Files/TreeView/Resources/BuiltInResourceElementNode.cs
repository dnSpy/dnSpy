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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.TextEditor;
using dnSpy.Contracts.TreeView;
using dnSpy.Shared.Files.TreeView.Resources;

namespace dnSpy.Files.TreeView.Resources {
	sealed class BuiltInResourceElementNode : ResourceElementNode, IBuiltInResourceElementNode, IDecompileSelf {
		public override Guid Guid => new Guid(FileTVConstants.BUILT_IN_RESOURCE_ELEMENT_NODE_GUID);

		protected override ImageReference GetIcon() {
			var asm = GetType().Assembly;
			if (ResourceElement.ResourceData.Code == ResourceTypeCode.String)
				return new ImageReference(asm, "Strings");
			else if (ResourceElement.ResourceData.Code >= ResourceTypeCode.UserTypes)
				return new ImageReference(asm, "UserDefinedDataType");
			return ResourceUtils.TryGetImageReference(asm, ResourceElement.Name) ?? new ImageReference(asm, "Binary");
		}

		public BuiltInResourceElementNode(ITreeNodeGroup treeNodeGroup, ResourceElement resourceElement)
			: base(treeNodeGroup, resourceElement) {
		}

		public override string ToString(CancellationToken token, bool canDecompile) {
			if (ResourceElement.ResourceData.Code == ResourceTypeCode.ByteArray || ResourceElement.ResourceData.Code == ResourceTypeCode.Stream) {
				var data = (byte[])((BuiltInResourceData)ResourceElement.ResourceData).Data;
				return ResourceUtils.TryGetString(new MemoryStream(data));
			}
			return null;
		}

		protected override IEnumerable<ResourceData> GetDeserializedData() {
			var re = ResourceElement;
			if (re.ResourceData.Code == ResourceTypeCode.Null)
				yield return new ResourceData(re.Name, token => new MemoryStream());
			else if (re.ResourceData.Code == ResourceTypeCode.String)
				yield return new ResourceData(re.Name, token => ResourceUtils.StringToStream((string)((BuiltInResourceData)re.ResourceData).Data));
			else if (re.ResourceData.Code == ResourceTypeCode.ByteArray || re.ResourceData.Code == ResourceTypeCode.Stream)
				yield return new ResourceData(re.Name, token => new MemoryStream((byte[])((BuiltInResourceData)re.ResourceData).Data));
			else if (re.ResourceData.Code >= ResourceTypeCode.UserTypes)
				yield return new ResourceData(re.Name, token => new MemoryStream(((BinaryResourceData)re.ResourceData).Data));
			else {
				var vs = this.ValueString;
				yield return new ResourceData(re.Name, token => ResourceUtils.StringToStream(vs));
			}
		}

		public bool Decompile(IDecompileNodeContext context) {
			if (ResourceElement.ResourceData.Code == ResourceTypeCode.String) {
				context.Output.Write((string)((BuiltInResourceData)ResourceElement.ResourceData).Data, BoxedOutputColor.Text);
				context.HighlightingExtension = ".txt";
				context.ContentTypeGuid = new Guid(ContentTypes.PLAIN_TEXT);
				return true;
			}

			if (ResourceElement.ResourceData.Code == ResourceTypeCode.ByteArray || ResourceElement.ResourceData.Code == ResourceTypeCode.Stream) {
				var data = (byte[])((BuiltInResourceData)ResourceElement.ResourceData).Data;
				return ResourceUtils.Decompile(context, new MemoryStream(data), Name);
			}

			return false;
		}
	}
}
