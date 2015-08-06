namespace Coderful.Analyzer
{
	using System.Collections.Immutable;
	using System.Linq;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.Diagnostics;

	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class MethodParameterAnalyzer : DiagnosticAnalyzer
	{
		private const string Category = "Architecture";

		private static readonly DiagnosticDescriptor ParametersShouldBeOfPrimitiveTypes = new DiagnosticDescriptor(
			DiagnosticIds.UsePrimitiveTypesForParameters,
			"Method accepts object as parameter",
			"Method '{0}' accepts object as parameter",
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "Methods in BL should only accept parameters of primitive types.");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ParametersShouldBeOfPrimitiveTypes);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
		}

		private static void AnalyzeMethod(SymbolAnalysisContext context)
		{
			var symbol = (IMethodSymbol)context.Symbol;

			if (symbol.ContainingType == null || !symbol.ContainingType.IsManager())
			{
				return;
			}

			if (symbol.MethodKind == MethodKind.Ordinary &&
				symbol.Parameters.Length > 0 &&
				!symbol.IsStatic &&
				!symbol.IsImplicitlyDeclared &&
				!symbol.IsOverride)
			{
				var any = symbol.Parameters.Any(p => p.Type.IsReferenceType || p.Type.IsAnonymousType);
				if (any)
				{
					var diagnostic = Diagnostic.Create(ParametersShouldBeOfPrimitiveTypes, symbol.Locations[0], symbol.Name);
					context.ReportDiagnostic(diagnostic);
				}
			}
		}
	}

	internal static class Extensions
	{
		public static bool IsManager(this INamedTypeSymbol type)
		{
			if (type.IsReferenceType)
			{
				if (type.Name.Contains("Manager") || 
					type.AllInterfaces.Any(i => i.Name.Contains("Manager")))
				{
					return true;
				}
			}

			return false;
		}
	}
}