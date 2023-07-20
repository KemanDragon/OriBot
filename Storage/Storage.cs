using System.Threading.Tasks;

namespace OriBot.Storage
{
    public abstract class Storage
    {
        protected Storage()
        {
            
        }

        public abstract T? Get<T>(string scope, string key, out bool success);
        public abstract void Set<T>(string scope, string key, T value);

        public abstract Task CleanupAsync();

        public abstract bool Exists(string scope, string key);

        public abstract bool Remove(string scope, string key);

        public abstract string[] GetScopes();

        public abstract string[] GetKeys(string scope);

    }
}
