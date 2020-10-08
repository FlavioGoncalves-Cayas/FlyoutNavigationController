using FlyoutNavigation;
using Foundation;
using UIKit;

namespace FlyoutNavigationControllerDemo
{
    [Register("AppDelegate")]
    public class AppDelegate : UIResponder, IUIApplicationDelegate
    {
        private UIWindow _window;

        [Export("application:didFinishLaunchingWithOptions:")]
        public bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            _window = new UIWindow(UIScreen.MainScreen.Bounds);
            _window.RootViewController = new MainViewController();
            _window.MakeKeyAndVisible();

            return true;
        }
    }
}

