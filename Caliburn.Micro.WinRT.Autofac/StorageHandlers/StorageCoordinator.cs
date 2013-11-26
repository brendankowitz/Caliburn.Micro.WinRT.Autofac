using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Caliburn.Micro.WinRT.Autofac.StorageHandlers {
    /// <summary>
    /// Coordinates the saving and loading of objects based on application lifecycle events.
    /// </summary>
    public class StorageCoordinator {
        readonly List<IStorageHandler> handlers = new List<IStorageHandler>();
        readonly List<IStorageMechanism> storageMechanisms;
        readonly List<WeakReference> tracked = new List<WeakReference>();
        StorageMode currentRestoreMode = StorageMode.Any;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageCoordinator"/> class.
        /// </summary>
        /// <param name="storageMechanisms">The storage mechanisms.</param>
        /// <param name="handlers">The handlers.</param>
        public StorageCoordinator(IEnumerable<IStorageMechanism> storageMechanisms, IEnumerable<IStorageHandler> handlers) {
            this.storageMechanisms = storageMechanisms.ToList();

            handlers.Apply(x => AddStorageHandler(x));
            Application.Current.Resuming += (sender, o) =>
            {
                currentRestoreMode = StorageMode.Any;
            };
        }

        /// <summary>
        /// Starts monitoring application and container events.
        /// </summary>
        public void Start() {
            Window.Current.Activated += OnActivated;

            var trackViewModels = Application.Current as IActivateComponent;
            if (trackViewModels != null)
                trackViewModels.Activated += OnContainerActivated;
        }

        /// <summary>
        /// Stops monitoring application and container events.
        /// </summary>
        public void Stop() {
            Window.Current.Activated -= OnActivated;
        }

        /// <summary>
        /// Gets the storage mechanism.
        /// </summary>
        /// <typeparam name="T">The type of storage mechanism to get.</typeparam>
        /// <returns>The storage mechanism.</returns>
        public T GetStorageMechanism<T>() where T : IStorageMechanism {
            return storageMechanisms.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Adds the storage mechanism.
        /// </summary>
        /// <param name="storageMechanism">The storage mechanism.</param>
        public void AddStorageMechanism(IStorageMechanism storageMechanism) {
            storageMechanisms.Add(storageMechanism);
        }

        /// <summary>
        /// Adds the storage handler.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns>Itself</returns>
        public StorageCoordinator AddStorageHandler(IStorageHandler handler) {
            handler.Coordinator = this;
            handler.Configure();
            handlers.Add(handler);
            return this;
        }

        /// <summary>
        /// Gets the storage handler for a paricular instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>The storage handler.</returns>
        public IStorageHandler GetStorageHandlerFor(object instance) {
            return handlers.FirstOrDefault(x => x.Handles(instance));
        }

        /// <summary>
        /// Saves all monitored instances according to the provided mode.
        /// </summary>
        /// <param name="saveMode">The save mode.</param>
        public void Save(StorageMode saveMode) {
            var toSave = tracked.Select(x => x.Target).Where(x => x != null).ToArray();
            var mechanisms = storageMechanisms.Where(x => x.Supports(saveMode));

            mechanisms.Apply(x => x.BeginStoring());

            foreach(var item in toSave) {
                var handler = GetStorageHandlerFor(item);
                handler.Save(item, saveMode);
            }

            mechanisms.Apply(x => x.EndStoring());
        }

        /// <summary>
        /// Restores the specified instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="restoreMode">The restore mode.</param>
        public void Restore(object instance, StorageMode restoreMode = StorageMode.Automatic) {
            var handler = GetStorageHandlerFor(instance);
            if (handler == null) {
                return;
            }

            tracked.Add(new WeakReference(instance));

            handler.Restore(instance, restoreMode == StorageMode.Automatic ? currentRestoreMode : restoreMode);
        }

        public void RemoveInstance(object instance)
        {
            var reference = tracked.FirstOrDefault(x => x.Target == instance);
            if (reference != null)
                tracked.Remove(reference);
        }

        public void ClearLastSession()
        {
            storageMechanisms.Apply(x => x.ClearLastSession());
        }

        void OnActivated(object sender, WindowActivatedEventArgs windowActivatedEventArgs) {
            if (windowActivatedEventArgs.WindowActivationState == CoreWindowActivationState.Deactivated)
                Save(StorageMode.Any);
        }

        void OnContainerActivated(object instance) {
            Restore(instance);
        }
    }
}