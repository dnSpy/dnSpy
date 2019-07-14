/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Threading;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnlib.IO;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Contracts.Documents.TreeView.Resources {
	/// <summary>
	/// Implemented by all resource nodes, and contains all raw data, RVA, and size
	/// </summary>
	public interface IResourceDataProvider {
		/// <summary>
		/// RVA of resource or 0
		/// </summary>
		uint RVA { get; }

		/// <summary>
		/// File offset of resource or 0
		/// </summary>
		uint FileOffset { get; }

		/// <summary>
		/// Length of the resource
		/// </summary>
		uint Length { get; }

		/// <summary>
		/// Gets the resource data
		/// </summary>
		/// <param name="type">Type of data</param>
		/// <returns></returns>
		IEnumerable<ResourceData> GetResourceData(ResourceDataType type);
	}

	/// <summary>
	/// Implemented by all resource nodes, and contains all raw data, RVA, and size
	/// </summary>
	public interface IResourceNode : IResourceDataProvider {
		/// <summary>
		/// Write a short string (typically one line) to <paramref name="output"/>
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="showOffset">true to write offset and size of resource in the PE image, if
		/// that info is available</param>
		void WriteShort(IDecompilerOutput output, IDecompiler decompiler, bool showOffset);

		/// <summary>
		/// Used by the searcher. Should only return a string if the data is text or compiled text.
		/// I.e., null should be returned if it's an <see cref="int"/>, but a string if it's eg. an
		/// XML doc.
		/// </summary>
		/// <param name="token">Cancellation token</param>
		/// <param name="canDecompile">true if the callee can decompile (eg. XAML), false otherwise</param>
		/// <returns></returns>
		string? ToString(CancellationToken token, bool canDecompile);
	}

	/// <summary>
	/// Utils
	/// </summary>
	public static class ResourceDataProviderUtils {
		/// <summary>
		/// Gets a <see cref="IResourceDataProvider"/> if <paramref name="node"/> is a resource node, else returns null
		/// </summary>
		/// <param name="node">Node</param>
		/// <returns></returns>
		public static IResourceDataProvider? GetResourceDataProvider(DocumentTreeNodeData node) {
			if (node is IResourceDataProvider provider)
				return provider;
			if (ResourceNode.GetResource(node) is Resource resource)
				return new ResourceNode_ResourceDataProvider(node, resource);
			if (ResourceElementNode.GetResourceElement(node) is ResourceElement resourceElement)
				return new ResourceElementNode_ResourceDataProvider(node, resourceElement);
			return null;
		}

		sealed class ResourceNode_ResourceDataProvider : IResourceDataProvider {
			public uint FileOffset {
				get {
					GetModuleOffset(out var fo);
					return (uint)fo;
				}
			}

			public uint Length {
				get {
					var er = resource as EmbeddedResource;
					return er is null ? 0 : er.Length;
				}
			}

			public uint RVA {
				get {
					var module = GetModuleOffset(out var fo);
					if (module is null)
						return 0;

					return (uint)module.Metadata.PEImage.ToRVA(fo);
				}
			}

			ModuleDefMD? GetModuleOffset(out FileOffset fileOffset) =>
				ResourceNode.GetModuleOffset(node, resource, out fileOffset);

			readonly DocumentTreeNodeData node;
			readonly Resource resource;

			public ResourceNode_ResourceDataProvider(DocumentTreeNodeData node, Resource resource) {
				this.node = node;
				this.resource = resource;
			}

			public IEnumerable<ResourceData> GetResourceData(ResourceDataType type) {
				if (resource is EmbeddedResource er)
					yield return new ResourceData(resource.Name, ct => er.CreateReader().AsStream());
			}
		}

		sealed class ResourceElementNode_ResourceDataProvider : IResourceDataProvider {
			public uint FileOffset {
				get {
					GetModuleOffset(out var fo);
					return (uint)fo;
				}
			}

			public uint Length => resourceElement.ResourceData.EndOffset - resourceElement.ResourceData.StartOffset;

			public uint RVA {
				get {
					var module = GetModuleOffset(out var fo);
					if (module is null)
						return 0;

					return (uint)module.Metadata.PEImage.ToRVA(fo);
				}
			}

			ModuleDefMD? GetModuleOffset(out FileOffset fileOffset) =>
				ResourceElementNode.GetModuleOffset(node, resourceElement, out fileOffset);

			readonly DocumentTreeNodeData node;
			readonly ResourceElement resourceElement;

			public ResourceElementNode_ResourceDataProvider(DocumentTreeNodeData node, ResourceElement resourceElement) {
				this.node = node;
				this.resourceElement = resourceElement;
			}

			public IEnumerable<ResourceData> GetResourceData(ResourceDataType type) =>
				ResourceElementNode.GetSerializedData(resourceElement);
		}
	}
}
