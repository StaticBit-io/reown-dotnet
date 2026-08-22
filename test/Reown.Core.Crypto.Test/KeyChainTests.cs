using Reown.Core.Storage;
using Xunit;

namespace Reown.Core.Crypto.Test;

public class KeyChainTests
{
    private static async Task<KeyChain> CreateInitializedKeyChainAsync()
    {
        var storage = new InMemoryStorage();
        await storage.Init();

        var keyChain = new KeyChain(storage);
        await keyChain.Init();
        return keyChain;
    }

    /// <summary>
    ///     Ensures a disposed keychain refuses to be reused instead of coming up empty.
    /// </summary>
    /// <remarks>
    ///     The instance lives on CoreOptions, which outlives the client that built it, so the next
    ///     client adopts it unless its owner clears the field. Reuse used to succeed silently: the
    ///     keys are gone from memory but the initialised flag still stands, so Init skips loading,
    ///     Crypto finds no client seed, generates a new one, and the keychain is written back
    ///     through the dead instance's storage — every session key on disk replaced by that seed.
    /// </remarks>
    [Fact, Trait("Category", "unit")]
    public async Task ADisposedKeyChainRefusesToBeReused()
    {
        var storage = new InMemoryStorage();
        await storage.Init();

        var keyChain = new KeyChain(storage);
        await keyChain.Init();
        await keyChain.Set("tag", "0123456789abcdef");

        keyChain.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => keyChain.Init());

        // The accessors deliberately do not: they are reached from message handling, parts of which
        // are async void, and an exception there ends the process rather than reporting a bug.
        Assert.False(await keyChain.Has("tag"));
    }

    /// <summary>
    ///     Ensures initialising against a storage that has no keychain writes nothing back.
    /// </summary>
    /// <remarks>
    ///     The write bought nothing — the first Set persists the whole keychain anyway — and cost
    ///     everything on the one occasion it mattered: a storage that failed to load its state
    ///     reports no keychain, and this replaced the keys on disk with an empty dictionary. Every
    ///     session key was gone at that point, and the sessions followed, since a session whose key
    ///     cannot be found looks exactly like one that was never valid. Seen on a real wallet.
    /// </remarks>
    [Fact, Trait("Category", "unit")]
    public async Task InitDoesNotWriteAnEmptyKeychainBack()
    {
        var storage = new CountingStorage();
        await storage.Init();

        var keyChain = new KeyChain(storage);
        await keyChain.Init();

        Assert.Equal(0, storage.Writes);
    }

    /// <summary>
    ///     Storage that counts what is written to it.
    /// </summary>
    private sealed class CountingStorage : InMemoryStorage
    {
        /// <summary>
        ///     Gets how many times a value was stored.
        /// </summary>
        public int Writes { get; private set; }

        /// <inheritdoc />
        public override Task SetItem<T>(string key, T value)
        {
            Writes++;
            return base.SetItem(key, value);
        }
    }

    [Fact, Trait("Category", "unit")]
    public async Task GetWithUnknownTagThrowsKeychainKeyNotFoundException()
    {
        var keyChain = await CreateInitializedKeyChainAsync();

        await Assert.ThrowsAsync<KeychainKeyNotFoundException>(() => keyChain.Get("unknown-tag"));
    }

    [Fact, Trait("Category", "unit")]
    public async Task DeleteWithUnknownTagThrowsKeychainKeyNotFoundException()
    {
        const string tag = "unknown-tag";
        var keyChain = await CreateInitializedKeyChainAsync();

        var exception = await Assert.ThrowsAsync<KeychainKeyNotFoundException>(() => keyChain.Delete(tag));

        Assert.Equal(tag, exception.Tag);
    }

    [Fact, Trait("Category", "unit")]
    public async Task KeychainKeyNotFoundExceptionIsAssignableToInvalidOperationException()
    {
        var keyChain = await CreateInitializedKeyChainAsync();

        var exception = await Assert.ThrowsAsync<KeychainKeyNotFoundException>(() => keyChain.Get("unknown-tag"));

        Assert.IsType<InvalidOperationException>(exception, exactMatch: false);
    }

    [Fact, Trait("Category", "unit")]
    public async Task KeychainKeyNotFoundExceptionCarriesMissingTag()
    {
        const string tag = "missing-tag";
        var keyChain = await CreateInitializedKeyChainAsync();

        var exception = await Assert.ThrowsAsync<KeychainKeyNotFoundException>(() => keyChain.Get(tag));

        Assert.Equal(tag, exception.Tag);
        Assert.Equal($"Keychain does not contain key with tag: {tag}.", exception.Message);
    }

    [Fact, Trait("Category", "unit")]
    public void KeychainKeyNotFoundExceptionThrowsForNullTag()
    {
        Assert.Throws<ArgumentNullException>(() => new KeychainKeyNotFoundException(null!));
    }
}
