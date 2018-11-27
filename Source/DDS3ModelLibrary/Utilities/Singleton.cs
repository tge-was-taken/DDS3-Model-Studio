using System;

namespace DDS3ModelLibrary.Utilities
{
    public class Singleton<T> where T : class, new()
    {
        private static readonly Lazy<T> sInstance = new Lazy<T>( () => new T() );

        public static T Instance => sInstance.Value;

        protected Singleton() { }
    }
}