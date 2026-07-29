using System.Diagnostics;
using Reflect.Models;

namespace Reflect.Core.Tests;

/// <summary>
/// The journal lock. Credential handling is the one area where a defect is both
/// easy to miss and genuinely harmful, so these assert the security properties
/// rather than only the happy path.
/// </summary>
public sealed class SettingsServiceTests
{
    private const string Passphrase = "correct horse";

    [Fact]
    public async Task The_lock_is_off_by_default()
    {
        using var journal = new TestJournal();

        Assert.False(await journal.Settings.IsLockEnabledAsync());
        Assert.Equal("light", await journal.Settings.GetThemeAsync());
    }

    [Fact]
    public async Task Verifying_against_no_lock_returns_false_rather_than_throwing()
    {
        using var journal = new TestJournal();

        Assert.False(await journal.Settings.VerifyPassphraseAsync("anything"));
    }

    [Fact]
    public async Task A_correct_passphrase_verifies_and_a_wrong_one_does_not()
    {
        using var journal = new TestJournal();
        await journal.Settings.SetPassphraseAsync(Passphrase);

        Assert.True(await journal.Settings.IsLockEnabledAsync());
        Assert.True(await journal.Settings.VerifyPassphraseAsync(Passphrase));
        Assert.False(await journal.Settings.VerifyPassphraseAsync("wrong horse"));
    }

    [Theory]
    [InlineData("Correct Horse")]   // wrong case
    [InlineData("correct hors")]    // one character short
    [InlineData("correct horse ")]  // trailing space
    [InlineData("")]
    public async Task Near_misses_are_rejected(string attempt)
    {
        using var journal = new TestJournal();
        await journal.Settings.SetPassphraseAsync(Passphrase);

        Assert.False(await journal.Settings.VerifyPassphraseAsync(attempt));
    }

    [Fact]
    public async Task The_passphrase_is_never_written_to_storage()
    {
        using var journal = new TestJournal();
        await journal.Settings.SetPassphraseAsync(Passphrase);

        var connection = await journal.Database.GetConnectionAsync();
        var row = await connection.Table<AppSettings>().FirstAsync();

        Assert.DoesNotContain("correct", row.PasswordHash);
        Assert.DoesNotContain("correct", row.PasswordSalt);
        Assert.NotEmpty(row.PasswordHash);
        Assert.NotEmpty(row.PasswordSalt);
        Assert.True(row.Iterations > 0);
        Assert.Equal(32, Convert.FromBase64String(row.PasswordHash).Length);
        Assert.Equal(32, Convert.FromBase64String(row.PasswordSalt).Length);
    }

    [Fact]
    public async Task The_same_passphrase_produces_different_hashes_on_different_installs()
    {
        using var first = new TestJournal();
        using var second = new TestJournal();

        await first.Settings.SetPassphraseAsync("shared passphrase");
        await second.Settings.SetPassphraseAsync("shared passphrase");

        var rowOne = await (await first.Database.GetConnectionAsync()).Table<AppSettings>().FirstAsync();
        var rowTwo = await (await second.Database.GetConnectionAsync()).Table<AppSettings>().FirstAsync();

        // A shared salt would let one stolen hash be compared against another.
        Assert.NotEqual(rowOne.PasswordSalt, rowTwo.PasswordSalt);
        Assert.NotEqual(rowOne.PasswordHash, rowTwo.PasswordHash);
        Assert.True(await first.Settings.VerifyPassphraseAsync("shared passphrase"));
        Assert.True(await second.Settings.VerifyPassphraseAsync("shared passphrase"));
    }

    [Fact]
    public async Task Changing_the_passphrase_retires_the_old_one()
    {
        using var journal = new TestJournal();
        await journal.Settings.SetPassphraseAsync("first one");
        await journal.Settings.SetPassphraseAsync("second one");

        Assert.False(await journal.Settings.VerifyPassphraseAsync("first one"));
        Assert.True(await journal.Settings.VerifyPassphraseAsync("second one"));
    }

    [Fact]
    public async Task The_lock_cannot_be_disabled_without_the_current_passphrase()
    {
        using var journal = new TestJournal();
        await journal.Settings.SetPassphraseAsync(Passphrase);

        Assert.False(await journal.Settings.DisableLockAsync("not it"));
        Assert.True(await journal.Settings.IsLockEnabledAsync());
    }

    [Fact]
    public async Task Disabling_the_lock_clears_the_stored_credential()
    {
        using var journal = new TestJournal();
        await journal.Settings.SetPassphraseAsync(Passphrase);

        Assert.True(await journal.Settings.DisableLockAsync(Passphrase));
        Assert.False(await journal.Settings.IsLockEnabledAsync());

        var row = await (await journal.Database.GetConnectionAsync()).Table<AppSettings>().FirstAsync();
        Assert.Empty(row.PasswordHash);
        Assert.Empty(row.PasswordSalt);
        Assert.Equal(0, row.Iterations);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    public async Task Passphrases_that_are_too_short_are_rejected(string candidate)
    {
        using var journal = new TestJournal();

        await Assert.ThrowsAsync<ArgumentException>(
            () => journal.Settings.SetPassphraseAsync(candidate));
    }

    [Fact]
    public async Task A_four_digit_PIN_is_accepted()
    {
        using var journal = new TestJournal();

        // The specification names a PIN as an acceptable option.
        await journal.Settings.SetPassphraseAsync("1234");

        Assert.True(await journal.Settings.VerifyPassphraseAsync("1234"));
    }

    [Fact]
    public async Task Corrupt_credential_material_fails_closed_without_throwing()
    {
        using var journal = new TestJournal();
        await journal.Settings.SetPassphraseAsync(Passphrase);

        var connection = await journal.Database.GetConnectionAsync();
        var row = await connection.Table<AppSettings>().FirstAsync();
        row.PasswordHash = "not!valid!base64!";
        await connection.UpdateAsync(row);

        Assert.False(await journal.Settings.VerifyPassphraseAsync(Passphrase));
    }

    [Fact]
    public async Task A_row_with_no_iteration_count_still_verifies()
    {
        using var journal = new TestJournal();
        await journal.Settings.SetPassphraseAsync(Passphrase);

        var connection = await journal.Database.GetConnectionAsync();
        var row = await connection.Table<AppSettings>().FirstAsync();
        row.Iterations = 0;
        await connection.UpdateAsync(row);

        // Rows written before the count was recorded fall back to the current
        // work factor rather than failing outright.
        Assert.True(await journal.Settings.VerifyPassphraseAsync(Passphrase));
    }

    [Theory]
    [InlineData("dark", "dark")]
    [InlineData("light", "light")]
    [InlineData("DARK", "dark")]
    [InlineData("banana", "light")]
    public async Task Theme_values_are_normalised(string input, string expected)
    {
        using var journal = new TestJournal();

        await journal.Settings.SetThemeAsync(input);

        Assert.Equal(expected, await journal.Settings.GetThemeAsync());
    }

    [Fact]
    public async Task Verification_cost_stays_inside_its_intended_window()
    {
        using var journal = new TestJournal();
        await journal.Settings.SetPassphraseAsync(Passphrase);

        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < 3; i++)
        {
            await journal.Settings.VerifyPassphraseAsync(Passphrase);
        }
        stopwatch.Stop();

        var perVerification = stopwatch.ElapsedMilliseconds / 3.0;

        // Fast enough that unlocking feels instant, slow enough that bulk
        // offline guessing is expensive. A collapse to near zero would mean the
        // work factor had been lost.
        Assert.InRange(perVerification, 15, 1000);
    }
}
