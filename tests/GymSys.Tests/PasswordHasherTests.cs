using GymSys.Core.Services;

namespace GymSys.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Verify_PasswordBenar_True()
    {
        var salt = PasswordHasher.NewSalt();
        var hash = PasswordHasher.Hash("rahasia", salt);

        Assert.True(PasswordHasher.Verify("rahasia", salt, hash));
    }

    [Fact]
    public void Verify_PasswordSalah_False()
    {
        var salt = PasswordHasher.NewSalt();
        var hash = PasswordHasher.Hash("rahasia", salt);

        Assert.False(PasswordHasher.Verify("Rahasia", salt, hash));
    }

    [Fact]
    public void Hash_UkuranSesuaiSkema()
    {
        var salt = PasswordHasher.NewSalt();

        Assert.Equal(16, salt.Length);
        Assert.Equal(32, PasswordHasher.Hash("x", salt).Length);
    }
}
