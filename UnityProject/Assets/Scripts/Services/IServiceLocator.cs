using System;

namespace SiteOwlXR.Services
{
    /// <summary>
    /// Service Locator for Dependency Injection
    /// Enterprise pattern for loose coupling and testability
    /// </summary>
    public static class ServiceLocator
    {
        private static System.Collections.Generic.Dictionary<Type, object> services = 
            new System.Collections.Generic.Dictionary<Type, object>();
        
        public static void Register<T>(T service) where T : class
        {
            services[typeof(T)] = service;
        }
        
        public static T Get<T>() where T : class
        {
            if (services.TryGetValue(typeof(T), out object service))
            {
                return service as T;
            }
            return null;
        }
        
        public static void Unregister<T>() where T : class
        {
            services.Remove(typeof(T));
        }
        
        public static void Clear()
        {
            services.Clear();
        }
    }
}
