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

using dnSpy.Contracts.Files.TreeView.Resources;

namespace dnSpy.Contracts.Files.TreeView {
	/// <summary>
	/// Treeview constants
	/// </summary>
	public static class FileTVConstants {
		/// <summary>Drag and drop nodes DataFormat. It's an <see cref="int"/>[] of indexes of the nodes.</summary>
		public static readonly string DATAFORMAT_COPIED_ROOT_NODES = "610D0A0F-ACDB-4B7B-AA03-8E08C834627D";

		/// <summary>Guid of root node</summary>
		public const string ROOT_NODE_GUID = "E0D1E8A9-4470-4CB8-8DD7-11708EA6ED44";

		/// <summary><see cref="IMessageNode"/></summary>
		public const string MESSAGE_NODE_GUID = "C6F57A88-A030-4E8F-BCBC-3F17A3EADE57";

		/// <summary><see cref="IUnknownFileNode"/></summary>
		public const string UNKNOWN_FILE_NODE_GUID = "3117F133-58FC-4BE3-ABA6-331D6C962701";

		/// <summary><see cref="IPEFileNode"/></summary>
		public const string PEFILE_NODE_GUID = "CBE3DD51-3C13-4E2D-92BB-6AAB6A64028A";

		/// <summary><see cref="IAssemblyFileNode"/></summary>
		public const string ASSEMBLY_NODE_GUID = "AB10C139-2735-4595-9E47-2EE0EE247C6D";

		/// <summary><see cref="IModuleFileNode"/></summary>
		public const string MODULE_NODE_GUID = "597B3358-A6F5-47EA-B0D2-57EDD1208333";

		/// <summary><see cref="IResourcesFolderNode"/></summary>
		public const string RESOURCES_FOLDER_NODE_GUID = "1DD75445-9DED-482F-B6EB-4FD13E4A2197";

		/// <summary><see cref="IReferencesFolderNode"/></summary>
		public const string REFERENCES_FOLDER_NODE_GUID = "D2C27572-6874-4287-BE59-2D2A28C4D80B";

		/// <summary><see cref="INamespaceNode"/></summary>
		public const string NAMESPACE_NODE_GUID = "21FE74FA-4413-4F4F-964C-63DC966D66CC";

		/// <summary><see cref="IAssemblyReferenceNode"/></summary>
		public const string ASSEMBLYREF_NODE_GUID = "13151761-85EA-4A95-9C2D-4F7A6AC3A69D";

		/// <summary><see cref="IModuleReferenceNode"/></summary>
		public const string MODULEREF_NODE_GUID = "E3883417-71E1-4E5A-AB16-A3FB874DA2D5";

		/// <summary><see cref="IBaseTypeFolderNode"/></summary>
		public const string BASETYPEFOLDER_NODE_GUID = "5D8A8AF8-6604-4031-845F-755745DFB7A7";

		/// <summary><see cref="IDerivedTypesFolderNode"/></summary>
		public const string DERIVEDTYPESFOLDER_NODE_GUID = "E40470B7-A638-4BCC-9426-8F696EC260D9";

		/// <summary><see cref="IBaseTypeNode"/></summary>
		public const string BASETYPE_NODE_GUID = "BB9DCFC7-3527-410A-A4DA-E12FDCAC351C";

		/// <summary><see cref="IDerivedTypeNode"/></summary>
		public const string DERIVEDTYPE_NODE_GUID = "497D974B-53C0-453C-A8B4-026884B2E5D1";

		/// <summary><see cref="ITypeNode"/></summary>
		public const string TYPE_NODE_GUID = "EB18E75B-3627-405F-B7A0-B2F38FCDC071";

		/// <summary><see cref="IFieldNode"/></summary>
		public const string FIELD_NODE_GUID = "B4CB8C07-A684-4AF5-8FA2-561DC3E63110";

		/// <summary><see cref="IMethodNode"/></summary>
		public const string METHOD_NODE_GUID = "8CBBC53F-74AB-46C9-B6CB-796225D5E58A";

		/// <summary><see cref="IPropertyNode"/></summary>
		public const string PROPERTY_NODE_GUID = "38247C2D-AD67-4664-8118-01D21644031E";

		/// <summary><see cref="IEventNode"/></summary>
		public const string EVENT_NODE_GUID = "CA3F5F2B-560C-43BD-A3E5-CF504E2184A0";

		/// <summary><see cref="IUnknownResourceNode"/></summary>
		public const string UNKNOWN_RESOURCE_NODE_GUID = "69EA14DC-0C68-4956-8100-956CD29C4B79";

		/// <summary><see cref="IResourceElementSetNode"/></summary>
		public const string RESOURCE_ELEMENT_SET_NODE_GUID = "1809FF98-C72F-49B7-9677-7208927E9981";

		/// <summary><see cref="IUnknownSerializedResourceElementNode"/></summary>
		public const string UNKNOWN_SERIALIZED_RESOURCE_ELEMENT_NODE_GUID = "7D98A4A3-DDA7-44F0-AD7C-A17CEBB254F8";

		/// <summary><see cref="IBuiltInResourceElementNode"/></summary>
		public const string BUILT_IN_RESOURCE_ELEMENT_NODE_GUID = "4C5C34F1-07F4-4367-91B5-F8EB06F3C224";

		/// <summary><see cref="IImageResourceNode"/></summary>
		public const string IMAGE_RESOURCE_NODE_GUID = "E98B5242-9BB4-4895-B228-225612CBB73E";

		/// <summary><see cref="IImageResourceElementNode"/></summary>
		public const string IMAGE_RESOURCE_ELEMENT_NODE_GUID = "17E968F8-3C66-4028-804A-1DDA6BC8AD60";

		/// <summary><see cref="ISerializedImageListStreamerResourceElementNode"/></summary>
		public const string SERIALIZED_IMAGE_LIST_STREAMER_RESOURCE_ELEMENT_NODE_GUID = "20DFF130-CD6B-4D8A-A629-E82ED9B15D5A";

		/// <summary><see cref="ISerializedImageResourceElementNode"/></summary>
		public const string SERIALIZED_IMAGE_RESOURCE_ELEMENT_NODE = "51AA3974-BD7A-4035-9D23-C2225776D965";

		/// <summary><c>BamlResourceElementNode</c></summary>
		public const string BAML_RESOURCE_ELEMENT_NODE_GUID = "2410E30D-D0D3-4BEA-8FA3-C2DBDDB25D56";

		/// <summary><c>PENode</c></summary>
		public const string PE_NODE_GUID = "44DCC53A-BC6D-41C4-B902-DE443A3FEA11";

		/// <summary><c>ImageCor20HeaderNode</c></summary>
		public const string IMGCOR20HEADER_NODE_GUID = "0B86A8A9-2C81-416D-B87F-4D5791471753";

		/// <summary><c>ImageDosHeaderNode</c></summary>
		public const string IMGDOSHEADER_NODE_GUID = "30741351-D485-42D7-9463-2BD9FAE4A591";

		/// <summary><c>ImageFileHeaderNode</c></summary>
		public const string IMGFILEHEADER_NODE_GUID = "EFB6A52C-FE1A-4C8B-803A-3163E952C8F7";

		/// <summary><c>ImageOptionalHeader32Node</c></summary>
		public const string IMGOPTHEADER32_NODE_GUID = "CC55C6DC-80B9-4EF7-B12F-D208FFB68782";

		/// <summary><c>ImageOptionalHeader64Node</c></summary>
		public const string IMGOPTHEADER64_NODE_GUID = "C35952E9-9886-4A71-A752-C359E3657198";

		/// <summary><c>ImageSectionHeaderNode</c></summary>
		public const string IMGSECTHEADER_NODE_GUID = "7CE7AA42-48FA-4C25-8AE8-FE07BDDFBF23";

		/// <summary><c>MetaDataTableNode</c></summary>
		public const string MDTBL_NODE_GUID = "C8477B7C-7F93-4479-B286-CBBBFE6CC102";

		/// <summary><c>MetaDataTableRecordNode</c></summary>
		public const string MDTBLREC_NODE_GUID = "ACAD28D4-699E-40F9-95D0-7ED34BA1558A";

		/// <summary><c>StorageHeaderNode</c></summary>
		public const string STRGHEADER_NODE_GUID = "1B171FEC-C3DA-4390-BE7A-FA0A98C00D20";

		/// <summary><c>StorageSignatureNode</c></summary>
		public const string STRGSIG_NODE_GUID = "5DB376D9-9092-4625-82DC-DC8986EC6F89";

		/// <summary><c>StorageStreamNode</c></summary>
		public const string STRGSTREAM_NODE_GUID = "037F16E2-0BEA-4BEE-9EDE-8E7CD1732E8E";

		/// <summary><c>TablesStreamNode</c></summary>
		public const string TBLSSTREAM_NODE_GUID = "8684B8BC-DFEB-4826-B078-A72F5CDFA4A7";

		/// <summary>Order of PE node</summary>
		public const double ORDER_MODULE_PE = 0;

		/// <summary>Order of <see cref="IReferencesFolderNode"/></summary>
		public const double ORDER_MODULE_REFERENCES_FOLDER = 100;

		/// <summary>Order of <see cref="IResourcesFolderNode"/></summary>
		public const double ORDER_MODULE_RESOURCES_FOLDER = 200;

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

		/// <summary>Order of <see cref="IResourceNode"/>s</summary>
		public const double ORDER_RESOURCE = 0;

		/// <summary>Order of <see cref="IResourceElementNode"/>s</summary>
		public const double ORDER_RESOURCE_ELEM = 0;

		/// <summary>Order of <see cref="IResourceElementSetNode"/> provider</summary>
		public const double ORDER_RSRCPROVIDER_RSRCELEMSET = 0;

		/// <summary>Order of <see cref="IImageResourceNode"/> and <see cref="IImageResourceElementNode"/> provider</summary>
		public const double ORDER_RSRCPROVIDER_IMAGE_RESOURCE_NODE = 1000;

		/// <summary>Order of <see cref="ISerializedImageResourceElementNode"/> provider</summary>
		public const double ORDER_RSRCPROVIDER_SERIALIZED_IMAGE_RESOURCE_ELEMENT_NODE = 2000;

		/// <summary>Order of <c>BamlResourceElementNode</c> provider</summary>
		public const double ORDER_RSRCPROVIDER_BAML_NODE = 3000;

		/// <summary>Order of <see cref="ISerializedImageListStreamerResourceElementNode"/> provider</summary>
		public const double ORDER_RSRCPROVIDER_SERIALIZED_IMAGE_LIST_STREAMER_RESOURCE_ELEMENT_NODE = 10000;

		/// <summary>Order of <see cref="IUnknownSerializedResourceElementNode"/> provider</summary>
		public const double ORDER_RSRCPROVIDER_UNKNOWNSERIALIZEDRSRCELEM = double.MaxValue;
	}
}
