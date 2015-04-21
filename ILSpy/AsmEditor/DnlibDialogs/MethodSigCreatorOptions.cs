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

using System;

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	public sealed class MethodSigCreatorOptions : ICloneable
	{
		/// <summary>
		/// True if it's a property signature, false if it's a method signature
		/// </summary>
		public bool IsPropertySig { get; set; }

		/// <summary>
		/// True if a vararg signature can be created
		/// </summary>
		public bool CanHaveSentinel { get; set; }

		public TypeSigCreatorOptions TypeSigCreatorOptions {
			get { return typeSigCreatorOptions; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				typeSigCreatorOptions = value;
			}
		}
		TypeSigCreatorOptions typeSigCreatorOptions;

		public MethodSigCreatorOptions(TypeSigCreatorOptions typeSigCreatorOptions)
		{
			this.TypeSigCreatorOptions = typeSigCreatorOptions;
		}

		public MethodSigCreatorOptions Clone()
		{
			var clone = (MethodSigCreatorOptions)MemberwiseClone();
			clone.TypeSigCreatorOptions = TypeSigCreatorOptions.Clone();
			return clone;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}
	}
}
