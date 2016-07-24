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

using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Files.Tabs.DocViewer;

namespace dnSpy.Files.Tabs.DocViewer {
	static class SpanDataReferenceInfoExtensions {
		public static bool CompareReferences(ReferenceInfo refInfoA, ReferenceInfo refInfoB) {
			if (refInfoA.Reference == null || refInfoB.Reference == null)
				return false;
			if (refInfoA.Reference.Equals(refInfoB.Reference))
				return true;

			var mra = refInfoA.Reference as IMemberRef;
			var mrb = refInfoB.Reference as IMemberRef;
			if (mra != null && mrb != null) {
				// PERF: Prevent expensive resolves by doing a quick name check
				if (mra.Name != mrb.Name)
					return false;

				var dta = mra.DeclaringType;
				var dtb = mrb.DeclaringType;
				if (dta != null) {
					if (dtb == null)
						return false;
					if (dta.Name != dtb.Name)
						return false;
					if (dta.Namespace != dtb.Namespace)
						return false;
				}
				else {
					if (dtb != null)
						return false;
				}

				if (mra is IType) {
					if (!(mrb is IType))
						return false;
				}
				else if (mra.IsMethod) {
					if (!mrb.IsMethod)
						return false;
				}
				else if (mra.IsField) {
					if (!mrb.IsField)
						return false;
				}
				else
					return false;

				mra = Resolve(mra) ?? mra;
				mrb = Resolve(mrb) ?? mrb;
				return new SigComparer(SigComparerOptions.CompareDeclaringTypes | SigComparerOptions.PrivateScopeIsComparable).Equals(mra, mrb);
			}

			return false;
		}

		static IMemberRef Resolve(IMemberRef memberRef) {
			if (memberRef is ITypeDefOrRef)
				return ((ITypeDefOrRef)memberRef).ResolveTypeDef();
			if (memberRef is IMethod && ((IMethod)memberRef).IsMethod)
				return ((IMethod)memberRef).ResolveMethodDef();
			if (memberRef is IField)
				return ((IField)memberRef).ResolveFieldDef();
			Debug.Assert(memberRef is PropertyDef || memberRef is EventDef || memberRef is GenericParam, "Unknown IMemberRef");
			return null;
		}
	}
}
