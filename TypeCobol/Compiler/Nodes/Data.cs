
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using JetBrains.Annotations;
using TypeCobol.Compiler.Text;
using TypeCobol.Compiler.Types;

namespace TypeCobol.Compiler.Nodes {

    using System;
    using System.Collections.Generic;
    using CodeElements.Expressions;
    using Scanner;
    using TypeCobol.Compiler.CodeElements;



    public class DataDivision: Node, CodeElementHolder<DataDivisionHeader>, Parent<DataSection> {

        public const string NODE_ID = "data-division";
	    public DataDivision(DataDivisionHeader header): base(header) { }
	    public override string ID { get { return NODE_ID; } }

	    public override void Add(Node child, int index = -1) {
		    if (index <= 0) index = WhereShouldIAdd(child.GetType());
		    base.Add(child,index);
	    }
        private int WhereShouldIAdd(System.Type section) {
            if (Tools.Reflection.IsTypeOf(section, typeof(FileSection))) return 0;
            int ifile = -2;
            int iworking = -2;
            int ilocal = -2;
            int ilinkage = -2;
            int c = 0;
            foreach(var child in this.Children()) {
                if (Tools.Reflection.IsTypeOf(child.GetType(), typeof(FileSection))) ifile = c;
                else
                if (Tools.Reflection.IsTypeOf(child.GetType(), typeof(WorkingStorageSection))) iworking = c;
                else
                if (Tools.Reflection.IsTypeOf(child.GetType(), typeof(LocalStorageSection))) ilocal = c;
                else
                if (Tools.Reflection.IsTypeOf(child.GetType(), typeof(LinkageSection))) ilinkage = c;
                c++;
            }
            if (Tools.Reflection.IsTypeOf(section, typeof(WorkingStorageSection))) return Math.Max(0,ifile+1);
            if (Tools.Reflection.IsTypeOf(section, typeof(LocalStorageSection))) return Math.Max(0,Math.Max(ifile+1,iworking+1));
            if (Tools.Reflection.IsTypeOf(section, typeof(LinkageSection))) return Math.Max(0,Math.Max(ifile+1,Math.Max(iworking+1,ilocal+1)));
            return 0;
        }

        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

        public abstract class DataSection: Node, CodeElementHolder<DataSectionHeader>, Child<DataDivision>{
	    protected DataSection(DataSectionHeader header): base(header) { }
	    public virtual bool IsShared { get { return false; } }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }
    public class FileSection: DataSection, CodeElementHolder<FileSectionHeader>{
	    public FileSection(FileSectionHeader header): base(header) { }
	    public override string ID { get { return "file"; } }
	    public override bool IsShared { get { return true; } }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return base.VisitNode(astVisitor) && astVisitor.Visit(this);
        }
    }

    public class FileDescriptionEntryNode : Node, CodeElementHolder<FileDescriptionEntry> {
	    public FileDescriptionEntryNode(FileDescriptionEntry entry): base(entry) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
   } 

    public class GlobalStorageSection : DataSection, CodeElementHolder<GlobalStorageSectionHeader>, Parent<DataDefinition>
    {
        public GlobalStorageSection(GlobalStorageSectionHeader header) : base(header) { }
        public override string ID { get { return "global-storage"; } }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return base.VisitNode(astVisitor) && astVisitor.Visit(this);
        }
    }


    public class WorkingStorageSection: DataSection, CodeElementHolder<WorkingStorageSectionHeader>, Parent<DataDefinition>
    {
        public WorkingStorageSection(WorkingStorageSectionHeader header) : base(header) { }

        public override IEnumerable<ITextLine> Lines
        {
            get
            {
                List<ITextLine> lines = new List<ITextLine>();
                lines.Add(new TextLineSnapshot(-1, CodeElement.SourceText, null));

                if (IsFlagSet(Flag.InsideProcedure))
                {
                    var declare = Parent?.Parent as FunctionDeclaration;
                    if (declare != null)
                    {
                        lines.Add(new TextLineSnapshot(-1,
                            string.Format("*{0}.{1} {2}", declare.Root.MainProgram.Name, declare.Name,
                                declare.Profile.Parameters.Count != 0 ? "- Params :" : " - No Params"), null));
                        lines.AddRange(declare.Profile.GetSignatureForComment());
                    }
                }

                return lines;
            }
        }
        public override string ID { get { return "working-storage"; } }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return base.VisitNode(astVisitor) && astVisitor.Visit(this);
        }
    }
    public class LocalStorageSection: DataSection, CodeElementHolder<LocalStorageSectionHeader>, Parent<DataDefinition>
        {
	    public LocalStorageSection(LocalStorageSectionHeader header): base(header) { }

        public override IEnumerable<ITextLine> Lines
        {
            get
            {
                List<ITextLine> lines = new List<ITextLine>();
                lines.Add(new TextLineSnapshot(-1, CodeElement.SourceText, null));

                if (IsFlagSet(Flag.InsideProcedure))
                {
                    var declare = Parent?.Parent as FunctionDeclaration;
                    if (declare != null)
                    {
                        lines.Add(new TextLineSnapshot(-1,
                            string.Format("*{0}.{1} {2}", declare.Root.MainProgram.Name, declare.Name,
                                declare.Profile.Parameters.Count != 0 ? "- Params :" : " - No Params"), null));
                        lines.AddRange(declare.Profile.GetSignatureForComment());
                    }
                }

                return lines;
            }
        }
        public override string ID { get { return "local-storage"; } }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return base.VisitNode(astVisitor) && astVisitor.Visit(this);
        }
    }
    public class LinkageSection: DataSection, CodeElementHolder<LinkageSectionHeader>, Parent<DataDefinition>
    {
	    public LinkageSection(LinkageSectionHeader header): base(header) { }

        public override IEnumerable<ITextLine> Lines
        {
            get
            {
                List<ITextLine> lines = new List<ITextLine>();
                lines.Add(new TextLineSnapshot(-1, CodeElement.SourceText, null));

                if (IsFlagSet(Flag.InsideProcedure))
                {
                    var declare = Parent?.Parent as FunctionDeclaration;
                    if (declare != null)
                    {
                        lines.Add(new TextLineSnapshot(-1,
                            string.Format("*{0}.{1} {2}", declare.Root.MainProgram.Name, declare.Name,
                                declare.Profile.Parameters.Count != 0 ? "- Params :" : " - No Params"), null));
                        lines.AddRange(declare.Profile.GetSignatureForComment());
                    }
                }

                return lines;
            }
        }
        public override string ID { get { return "linkage"; } }
	    public override bool IsShared { get { return true; } }

        public override bool VisitNode(IASTVisitor astVisitor) {
            return base.VisitNode(astVisitor) && astVisitor.Visit(this);
        }
    }

    /// <summary>
    /// DataDefinition
    ///   DataDescription
    ///      ParameterDescription
    ///      TypedDataNode
    ///   DataCondition
    ///   DataRedefines
    ///   DataRenames
    ///   TypeDefinition
    ///   IndexDefinition
    ///   GeneratedDefinition
    /// </summary>
    public abstract class DataDefinition: Node, Parent<DataDefinition>, ITypedNode {

        private CommonDataDescriptionAndDataRedefines _ComonDataDesc { get { return this.CodeElement as CommonDataDescriptionAndDataRedefines; } }

        protected DataDefinition(DataDefinitionEntry entry) : base(entry) { }
        public override string ID { get { return "data-definition"; } }
        private string _name;

        public override string Name
        {
            get
            {
                if (_name != null) return _name;
                _name = ((DataDefinitionEntry) this.CodeElement).Name;
                return _name;
            }
        }

        private Dictionary<StorageArea, Node> _References;

        public void AddReferences(StorageArea storageArea, Node node)
        {
            if(_References == null)
                _References = new Dictionary<StorageArea, Node>();

            if (!_References.ContainsKey(storageArea))
                _References.Add(storageArea, node);
        }

        public Dictionary<StorageArea, Node> GetReferences()
        {
            return _References ?? (_References = new Dictionary<StorageArea, Node>());
        }

        private TypeDefinition _typeDefinition;

        /// <summary>
        /// Get the TypeDefinition node associated to this Node
        /// </summary>
        public TypeDefinition TypeDefinition
        {
            get { return _typeDefinition; }
            set
            {
                if (_typeDefinition == null)
                    _typeDefinition = value;
            }
        }


        public override bool VisitNode(IASTVisitor astVisitor) {
            return astVisitor.Visit(this);
        }

        private DataType _dataType;
        public virtual DataType DataType
        {
            get
            {
                if (_dataType != null) return _dataType;

                _dataType = this.CodeElement != null ? ((DataDefinitionEntry) this.CodeElement).DataType : DataType.Unknown;
                return _dataType;
            }
        }

        private DataType _primitiveDataType;
        public virtual DataType PrimitiveDataType
        {
            get
            {
                if (_primitiveDataType != null) return _primitiveDataType;
                if (this.Picture != null) //Get DataType based on Picture clause
                    _primitiveDataType = DataType.Create(this.Picture.Value);
                else if (this.Usage.HasValue) //Get DataType based on Usage clause
                    _primitiveDataType = DataType.Create(this.Usage.Value);
                else
                    return null;

                return _primitiveDataType;
            }
        }

        private PictureValidator _pictureValidator;
        public PictureValidator PictureValidator
        {
            get
            {
                if (_pictureValidator != null) return _pictureValidator;

                _pictureValidator = new PictureValidator(Picture.Value, SignIsSeparate);

                return _pictureValidator;
            }
        }

        /// <summary>
        /// PhysicalLength is the size taken by a DataDefinition and its children in memory
        /// </summary>
        private long _physicalLength = 0;
        public virtual long PhysicalLength
        {
            get
            {
                if (_physicalLength > 0)
                {
                    return _physicalLength;
                }

                if (children != null)
                {
                    if(Picture != null || (Usage != null && Usage != DataUsage.None && Children.Count == 0))
                    {
                        _physicalLength = GetPhysicalLength();
                    }
                    else
                    {
                        foreach (var node in children)
                        {
                            var dataDefinition = (DataDefinition)node;
                            if (dataDefinition != null)
                            {
                                _physicalLength += dataDefinition.PhysicalLength;
                                _physicalLength += dataDefinition.SlackBytes;
                            }

                            if (dataDefinition is DataRedefines)
                            {
                                SymbolReference redefined = ((DataRedefinesEntry)dataDefinition.CodeElement)?.RedefinesDataName;
                                var result = SymbolTable.GetRedefinedVariable((DataRedefines) dataDefinition, redefined);

                                _physicalLength -= result.PhysicalLength > dataDefinition.PhysicalLength ? dataDefinition.PhysicalLength : result.PhysicalLength;
                            }
                        }
                    }
                    
                }

                if (MaxOccurencesCount > 1)
                {
                    _physicalLength = _physicalLength * MaxOccurencesCount;
                }

                return _physicalLength > 0 ? _physicalLength : 1;
            }
        }

        /// <summary>
        /// Gets the actual size of one DataDefinition
        /// </summary>
        /// <returns></returns>
        private long GetPhysicalLength()
        {
            TypeCobolType.UsageFormat usage = TypeCobolType.UsageFormat.None;
            if (Usage != null)
            {
                switch (Usage.Value)
                {
                    case DataUsage.Binary:
                    case DataUsage.NativeBinary:
                        usage = TypeCobolType.UsageFormat.Binary;
                        break;
                    case DataUsage.FloatingPoint:
                        usage = TypeCobolType.UsageFormat.Comp1;
                        break;
                    case DataUsage.Display:
                        usage = TypeCobolType.UsageFormat.Display;
                        break;
                    case DataUsage.FunctionPointer:
                        usage = TypeCobolType.UsageFormat.FunctionPointer;
                        break;
                    case DataUsage.Index:
                        usage = TypeCobolType.UsageFormat.Index;
                        break;
                    case DataUsage.National:
                        usage = TypeCobolType.UsageFormat.National;
                        break;
                    case DataUsage.None:
                        usage = TypeCobolType.UsageFormat.None;
                        break;
                    case DataUsage.ObjectReference:
                        usage = TypeCobolType.UsageFormat.ObjectReference;
                        break;
                    case DataUsage.PackedDecimal:
                        usage = TypeCobolType.UsageFormat.PackedDecimal;
                        break;
                    case DataUsage.Pointer:
                        usage = TypeCobolType.UsageFormat.Pointer;
                        break;
                    case DataUsage.ProcedurePointer:
                        usage = TypeCobolType.UsageFormat.ProcedurePointer;
                        break;
                    case DataUsage.LongFloatingPoint:
                        usage = TypeCobolType.UsageFormat.Comp2;
                        break;
                    case DataUsage.DBCS:
                        usage = TypeCobolType.UsageFormat.Display1;
                        break;
                }
            }
            
            if (Picture == null)
            {
                if (Usage != null && Usage.Value != DataUsage.None)
                {
                    return new TypeCobolType(TypeCobolType.Tags.Usage, usage).Length;
                }
                return 1;
            }
            if (PictureValidator.IsValid())
            {
                PictureType type = new PictureType(PictureValidator);
                type.Usage = usage;
                return type.Length;
            }
            else
                return 1;
        }

        /// <summary>
        /// A SlackByte is a unit that is used to synchronize DataDefinitions in memory.
        /// One or more will be present only if the keyword SYNC is placed on a DataDefinition
        /// To calculated the number of slackbytes present :
        /// - Calculate the size of all previous DataDefinitions to the current one
        /// - The number of SlackBytes inserted will be determined by the formula m - (occupiedMemory % m) where m is determined by the usage of the DataDefinition
        /// - Whether it will be put before or after the size of the DataDefinition is determined by the keyword LEFT or RIGHT after the keyword SYNC (not implemented yet)
        /// </summary>
        private long? _slackBytes = null;
        public long SlackBytes
        {
            get
            {
                if (_slackBytes != null)
                {
                    return _slackBytes.Value;
                }

                int index = Parent.ChildIndex(this);
                long occupiedMemory = 0;
                DataDefinition parent = Parent as DataDefinition;
                _slackBytes = 0;

                if (IsSynchronized && Usage != null && Usage != DataUsage.None && parent != null)
                {
                    while (parent != null && parent.Type != CodeElementType.SectionHeader)
                    {

                        for (int i = 0; i < index; i++)
                        {
                            occupiedMemory += ((DataDefinition)parent.Children[i]).PhysicalLength;
                            occupiedMemory += ((DataDefinition)parent.Children[i]).SlackBytes;
                        }

                        index = parent.Parent.ChildIndex(parent);
                        parent = parent.Parent as DataDefinition;
                    }

                    
                    int m = 1;

                    switch (Usage.Value)
                    {
                        case DataUsage.Binary:
                            if (PictureValidator.ValidationContext.Digits <= 4)
                            {
                                m = 2;
                            }
                            else
                            {
                                m = 4;
                            }
                            break;
                        case DataUsage.Index:
                        case DataUsage.Pointer:
                        case DataUsage.ProcedurePointer:
                        case DataUsage.ObjectReference:
                        case DataUsage.FunctionPointer:
                        case DataUsage.PackedDecimal:
                            m = 4; 
                            break;
                        case DataUsage.FloatingPoint:
                            m = 8;
                            break;
                    }

                    if (occupiedMemory % m > 0)
                    {
                        _slackBytes = m - (occupiedMemory % m);
                    }


                }

                return _slackBytes.Value;

            }
        }

        private long? _startPosition = null;
        public virtual long StartPosition
        {
            get
            {
                if (_startPosition.HasValue)
                {
                    return _startPosition.Value;
                }

                var node = this as DataRedefines;
                if (node != null)
                {
                    SymbolReference redefined = ((DataRedefinesEntry)CodeElement).RedefinesDataName;
                    var result = SymbolTable.GetRedefinedVariable(node, redefined);

                        _startPosition = result.StartPosition;
                        return _startPosition.Value;
                    
                }

                if (Parent is DataSection)
                {
                    _startPosition = 1;
                }
                else
                {
                    for (int i = 0; i < Parent.Children.Count; i++)
                    {
                        Node sibling = Parent.Children[i];
                        
                        if (i == Parent.ChildIndex(this) - 1)
                        {
                            while(sibling is DataRedefines)
                                sibling = Parent.Children[i - 1];

                            //Add 1 for the next free Byte in memory
                            _startPosition = ((DataDefinition)sibling).PhysicalPosition + 1 + SlackBytes;
                        }
                            
                    }
                    if (_startPosition == null)
                    {
                        _startPosition = (Parent as DataDefinition)?.StartPosition;
                    }
                }

                return _startPosition ?? 0;
            }
        }

        /// PhysicalPosition is the position of the last Byte used by a DataDefinition in memory
        /// Minus 1 is due to PhysicalLength, which is calculated from 0. 
        public virtual long PhysicalPosition => StartPosition + PhysicalLength - 1 + SlackBytes;

        /// <summary>If this node a subordinate of a TYPEDEF entry?</summary>
        public virtual bool IsPartOfATypeDef { get { return _ParentTypeDefinition != null; } }
    
        private TypeDefinition _ParentTypeDefinition;
        /// <summary>
        /// Return Parent TypeDefinition if this node is under it
        /// Otherwise return null
        /// </summary>
        [CanBeNull]
        public TypeDefinition ParentTypeDefinition {
            get { return _ParentTypeDefinition; }
            set
            {
                if (_ParentTypeDefinition == null)
                    _ParentTypeDefinition = value;
            }
        }

        public TypeDefinition GetParentTypeDefinitionWithPath([NotNull] List<string> qualifiedPath)
        {
            Node currentNode = this;
            while (currentNode != null)
            {
                var typedNode = currentNode as TypeDefinition;
                if (typedNode != null) return typedNode;
                else
                    qualifiedPath.Add(currentNode.Name); //Store the path and ignore Type Name

                //Stop if we reach a Parent which is not a DataDefinion (Working storage section for example)
                if (!(currentNode is DataDefinition)) return null;

                currentNode = currentNode.Parent;
                
            }
            return null;
        }

        public bool IsStronglyTyped
        {
            get
            {
                if (DataType.RestrictionLevel == RestrictionLevel.STRONG) return true;
                var parent = Parent as DataDefinition;
                return parent != null && parent.IsStronglyTyped;
            }
        }

        public bool IsStrictlyTyped
        {
            get
            {
                if (DataType.RestrictionLevel == RestrictionLevel.STRICT) return true;
                var parent = Parent as DataDefinition;
                return parent != null && parent.IsStrictlyTyped;
            }
        }

        public bool IsIndex { get; internal set; }
        public string Hash
        {
            get
            {
                var hash = new StringBuilder();
                hash.Append(Name);
                return Tools.Hash.CreateCOBOLNameHash(hash.ToString(), 8);
            }
        }

        #region TypeProperties
        public AlphanumericValue Picture { get {return _ComonDataDesc != null ? _ComonDataDesc.Picture : null;}}
        public bool IsJustified { get {  if(_ComonDataDesc != null && _ComonDataDesc.IsJustified != null) return _ComonDataDesc.IsJustified.Value; else return false; } }
        public DataUsage? Usage { get { if (_ComonDataDesc != null && _ComonDataDesc.Usage != null) return _ComonDataDesc.Usage.Value; else return null; } }
        public bool IsGroupUsageNational { get { if (_ComonDataDesc != null && _ComonDataDesc.IsGroupUsageNational != null) return _ComonDataDesc.IsGroupUsageNational.Value; else return false; } }
        public long MinOccurencesCount { get { if (_ComonDataDesc != null && _ComonDataDesc.MinOccurencesCount != null) return _ComonDataDesc.MinOccurencesCount.Value; else return 1; } }
        public long MaxOccurencesCount { get { return _ComonDataDesc != null && _ComonDataDesc.MaxOccurencesCount != null ? _ComonDataDesc.MaxOccurencesCount.Value : 1; } }


        public NumericVariable OccursDependingOn { get { return _ComonDataDesc != null ? _ComonDataDesc.OccursDependingOn : null; } }
        public bool HasUnboundedNumberOfOccurences { get { if (_ComonDataDesc != null && _ComonDataDesc.HasUnboundedNumberOfOccurences != null) return _ComonDataDesc.HasUnboundedNumberOfOccurences.Value; else return false; } }
        public bool IsTableOccurence { get { if (_ComonDataDesc != null) return _ComonDataDesc.IsTableOccurence; else return false; } }
        public CodeElementType? Type { get { if (_ComonDataDesc != null) return _ComonDataDesc.Type; else return null; } }
        public bool SignIsSeparate { get { if (_ComonDataDesc != null && _ComonDataDesc.SignIsSeparate != null) return _ComonDataDesc.SignIsSeparate.Value; else return false; } }
        public SignPosition? SignPosition { get { if (_ComonDataDesc != null && _ComonDataDesc.SignPosition != null) return _ComonDataDesc.SignPosition.Value; else return null; } }

        public bool IsSynchronized
        {
            get
            {
                if (_ComonDataDesc != null && _ComonDataDesc.IsSynchronized != null)
                    return _ComonDataDesc.IsSynchronized.Value;

                else if (Parent is DataDefinition)
                    return ((DataDefinition)Parent).IsSynchronized;

                else return false;
            }
        }
        public SymbolReference ObjectReferenceClass { get { if (_ComonDataDesc != null) return _ComonDataDesc.ObjectReferenceClass; else return null; } }
        #endregion
    }

    public class DataDescription: DataDefinition, CodeElementHolder<DataDescriptionEntry>, Parent<DataDescription>{
        public DataDescription(DataDescriptionEntry entry): base(entry) { }

        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return base.VisitNode(astVisitor) && astVisitor.Visit(this);
        }
        /// <summary>
        /// A Dictonary that gives for a Token that appears in a qualified name its subtitution.
        /// </summary>
        public Dictionary<Token, string> QualifiedTokenSubsitutionMap;

        
    }
    public class DataCondition: DataDefinition, CodeElementHolder<DataConditionEntry> 
    {
        public DataCondition(DataConditionEntry entry): base(entry) { }

        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return base.VisitNode(astVisitor) && astVisitor.Visit(this);
        }
    }
    public class DataRedefines: DataDefinition, CodeElementHolder<DataRedefinesEntry> {
        public DataRedefines(DataRedefinesEntry entry): base(entry) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return base.VisitNode(astVisitor) && astVisitor.Visit(this);
        }
    }
    public class DataRenames: DataDefinition, CodeElementHolder<DataRenamesEntry> {
        public DataRenames(DataRenamesEntry entry): base(entry) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return base.VisitNode(astVisitor) && astVisitor.Visit(this);
        }
    }
    // [COBOL 2002]
    public class TypeDefinition: DataDefinition, CodeElementHolder<DataTypeDescriptionEntry>, Parent<DataDescription>, IDocumentable
    {
        public TypeDefinition(DataTypeDescriptionEntry entry) : base(entry) { }
        public RestrictionLevel RestrictionLevel { get { return this.CodeElement().RestrictionLevel; } }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return base.VisitNode(astVisitor) && astVisitor.Visit(this);
        }


        public override bool IsPartOfATypeDef => true;

        public override bool Equals(object obj)
        {
            if ((obj as TypeDefinition) != null)
            {
                var compareTypeDef = (TypeDefinition) obj;
                return compareTypeDef.DataType == this.DataType &&
                       //compareTypeDef.PrimitiveDataType == this.PrimitiveDataType &&
                       compareTypeDef.QualifiedName.ToString() == this.QualifiedName.ToString();
            }

            var generatedDataType = (obj as GeneratedDefinition);
            if (generatedDataType  != null && 
                !(generatedDataType.DataType == DataType.Alphabetic ||
                  generatedDataType .DataType == DataType.Alphanumeric)) //Remove these two check on Alpha.. to allow move "fezf" TO alphatypedVar
            {
                if (this.PrimitiveDataType != null)
                    return this.PrimitiveDataType == generatedDataType.DataType;
                else
                    return this.DataType == generatedDataType.DataType;
            }
            return false;
        }
    }
    // [/COBOL 2002]

    // [TYPECOBOL]
    public class ParameterDescription: TypeCobol.Compiler.Nodes.DataDescription, CodeElementHolder<ParameterDescriptionEntry>, Parent<ParametersProfileNode> {

        private readonly ParameterDescriptionEntry _CodeElement;

        public ParameterDescription(ParameterDescriptionEntry entry): base(entry) { _CodeElement = (ParameterDescriptionEntry)this.CodeElement; }
       
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return base.VisitNode(astVisitor) && astVisitor.Visit(this);
        }

        public new DataType DataType {
            get
            {
                return _CodeElement.DataType;
            }
        }

        public PassingTypes PassingType { get; set; }
        public IntegerValue LevelNumber { get { return _CodeElement.LevelNumber; } }
        public SymbolDefinition DataName { get { return _CodeElement.DataName; } }

        public bool IsOmittable { get { return _CodeElement.IsOmittable; } }

        public enum PassingTypes
        {
            Input,
            Output,
            InOut
        }
    }
    // [/TYPECOBOL]

    public class IndexDefinition : DataDefinition
    {
        public IndexDefinition(SymbolDefinition symbolDefinition) : base(null)
        {
            _SymbolDefinition = symbolDefinition;
            IsIndex = true;
        }

        private SymbolDefinition _SymbolDefinition;

        public override string Name
        {
            get { return _SymbolDefinition.Name; }
        }

        public override DataType DataType
        {
            get { return DataType.Numeric; }
        }

        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    /// <summary>
    /// Allow to generate DataDefinition which can take any desired form/type. 
    /// Give access to GeneratedDefinition of Numeric/Alphanumeric/Boolean/... DataType
    /// </summary>
    public class GeneratedDefinition : DataDefinition
    {
        private string _Name;
        private DataType _DataType;

        public static GeneratedDefinition NumericGeneratedDefinition =       new GeneratedDefinition("Numeric", DataType.Numeric);
        public static GeneratedDefinition AlphanumericGeneratedDefinition =  new GeneratedDefinition("Alphanumeric", DataType.Alphanumeric);
        public static GeneratedDefinition AlphabeticGeneratedDefinition =    new GeneratedDefinition("Alphabetic", DataType.Alphabetic);
        public static GeneratedDefinition BooleanGeneratedDefinition =       new GeneratedDefinition("Boolean", DataType.Boolean);
        public static GeneratedDefinition DBCSGeneratedDefinition =          new GeneratedDefinition("DBCS", DataType.DBCS);
        public static GeneratedDefinition DateGeneratedDefinition =          new GeneratedDefinition("Date", DataType.Date);
        public static GeneratedDefinition CurrencyGeneratedDefinition =      new GeneratedDefinition("Currency", DataType.Currency);
        public static GeneratedDefinition FloatingPointGeneratedDefinition = new GeneratedDefinition("FloatingPoint", DataType.FloatingPoint);
        public static GeneratedDefinition OccursGeneratedDefinition =        new GeneratedDefinition("Occurs", DataType.Occurs);
        public static GeneratedDefinition StringGeneratedDefinition =        new GeneratedDefinition("String", DataType.String);
        public GeneratedDefinition(string name, DataType dataType) : base(null)
        {
            _Name = name;
            _DataType = dataType;
        }

        public override string Name
        {
            get { return _Name; }
        }

        public override DataType DataType
        {
            get { return _DataType; }
        }


        public override bool Equals(object obj)
        {
            //In this case we can only compare the DataType
            if((obj as DataDefinition) != null)
                return ((DataDefinition) obj).DataType == _DataType;
            return false;
        }
    }

} // end of namespace TypeCobol.Compiler.Nodes
