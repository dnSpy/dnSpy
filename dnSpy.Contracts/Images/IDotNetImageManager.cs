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

using dnlib.DotNet;
using dnlib.PE;

namespace dnSpy.Contracts.Images {
	/// <summary>
	/// Image manager for .NET fields, types, etc
	/// </summary>
	public interface IDotNetImageManager {
		/// <summary>
		/// Gets an image
		/// </summary>
		/// <param name="mod">Module</param>
		/// <returns></returns>
		ImageReference GetImageReference(ModuleDef mod);

		/// <summary>
		/// Gets an image
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		ImageReference GetImageReference(TypeDef type);

		/// <summary>
		/// Gets an image
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		ImageReference GetImageReference(FieldDef field);

		/// <summary>
		/// Gets an image
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		ImageReference GetImageReference(MethodDef method);

		/// <summary>
		/// Gets an image
		/// </summary>
		/// <param name="event">Event</param>
		/// <returns></returns>
		ImageReference GetImageReference(EventDef @event);

		/// <summary>
		/// Gets an image
		/// </summary>
		/// <param name="property">Property</param>
		/// <returns></returns>
		ImageReference GetImageReference(PropertyDef property);

		/// <summary>
		/// Gets an image
		/// </summary>
		/// <param name="modRef">Module reference</param>
		/// <returns></returns>
		ImageReference GetImageReference(ModuleRef modRef);

		/// <summary>
		/// Gets an image
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <returns></returns>
		ImageReference GetImageReference(AssemblyDef assembly);

		/// <summary>
		/// Gets an image
		/// </summary>
		/// <param name="asmRef">Assembly reference</param>
		/// <returns></returns>
		ImageReference GetImageReference(AssemblyRef asmRef);

		/// <summary>
		/// Gets an image
		/// </summary>
		/// <param name="peImage">PE image</param>
		/// <returns></returns>
		ImageReference GetImageReference(IPEImage peImage);

		/// <summary>
		/// Gets a namespace image
		/// </summary>
		/// <returns></returns>
		ImageReference GetNamespaceImageReference();
	}
}
