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
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;

namespace ICSharpCode.ILSpy.AsmEditor.MethodBody
{
	sealed class CilBodyOptions
	{
		public bool KeepOldMaxStack;
		public bool InitLocals;
		public ushort MaxStack;
		public uint LocalVarSigTok;
		public List<Instruction> Instructions = new List<Instruction>();
		public List<ExceptionHandler> ExceptionHandlers = new List<ExceptionHandler>();
		public List<Local> Locals = new List<Local>();
		public PdbScope Scope;//TODO: Use this

		public CilBodyOptions()
		{
		}

		public CilBodyOptions(CilBody body)
		{
			this.KeepOldMaxStack = body.KeepOldMaxStack;
			this.InitLocals = body.InitLocals;
			this.MaxStack = body.MaxStack;
			this.LocalVarSigTok = body.LocalVarSigTok;
			this.Instructions.AddRange(body.Instructions);
			this.ExceptionHandlers.AddRange(body.ExceptionHandlers);
			this.Locals.AddRange(body.Variables);
			this.Scope = body.Scope;
		}

		public CilBody CopyTo(CilBody body)
		{
			body.KeepOldMaxStack = KeepOldMaxStack;
			body.InitLocals = InitLocals;
			body.MaxStack = MaxStack;
			body.LocalVarSigTok = LocalVarSigTok;
			body.Instructions.Clear();
			body.Instructions.AddRange(Instructions);
			body.ExceptionHandlers.Clear();
			body.ExceptionHandlers.AddRange(ExceptionHandlers);
			body.Variables.Clear();
			body.Variables.AddRange(this.Locals);
			body.Scope = this.Scope;
			body.UpdateInstructionOffsets();
			return body;
		}

		public CilBody Create()
		{
			return CopyTo(new CilBody());
		}
	}
}
