using Apachi.Shared.Crypto;
using Org.BouncyCastle.Math;

namespace Apachi.UnitTests.Shared.CryptoTests;

public class EqualityProofTests
{
    private readonly BigInteger _b1;
    private readonly BigInteger _b2;

    public EqualityProofTests()
    {
        _b1 = GenerateBigInteger();
        _b2 = GenerateBigInteger();
    }

    [Theory, AutoData]
    public void Verify_ShouldReturnTrue_WhenProofIsValid([ArraySize(100)] byte[] value)
    {
        var proof = EqualityProof.Create(_b1, _b2);

        var c1 = Commitment.Create(value, _b1);
        var c2 = Commitment.Create(value, _b2);

        var actual = proof.Verify(c1, c2);

        actual.Should().BeTrue();
    }
}

