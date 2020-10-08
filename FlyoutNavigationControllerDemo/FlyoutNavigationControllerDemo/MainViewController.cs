using System;
using FlyoutNavigation;
using MonoTouch.Dialog;
using UIKit;

namespace FlyoutNavigationControllerDemo
{
    public class MainViewController : UIViewController
    {
		private FlyoutNavigationController _navigation;

        public MainViewController()
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

			_navigation = new FlyoutNavigationController(UITableViewStyle.Grouped)
			{
				// Create the navigation menu
				NavigationRoot = new RootElement("Navigation")
				{
					new Section("Animals")
					{
						new MyStringElement("Cat"),
						new MyStringElement("Dog"),
						new MyStringElement("Horse"),
					},
					new Section("Vegetables")
					{
						new MyStringElement("Cucumber"),
						new MyStringElement("Lettuce"),
						new MyStringElement("Pepper")
					},
				},
			};
			// Supply view controllers corresponding to menu items:
			_navigation.ViewControllers = new[]
			{
					new UINavigationController(new ContentViewController(_navigation, "Cat", "Cat")),
					new UINavigationController(new ContentViewController(_navigation, "Dog", "Dog")),
					new UINavigationController(new ContentViewController(_navigation, "Horse", "Horse")),
					new UINavigationController(new ContentViewController(_navigation, "Cucumber", "Cucumber")),
					new UINavigationController(new ContentViewController(_navigation, "Lettuce", "Lettuce")),
					new UINavigationController(new ContentViewController(_navigation, "Pepper", "Pepper"))
			};

			// Specify navigation position
			_navigation.Position = FlyOutNavigationPosition.Left;
			_navigation.NavigationTableView.BackgroundColor = UIColor.GroupTableViewBackgroundColor;
			_navigation.View.Frame = UIScreen.MainScreen.Bounds;

			View.AddSubview(_navigation.View);
			AddChildViewController(_navigation);
		}

        private class MyStringElement : StringElement
        {
            public MyStringElement(string title) : base(title)
            {
            }

            public override UITableViewCell GetCell(UITableView tv)
            {
                var cell = base.GetCell(tv);
				cell.SelectionStyle = UITableViewCellSelectionStyle.Gray;
				return cell;
            }
        }
    }
}
