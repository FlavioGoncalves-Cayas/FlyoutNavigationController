using System;
using CoreGraphics;
using FlyoutNavigation;
using UIKit;

namespace FlyoutNavigationControllerDemo
{
    public class ContentViewController : UIViewController
    {
        private string _text;
        private FlyoutNavigationController _navigation;
        private string _title;
        private UILabel _label;

        public ContentViewController(FlyoutNavigationController navigation, string title, string text)
        {
            _navigation = navigation;
            _text = text;
            _title = title;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            NavigationItem.LeftBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Action, (s, e) => _navigation.ToggleMenu());

            Title = _title;

            View.BackgroundColor = UIColor.White;

            _label = new UILabel
            {
                Text = _text,
                TextAlignment = UITextAlignment.Center,
            };

            View.AddSubview(_label);
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            _label.Frame = new CGRect(10, View.Bounds.Height / 2 - 15, View.Bounds.Width - 20, 30);
        }
    }
}
