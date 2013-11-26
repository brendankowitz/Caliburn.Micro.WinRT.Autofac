using System;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Caliburn.Micro.WinRT.Autofac.StorageHandlers {
    

    /// <summary>
    /// Stores data in the application settings.
    /// </summary>
    public class AppSettingsStorageMechanism : IStorageMechanism {
        List<string> keys;
        private ApplicationDataContainer Storage { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettingsStorageMechanism"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public AppSettingsStorageMechanism() {
            Storage = ApplicationData.Current.LocalSettings;
            IsolatedStorageSettings = Storage.Values;
        }

        protected IPropertySet IsolatedStorageSettings { get; set; }

        /// <summary>
        /// Indicates what storage modes this mechanism provides.
        /// </summary>
        /// <param name="mode">The storage mode to check.</param>
        /// <returns>
        /// Whether or not it is supported.
        /// </returns>
        public bool Supports(StorageMode mode) {
            return (mode & StorageMode.Permanent) == StorageMode.Permanent;
        }

        /// <summary>
        /// Begins the storage transaction.
        /// </summary>
        public void BeginStoring() {
            keys = new List<string>();
        }

        /// <summary>
        /// Stores the value with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="data">The data.</param>
        public void Store(string key, object data) {
            if (!IsolatedStorageSettings.ContainsKey(key))
            {
                keys.Add(key);
            }

            IsolatedStorageSettings[key] = data;
        }

        /// <summary>
        /// Ends the storage transaction.
        /// </summary>
        public void EndStoring() {
        }

        /// <summary>
        /// Tries to get the data previously stored with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>true if found; false otherwise</returns>
        public bool TryGet(string key, out object value) {
            return IsolatedStorageSettings.TryGetValue(key, out value);
        }

        /// <summary>
        /// Deletes the data with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        public void Delete(string key) {
            IsolatedStorageSettings.Remove(key);
        }

        /// <summary>
        /// Clears the data stored in the last storage transaction.
        /// </summary>
        public void ClearLastSession() {
            if (keys != null) {
                keys.Apply(x => IsolatedStorageSettings.Remove((string) x));
                keys = null;
            }
        }

        public void RegisterSingleton(Type service, string key, Type implementation)
        {
            
        }
    }
}