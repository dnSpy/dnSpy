/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	static class DbgEvaluationOptionsUtils {
		public static DbgValueNodeEvaluationOptions ToValueNodeEvaluationOptions(DbgEvaluationOptions options) {
			var res = DbgValueNodeEvaluationOptions.None;
			if ((options & DbgEvaluationOptions.NoFuncEval) != 0)
				res |= DbgValueNodeEvaluationOptions.NoFuncEval;
			if ((options & DbgEvaluationOptions.RawView) != 0)
				res |= DbgValueNodeEvaluationOptions.RawView;
			if ((options & DbgEvaluationOptions.HideCompilerGeneratedMembers) != 0)
				res |= DbgValueNodeEvaluationOptions.HideCompilerGeneratedMembers;
			if ((options & DbgEvaluationOptions.RespectHideMemberAttributes) != 0)
				res |= DbgValueNodeEvaluationOptions.RespectHideMemberAttributes;
			if ((options & DbgEvaluationOptions.PublicMembers) != 0)
				res |= DbgValueNodeEvaluationOptions.PublicMembers;
			if ((options & DbgEvaluationOptions.NoHideRoots) != 0)
				res |= DbgValueNodeEvaluationOptions.NoHideRoots;
			return res;
		}
	}
}
