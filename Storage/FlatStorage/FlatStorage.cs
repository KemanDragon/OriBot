using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OriBot.Storage.FlatStorage
{
    public class FlatStorage : Storage
    {
        private readonly bool _initialized;

        private readonly string _basestorageLocation;

        private readonly string _storageName;

        private readonly string _computed;

        //private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _storage;

        private FlatStorage(bool initialized, string storagename, string basestoragelocation, string computed) 
        {
            _initialized = initialized;
            _basestorageLocation = basestoragelocation;
            _storageName = storagename;
            _computed = Path.Join(basestoragelocation, storagename);
        }
        public override Task CleanupAsync()
        {
            throw new NotImplementedException();
        }

        public override bool Exists(string scope, string key)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Storage is not initialized.");
            }
            scope = Escape(scope);
            key = Escape(key);

            return File.Exists(Path.Join(_computed, scope, key));
        }

        public override T Get<T>(string scope, string key, out bool success)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Storage is not initialized.");
            }
            scope = Escape(scope);
            key = Escape(key);

            var exists = File.Exists(Path.Join(_computed, scope, key));
            if (exists)
            {
                success = true;
                return Deserialize<T>(Path.Join(_computed, scope, key));
            }
            else
            {
                success = false;
                return default(T);
            }
        }

        public static Storage InitializeOrLoad(string storagename, string basestoragelocation)
        {
            var path = Path.Join(basestoragelocation, storagename);
            if (!Directory.Exists(Path.Join(basestoragelocation, storagename)))
            {
                Directory.CreateDirectory(path);
            }
            return new FlatStorage(true, storagename, basestoragelocation, path);
        }

        public override bool Remove(string scope, string key)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Storage is not initialized.");
            }
            scope = Escape(scope);
            key = Escape(key);
            var exists = File.Exists(Path.Join(_computed, scope, key));
            if (exists)
            {
                File.Delete(Path.Join(_computed, scope, key));
                return true;
            }
            else
            {
                return false;
            }
        }

        

        public override void Set<T>(string scope, string key, T value)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Storage is not initialized.");
            }
            scope = Escape(scope);
            key = Escape(key);

            if (!Directory.Exists(Path.Join(_computed, scope)))
            {
                Directory.CreateDirectory(Path.Join(_computed, scope));
            }
            File.WriteAllText(Path.Join(_computed, scope, key), Serialize(value));
        }

        private static string Serialize<T>(T value)
        {
            switch (value)
            {
                case string v:
                    return (string)Convert.ChangeType(value, typeof(string));
                case int v:
                    return v.ToString();
                case bool v:
                    return v.ToString();
                default:
                    return null;     
            }
        }

        private static T Deserialize<T>(string value)
        {
            var type = default(T);
            switch (type)
            {
                case string v:
                    return (T)(object)v;
                case int v:
                    return (T)(object)int.Parse(value);
                case bool v:
                    return (T)(object)bool.Parse(value);
                default:
                    return default(T);
            }
        }

        public override string[] GetScopes()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Storage is not initialized.");
            }
            return Directory.GetDirectories(_computed).Select(x => Unescape(Path.GetFileName(x))).ToArray();
        }

        public override string[] GetKeys(string scope)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Storage is not initialized.");
            }
            scope = Escape(scope);

            return Directory.GetFiles(Path.Join(_computed, scope)).Select(x => Unescape(Path.GetFileName(x))).ToArray();
        }

        // For now, escaping is done the old way
        // Ill find better ways to escape names
        public static string Escape(string s)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
        }

        public static string Unescape(string s)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(s));
        }
    }
}
