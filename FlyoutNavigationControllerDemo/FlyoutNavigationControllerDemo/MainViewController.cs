using System;
using CoreGraphics;
using FlyoutNavigation;
using MonoTouch.Dialog;
using UIKit;

namespace FlyoutNavigationControllerDemo
{
    public class ButtonViewController : UIViewController
    {
        private readonly FlyoutNavigationController _navigation;
        UIButton _button;

        public ButtonViewController(FlyoutNavigationController navigation)
        {
            _navigation = navigation;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.BackgroundColor = UIColor.Red;
            EdgesForExtendedLayout = UIRectEdge.None;

            _button = new UIButton();
            _button.SetTitle("Test", UIControlState.Normal);
            _button.BackgroundColor = UIColor.Purple;
            _button.TouchUpInside += Button_TouchUpInside;
            base.View.AddSubview(_button);
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            _button.Frame = new CGRect(10, 90, View.Frame.Width - 20, 20);
        }

        private void Button_TouchUpInside(object sender, EventArgs e)
        {
            _navigation.SetCurrentViewController(new UINavigationController(new ContentViewController(_navigation, "Hello World 2", "Hello World 2")));
        }
    }

    public class MainViewController : UIViewController
    {
        private FlyoutNavigationController _navigation;

        public MainViewController()
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var navigationController = new UITabBarController();

            _navigation = new FlyoutNavigationController(navigationController);
            _navigation.SetCurrentViewController(new UINavigationController(new ContentViewController(_navigation, "Hello", "Hello World")));

            navigationController.AddChildViewController(new UINavigationController(new ButtonViewController(_navigation) { Title = "Tab1" }));
            navigationController.AddChildViewController(new UIViewController() { Title = "Tab2" });
            navigationController.AddChildViewController(new UIViewController() { Title = "Tab3" });
            navigationController.AddChildViewController(new UIViewController() { Title = "Tab4" });
            navigationController.AddChildViewController(new UIViewController() { Title = "Tab5" });
            navigationController.AddChildViewController(new UIViewController() { Title = "Tab6" });

            // Specify navigation position
            _navigation.Position = FlyOutNavigationPosition.Right;
            _navigation.View.BackgroundColor = UIColor.GroupTableViewBackgroundColor;
            _navigation.View.Frame = UIScreen.MainScreen.Bounds;

            View.AddSubview(_navigation.View);
            AddChildViewController(_navigation);
        }
    }
}
