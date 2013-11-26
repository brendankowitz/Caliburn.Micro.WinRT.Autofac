using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Caliburn.Micro.WinRT.Autofac
{
    public class AutofacFrameAdapter : FrameAdapter
    {
        private readonly bool _treatViewAsLoaded;
        public event EventHandler<NavigationEventArgs> BeginNavigationContext = (s, e) => { };
        public event EventHandler EndNavigationContext = (s, e) => { };

        public AutofacFrameAdapter(Frame frame, bool treatViewAsLoaded = false) : base(frame, treatViewAsLoaded)
        {
            _treatViewAsLoaded = treatViewAsLoaded;
        }

        protected override void OnNavigated(object sender, NavigationEventArgs e)
        {
            BeginNavigationContext(sender, e);

            try
            {
                if (e.Content == null)
                    return;
                ViewLocator.InitializeComponent(e.Content);
                var viewModel = ViewModelLocator.LocateForView(e.Content);
                if (viewModel == null)
                    return;
                var view = e.Content as Page;
                if (view == null)
                    throw new ArgumentException("View '" + e.Content.GetType().FullName + "' should inherit from Page or one of its descendents.");
                if (_treatViewAsLoaded)
                    view.SetValue(View.IsLoadedProperty, true);
                TryInjectParameters(viewModel, e.Parameter);
                ViewModelBinder.Bind(viewModel, view, null);
                var activate = viewModel as IActivate;
                if (activate != null)
                    activate.Activate();
                var viewAware = viewModel as IViewAware;
                if (viewAware != null)
                    View.ExecuteOnLayoutUpdated(view, (s, a) => viewAware.OnViewReady(view));

                //same as base method but excluding the GC.Collect();
            }
            finally
            {
                EndNavigationContext(sender, EventArgs.Empty);
            }
        }
    }
}