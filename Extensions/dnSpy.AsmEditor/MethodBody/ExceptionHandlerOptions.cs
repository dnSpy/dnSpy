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

using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class ExceptionHandlerOptions {
		public InstructionVM TryStart;
		public InstructionVM TryEnd;
		public InstructionVM FilterStart;
		public InstructionVM HandlerStart;
		public InstructionVM HandlerEnd;
		public ITypeDefOrRef CatchType;
		public ExceptionHandlerType HandlerType;

		public ExceptionHandlerOptions() {
		}

		public ExceptionHandlerOptions(Dictionary<object, object> ops, ExceptionHandler eh) {
			this.TryStart = (InstructionVM)BodyUtils.TryGetVM(ops, eh.TryStart);
			this.TryEnd = (InstructionVM)BodyUtils.TryGetVM(ops, eh.TryEnd);
			this.FilterStart = (InstructionVM)BodyUtils.TryGetVM(ops, eh.FilterStart);
			this.HandlerStart = (InstructionVM)BodyUtils.TryGetVM(ops, eh.HandlerStart);
			this.HandlerEnd = (InstructionVM)BodyUtils.TryGetVM(ops, eh.HandlerEnd);
			this.CatchType = eh.CatchType;
			this.HandlerType = eh.HandlerType;
		}

		public ExceptionHandler CopyTo(Dictionary<object, object> ops, ExceptionHandler eh) {
			eh.TryStart = BodyUtils.TryGetModel(ops, this.TryStart) as Instruction;
			eh.TryEnd = BodyUtils.TryGetModel(ops, this.TryEnd) as Instruction;
			eh.FilterStart = BodyUtils.TryGetModel(ops, this.FilterStart) as Instruction;
			eh.HandlerStart = BodyUtils.TryGetModel(ops, this.HandlerStart) as Instruction;
			eh.HandlerEnd = BodyUtils.TryGetModel(ops, this.HandlerEnd) as Instruction;
			eh.CatchType = this.CatchType;
			eh.HandlerType = this.HandlerType;
			return eh;
		}

		public ExceptionHandler Create(Dictionary<object, object> ops) => CopyTo(ops, new ExceptionHandler());
	}
}
