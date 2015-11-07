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

using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.Contracts;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.TreeNodes {
	/// <summary>
	/// Creates resource nodes
	/// </summary>
	/// <typeparam name="TInput">Resource input data</typeparam>
	/// <typeparam name="TOutput">Resource node</typeparam>
	public interface IResourceFactory<TInput, TOutput> {
		/// <summary>
		/// Higher priority factories get called before lower priority factories
		/// </summary>
		int Priority { get; }

		/// <summary>
		/// Tries to create the resource node. Returns null if it's not a supported resource.
		/// The input stream can be at any position so initialize its position to 0 before use.
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="resInput">Resource</param>
		/// <returns></returns>
		TOutput Create(ModuleDef module, TInput resInput);
	}

	public static class ResourceFactory {
		static TOutput Create<TInput, TOutput>(ModuleDef module, TInput resInput) where TOutput : class {
			foreach (var creator in DnSpy.App.CompositionContainer.GetExportedValues<IResourceFactory<TInput, TOutput>>().OrderByDescending(a => a.Priority)) {
				try {
					var resNode = creator.Create(module, resInput);
					if (resNode != null)
						return resNode;
				}
				catch {
				}
			}

			return null;
		}

		public static ResourceTreeNode Create(ModuleDef module, Resource resource) {
			return Create<Resource, ResourceTreeNode>(module, resource) ?? CreateDefault(module, resource);
		}

		static ResourceTreeNode CreateDefault(ModuleDef module, Resource resource) {
			return new UnknownResourceTreeNode(resource);
		}

		public static ResourceElementTreeNode Create(ModuleDef module, ResourceElement resElem) {
			return Create<ResourceElement, ResourceElementTreeNode>(module, resElem) ?? CreateDefault(module, resElem);
		}

		static ResourceElementTreeNode CreateDefault(ModuleDef module, ResourceElement resElem) {
			return new BuiltInResourceElementTreeNode(resElem);
		}
	}
}
