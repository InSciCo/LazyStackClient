﻿using Microsoft.CodeAnalysis;

namespace LazyStack.LzItemViewModelGenerator;

// Example:
// namespace MyNamespace;
// public class YadaViewModel : LzItemViewModelBase<Yada, YadaModel>
// {
// }
//
// Generated: YadaModel.cs
// namespace MyNamespace;
// public partial class YadaModel : IRegisterObservables
// {
//      public virtual void RegisterObservables() { }
// }
//
// Generated: YadaModelValidator.cs
// namespace MyNamespace;   
// public partial class YadaModelValidator : AbstractValidator<YadaModel>   
// {
// }
//

[Generator]
public class LazyStackLzItemViewModelGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                var model = context.Compilation.GetSemanticModel(syntaxTree);
                // The following query to get derived classes is fragile. However, the alternative use of semantic 
                // analysis to find derived classes proved difficult to implement and seems to be overkill for this
                // use case.
                var derivedClasses = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(x => (bool)(x.BaseList?.Types.Any(y => y.Type.ToString().Contains("LzItemViewModel")) ?? false));

                //Log(context, $"derivedClasses: {derivedClasses.Count()} null: {derivedClasses == null}");
                if (derivedClasses != null && derivedClasses.Count() > 0)
                    foreach (var derivedClass in derivedClasses)
                    {
                        var namespaceName = GetNamespace(context, model, derivedClass);
                        var dtoType = ExtractGenericDtoType(context, derivedClass); // example Yada  
                        var modelType = ExtractGenericModelType(context,derivedClass); // example YadaModel 
                        //Log(context, $"namespaceName: {namespaceName} dtoType: {dtoType} modelType: {modelType}");
                        if(dtoType != null && modelType != null)
                        {
                            var source = GenerateClassModelSource(context, namespaceName, dtoType, modelType);
                            context.AddSource($"{modelType}.g.cs", source);
                            source = GenerateClassModelValidatorSource(context, namespaceName, modelType);
                            context.AddSource($"{modelType}Validator.g.cs", source);
                        }
                    }
            }
        } catch (Exception ex)
        {
            var diagnostic = Diagnostic.Create(_messageRule, Location.None, ex.Message + "01");
            context.ReportDiagnostic(diagnostic);
        }   
    }
    private static SourceText GenerateClassModelSource(GeneratorExecutionContext context, string namespaceName, string dtoType, string modelType)
    {
        //Log(context, $"GenerateClassModelSource: {namespaceName} {modelType}");
        var sourceBuilder = new StringBuilder();
        sourceBuilder.AppendLine(@$"
// <auto-generated />
namespace {namespaceName};
public partial class {modelType} : {dtoType}, IRegisterObservables
{{
}}");
        SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceBuilder.ToString());
        SyntaxNode root = tree.GetRoot();
        SyntaxNode formattedRoot = root.NormalizeWhitespace();
        return SourceText.From(formattedRoot.ToString(), Encoding.UTF8);
    }
    private static SourceText GenerateClassModelValidatorSource(GeneratorExecutionContext context, string namespaceName, string typename)
    {
        //Log(context, $"GenerateClassModelValidatorSource: {namespaceName} {typename}"); 
        var sourceBuilder = new StringBuilder();
        sourceBuilder.AppendLine(@$"
// <auto-generated />
namespace {namespaceName};
public partial class {typename}Validator : AbstractValidator<{typename}>
{{
}}");

        SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceBuilder.ToString());
        SyntaxNode root = tree.GetRoot();
        SyntaxNode formattedRoot = root.NormalizeWhitespace();
        return SourceText.From(formattedRoot.ToString(), Encoding.UTF8);
    }
    private static string? ExtractGenericModelType(GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
    {
        // 1. Get the BaseListSyntax
        var baseList = classDeclaration.BaseList;

        if (baseList != null)
        {
            foreach (var baseType in baseList.Types)
            {
                // 2. Get the SimpleBaseTypeSyntax and check if it's a GenericNameSyntax
                if (baseType.Type is GenericNameSyntax genericBaseType)
                {
                    //Log(context, $"genericBaseType: {genericBaseType.Identifier.Text}");
                    // Check the generic type name (optional)
                    if (genericBaseType.Identifier.Text.Contains("LzItemViewModel"))
                    {
                        // 3. Navigate and get the generic type arguments
                        var typeArguments = genericBaseType.TypeArgumentList.Arguments;
                        if (typeArguments.Count > 1)
                        {
                            return typeArguments[1].ToString();
                        }
                    }

                }
            }
        }
        return null;  // Return null if not found
    }
    private static string? ExtractGenericDtoType(GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
    {
        // 1. Get the BaseListSyntax
        var baseList = classDeclaration.BaseList;

        if (baseList != null)
        {
            foreach (var baseType in baseList.Types)
            {
                //Log(context, $"baseType: {baseType.Type}");
                // 2. Get the SimpleBaseTypeSyntax and check if it's a GenericNameSyntax
                if (baseType.Type is GenericNameSyntax genericBaseType)
                {
                    // Check the generic type name (optional)
                    if (genericBaseType.Identifier.Text.Contains("LzItemViewModel"))
                    {
                        // 3. Navigate and get the generic type arguments
                        var typeArguments = genericBaseType.TypeArgumentList.Arguments;
                        if (typeArguments.Count > 0)
                        {
                            return typeArguments[0].ToString();
                        }
                    }

                }
            }
        }
        return null;  // Return null if not found
    }

    private static string GetNamespace(GeneratorExecutionContext context, SemanticModel model, ClassDeclarationSyntax classNode)
    {
        var namespaceName = string.Empty;

        var classSymbol = model.GetDeclaredSymbol(classNode) as INamedTypeSymbol;

        if (classSymbol != null)
            namespaceName = classSymbol.ContainingNamespace.ToString();
        else
        {
            var diagnostic = Diagnostic.Create(_messageRule, Location.None, "Namespace not found.");
            context.ReportDiagnostic(diagnostic);
        }
        return namespaceName;
    }
    private static readonly DiagnosticDescriptor _messageRule = new(
        id: "LZI0002",
        title: "LazyStack.LzItemViewModelGenerator Source Generator Message",
        messageFormat: "{0}",
        category: "SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
    private static void Log(GeneratorExecutionContext context, string? message)
    {
        if (message == null) return;
        string[] lines = message.Split(new[] { '\n' }, StringSplitOptions.None);
        foreach (var line in lines)
        {
            var diagnostic = Diagnostic.Create(_messageRule, Location.None, line);
            context.ReportDiagnostic(diagnostic);
        }
    }
}