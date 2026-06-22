namespace SharpMemory.Core.Common.Models;

public enum SegmentKind
{
    // C# type declarations
    Class,
    Struct,
    Interface,
    Record,
    Enum,

    // C# members
    Method,
    Constructor,
    Property,
    Field,

    // Repository structure
    File,
    Project,
    Solution,
    Directory,
}
