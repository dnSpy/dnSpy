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

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// <see cref="IDecompilerOption"/> constants
	/// </summary>
	public static class DecompilerOptionConstants {
		/// <summary />
		public static readonly Guid ShowILComments_GUID = new Guid("241A61B8-0F12-438C-A431-029B9FAEB124");
		/// <summary />
		public static readonly string ShowILComments_NAME = "comments";

		/// <summary />
		public static readonly Guid ShowXmlDocumentation_GUID = new Guid("6D50BA49-D76C-4EDD-BF5A-F81B894CCD2C");
		/// <summary />
		public static readonly string ShowXmlDocumentation_NAME = "xml-doc";

		/// <summary />
		public static readonly Guid ShowTokenAndRvaComments_GUID = new Guid("99475485-C462-4D60-AF90-C14008577A9D");
		/// <summary />
		public static readonly string ShowTokenAndRvaComments_NAME = "tokens";

		/// <summary />
		public static readonly Guid ShowILBytes_GUID = new Guid("E03DA52E-927C-4AA0-8FDE-9607125B606C");
		/// <summary />
		public static readonly string ShowILBytes_NAME = "bytes";

		/// <summary />
		public static readonly Guid SortMembers_GUID = new Guid("51FEEFED-5353-4637-A75E-7CA87EFA3998");
		/// <summary />
		public static readonly string SortMembers_NAME = "sort-members";

		/// <summary />
		public static readonly Guid MemberOrder_GUID = new Guid("8E6FE77A-2BCB-4F34-A41B-7F097560A211");
		/// <summary />
		public static readonly string MemberOrder_NAME = "member-order";

		/// <summary />
		public static readonly Guid AnonymousMethods_GUID = new Guid("74BBA9E7-CD43-4C81-9A7C-9F49D4BDA3D9");
		/// <summary />
		public static readonly string AnonymousMethods_NAME = "anon-methods";

		/// <summary />
		public static readonly Guid ExpressionTrees_GUID = new Guid("DDF1B2BA-AD71-4A7A-ADCB-690D5F54FAF8");
		/// <summary />
		public static readonly string ExpressionTrees_NAME = "expr-trees";

		/// <summary />
		public static readonly Guid YieldReturn_GUID = new Guid("07FD6290-2B5B-424E-B7E5-B64B32881F81");
		/// <summary />
		public static readonly string YieldReturn_NAME = "yield";

		/// <summary />
		public static readonly Guid AsyncAwait_GUID = new Guid("A85EDBC5-88C4-417F-9BE0-3AC3CB8107B7");
		/// <summary />
		public static readonly string AsyncAwait_NAME = "async";

		/// <summary />
		public static readonly Guid AutomaticProperties_GUID = new Guid("F6B11911-94BF-486B-AB8E-100B6A1B71FC");
		/// <summary />
		public static readonly string AutomaticProperties_NAME = "auto-props";

		/// <summary />
		public static readonly Guid AutomaticEvents_GUID = new Guid("36D8D33E-4E5B-44E4-B638-6A4791F16024");
		/// <summary />
		public static readonly string AutomaticEvents_NAME = "auto-events";

		/// <summary />
		public static readonly Guid UsingStatement_GUID = new Guid("24511B87-F19D-4AAC-80FC-A7E67D92A40E");
		/// <summary />
		public static readonly string UsingStatement_NAME = "using-stmt";

		/// <summary />
		public static readonly Guid ForEachStatement_GUID = new Guid("60ADA4EC-B7D1-407A-AE07-35D651956014");
		/// <summary />
		public static readonly string ForEachStatement_NAME = "foreach-stmt";

		/// <summary />
		public static readonly Guid LockStatement_GUID = new Guid("E1BD857F-B895-43C7-8844-5CF9F874E8C0");
		/// <summary />
		public static readonly string LockStatement_NAME = "lock-stmt";

		/// <summary />
		public static readonly Guid SwitchStatementOnString_GUID = new Guid("E0FC32C0-74DA-4D8C-886C-3182D7A77C16");
		/// <summary />
		public static readonly string SwitchStatementOnString_NAME = "switch-string";

		/// <summary />
		public static readonly Guid UsingDeclarations_GUID = new Guid("DAC40A5C-C867-4EF2-8DA3-9AFA285A47CE");
		/// <summary />
		public static readonly string UsingDeclarations_NAME = "using-decl";

		/// <summary />
		public static readonly Guid QueryExpressions_GUID = new Guid("CFB4D69F-0A98-4763-A863-6227159A0BD6");
		/// <summary />
		public static readonly string QueryExpressions_NAME = "query-expr";

		/// <summary />
		public static readonly Guid FullyQualifyAmbiguousTypeNames_GUID = new Guid("C60EA9B7-D469-4FA4-BF85-4DF755B521A3");
		/// <summary />
		public static readonly string FullyQualifyAmbiguousTypeNames_NAME = "ambig-full-names";

		/// <summary />
		public static readonly Guid FullyQualifyAllTypes_GUID = new Guid("916F3D3C-00E1-4D1D-ABFD-EA7092DF5028");
		/// <summary />
		public static readonly string FullyQualifyAllTypes_NAME = "full-names";

		/// <summary />
		public static readonly Guid UseDebugSymbols_GUID = new Guid("D8E085BF-9A1B-463C-96C6-894979DD131D");
		/// <summary />
		public static readonly string UseDebugSymbols_NAME = "use-debug-syms";

		/// <summary />
		public static readonly Guid ObjectOrCollectionInitializers_GUID = new Guid("1CD4E962-2DF0-4681-A631-E0A2F6DD641E");
		/// <summary />
		public static readonly string ObjectOrCollectionInitializers_NAME = "obj-inits";

		/// <summary />
		public static readonly Guid RemoveEmptyDefaultConstructors_GUID = new Guid("79DEF757-13D3-413A-9524-4104F2F179BE");
		/// <summary />
		public static readonly string RemoveEmptyDefaultConstructors_NAME = "remove-emtpy-ctors";

		/// <summary />
		public static readonly Guid IntroduceIncrementAndDecrement_GUID = new Guid("A7A2255B-3089-448E-BF3E-CB2AB2050D8F");
		/// <summary />
		public static readonly string IntroduceIncrementAndDecrement_NAME = "inc-dec";

		/// <summary />
		public static readonly Guid MakeAssignmentExpressions_GUID = new Guid("6FFF99B5-D59A-4EB0-87A9-D297176D51B9");
		/// <summary />
		public static readonly string MakeAssignmentExpressions_NAME = "make-assign-expr";

		/// <summary />
		public static readonly Guid AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject_GUID = new Guid("5303AAB5-CAFF-469A-B19C-CE4FBAC88CC3");
		/// <summary />
		public static readonly string AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject_NAME = "always-create-ex-var";

		/// <summary />
		public static readonly Guid ForceShowAllMembers_GUID = new Guid("237BFCDA-07A5-493C-9ACF-8698EBF395FA");
		/// <summary />
		public static readonly string ForceShowAllMembers_NAME = "show-all";

		/// <summary />
		public static readonly Guid SortSystemUsingStatementsFirst_GUID = new Guid("48BF91F6-A186-45B9-8A79-EEFEA5E5BAB1");
		/// <summary />
		public static readonly string SortSystemUsingStatementsFirst_NAME = "system-first";

		/// <summary />
		public static readonly Guid MaxArrayElements_GUID = new Guid("E0DEC360-EE40-4F61-9106-D9F142F4CC9A");
		/// <summary />
		public static readonly string MaxArrayElements_NAME = "max-array-elems";

		/// <summary />
		public static readonly Guid SortCustomAttributes_GUID = new Guid("8E3E8009-AC00-436E-B476-08B2E697AD32");
		/// <summary />
		public static readonly string SortCustomAttributes_NAME = "sort-custom-attrs";

		/// <summary />
		public static readonly Guid UseSourceCodeOrder_GUID = new Guid("11B1D294-1D85-4A5B-B9E9-B2A8AB889F51");
		/// <summary />
		public static readonly string UseSourceCodeOrder_NAME = "src-order";

		/// <summary />
		public static readonly Guid AllowFieldInitializers_GUID = new Guid("148CE5B9-95EC-441A-BDC8-1EAFFC02B097");
		/// <summary />
		public static readonly string AllowFieldInitializers_NAME = "field-initializers";
	}
}
