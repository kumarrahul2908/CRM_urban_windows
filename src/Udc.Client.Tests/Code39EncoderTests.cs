using Udc.Client;
using Xunit;

namespace Udc.Client.Tests;

public class Code39EncoderTests
{
    [Theory]
    [InlineData("ST-0055-01")]
    [InlineData("abc")]
    [InlineData("A")]
    [InlineData("0")]
    public void CanEncode_AcceptsSupportedCharacters(string value) => Assert.True(Code39Encoder.CanEncode(value));

    [Theory]
    [InlineData("")]
    [InlineData("ST_0055")]
    [InlineData("héllo")]
    public void CanEncode_RejectsUnsupportedInput(string value) => Assert.False(Code39Encoder.CanEncode(value));

    [Fact]
    public void Encode_SingleDigitZero_MatchesKnownPattern()
    {
        // Edge + Middle + pattern('0') + Middle + Edge, from the python-barcode reference table.
        Assert.Equal("10001011101110101010001110111010100010111011101", Code39Encoder.Encode("0"));
    }

    [Fact]
    public void Encode_SingleLetterA_MatchesKnownPattern()
    {
        Assert.Equal("10001011101110101110101000101110100010111011101", Code39Encoder.Encode("A"));
    }

    [Fact]
    public void Encode_MultiCharacterValue_MatchesKnownPattern()
    {
        Assert.Equal("100010111011101010111010111000101010111011100010100010111011101", Code39Encoder.Encode("ST"));
    }

    [Fact]
    public void Encode_IsCaseInsensitive()
    {
        Assert.Equal(Code39Encoder.Encode("ST"), Code39Encoder.Encode("st"));
    }

    [Fact]
    public void Encode_OnlyProducesBinaryCharacters()
    {
        var modules = Code39Encoder.Encode("ST-0055-01");
        Assert.All(modules, c => Assert.True(c is '0' or '1'));
    }

    [Fact]
    public void Encode_StartsAndEndsWithTheEdgePattern()
    {
        var modules = Code39Encoder.Encode("A");
        Assert.StartsWith("100010111011101", modules);
        Assert.EndsWith("100010111011101", modules);
    }

    [Fact]
    public void Encode_RealBarcodeValueFormat_DoesNotThrow()
    {
        // matches GarmentTagService.buildIdentifier()'s "{shortOrderCode}-{paddedSequence}" shape.
        var modules = Code39Encoder.Encode("ST-0055-01");
        Assert.NotEmpty(modules);
    }

    [Fact]
    public void Encode_UnsupportedCharacter_Throws()
    {
        Assert.Throws<ArgumentException>(() => Code39Encoder.Encode("ST_0055"));
    }
}
