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

using System.Diagnostics;
using dnSpy.Contracts.Debugger.Evaluation;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace dnSpy.Roslyn.Debugger.ExpressionCompiler {
	static class ImageNameUtils {
		public static string GetImageName(this ResultProperties resultProperties) {
			switch (resultProperties.Category) {
			case DkmEvaluationResultCategory.Other:
			case DkmEvaluationResultCategory.Data:
				if ((resultProperties.ModifierFlags & DkmEvaluationResultTypeModifierFlags.Constant) != 0)
					goto case DkmEvaluationResultCategory.Field;
				return PredefinedDbgValueNodeImageNames.Data;

			case DkmEvaluationResultCategory.Method:
				switch (resultProperties.AccessType) {
				case DkmEvaluationResultAccessType.None:
					return PredefinedDbgValueNodeImageNames.Method;
				case DkmEvaluationResultAccessType.Public:
					return PredefinedDbgValueNodeImageNames.MethodPublic;
				case DkmEvaluationResultAccessType.Private:
					return PredefinedDbgValueNodeImageNames.MethodPrivate;
				case DkmEvaluationResultAccessType.Protected:
					return PredefinedDbgValueNodeImageNames.MethodFamily;
				case DkmEvaluationResultAccessType.Final:
					return PredefinedDbgValueNodeImageNames.Method;
				case DkmEvaluationResultAccessType.Internal:
					return PredefinedDbgValueNodeImageNames.MethodFamily;
				default:
					Debug.Fail($"Unknown access type: {resultProperties.AccessType}");
					return PredefinedDbgValueNodeImageNames.Method;
				}

			case DkmEvaluationResultCategory.Event:
				switch (resultProperties.AccessType) {
				case DkmEvaluationResultAccessType.None:
					return PredefinedDbgValueNodeImageNames.Event;
				case DkmEvaluationResultAccessType.Public:
					return PredefinedDbgValueNodeImageNames.EventPublic;
				case DkmEvaluationResultAccessType.Private:
					return PredefinedDbgValueNodeImageNames.EventPrivate;
				case DkmEvaluationResultAccessType.Protected:
					return PredefinedDbgValueNodeImageNames.EventFamily;
				case DkmEvaluationResultAccessType.Final:
					return PredefinedDbgValueNodeImageNames.Event;
				case DkmEvaluationResultAccessType.Internal:
					return PredefinedDbgValueNodeImageNames.EventFamily;
				default:
					Debug.Fail($"Unknown access type: {resultProperties.AccessType}");
					return PredefinedDbgValueNodeImageNames.Event;
				}

			case DkmEvaluationResultCategory.Property:
				switch (resultProperties.AccessType) {
				case DkmEvaluationResultAccessType.None:
					return PredefinedDbgValueNodeImageNames.Property;
				case DkmEvaluationResultAccessType.Public:
					return PredefinedDbgValueNodeImageNames.PropertyPublic;
				case DkmEvaluationResultAccessType.Private:
					return PredefinedDbgValueNodeImageNames.PropertyPrivate;
				case DkmEvaluationResultAccessType.Protected:
					return PredefinedDbgValueNodeImageNames.PropertyFamily;
				case DkmEvaluationResultAccessType.Final:
					return PredefinedDbgValueNodeImageNames.Property;
				case DkmEvaluationResultAccessType.Internal:
					return PredefinedDbgValueNodeImageNames.PropertyFamily;
				default:
					Debug.Fail($"Unknown access type: {resultProperties.AccessType}");
					return PredefinedDbgValueNodeImageNames.Property;
				}

			case DkmEvaluationResultCategory.Field:
				if ((resultProperties.ModifierFlags & DkmEvaluationResultTypeModifierFlags.Constant) != 0) {
					switch (resultProperties.AccessType) {
					case DkmEvaluationResultAccessType.None:
						return PredefinedDbgValueNodeImageNames.Constant;
					case DkmEvaluationResultAccessType.Public:
						return PredefinedDbgValueNodeImageNames.ConstantPublic;
					case DkmEvaluationResultAccessType.Private:
						return PredefinedDbgValueNodeImageNames.ConstantPrivate;
					case DkmEvaluationResultAccessType.Protected:
						return PredefinedDbgValueNodeImageNames.ConstantFamily;
					case DkmEvaluationResultAccessType.Final:
						return PredefinedDbgValueNodeImageNames.Constant;
					case DkmEvaluationResultAccessType.Internal:
						return PredefinedDbgValueNodeImageNames.ConstantFamily;
					default:
						Debug.Fail($"Unknown access type: {resultProperties.AccessType}");
						return PredefinedDbgValueNodeImageNames.Constant;
					}
				}
				else {
					switch (resultProperties.AccessType) {
					case DkmEvaluationResultAccessType.None:
						return PredefinedDbgValueNodeImageNames.Field;
					case DkmEvaluationResultAccessType.Public:
						return PredefinedDbgValueNodeImageNames.FieldPublic;
					case DkmEvaluationResultAccessType.Private:
						return PredefinedDbgValueNodeImageNames.FieldPrivate;
					case DkmEvaluationResultAccessType.Protected:
						return PredefinedDbgValueNodeImageNames.FieldFamily;
					case DkmEvaluationResultAccessType.Final:
						return PredefinedDbgValueNodeImageNames.Field;
					case DkmEvaluationResultAccessType.Internal:
						return PredefinedDbgValueNodeImageNames.FieldFamily;
					default:
						Debug.Fail($"Unknown access type: {resultProperties.AccessType}");
						return PredefinedDbgValueNodeImageNames.Field;
					}
				}

			case DkmEvaluationResultCategory.Class:
			case DkmEvaluationResultCategory.BaseClass:
			case DkmEvaluationResultCategory.InnerClass:
			case DkmEvaluationResultCategory.MostDerivedClass:
				switch (resultProperties.AccessType) {
				case DkmEvaluationResultAccessType.None:
					return PredefinedDbgValueNodeImageNames.Class;
				case DkmEvaluationResultAccessType.Public:
					return PredefinedDbgValueNodeImageNames.ClassPublic;
				case DkmEvaluationResultAccessType.Private:
					return PredefinedDbgValueNodeImageNames.ClassPrivate;
				case DkmEvaluationResultAccessType.Protected:
					return PredefinedDbgValueNodeImageNames.ClassProtected;
				case DkmEvaluationResultAccessType.Final:
					return PredefinedDbgValueNodeImageNames.Class;
				case DkmEvaluationResultAccessType.Internal:
					return PredefinedDbgValueNodeImageNames.ClassInternal;
				default:
					Debug.Fail($"Unknown access type: {resultProperties.AccessType}");
					return PredefinedDbgValueNodeImageNames.Class;
				}

			case DkmEvaluationResultCategory.Interface:
				switch (resultProperties.AccessType) {
				case DkmEvaluationResultAccessType.None:
					return PredefinedDbgValueNodeImageNames.Interface;
				case DkmEvaluationResultAccessType.Public:
					return PredefinedDbgValueNodeImageNames.InterfacePublic;
				case DkmEvaluationResultAccessType.Private:
					return PredefinedDbgValueNodeImageNames.InterfacePrivate;
				case DkmEvaluationResultAccessType.Protected:
					return PredefinedDbgValueNodeImageNames.InterfaceProtected;
				case DkmEvaluationResultAccessType.Final:
					return PredefinedDbgValueNodeImageNames.Interface;
				case DkmEvaluationResultAccessType.Internal:
					return PredefinedDbgValueNodeImageNames.InterfaceInternal;
				default:
					Debug.Fail($"Unknown access type: {resultProperties.AccessType}");
					return PredefinedDbgValueNodeImageNames.Interface;
				}

			case DkmEvaluationResultCategory.Local:
				return PredefinedDbgValueNodeImageNames.Local;

			case DkmEvaluationResultCategory.Parameter:
				return PredefinedDbgValueNodeImageNames.Parameter;

			default:
				Debug.Fail($"Unknown category: {resultProperties.Category}");
				return PredefinedDbgValueNodeImageNames.Data;
			}
		}
	}
}
