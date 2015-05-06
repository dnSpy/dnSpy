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

using ICSharpCode.ILSpy.AsmEditor.DnlibDialogs;

namespace ICSharpCode.ILSpy.AsmEditor.ViewHelpers
{
	interface ICreateConstantType
	{
		/// <summary>
		/// Create a constant
		/// </summary>
		/// <param name="value">null or value to show user</param>
		/// <param name="validConstants">Allowed constants or null if default custom attribute constants can be returned</param>
		/// <param name="allowNullString">true if strings can be null</param>
		/// <param name="arraysCanBeNull">true if arrays can be null</param>
		/// <param name="options">TypeSig creator options. Can be null if <paramref name="validConstants"/>
		/// doesn't contain <see cref="ConstantType.Type"/>, <see cref="ConstantType.TypeArray"/>
		/// and <see cref="ConstantType.ObjectArray"/>.</param>
		/// <param name="resultNoSpecialNull">Same as return value except it doesn't contain any
		/// special Null classes.</param>
		/// <param name="canceled">true if user canceled</param>
		/// <returns></returns>
		object Create(object value, ConstantType[] validConstants, bool allowNullString, bool arraysCanBeNull, TypeSigCreatorOptions options, out object resultNoSpecialNull, out bool canceled);
	}
}
