namespace ServiceBusViewer.UnitTests.Api.Endpoints.Viewer.UpdatePeekMessageCount;

using ServiceBusViewer.Api.Endpoints.Viewer.UpdatePeekMessageCount;
using Xunit;

public sealed class UpdatePeekMessageCountRequestValidatorTests
{
	[Theory]
	[InlineData(1)]
	[InlineData(50)]
	[InlineData(200)]
	[InlineData(500)]
	public void UpdatePeekMessageCountRequestValidator_Validate_CountWithinBounds_ReturnsValid(int peekMessageCount)
	{
		// Arrange
		var request = new UpdatePeekMessageCountRequest(peekMessageCount);

		// Act
		bool isValid = UpdatePeekMessageCountRequestValidator.Validate(request, out IReadOnlyDictionary<string, string[]>? errors);

		// Assert
		Assert.True(isValid);
		Assert.Null(errors);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(501)]
	public void UpdatePeekMessageCountRequestValidator_Validate_CountOutOfBounds_ReturnsError(int peekMessageCount)
	{
		// Arrange
		var request = new UpdatePeekMessageCountRequest(peekMessageCount);

		// Act
		bool isValid = UpdatePeekMessageCountRequestValidator.Validate(request, out IReadOnlyDictionary<string, string[]>? errors);

		// Assert
		Assert.False(isValid);
		Assert.NotNull(errors);
		Assert.True(errors.ContainsKey(nameof(request.PeekMessageCount)));
	}
}
