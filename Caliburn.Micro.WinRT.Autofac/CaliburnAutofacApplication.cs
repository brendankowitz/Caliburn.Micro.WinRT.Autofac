using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Windows.ApplicationModel.Activation;
using Autofac;
using Autofac.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Diagnostics;
using Windows.UI.Xaml.Navigation;
using Caliburn.Micro.WinRT.Autofac.StorageHandlers;

namespace Caliburn.Micro.WinRT.Autofac
{
    public abstract class CaliburnAutofacApplication : CaliburnApplication, IActivateComponent
    {
        protected IContainer Container;
        private readonly ContainerBuilder _builder;
        private AutofacFrameAdapter _frameAdapter;
        private Frame _rootFrame;
        readonly IDictionary<WeakReference, ILifetimeScope> _viewsToScope = new Dictionary<WeakReference, ILifetimeScope>();
        private static Type[] _exportedTypeCache;
        private StorageCoordinator _storageCoordinator;
        protected object NavigationContext { get; set; }

        public event Action<object> Activated = _ => { };
        protected ISharingService SharingService { get; private set; }

        protected CaliburnAutofacApplication()
        {
            _builder = new ContainerBuilder();
        }

        protected override void Configure()
        {
            _builder.RegisterType<EventAggregator>().As<IEventAggregator>().SingleInstance();
            _builder.Register(x => _frameAdapter).As<INavigationService>().SingleInstance();
            _builder.Register(x => Container).As<IContainer>().SingleInstance();

            _builder.RegisterType<SharingService>().As<ISharingService>().SingleInstance();
            _builder.RegisterType<SettingsService>().As<ISettingsService>().SingleInstance();

            _builder.RegisterAssemblyTypes(AssemblySource.Instance.Concat(new[] { typeof(IStorageMechanism).GetTypeInfo().Assembly }).ToArray())
                .AssignableTo<IStorageMechanism>()
                .AsImplementedInterfaces();

            _builder.RegisterType<StorageCoordinator>().AsSelf().SingleInstance();

            RegisterViewModels(x => !string.IsNullOrEmpty(x.Namespace) &&
                x.Namespace.Contains("ViewModels") &&
                    typeof(INotifyPropertyChanged).IsAssignableFrom(x));

            _builder.RegisterAssemblyTypes(AssemblySource.Instance.ToArray())
                .AssignableTo<IStorageHandler>()
                .AsSelf()
                .AsImplementedInterfaces()
                .SingleInstance();

            HandleConfigure(_builder);
            Container = _builder.Build();

            ViewModelLocator.LocateForView = LocateForView;

            _exportedTypeCache = AssemblySource.Instance.SelectMany(a => a.GetExportedTypes()).Where(x => x.IsAssignableTo<FrameworkElement>()).ToArray();
            ViewLocator.LocateTypeForModelType = (modelType, displayLocation, context) =>
            {
                var viewTypeName = modelType.FullName;

                if (Execute.InDesignMode)
                {
                    viewTypeName = ViewLocator.ModifyModelTypeAtDesignTime(viewTypeName);
                }

                viewTypeName = viewTypeName.Substring(
                    0,
                    viewTypeName.IndexOf('`') < 0
                        ? viewTypeName.Length
                        : viewTypeName.IndexOf('`')
                    );

                var viewTypeList = ViewLocator.TransformName(viewTypeName, context);
                var viewType = viewTypeList.Join(_exportedTypeCache, n => n, t => t.FullName, (n, t) => t).FirstOrDefault();

                if (viewType == null)
                    Debug.WriteLine("View not found. Searched: {0}.", string.Join(", ", viewTypeList.ToArray()));

                return viewType;
            };

            SharingService = Container.Resolve<ISharingService>();
            _storageCoordinator = Container.Resolve<StorageCoordinator>();
            _storageCoordinator.Start();

            _rootFrame = CreateApplicationFrame();
        }

        public virtual void RegisterViewModels(Predicate<Type> isViewModel)
        {
            _builder.RegisterAssemblyTypes(AssemblySource.Instance.ToArray())
                .Where(x => isViewModel(x))
                .AsSelf()
                .InstancePerLifetimeScope()
                .OnActivated(x => OnActivated(x));
        }

        protected virtual object LocateForView(object view)
        {
            if (view == null)
                return null;
            var element = view as FrameworkElement;
            if (element != null && element.DataContext != null && (element.DataContext as INotifyPropertyChanged != null))
                return element.DataContext;

            if (_viewsToScope.Keys.Where(x => x.IsAlive).All(x => x.Target != view))
            {
                var scope = Container.BeginLifetimeScope(builder =>
                {
                    builder.RegisterInstance(view)
                        .AsSelf()
                        .AsImplementedInterfaces();
                    if (NavigationContext != null)
                    {
                        builder.RegisterInstance(NavigationContext)
                            .AsSelf()
                            .AsImplementedInterfaces();
                    }
                });
                _viewsToScope.Add(new WeakReference(view), scope);
            }

            return ViewModelLocator.LocateForViewType(view.GetType());
        }

        protected override object GetInstance(Type service, string key)
        {
            var weakKey = _viewsToScope.Keys.FirstOrDefault(x => x.IsAlive && x.Target == _rootFrame.Content);
            var scope = weakKey != null ? _viewsToScope[weakKey] : Container;

            try
            {
                object instance;
                if (string.IsNullOrEmpty(key))
                {
                    if (scope.TryResolve(service, out instance))
                        return instance;
                }
                else
                {
                    //caliburn can ask for a Keyed service without providing the type,
                    //to fullfil this we must scan the actual component registry
                    if (service == null)
                    {
                        var unTyped = Container.ComponentRegistry.Registrations.SelectMany(
                            x => x.Services.OfType<KeyedService>().Where(y => y.ServiceKey as string == key))
                                .FirstOrDefault();
                        service = unTyped.ServiceType;
                    }

                    if (scope.TryResolveNamed(key, service, out instance))
                        return instance;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                if (Debugger.IsAttached) Debugger.Break();
                throw;
            }

            throw new Exception(string.Format("Could not locate any instances of service {0}.", service.Name));
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            var weakKey = _viewsToScope.Keys.FirstOrDefault(x => x.IsAlive && x.Target == _rootFrame.Content);
            var scope = weakKey != null ? _viewsToScope[weakKey] : Container;

            var result = scope.Resolve(typeof(IEnumerable<>).MakeGenericType(service)) as IEnumerable<object>;
            return result;
        }

        protected override void PrepareViewFirst(Frame rootFrame)
        {
            _frameAdapter.BeginNavigationContext += FrameAdapterOnBeginNavigationContext;
            _frameAdapter.EndNavigationContext += FrameAdapterOnEndNavigationContext;
            _rootFrame.Navigating += RootFrameOnNavigating;
        }

        protected sealed override Frame CreateApplicationFrame()
        {
            if (_rootFrame == null)
            {
                _rootFrame = base.CreateApplicationFrame();
                _frameAdapter = new AutofacFrameAdapter(_rootFrame);
            }
            return _rootFrame;
        }

        private void FrameAdapterOnBeginNavigationContext(object sender, NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New)
            {
                NavigationContext = e.Parameter;
            }
        }

        private void FrameAdapterOnEndNavigationContext(object sender, EventArgs e)
        {
            NavigationContext = null;
        }

        private void RootFrameOnNavigating(object sender, NavigatingCancelEventArgs e)
        {
            var page = _rootFrame.Content as Page;

            if (e.NavigationMode == NavigationMode.New)
            {
                _storageCoordinator.Save(StorageMode.Temporary);
            }

            if (!e.Cancel)
            {
                if (page != null && page.NavigationCacheMode == NavigationCacheMode.Disabled)
                {
                    //pages that have a cache mode of disabled won't be visited again
                    var key = _viewsToScope.Keys.SingleOrDefault(x => x.Target == page);
                    if (key != null)
                    {
                        _storageCoordinator.RemoveInstance(page.DataContext);
                        var scope = _viewsToScope[key];
                        scope.Dispose();
                        _viewsToScope.Remove(key);
                    }
                }

                var expired = new List<WeakReference>();
                foreach (var scope in _viewsToScope.Where(scope => scope.Key.IsAlive == false))
                {
                    expired.Add(scope.Key);
                    scope.Value.Dispose();
                }
                foreach (var key in expired)
                {
                    _viewsToScope.Remove(key);
                }
            }
        }


        protected override void BuildUp(object instance)
        {
            Container.InjectProperties(instance);
        }

        public virtual void HandleConfigure(ContainerBuilder builder)
        {
        }

        public void OnActivated(IActivatedEventArgs<object> activation)
        {
            var handle = Activated;
            if (handle != null)
            {
                handle(activation.Instance);
            }
        }

        protected sealed override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Initialise();

            if (args.PreviousExecutionState == ApplicationExecutionState.ClosedByUser ||
                args.PreviousExecutionState == ApplicationExecutionState.NotRunning)
            {
                _storageCoordinator.ClearLastSession();
            }
            HandleLaunched(args);
        }

        protected virtual void HandleLaunched(LaunchActivatedEventArgs args)
        {
            
        }
    }
}
