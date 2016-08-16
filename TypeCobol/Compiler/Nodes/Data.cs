﻿namespace TypeCobol.Compiler.Nodes {

	using System.Collections.Generic;
	using TypeCobol.Compiler.CodeElements;




public class DataDivision: Node<DataDivisionHeader> {
	public DataDivision(DataDivisionHeader header): base(header) { }
	public override string ID { get { return "data-division"; } }
}

public abstract class DataSection: Node<DataSectionHeader>, Child<DataDivision>, Parent<DataDefinition> {
	public DataSection(DataSectionHeader header): base(header) { }
	public virtual bool IsShared { get { return false; } }
}
public abstract class GenericDataSection<T>: DataSection where T:DataSectionHeader {
	public GenericDataSection(T header): base(header) { }
}
public class FileSection: GenericDataSection<FileSectionHeader> {
	public FileSection(FileSectionHeader header): base(header) { }
	public override string ID { get { return "file"; } }
	public override bool IsShared { get { return true; } }
}
public class WorkingStorageSection: GenericDataSection<WorkingStorageSectionHeader> {
	public WorkingStorageSection(WorkingStorageSectionHeader header): base(header) { }
	public override string ID { get { return "working-storage"; } }
}
public class LocalStorageSection: GenericDataSection<LocalStorageSectionHeader> {
	public LocalStorageSection(LocalStorageSectionHeader header): base(header) { }
	public override string ID { get { return "local-storage"; } }
}
public class LinkageSection: GenericDataSection<LinkageSectionHeader> {
	public LinkageSection(LinkageSectionHeader header): base(header) { }
	public override string ID { get { return "linkage"; } }
	public override bool IsShared { get { return true; } }
}

public abstract class DataDefinition: Node<DataDefinitionEntry>, Child<DataSection> {
	public DataDefinition(DataDefinitionEntry entry): base(entry) { }
	public override string ID { get { return this.CodeElement.Name; } }
	public string Name { get { return CodeElement.Name; } }
}
public abstract class GenericDataDefinition<T>: DataDefinition where T:DataDefinitionEntry {
	public GenericDataDefinition(T entry): base(entry) { }
}
public class DataDescription: GenericDataDefinition<DataDescriptionEntry>, Parent<DataDescription> {
	public DataDescription(DataDescriptionEntry entry): base(entry) { }
}
public class DataCondition: GenericDataDefinition<DataConditionEntry> {
	public DataCondition(DataConditionEntry entry): base(entry) { }
}
public class DataRedefines: GenericDataDefinition<DataRedefinesEntry> {
	public DataRedefines(DataRedefinesEntry entry): base(entry) { }
}
public class DataRenames: GenericDataDefinition<DataRenamesEntry> {
	public DataRenames(DataRenamesEntry entry): base(entry) { }
}
// [COBOL 2002]
public class TypeDefinition: GenericDataDefinition<TypeDefinitionEntry>, Parent<DataDescription> {
	public TypeDefinition(TypeDefinitionEntry entry): base(entry) { }
	public bool IsStrong { get; internal set; }
}
// [/COBOL 2002]



} // end of namespace TypeCobol.Compiler.Nodes