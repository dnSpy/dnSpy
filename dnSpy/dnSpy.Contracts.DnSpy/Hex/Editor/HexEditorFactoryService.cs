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
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Creates hex views and hosts
	/// </summary>
	public abstract class HexEditorFactoryService {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexEditorFactoryService() { }

		/// <summary>
		/// Gets all predefined hex view roles
		/// </summary>
		public abstract VSTE.ITextViewRoleSet AllPredefinedRoles { get; }

		/// <summary>
		/// Gets the default hex view roles
		/// </summary>
		public abstract VSTE.ITextViewRoleSet DefaultRoles { get; }

		/// <summary>
		/// Gets an empty role set
		/// </summary>
		public abstract VSTE.ITextViewRoleSet NoRoles { get; }

		/// <summary>
		/// Raised when a new hex view is created
		/// </summary>
		public abstract event EventHandler<HexViewCreatedEventArgs> HexViewCreated;

		/// <summary>
		/// Creates a new <see cref="WpfHexView"/>
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <returns></returns>
		public virtual WpfHexView Create(HexBuffer buffer) => Create(buffer, (HexViewCreatorOptions)null);

		/// <summary>
		/// Creates a new <see cref="WpfHexView"/>
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="roles">Roles</param>
		/// <returns></returns>
		public virtual WpfHexView Create(HexBuffer buffer, VSTE.ITextViewRoleSet roles) => Create(buffer, roles, (HexViewCreatorOptions)null);

		/// <summary>
		/// Creates a new <see cref="WpfHexView"/>
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="roles">Roles</param>
		/// <param name="parentOptions">Parent options</param>
		/// <returns></returns>
		public virtual WpfHexView Create(HexBuffer buffer, VSTE.ITextViewRoleSet roles, VSTE.IEditorOptions parentOptions) => Create(buffer, roles, parentOptions, (HexViewCreatorOptions)null);

		/// <summary>
		/// Creates a new <see cref="WpfHexView"/>
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="options">Options or null</param>
		/// <returns></returns>
		public abstract WpfHexView Create(HexBuffer buffer, HexViewCreatorOptions options);

		/// <summary>
		/// Creates a new <see cref="WpfHexView"/>
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="roles">Roles</param>
		/// <param name="options">Options or null</param>
		/// <returns></returns>
		public abstract WpfHexView Create(HexBuffer buffer, VSTE.ITextViewRoleSet roles, HexViewCreatorOptions options);

		/// <summary>
		/// Creates a new <see cref="WpfHexView"/>
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="roles">Roles</param>
		/// <param name="parentOptions">Parent options</param>
		/// <param name="options">Options or null</param>
		/// <returns></returns>
		public abstract WpfHexView Create(HexBuffer buffer, VSTE.ITextViewRoleSet roles, VSTE.IEditorOptions parentOptions, HexViewCreatorOptions options);

		/// <summary>
		/// Creates a new <see cref="WpfHexViewHost"/>
		/// </summary>
		/// <param name="wpfHexView">Hex view</param>
		/// <param name="setFocus">true to set focus</param>
		/// <returns></returns>
		public abstract WpfHexViewHost CreateHost(WpfHexView wpfHexView, bool setFocus);

		/// <summary>
		/// Creates a role set
		/// </summary>
		/// <param name="roles">Roles</param>
		/// <returns></returns>
		public abstract VSTE.ITextViewRoleSet CreateTextViewRoleSet(IEnumerable<string> roles);

		/// <summary>
		/// Creates a role set
		/// </summary>
		/// <param name="roles">Roles</param>
		/// <returns></returns>
		public abstract VSTE.ITextViewRoleSet CreateTextViewRoleSet(params string[] roles);
	}
}
