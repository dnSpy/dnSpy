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

namespace dnSpy.Contracts.Files.TreeView {
	/// <summary>
	/// Tree view constants
	/// </summary>
	public static class FileTVConstants {
		/// <summary>Guid of root node</summary>
		public const string ROOT_NODE_GUID = "E0D1E8A9-4470-4CB8-8DD7-11708EA6ED44";

		/// <summary><see cref="IMessageNode"/></summary>
		public const string DNSPY_MESSAGE_NODE_GUID = "C6F57A88-A030-4E8F-BCBC-3F17A3EADE57";

		/// <summary><see cref="IUnknownFileNode"/></summary>
		public const string DNSPY_UNKNOWN_FILE_NODE_GUID = "3117F133-58FC-4BE3-ABA6-331D6C962701";

		/// <summary><see cref="IPEFileNode"/></summary>
		public const string DNSPY_PEFILE_NODE_GUID = "CBE3DD51-3C13-4E2D-92BB-6AAB6A64028A";

		/// <summary><see cref="IAssemblyFileNode"/></summary>
		public const string DNSPY_ASSEMBLY_NODE_GUID = "AB10C139-2735-4595-9E47-2EE0EE247C6D";

		/// <summary><see cref="IModuleFileNode"/></summary>
		public const string DNSPY_MODULE_NODE_GUID = "597B3358-A6F5-47EA-B0D2-57EDD1208333";

		/// <summary><see cref="IResourcesNode"/></summary>
		public const string DNSPY_RESOURCES_NODE_GUID = "1DD75445-9DED-482F-B6EB-4FD13E4A2197";

		/// <summary><see cref="IResourcesNode"/></summary>
		public const string DNSPY_REFERENCES_NODE_GUID = "D2C27572-6874-4287-BE59-2D2A28C4D80B";

		/// <summary><see cref="INamespaceNode"/></summary>
		public const string DNSPY_NAMESPACE_NODE_GUID = "21FE74FA-4413-4F4F-964C-63DC966D66CC";

		/// <summary><see cref="IAssemblyReferenceNode"/></summary>
		public const string DNSPY_ASSEMBLYREF_NODE_GUID = "13151761-85EA-4A95-9C2D-4F7A6AC3A69D";

		/// <summary><see cref="IModuleReferenceNode"/></summary>
		public const string DNSPY_MODULEREF_NODE_GUID = "E3883417-71E1-4E5A-AB16-A3FB874DA2D5";

		/// <summary><see cref="IBaseTypeFolderNode"/></summary>
		public const string DNSPY_BASETYPEFOLDER_NODE_GUID = "5D8A8AF8-6604-4031-845F-755745DFB7A7";

		/// <summary><see cref="IDerivedTypesFolderNode"/></summary>
		public const string DNSPY_DERIVEDTYPESFOLDER_NODE_GUID = "E40470B7-A638-4BCC-9426-8F696EC260D9";

		/// <summary><see cref="IBaseTypeNode"/></summary>
		public const string DNSPY_BASETYPE_NODE_GUID = "BB9DCFC7-3527-410A-A4DA-E12FDCAC351C";

		/// <summary><see cref="IDerivedTypeNode"/></summary>
		public const string DNSPY_DERIVEDTYPE_NODE_GUID = "497D974B-53C0-453C-A8B4-026884B2E5D1";

		/// <summary><see cref="ITypeNode"/></summary>
		public const string DNSPY_TYPE_NODE_GUID = "EB18E75B-3627-405F-B7A0-B2F38FCDC071";

		/// <summary><see cref="IFieldNode"/></summary>
		public const string DNSPY_FIELD_NODE_GUID = "B4CB8C07-A684-4AF5-8FA2-561DC3E63110";

		/// <summary><see cref="IMethodNode"/></summary>
		public const string DNSPY_METHOD_NODE_GUID = "8CBBC53F-74AB-46C9-B6CB-796225D5E58A";

		/// <summary><see cref="IPropertyNode"/></summary>
		public const string DNSPY_PROPERTY_NODE_GUID = "38247C2D-AD67-4664-8118-01D21644031E";

		/// <summary><see cref="IEventNode"/></summary>
		public const string DNSPY_EVENT_NODE_GUID = "CA3F5F2B-560C-43BD-A3E5-CF504E2184A0";

		/// <summary>Order of PE node</summary>
		public const double ORDER_MODULE_PE = 0;

		/// <summary>Order of <see cref="IReferencesNode"/></summary>
		public const double ORDER_MODULE_REFERENCES = 100;

		/// <summary>Order of <see cref="IResourcesNode"/></summary>
		public const double ORDER_MODULE_RESOURCES = 200;

		/// <summary>Order of <see cref="INamespaceNode"/>s</summary>
		public const double ORDER_MODULE_NAMESPACE = 300;

		/// <summary>Order of <see cref="IAssemblyReferenceNode"/>s</summary>
		public const double ORDER_REFERENCES_ASSEMBLYREF = 0;

		/// <summary>Order of <see cref="IModuleReferenceNode"/>s</summary>
		public const double ORDER_REFERENCES_MODULEREF = 100;

		/// <summary>Order of <see cref="IAssemblyReferenceNode"/>s</summary>
		public const double ORDER_ASSEMBLYREF_ASSEMBLYREF = 0;

		/// <summary>Order of non-nested <see cref="ITypeNode"/>s</summary>
		public const double ORDER_NAMESPACE_TYPE = 0;

		/// <summary>Order of <see cref="IBaseTypeFolderNode"/>s</summary>
		public const double ORDER_TYPE_BASE = 0;

		/// <summary>Order of <see cref="IDerivedTypesFolderNode"/>s</summary>
		public const double ORDER_TYPE_DERIVED = 100;

		/// <summary>Order of nested <see cref="IMethodNode"/>s</summary>
		public const double ORDER_TYPE_METHOD = 200;

		/// <summary>Order of nested <see cref="IPropertyNode"/>s</summary>
		public const double ORDER_TYPE_PROPERTY = 300;

		/// <summary>Order of nested <see cref="IEventNode"/>s</summary>
		public const double ORDER_TYPE_EVENT = 400;

		/// <summary>Order of nested <see cref="IFieldNode"/>s</summary>
		public const double ORDER_TYPE_FIELD = 500;

		/// <summary>Order of nested <see cref="ITypeNode"/>s</summary>
		public const double ORDER_TYPE_TYPE = 600;

		/// <summary>Order of <see cref="IMethodNode"/>s</summary>
		public const double ORDER_PROPERTY_METHOD = 0;

		/// <summary>Order of <see cref="IMethodNode"/>s</summary>
		public const double ORDER_EVENT_METHOD = 0;

		/// <summary>Order of base type <see cref="IBaseTypeNode"/></summary>
		public const double ORDER_BASETYPEFOLDER_BASETYPE = 0;

		/// <summary>Order of interface <see cref="IBaseTypeNode"/>s</summary>
		public const double ORDER_BASETYPEFOLDER_INTERFACE = 100;

		/// <summary>Order of <see cref="IMessageNode"/>s</summary>
		public const double ORDER_DERIVEDTYPES_TEXT = 0;

		/// <summary>Order of interface <see cref="IDerivedTypeNode"/>s</summary>
		public const double ORDER_DERIVEDTYPES_TYPE = 100;
	}
}
