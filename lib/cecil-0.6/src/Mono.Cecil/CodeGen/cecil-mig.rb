#!/usr/bin/env ruby

#
# usage cecil-mig.rb directory
#

dir = ARGV.length > 0 ? ARGV[0] : "."

$replaces = {
	"GenericParamAttributes" => "GenericParameterAttributes",
	"ParamAttributes" => "ParameterAttributes",
	"IParameterReference" => "ParameterDefinition",
	"IPropertyReference" => "PropertyDefinition",
	"IEventReference" => "EventDefinition",
	"IVariableReference" => "VariableDefinition",
	"IMarshalDesc" => "MarshalSpec",
	"MarshalDesc" => "MarshalSpec",
	"IArrayMarshalDesc" => "ArrayMarshalSpec",
	"ArrayMarshalDesc" => "ArrayMarshalSpec",
	"ICustomMarshalerDesc" => "CustomMarshalerSpec",
	"CustomMarshalerDesc" => "CustomMarshalerSpec",
	"ISafeArrayDesc" => "SafeArraySpec",
	"SafeArrayDesc" => "SafeArraySpec",
	"IFixedArrayDesc" => "FixedArraySpec",
	"FixedArrayDesc" => "FixedArraySpec",
	"IFixedSysStringDesc" => "FixedSysStringSpec",
	"FixedSysStringDesc" => "FixedSysStringSpec",
	"IModifierType" => "ModType"
}

$collections = [
	"AssemblyNameReferenceCollection",
	"ModuleReferenceCollection",
	"ModuleDefinitionCollection",
	"ResourceCollection",
	"TypeDefinitionCollection",
	"TypeReferenceCollection",
	"InterfaceCollection",
	"ParameterDefinitionCollection",
	"OverrideCollection",
	"MethodDefinitionCollection",
	"ConstructorCollection",
	"EventDefinitionCollection",
	"FieldDefinitionCollection",
	"PropertyDefinitionCollection",
	"InstructionCollection",
	"ExceptionHandlerCollection",
	"VariableDefinitionCollection",
	"ArrayDimensionCollection",
	"CustomAttributeCollection",
	"ExternTypeCollection",
	"NestedTypeCollection",
	"SecurityDeclarationCollection",
	"MemberReferenceCollection",
	"GenericParameterCollection",
	"GenericArgumentCollection",
	"ConstraintCollection"
]

$types = [
	"AssemblyDefinition",
	"ArrayDimension",
	"ArrayType",
	"AssemblyLinkedResource",
	"AssemblyNameReference",
	"AssemblyNameDefinition",
	"CallSite",
	"CustomAttribute",
	"EmbeddedResource",
	"EventDefinition",
	"EventReference",
	"FieldDefinition",
	"FieldReference",
	"FunctionPointerType",
	"GenericInstanceMethod",
	"GenericInstanceType",
	"GenericParameter",
	"LinkedResource",
	"MethodDefinition",
	"MethodReference",
	"MethodReturnType",
	"ModifierOptional",
	"ModifierRequired",
	"ModuleDefinition",
	"ModuleReference",
	"ParameterDefinition",
	"ParameterReference",
	"PinnedType",
	"PInvokeInfo",
	"PropertyDefinition",
	"PropertyReference",
	"ReferenceType",
	"Resource",
	"SecurityDeclaration",
	"TypeDefinition",
	"TypeReference",
	"TypeSpecification",

	"Instruction",
	"ExceptionHandler",
	"MethodBody",
	"VariableDefinition",
	"VariableReference"
]

def iface(name)
	return "I" + name
end

def bang(buffer, re, str)
	nl = "([\\W])"
	buffer.gsub!(Regexp.new("#{nl}(#{re})#{nl}"), "\\1" + str + "\\3")
end

def process_replaces(buffer)
	$replaces.each_key { |key|
		bang(buffer, key, $replaces[key])
	}
end

def process_collections(buffer)
	$collections.each { |name|
		bang(buffer, iface(name), name)
	}
end

def process_types(buffer)
	$types.each { |name|
		bang(buffer, iface(name), name)
	}
end

def process_unbreak(buffer)
	$unbreak.each { |name|
		bang(buffer, name, iface(name))
	}
end

def process(file)
	buffer = ""
	original = ""
	File.open(file, File::RDONLY) { |f|
		original = f.read()
		buffer = original.clone
		process_replaces(buffer)
		process_collections(buffer)
		process_types(buffer)
	}

	File.open(file, File::WRONLY | File::TRUNC) { |f|
		f.write(buffer)
		puts("#{file} processed")
	} if (original != buffer)

end

Dir[File.join(dir, "**", "*.*")].each { |file|
	process(file) if not File.directory?(file)
}
