using System;
using System.Collections.Generic;

using MonoCross.Navigation;
using MonoCross.Touch;
using UIKit;
using CoreGraphics;
using System.Diagnostics;

namespace MonoCross.Touch
{
    public enum TabletLayout
    {
        SinglePane,
        MasterPane
    }

    public static class UIColorExtensions
    {
        public static UIColor ColorFromInt(this uint rgbValue)
        {
            float red = (rgbValue & 0xFF0000) >> 16;
            float green = (rgbValue & 0xFF00) >> 8;
            float blue = rgbValue & 0xFF;
            return UIColor.FromRGB(red / 255, green / 255, blue / 255);
        }

        public static UIColor ColorWithAlphaFromInt(this uint rgbaValue)
        {
            float red = (rgbaValue & 0xFF0000) >> 16;
            float green = (rgbaValue & 0xFF00) >> 8;
            float blue = rgbaValue & 0xFF;
            float alpha = (rgbaValue & 0xFF000000) >> 24;
            return UIColor.FromRGBA(red / 255, green / 255, blue / 255, alpha / 255);
        }

        public static uint IntFromColor(this UIColor color)
        {
            nfloat red, green, blue, alpha;
            color.GetRGBA(out red, out green, out blue, out alpha);
            uint rgbaValue = (uint)(((long)alpha) << 24 | ((long)red) << 16 | ((long)green) << 8 | ((long)blue));
            return rgbaValue;
        }
    }

    /// <summary>
    ///
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MXTouchTabletOptions : System.Attribute
    {
        public MXTouchTabletOptions(TabletLayout tabletLayout)
        {
            TabletLayout = tabletLayout;
        }

        public TabletLayout TabletLayout { get; set; }
        public bool MasterShowsinPotrait
        {
            get { return _masterShowsInPortrait; }
            set { _masterShowsInPortrait = value; }
        }
        private bool _masterShowsInPortrait = false;

        public bool MasterShowsinLandscape
        {
            get { return _masterShowsInLandscape; }
            set { _masterShowsInLandscape = value; }
        }
        private bool _masterShowsInLandscape = true;

        public bool MasterBeforeDetail
        {
            get { return _masterBeforeDetail; }
            set { _masterBeforeDetail = value; }
        }
        private bool _masterBeforeDetail = true;

        public bool AllowDividerResize
        {
            get { return _allowDividerResize; }
            set { _allowDividerResize = value; }
        }
        private bool _allowDividerResize = false;

        public String MasterButtonText
        {
            get { return _masterButtonText; }
            set { _masterButtonText = value; }
        }
        private String _masterButtonText = "Master";
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MXTouchContainerOptions : System.Attribute
    {
        public MXTouchContainerOptions()
        {
            NavigationBarTintColor = UIColor.Brown;
            //NavigationBarTintColor = UIColor.Clear; // navigation bar has to have a color, hence this means leave as default
        }

        public String SplashBitmap { get; set; }

        public uint NavigationBarTint
        {
            get
            {
                return NavigationBarTintColor.IntFromColor();
            }
            set
            {
                NavigationBarTintColor = UIColorExtensions.ColorWithAlphaFromInt(value);
            }
        }

        public UIColor NavigationBarTintColor = UIColor.Clear;

        //public String Icon { get; set; }
    }

    public class MXTouchContainer : MXContainer
    {
        MXTouchNavigation _touchNavigation;
        UIWindow _window;
        UIApplicationDelegate _appDelegate;
        SplashViewController _splashViewController = null;

        private MXTouchContainer(MXApplication theApp, UIApplicationDelegate appDelegate, UIWindow window)
            : base(theApp)
        {
            _appDelegate = appDelegate;
            _touchNavigation = new MXTouchNavigation(_appDelegate);
            _window = window;

            ViewGroups = new List<MXTouchViewGroup>();
        }

        public List<MXTouchViewGroup> ViewGroups { get; private set; }

        private void StartApplication()
        {
            if (_window.Subviews.Length == 0)
            {
                // toss in a temporary view until async initialization is complete
                string bitmapFile = string.Empty;
                MXTouchContainerOptions options = Attribute.GetCustomAttribute(_appDelegate.GetType(), typeof(MXTouchContainerOptions)) as MXTouchContainerOptions;
                if (options != null)
                {
                    bitmapFile = options.SplashBitmap;
                }
                _splashViewController = new SplashViewController(bitmapFile);
				_window.RootViewController = _splashViewController;
                _window.MakeKeyAndVisible();
            }
        }

        public static void Initialize(MXApplication theApp, UIApplicationDelegate appDelegate, UIWindow window)
        {
            // initialize the application and hold a reference for a bit
            MXTouchContainer thisContainer = new MXTouchContainer(theApp, appDelegate, window);
            MXContainer.Instance = thisContainer;

            thisContainer.StartApplication();
        }

        public UINavigationController MasterNavigationController { get { return _touchNavigation.MasterNavigationController; } }
        public UINavigationController DetailNavigationController { get { return _touchNavigation.DetailNavigationController; } }

        public override void Redirect(string url)
        {
            MXTouchContainer.Navigate(null, url);
            CancelLoad = true;
        }

        [Obsolete]
        protected override void OnControllerLoadBegin(IMXController controller)
        {
            Debug.WriteLine("Controller Load Begin");

            if (ControllerLoadBegin != null)
            {
                ControllerLoadBegin(controller);
                return;
            }

            ShowLoading();
        }
        public event Action<IMXController> ControllerLoadBegin;


        protected override void OnSetDefinitions() { }

        protected override void OnControllerLoadComplete(IMXView fromView, IMXController controller, string viewPerspective, string navigatedUri)
        {
            Debug.WriteLine("Controller Load End");

            _appDelegate.InvokeOnMainThread(delegate
            {
                LoadViewForController(fromView, controller, viewPerspective);
                if (ControllerLoadComplete != null)
                    ControllerLoadComplete(controller);
            });
        }
        public event Action<IMXController> ControllerLoadComplete;

        protected override void OnControllerLoadFailed(IMXController controller, Exception ex)
        {
            Debug.WriteLine("Controller Load Failed: " + ex.Message);

            if (ControllerLoadFailed != null)
            {
                ControllerLoadFailed(controller, ex);
                return;
            }

            HideLoading();

            UIApplication.SharedApplication.InvokeOnMainThread(() =>
            {
                UIAlertView alert = new UIAlertView("Load Failed", ex.Message, null, "OK", null);
                alert.Show();
            });
        }
        public event Action<IMXController, Exception> ControllerLoadFailed;

        void ShowLoading()
        {
            /*
            if (_loadingView == null)
                _loadingView = new LoadingView();
			
            _loadingView.Show("Loading...");
            */
        }

        void HideLoading()
        {
            /*
            if (_loadingView != null)
                _loadingView.Hide();
            */
        }

        static bool _firstView = true;

        private void ShowView()
        {
            if (_firstView)
            {
                foreach (var view in _window.Subviews)
                    view.RemoveFromSuperview();

                _firstView = false;
                _window.Add(_touchNavigation.View);
                _window.MakeKeyAndVisible();
            }
        }

        /*
        public static IMXController NavigateFromButton(string url, Dictionary<string, string> parameters, UIBarButtonItem button)
        {
            //_stashButton = button;
        a
            return Navigate(url, parameters);
        }
        */

        void LoadViewForController(IMXView fromView, IMXController controller, string viewPerspective)
        {
            HideLoading();

            // get the view, create it if it has yet been created
            IMXView view = Views.GetOrCreateView(controller.ModelType, viewPerspective);
            if (view == null)
            {
                Debug.WriteLine("View not found for perspective!" + viewPerspective.ToString());
                throw new ArgumentException("View creation failed for perspective!" + viewPerspective.ToString());
            }

            // asign the view it's model and render the contents of the view
            view.SetModel(controller.GetModel());

            // pull the type from the view
            ViewNavigationContext navigationContext = MXTouchNavigation.GetViewNavigationContext(view);
            UIViewController viewController = view as UIViewController;

            if (navigationContext == ViewNavigationContext.InContext)
            {
                view.Render();

                // it's just an in-context view, just slap it on top of the view that navigated it here!
                UIViewController parentViewController = fromView as UIViewController;
                parentViewController.NavigationController.PushViewController(viewController, true);
            }
            else
            {
                // if the view is one of the views in the group
                MXTouchViewGroup viewGroup = null;
                MXTouchViewGroupItem viewGroupItem = null;

                foreach (MXTouchViewGroup vg in ViewGroups)
                {
                    var viewType = view.GetType();
                    // Check the type itself and its interfaces
                    viewGroupItem = vg.Items.Find(item => item.ViewType == viewType || viewType.GetInterface(item.ViewType.ToString()) != null);

                    if (viewGroupItem != null)
                    {
                        viewGroup = vg;
                        break;
                    }
                }

                if (viewGroup != null)
                {
                    // activate the group!
                    _touchNavigation.PushToViewGroup(viewGroup, viewGroupItem, viewController);
                }
                else
                {
                    view.Render();

                    switch (navigationContext)
                    {
                        case ViewNavigationContext.Master:
                            _touchNavigation.PushToMaster(viewController);
                            break;
                        case ViewNavigationContext.Detail:
                            _touchNavigation.PushToDetail(viewController);
                            break;
                        case ViewNavigationContext.Modal:
                            _touchNavigation.PushToModel(viewController);
                            break;
                    }
                }
            }

            // handle initial view display if not already handled
            ShowView();
        }
    }
}

