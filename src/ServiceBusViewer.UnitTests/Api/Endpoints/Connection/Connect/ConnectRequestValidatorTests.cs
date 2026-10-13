namespace ServiceBusViewer.UnitTests.Api.Endpoints.Connection.Connect;

using ServiceBusViewer.Api.Endpoints.Connection.Connect;
using Xunit;

public sealed class ConnectRequestValidatorTests
{
	[Theory]
	[InlineData(1)]
	[InlineData(500)]
	public void ConnectRequestValidator_Validate_PeekCountWithinBounds_ReturnsValid(int peekMessageCount)
	{
		// Arrange
		var request = new ConnectRequest("Endpoint=sb://test/", null, null, null, peekMessageCount);

		// Act
		bool isValid = ConnectRequestValidator.Validate(request, out IReadOnlyDictionary<string, string[]>? errors);

		// Assert
		Assert.True(isValid);
		Assert.Null(errors);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(501)]
	public void ConnectRequestValidator_Validate_PeekCountOutOfBounds_ReturnsError(int peekMessageCount)
	{
		// Arrange
		var request = new ConnectRequest("Endpoint=sb://test/", null, null, null, peekMessageCount);

		// Act
		bool isValid = ConnectRequestValidator.Validate(request, out IReadOnlyDictionary<string, string[]>? errors);

		// Assert
		Assert.False(isValid);
		Assert.NotNull(errors);
		Assert.True(errors.ContainsKey(nameof(request.PeekMessageCount)));
	}
}
