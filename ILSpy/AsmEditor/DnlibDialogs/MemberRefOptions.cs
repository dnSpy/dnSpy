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
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	sealed class MemberRefOptions
	{
		public IMemberRefParent Class;
		public UTF8String Name;
		public CallingConventionSig Signature;
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();

		public MemberRefOptions()
		{
		}

		public MemberRefOptions(MemberRef mr)
		{
			this.Class = mr.Class;
			this.Name = mr.Name;
			this.Signature = mr.Signature;
			this.CustomAttributes.AddRange(mr.CustomAttributes);
		}

		public MemberRef CopyTo(MemberRef mr)
		{
			mr.Class = this.Class;
			mr.Name = this.Name ?? UTF8String.Empty;
			mr.Signature = this.Signature;
			mr.CustomAttributes.Clear();
			mr.CustomAttributes.AddRange(this.CustomAttributes);
			return mr;
		}

		public MemberRef Create(ModuleDef ownerModule)
		{
			return ownerModule.UpdateRowId(CopyTo(new MemberRefUser(ownerModule)));
		}
	}
}
