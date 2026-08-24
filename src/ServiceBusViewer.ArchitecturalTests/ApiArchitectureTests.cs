namespace ServiceBusViewer.ArchitecturalTests;

using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Syntax.Elements.Types;
using ArchUnitNET.Loader;
using ServiceBusViewer.Business.Viewer.Contracts;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

public sealed class ApiArchitectureTests
{
	private const string _clientSessionAccessorFullName = "ServiceBusViewer.Infrastructure.ClientSession.ClientSessionStateMiddlewareExtensions";

	private static readonly Architecture _architecture = new ArchLoader()
		.LoadAssemblies(typeof(ConnectionSettings).Assembly)
		.Build();

	[Fact]
	public void ApiModels_BusinessServiceBusModelsAreReferenced_HasNoDependencies()
	{
		// Arrange
		IObjectProvider<IType> apiModels = Types().That().ResideInNamespace("ServiceBusViewer.Api.Models");
		IObjectProvider<IType> businessModels = Types().That().ResideInNamespace("ServiceBusViewer.Business.Viewer.Contracts.ServiceBus");

		// Act
		TypesShouldConjunction rule = Types().That().Are(apiModels).Should().NotDependOnAny(businessModels);

		// Assert
		Assert.True(rule.HasNoViolations(_architecture));
	}

	[Fact]
	public void ApiEndpoints_OnlyUseClientSessionAccessor_HasNoInfrastructureImplementationDependencies()
	{
		// Arrange
		IObjectProvider<IType> apiEndpoints = Types().That().HaveFullNameContaining("ServiceBusViewer.Api.Endpoints");
		IEnumerable<IType> prohibitedInfrastructure = _architecture.Types.Where(type =>
			type.FullName.Contains("ServiceBusViewer.Infrastructure", StringComparison.Ordinal)
			&& !type.FullName.Equals(_clientSessionAccessorFullName, StringComparison.Ordinal));

		// Act
		TypesShouldConjunction rule = Types().That().Are(apiEndpoints).Should().NotDependOnAny(prohibitedInfrastructure);

		// Assert
		Assert.True(rule.HasNoViolations(_architecture));
	}

	[Fact]
	public void ApiEndpoints_BusinessModelsAreReturned_HaveNoBusinessReturnTypes()
	{
		// Arrange
		IObjectProvider<IType> apiEndpoints = Types().That().HaveFullNameContaining("ServiceBusViewer.Api.Endpoints");
		IObjectProvider<IType> businessModels = Types().That().ResideInNamespace("ServiceBusViewer.Business.Viewer.Contracts");

		// Act
		var rule = MethodMembers().That().AreDeclaredIn(apiEndpoints).Should().NotHaveReturnType(businessModels);

		// Assert
		Assert.True(rule.HasNoViolations(_architecture));
	}
}
