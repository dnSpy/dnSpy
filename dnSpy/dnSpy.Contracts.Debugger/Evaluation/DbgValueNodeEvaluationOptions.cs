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

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// <see cref="DbgValueNode"/> evaluation options
	/// </summary>
	[Flags]
	public enum DbgValueNodeEvaluationOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None						= 0,

		/// <summary>
		/// Don't allow function evaluations (calling code in debugged process)
		/// </summary>
		NoFuncEval					= 0x00000001,

		/// <summary>
		/// Show the Results View only
		/// </summary>
		ResultsView					= 0x00000002,

		/// <summary>
		/// Show the Dynamic View only
		/// </summary>
		DynamicView					= 0x00000004,

		/// <summary>
		/// Show the raw view (don't use debugger type proxies)
		/// </summary>
		RawView						= 0x00000008,

		/// <summary>
		/// Hide compiler generated members in variables windows (respect debugger attributes, eg. <see cref="CompilerGeneratedAttribute"/>)
		/// </summary>
		HideCompilerGeneratedMembers= 0x00000010,

		/// <summary>
		/// Respect attributes that can hide a member, eg. <see cref="DebuggerBrowsableAttribute"/> and <see cref="DebuggerBrowsableState.Never"/>
		/// </summary>
		RespectHideMemberAttributes	= 0x00000020,

		/// <summary>
		/// Only show public members
		/// </summary>
		PublicMembers				= 0x00000040,

		/// <summary>
		/// Roots can't be hidden
		/// </summary>
		NoHideRoots					= 0x00000080,

		/// <summary>
		/// Hide deprecated members
		/// </summary>
		HideDeprecatedError			= 0x00000100,
	}
}
