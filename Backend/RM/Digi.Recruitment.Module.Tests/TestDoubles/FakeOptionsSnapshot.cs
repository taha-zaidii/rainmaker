using Microsoft.Extensions.Options;

namespace Digi.Recruitment.Module.Tests.TestDoubles
{
    /// <summary>
    /// MultinetAiProvider takes <see cref="IOptionsSnapshot{T}"/> (it re-reads per
    /// scope, since options can change per company); OptionsWrapper only
    /// implements <see cref="IOptions{T}"/>. This is the smallest fake that
    /// satisfies the snapshot contract with one fixed value.
    /// </summary>
    public sealed class FakeOptionsSnapshot<T> : IOptionsSnapshot<T> where T : class
    {
        private readonly T _value;

        public FakeOptionsSnapshot(T value) => _value = value;

        public T Value => _value;

        public T Get(string? name) => _value;
    }
}
