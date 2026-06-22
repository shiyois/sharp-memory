using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SharpMemory.Core.Business.Segments.Extensions;
using SharpMemory.Core.Business.Segments.Models;
using SharpMemory.Core.Common.Models;

namespace SharpMemory.Core.Business.Segments.Extraction;

public sealed class CSharpSegmentExtractor : ISegmentExtractor
{
    public bool CanExtract(string extension) =>
        extension.Equals(".cs", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<MemorySegment> Extract(ScannedFile file)
    {
        SyntaxTree tree;
        using (var stream = File.OpenRead(file.FullPath))
        {
            var sourceText = SourceText.From(stream, Encoding.UTF8);
            tree = CSharpSyntaxTree.ParseText(sourceText, path: file.FullPath);
        }

        var root = tree.GetRoot();

        foreach (var node in root.DescendantNodes())
        {
            MemorySegment? segment = node switch
            {
                TypeDeclarationSyntax type
                    when type is ClassDeclarationSyntax
                        or InterfaceDeclarationSyntax
                        or RecordDeclarationSyntax
                        or StructDeclarationSyntax => MapType(type, tree, file),
                EnumDeclarationSyntax enm => MapEnum(enm, tree, file),
                MethodDeclarationSyntax method => MapMethod(method, tree, file),
                ConstructorDeclarationSyntax ctor => MapConstructor(ctor, tree, file),
                PropertyDeclarationSyntax prop => MapProperty(prop, tree, file),
                FieldDeclarationSyntax field => null,
                _ => null,
            };

            if (segment is not null)
            {
                yield return segment;
            }

            if (node is FieldDeclarationSyntax fieldNode)
            {
                foreach (var fieldSegment in MapFields(fieldNode, tree, file))
                {
                    yield return fieldSegment;
                }
            }
        }
    }

    private static MemorySegment MapType(TypeDeclarationSyntax type, SyntaxTree tree, ScannedFile file)
    {
        var kind = type switch
        {
            ClassDeclarationSyntax => SegmentKind.Class,
            InterfaceDeclarationSyntax => SegmentKind.Interface,
            RecordDeclarationSyntax => SegmentKind.Record,
            _ => SegmentKind.Struct,
        };

        var name = type.Identifier.Text;
        var containerName = GetContainerName(type);
        var fqn = BuildFqn(type, name);
        var isPartial = type.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        var stableKey = BuildStableKey(file, kind, fqn, signature: null, isPartial ? file.RelativePath : null);

        var (startLine, endLine) = GetLines(type, tree);

        return new MemorySegment
        {
            SegmentId = stableKey.ToSegmentId(),
            StableKey = stableKey,
            RepoId = file.RepoId,
            Kind = kind,
            Name = name,
            ContainerName = containerName ?? string.Empty,
            ProjectName = file.ProjectName,
            FilePath = file.RelativePath,
            StartLine = startLine,
            EndLine = endLine,
            ContentHash = file.ContentHash,
            Metadata = BuildMetadata(
                signature: BuildTypeSignature(type),
                visibility: ExtractVisibility(type.Modifiers),
                xmlDoc: ExtractXmlDoc(type),
                attributes: ExtractAttributes(type.AttributeLists),
                baseTypes: ExtractBaseList(type.BaseList)
            ),
        };
    }

    private static MemorySegment MapEnum(EnumDeclarationSyntax enm, SyntaxTree tree, ScannedFile file)
    {
        var name = enm.Identifier.Text;
        var containerName = GetContainerName(enm);
        var fqn = BuildFqn(enm, name);
        var stableKey = BuildStableKey(file, SegmentKind.Enum, fqn, signature: null);

        var (startLine, endLine) = GetLines(enm, tree);

        return new MemorySegment
        {
            SegmentId = stableKey.ToSegmentId(),
            StableKey = stableKey,
            RepoId = file.RepoId,
            Kind = SegmentKind.Enum,
            Name = name,
            ContainerName = containerName ?? string.Empty,
            ProjectName = file.ProjectName,
            FilePath = file.RelativePath,
            StartLine = startLine,
            EndLine = endLine,
            ContentHash = file.ContentHash,
            Metadata = BuildMetadata(
                visibility: ExtractVisibility(enm.Modifiers),
                xmlDoc: ExtractXmlDoc(enm)
            ),
        };
    }

    private static MemorySegment MapMethod(MethodDeclarationSyntax method, SyntaxTree tree, ScannedFile file)
    {
        var name = method.Identifier.Text;
        var containerName = GetContainerName(method);
        var fqn = BuildFqn(method, name);
        var signature = BuildParameterSignature(method.ParameterList);
        var stableKey = BuildStableKey(file, SegmentKind.Method, fqn, signature);

        var (startLine, endLine) = GetLines(method, tree);
        SyntaxNode? body = method.Body ?? (SyntaxNode?)method.ExpressionBody;

        return new MemorySegment
        {
            SegmentId = stableKey.ToSegmentId(),
            StableKey = stableKey,
            RepoId = file.RepoId,
            Kind = SegmentKind.Method,
            Name = name,
            ContainerName = containerName ?? string.Empty,
            ProjectName = file.ProjectName,
            FilePath = file.RelativePath,
            StartLine = startLine,
            EndLine = endLine,
            ContentHash = file.ContentHash,
            Metadata = BuildMetadata(
                signature: $"{method.ReturnType.ToString().Trim()} {name}{method.ParameterList}",
                visibility: ExtractVisibility(method.Modifiers),
                xmlDoc: ExtractXmlDoc(method),
                attributes: ExtractAttributes(method.AttributeLists),
                calledMethods: ExtractCalledMethods(body)
            ),
        };
    }

    private static MemorySegment MapConstructor(ConstructorDeclarationSyntax ctor, SyntaxTree tree, ScannedFile file)
    {
        var name = ctor.Identifier.Text;
        var containerName = GetContainerName(ctor);
        var fqn = BuildFqn(ctor, name);
        var signature = BuildParameterSignature(ctor.ParameterList);
        var stableKey = BuildStableKey(file, SegmentKind.Constructor, fqn, signature);

        var (startLine, endLine) = GetLines(ctor, tree);

        return new MemorySegment
        {
            SegmentId = stableKey.ToSegmentId(),
            StableKey = stableKey,
            RepoId = file.RepoId,
            Kind = SegmentKind.Constructor,
            Name = name,
            ContainerName = containerName ?? string.Empty,
            ProjectName = file.ProjectName,
            FilePath = file.RelativePath,
            StartLine = startLine,
            EndLine = endLine,
            ContentHash = file.ContentHash,
            Metadata = BuildMetadata(
                signature: $"{name}{ctor.ParameterList}",
                visibility: ExtractVisibility(ctor.Modifiers),
                xmlDoc: ExtractXmlDoc(ctor),
                calledMethods: ExtractCalledMethods(ctor.Body)
            ),
        };
    }

    private static MemorySegment MapProperty(PropertyDeclarationSyntax prop, SyntaxTree tree, ScannedFile file)
    {
        var name = prop.Identifier.Text;
        var containerName = GetContainerName(prop);
        var fqn = BuildFqn(prop, name);
        var stableKey = BuildStableKey(file, SegmentKind.Property, fqn, signature: null);

        var (startLine, endLine) = GetLines(prop, tree);

        return new MemorySegment
        {
            SegmentId = stableKey.ToSegmentId(),
            StableKey = stableKey,
            RepoId = file.RepoId,
            Kind = SegmentKind.Property,
            Name = name,
            ContainerName = containerName ?? string.Empty,
            ProjectName = file.ProjectName,
            FilePath = file.RelativePath,
            StartLine = startLine,
            EndLine = endLine,
            ContentHash = file.ContentHash,
            Metadata = BuildMetadata(
                signature: $"{prop.Type.ToString().Trim()} {name}",
                visibility: ExtractVisibility(prop.Modifiers),
                xmlDoc: ExtractXmlDoc(prop),
                attributes: ExtractAttributes(prop.AttributeLists)
            ),
        };
    }

    private static IEnumerable<MemorySegment> MapFields(FieldDeclarationSyntax field, SyntaxTree tree, ScannedFile file)
    {
        var (startLine, endLine) = GetLines(field, tree);
        var containerName = GetContainerName(field);
        var xmlDoc = ExtractXmlDoc(field);
        var attributes = ExtractAttributes(field.AttributeLists);
        var visibility = ExtractVisibility(field.Modifiers);

        foreach (var variable in field.Declaration.Variables)
        {
            var name = variable.Identifier.Text;
            var fqn = BuildFqn(field, name);
            var stableKey = BuildStableKey(file, SegmentKind.Field, fqn, signature: null);

            yield return new MemorySegment
            {
                SegmentId = stableKey.ToSegmentId(),
                StableKey = stableKey,
                RepoId = file.RepoId,
                Kind = SegmentKind.Field,
                Name = name,
                ContainerName = containerName ?? string.Empty,
                ProjectName = file.ProjectName,
                FilePath = file.RelativePath,
                StartLine = startLine,
                EndLine = endLine,
                ContentHash = file.ContentHash,
                Metadata = BuildMetadata(
                    signature: $"{field.Declaration.Type.ToString().Trim()} {name}",
                    visibility: visibility,
                    xmlDoc: xmlDoc,
                    attributes: attributes
                ),
            };
        }
    }

    private static string BuildStableKey(ScannedFile file, SegmentKind kind, string fqn, string? signature, string? partialDisambiguator = null)
    {
        var sb = new StringBuilder();
        sb.Append(file.RepoId).Append(':');
        sb.Append(file.ProjectName).Append(':');
        sb.Append(kind).Append(':');
        sb.Append(fqn);

        if (signature is not null)
        {
            sb.Append('(').Append(signature).Append(')');
        }

        if (partialDisambiguator is not null)
        {
            sb.Append(':').Append(partialDisambiguator);
        }

        return sb.ToString();
    }

    private static string BuildFqn(SyntaxNode node, string name)
    {
        var parts = new List<string> { name };

        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is TypeDeclarationSyntax type)
            {
                parts.Insert(0, type.Identifier.Text);
            }
            else if (current is EnumDeclarationSyntax enm)
            {
                parts.Insert(0, enm.Identifier.Text);
            }
            else if (current is BaseNamespaceDeclarationSyntax ns)
            {
                parts.Insert(0, ns.Name.ToString());
            }
        }

        return string.Join(".", parts);
    }

    private static string? GetContainerName(SyntaxNode node)
    {
        var typeParts = new List<string>();
        string? namespaceName = null;

        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is TypeDeclarationSyntax type)
            {
                typeParts.Insert(0, type.Identifier.Text);
            }
            else if (current is EnumDeclarationSyntax enm)
            {
                typeParts.Insert(0, enm.Identifier.Text);
            }
            else if (current is BaseNamespaceDeclarationSyntax ns && typeParts.Count == 0 && namespaceName is null)
            {
                namespaceName = ns.Name.ToString();
            }
        }

        if (typeParts.Count > 0)
        {
            return string.Join(".", typeParts);
        }

        return namespaceName;
    }

    private static string BuildParameterSignature(ParameterListSyntax parameterList)
    {
        return string.Join(", ", parameterList.Parameters.Select(p => p.Type?.ToString().Trim() ?? string.Empty));
    }

    private static string BuildTypeSignature(TypeDeclarationSyntax type)
    {
        var sb = new StringBuilder();
        AppendModifiers(sb, type.Modifiers);
        sb.Append(type.Keyword.Text).Append(' ').Append(type.Identifier.Text);
        if (type.BaseList is not null)
        {
            sb.Append(" : ").Append(string.Join(", ", type.BaseList.Types));
        }

        return sb.ToString();
    }

    private static (int Start, int End) GetLines(SyntaxNode node, SyntaxTree tree)
    {
        var span = tree.GetLineSpan(node.Span);
        return (span.StartLinePosition.Line + 1, span.EndLinePosition.Line + 1);
    }

    private static string? ExtractXmlDoc(SyntaxNode node)
    {
        var trivia = node.GetLeadingTrivia()
            .Where(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                     || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            .ToList();

        if (trivia.Count == 0)
        {
            return null;
        }

        var raw = string.Concat(trivia.Select(t => t.ToFullString())).Trim();
        return string.IsNullOrWhiteSpace(raw) ? null : raw;
    }

    private static IReadOnlyList<string> ExtractAttributes(SyntaxList<AttributeListSyntax> attributeLists) =>
        attributeLists.SelectMany(al => al.Attributes).Select(a => $"[{a}]").ToList();

    private static string? ExtractVisibility(SyntaxTokenList modifiers)
    {
        var tokens = modifiers
            .Where(m => m.IsKind(SyntaxKind.PublicKeyword)
                     || m.IsKind(SyntaxKind.PrivateKeyword)
                     || m.IsKind(SyntaxKind.ProtectedKeyword)
                     || m.IsKind(SyntaxKind.InternalKeyword))
            .Select(m => m.Text)
            .ToList();

        return tokens.Count > 0 ? string.Join(" ", tokens) : null;
    }

    private static IReadOnlyList<string> ExtractBaseList(BaseListSyntax? baseList)
    {
        if (baseList is null)
        {
            return [];
        }

        return baseList.Types.Select(t => t.ToString()).ToArray();
    }

    private static IReadOnlyList<string> ExtractCalledMethods(SyntaxNode? body)
    {
        if (body is null)
        {
            return [];
        }

        return body.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(inv => inv.Expression switch
            {
                MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
                IdentifierNameSyntax id => id.Identifier.Text,
                _ => null,
            })
            .Where(name => name is not null)
            .Select(name => name!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static void AppendModifiers(StringBuilder sb, SyntaxTokenList modifiers)
    {
        if (modifiers.Count > 0)
        {
            sb.Append(string.Join(" ", modifiers.Select(m => m.Text))).Append(' ');
        }
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(
        string? signature = null,
        string? visibility = null,
        string? xmlDoc = null,
        IReadOnlyList<string>? attributes = null,
        IReadOnlyList<string>? baseTypes = null,
        IReadOnlyList<string>? calledMethods = null)
    {
        var dict = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(signature)) { dict["signature"] = signature; }
        if (!string.IsNullOrWhiteSpace(visibility)) { dict["visibility"] = visibility; }
        if (!string.IsNullOrWhiteSpace(xmlDoc)) { dict["xmlDoc"] = xmlDoc; }
        if (attributes?.Count > 0) { dict["attributes"] = string.Join(",", attributes); }
        if (baseTypes?.Count > 0) { dict["baseTypes"] = string.Join(",", baseTypes); }
        if (calledMethods?.Count > 0) { dict["called_methods"] = string.Join(",", calledMethods); }

        return dict;
    }
}
