using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Caliburn.Micro.WinRT.Autofac.StorageHandlers {
    /// <summary>
    /// Stores data in the phone state.
    /// </summary>
    public class WinRTRoamingStorageMechanism : IStorageMechanism {
        private const string SessionStateFilename = "_sessionStateMechanism.json";
        List<string> keys;
        HashSet<string> _removedThisSession = new HashSet<string>();
        private static Dictionary<string,object> _sessionState;
        private static List<Type> _knownTypes = new List<Type>();
        private StorageFolder Storage { get; set; }

        /// <summary>
        /// Provides access to global session state for the current session.  This state is
        /// serialized by <see cref="SaveAsync"/> and restored by
        /// <see cref="RestoreAsync"/>, so values must be serializable by
        /// <see cref="DataContractSerializer"/> and should be as compact as possible.  Strings
        /// and other self-contained data types are strongly recommended.
        /// </summary>
        public static Dictionary<string, object> SessionState
        {
            get { return _sessionState; }
        }

        /// <summary>
        /// List of custom types provided to the <see cref="DataContractSerializer"/> when
        /// reading and writing session state.  Initially empty, additional types may be
        /// added to customize the serialization process.
        /// </summary>
        public static List<Type> KnownTypes
        {
            get { return _knownTypes; }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="WinRTLocalStorageMechanism"/> class.
        /// </summary>
        public WinRTRoamingStorageMechanism()
        {
            _knownTypes.AddRange(AssemblySource.Instance.SelectMany(x => x.GetExportedTypes()));
            AsyncHelpers.RunSync(RestoreAsync);            
        }

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
            if (!SessionState.ContainsKey(key))
            {
                keys.Add(key);
            }

            SessionState[key] = data;
        }

        /// <summary>
        /// Ends the storage transaction.
        /// </summary>
        public void EndStoring()
        {
            SaveAsync();
        }

        /// <summary>
        /// Tries to get the data previously stored with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// true if found; false otherwise
        /// </returns>
        public bool TryGet(string key, out object value) {
            return SessionState.TryGetValue(key, out value);
        }

        /// <summary>
        /// Deletes the data with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        public void Delete(string key) {
            SessionState.Remove(key);
            _removedThisSession.Add(key);
        }

        /// <summary>
        /// Clears the data stored in the last storage transaction.
        /// </summary>
        public void ClearLastSession() {
            if(keys != null) {
                keys.Apply(x => SessionState.Remove(x));
                keys = null;
            }
        }

        public void RegisterSingleton(Type service, string key, Type implementation)
        {
            
        }

        public static async Task SaveAsync()
        {
            try
            {
                var sessionData = new MemoryStream();
                var serializer = new DataContractJsonSerializer(typeof(Dictionary<string, object>), _knownTypes);
                serializer.WriteObject(sessionData, _sessionState);

                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(SessionStateFilename, CreationCollisionOption.ReplaceExisting);
                using (Stream fileStream = await file.OpenStreamForWriteAsync())
                {
                    sessionData.Seek(0, SeekOrigin.Begin);
                    await sessionData.CopyToAsync(fileStream);
                    await fileStream.FlushAsync();
                }
            }
            catch (Exception e)
            {
                throw new SuspensionManagerException(e);
            }
        }

        public static async Task RestoreAsync()
        {
            _sessionState = new Dictionary<String, Object>();

            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(SessionStateFilename);
                using (IInputStream inStream = await file.OpenSequentialReadAsync())
                {
                    var serializer = new DataContractJsonSerializer(typeof(Dictionary<string, object>), _knownTypes);
                    _sessionState = (Dictionary<string, object>)serializer.ReadObject(inStream.AsStreamForRead());
                }
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch
            // ReSharper restore EmptyGeneralCatchClause
            {
            }
        }

    }
}