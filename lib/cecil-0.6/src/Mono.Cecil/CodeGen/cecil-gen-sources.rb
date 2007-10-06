#!/usr/bin/env ruby

f = File.new("../Mono.Cecil.dll.sources", File::CREAT | File::WRONLY | File::TRUNC)

[ "Mono.Cecil", "Mono.Cecil.Binary",
	"Mono.Cecil.Metadata", "Mono.Cecil.Cil",
	"Mono.Cecil.Signatures", "Mono.Xml" ].each { |dir|

	Dir.foreach("../" + dir) { |file|
		f.print("./#{dir}/#{file}\n") if file[(file.length - 3)..file.length] == ".cs"
	}
}

f.close
