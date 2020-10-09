//  Copyright 2011  Clancey
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Diagnostics;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace FlyoutNavigation
{
    public enum FlyOutNavigationPosition
    {
        Left = 0, // default
        Right
    };

    [Register("FlyoutNavigationController")]
    public class FlyoutNavigationController : UIViewController
    {
        const float sidebarFlickVelocity = 1000.0f;
        const int menuWidth = 280;

        private UIButton closeButton;
        FlyOutNavigationPosition position;
        UIView shadowView;
        nfloat startX;
        bool hideShadow;
        private bool AlreadyLayedOut = false;
        protected UIView menuBorder;
        protected UIColor menuBorderColor = UIColor.LightGray;
        protected bool showMenuBorder = false;
        private bool _prevIsOpen = false;
        OpenMenuGestureRecognizer closeGesture;
        UIScreenEdgePanGestureRecognizer openGesture;

        public event UITouchEventArgs ShouldReceiveTouch;
        public event EventHandler BeginAnimation;
        public event EventHandler EndAnimation;
        public event EventHandler OpenChanged;

        public bool DisableGesture { get; set; }

        public UIViewController NavigationViewController { get; private set; }

        public UIColor MenuBorderColor
        {
            get { return menuBorderColor; }
            set
            {
                menuBorderColor = value;
                if (menuBorder != null)
                {
                    menuBorder.BackgroundColor = menuBorderColor;
                    menuBorder.SetNeedsDisplay();
                }
            }
        }

        public bool ShowMenuBorder
        {
            get { return showMenuBorder; }
            set { showMenuBorder = value; }
        }

        public FlyOutNavigationPosition Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
                shadowView.Layer.ShadowOffset = new CGSize(Position == FlyOutNavigationPosition.Left ? -5 : 5, -1);
                if (openGesture != null)
                    openGesture.Edges = position == FlyOutNavigationPosition.Left ? UIRectEdge.Left : UIRectEdge.Right;
            }
        }

        public bool AlwaysShowLandscapeMenu { get; set; }

        public bool ForceMenuOpen { get; set; }

        public bool NavigationOpenedByLandscapeRotation { get; private set; }

        public bool HideShadow
        {
            get { return hideShadow; }
            set
            {
                if (value == hideShadow)
                    return;
                hideShadow = value;
                if (hideShadow)
                {
                    if (mainView != null)
                        View.InsertSubviewBelow(shadowView, mainView);
                }
                else
                {
                    shadowView.RemoveFromSuperview();
                }

            }
        }

        public UIColor ShadowViewColor
        {
            get { return new UIColor(shadowView.Layer.BackgroundColor); }
            set { shadowView.Layer.BackgroundColor = value.CGColor; }
        }

        public UIViewController CurrentViewController { get; private set; }

        UIView mainView
        {
            get
            {
                if (CurrentViewController == null)
                    return null;
                return CurrentViewController.View;
            }
        }

        public bool IsOpen
        {
            get
            {
                if (Position == FlyOutNavigationPosition.Left)
                {
                    return mainView.Frame.X == menuWidth;
                }
                else
                {
                    return mainView.Frame.X == -menuWidth;
                }
            }
            set
            {
                if (value)
                    HideMenu();
                else
                    ShowMenu();
            }
        }

        bool ShouldStayOpen
        {
            get
            {
                if (ForceMenuOpen || (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad &&
                    AlwaysShowLandscapeMenu &&
                    (InterfaceOrientation == UIInterfaceOrientation.LandscapeLeft
                        || InterfaceOrientation == UIInterfaceOrientation.LandscapeRight)))
                    return true;
                return false;
            }
        }

        public bool DisableRotation { get; set; }

        public override bool ShouldAutomaticallyForwardRotationMethods
        {
            get { return true; }
        }

        public FlyoutNavigationController(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        public FlyoutNavigationController(UIViewController navigationController)
        {
            Initialize(navigationController);
        }

        void Initialize(UIViewController navigationController = null)
        {
            NavigationViewController = navigationController ?? new UIViewController();
            NavigationViewController.View.AccessibilityIdentifier = "FlyoutMenu";

            var navFrame = NavigationViewController.View.Frame;
            navFrame.Width = menuWidth;
            if (Position == FlyOutNavigationPosition.Right)
                navFrame.X = mainView.Frame.Width - menuWidth;
            NavigationViewController.View.Frame = navFrame;

            View.AddSubview(NavigationViewController.View);
            AddChildViewController(NavigationViewController);

            shadowView = new UIView() { AccessibilityLabel = "flyOutShadowLayeLabel", IsAccessibilityElement = true };
            shadowView.BackgroundColor = UIColor.Clear;
            shadowView.Layer.ShadowOffset = new CGSize(Position == FlyOutNavigationPosition.Left ? -5 : 5, -1);
            shadowView.Layer.ShadowColor = UIColor.Black.CGColor;
            shadowView.Layer.ShadowOpacity = .75f;

            closeButton = new UIButton();
            closeButton.AccessibilityHint = closeButton.AccessibilityIdentifier = "CloseMenu";
            closeButton.AccessibilityLabel = "Close Menu";
            closeButton.TouchUpInside += CloseButtonTapped;

            AlwaysShowLandscapeMenu = true;
            NavigationOpenedByLandscapeRotation = false;

            View.AddGestureRecognizer(openGesture = new UIScreenEdgePanGestureRecognizer(() => DragContentView(openGesture)) { Edges = Position == FlyOutNavigationPosition.Left ? UIRectEdge.Left : UIRectEdge.Right });
            View.AddGestureRecognizer(closeGesture = new OpenMenuGestureRecognizer(DragContentView, shouldReceiveTouch));
        }

        void CloseButtonTapped(object sender, EventArgs e)
        {
            HideMenu();
        }

        internal bool shouldReceiveTouch(UIGestureRecognizer gesture, UITouch touch)
        {
            if (gesture == closeGesture && !IsOpen)
                return false;

            if (DisableGesture)
                return false;
            if (ShouldReceiveTouch != null)
                return ShouldReceiveTouch(gesture, touch);
            return true;
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();
            CGRect navFrame = GetViewBounds();

            navFrame.Width = menuWidth;
            if (Position == FlyOutNavigationPosition.Right)
                navFrame.X = mainView.Frame.Width - menuWidth;
            if (NavigationViewController.View.Frame != navFrame)
                NavigationViewController.View.Frame = navFrame;

            if (!AlreadyLayedOut)
            {
                AlreadyLayedOut = true;

                if (AlwaysShowLandscapeMenu && (InterfaceOrientation == UIInterfaceOrientation.LandscapeRight || InterfaceOrientation == UIInterfaceOrientation.LandscapeLeft))
                    NavigationOpenedByLandscapeRotation = true;

                if (showMenuBorder)
                {
                    DisplayMenuBorder(mainView.Frame);
                }
            }
        }

        protected virtual void OnBeginAnimation(EventArgs e)
        {
            BeginAnimation?.Invoke(this, e);
        }

        protected virtual void OnEndAnimation(EventArgs e)
        {
            EndAnimation?.Invoke(this, e);
        }

        protected void CheckRaiseOpenChanged()
        {
            OnEndAnimation(EventArgs.Empty);
            if (_prevIsOpen != IsOpen)
            {
                OnOpenChanged(new OpenChangedEventArgs(IsOpen));
                _prevIsOpen = IsOpen;
            }
        }

        protected virtual void OnOpenChanged(OpenChangedEventArgs e)
        {
            OpenChanged?.Invoke(this, e);
        }

        public void DragContentView(UIGestureRecognizer gesture)
        {
            if (ShouldStayOpen || mainView == null)
                return;

            if (!HideShadow)
                View.InsertSubviewBelow(shadowView, mainView);

            NavigationViewController.View.Hidden = false;

            CGRect frame = mainView.Frame;
            shadowView.Frame = frame;

            var panGesture = gesture as UIPanGestureRecognizer;
            var translation = panGesture.TranslationInView(View).X;

            if (panGesture.State == UIGestureRecognizerState.Began)
            {
                OnBeginAnimation(EventArgs.Empty);
                startX = frame.X;
            }
            else if (panGesture.State == UIGestureRecognizerState.Changed)
            {
                frame.X = translation + startX;
                if (Position == FlyOutNavigationPosition.Left)
                {
                    if (frame.X < 0)
                        frame.X = 0;
                    else if (frame.X > menuWidth)
                        frame.X = menuWidth;
                }
                else
                {
                    if (frame.X > 0)
                        frame.X = 0;
                    else if (frame.X < -menuWidth)
                        frame.X = -menuWidth;
                }
                SetLocation(frame);
            }
            else if (panGesture.State == UIGestureRecognizerState.Ended)
            {
                var velocity = panGesture.VelocityInView(View).X;
                var newX = translation + startX;
                bool show = Math.Abs(velocity) > sidebarFlickVelocity ? velocity > 0 : newX > (menuWidth / 2);
                if (Position == FlyOutNavigationPosition.Right)
                {
                    show = Math.Abs(velocity) > sidebarFlickVelocity ? velocity < 0 : newX < -(menuWidth / 2);
                }
                if (show)
                {
                    ShowMenu();
                }
                else
                {
                    HideMenu();
                }
            }
            if (!IsOpen && closeButton != null)
                closeButton.RemoveFromSuperview();
        }

        public override void ViewWillAppear(bool animated)
        {
            CGRect navFrame = NavigationViewController.View.Frame;
            navFrame.Width = menuWidth;
            if (Position == FlyOutNavigationPosition.Right)
                navFrame.X = mainView.Frame.Width - menuWidth;
            navFrame.Location = CGPoint.Empty;
            NavigationViewController.View.Frame = navFrame;

            View.BackgroundColor = NavigationViewController.View.BackgroundColor;

            var frame = mainView.Frame;
            setViewSize();
            SetLocation(frame);
            base.ViewWillAppear(animated);
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            NavigationViewController.View.Hidden = false;
            var frame = mainView.Frame;
            setViewSize();
            SetLocation(frame);
        }

        public void SetCurrentViewController(UIViewController viewController)
        {
            bool isOpen = false;

            if (mainView != null)
            {
                mainView.RemoveFromSuperview();
                isOpen = IsOpen;
            }

            CurrentViewController = viewController;

            View.AddSubview(mainView);
            AddChildViewController(CurrentViewController);

            CGRect frame = GetViewBounds();
            if (isOpen || ShouldStayOpen)
                frame.X = Position == FlyOutNavigationPosition.Left ? menuWidth : -menuWidth;

            setViewSize();
            SetLocation(frame);

            if (!ShouldStayOpen)
                HideMenu();
        }

        public void ShowMenu()
        {
            if (mainView == null)
                return;
            EnsureInvokedOnMainThread(delegate
            {
                NavigationViewController.View.Hidden = false;
                shadowView.Frame = mainView.Frame;
                if (!ShouldStayOpen)
                    View.AddSubview(closeButton);
                if (!HideShadow)
                    View.InsertSubviewBelow(shadowView, mainView);
                if (ShowMenuBorder)
                {
                    //menuBorder.Frame = mainView.Frame;
                    //menuBorder.Frame.Width = 1f;
                    View.InsertSubviewBelow(menuBorder, mainView);
                }

            });

            OnBeginAnimation(EventArgs.Empty);
            UIView.Animate(.2, () =>
            {
                UIView.SetAnimationCurve(UIViewAnimationCurve.EaseIn);
                CGRect frame = mainView.Frame;
                frame.X = Position == FlyOutNavigationPosition.Left ? menuWidth : -menuWidth;
                setViewSize();
                SetLocation(frame);
                shadowView.Frame = frame;
            }, showComplete);

        }

        void showComplete()
        {
            CheckRaiseOpenChanged();
        }

        void setViewSize()
        {
            CGRect frame = GetViewBounds();
            frame.Width -= ShouldStayOpen ? menuWidth : 0;

            mainView.Bounds = frame;

            DisplayMenuBorder(mainView.Frame);
        }

        CGRect GetViewBounds()
        {
            CGRect frame = View.Bounds;
            if ((InterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || InterfaceOrientation == UIInterfaceOrientation.LandscapeRight))
            {
                var width = NMath.Max(frame.Width, frame.Height);
                var height = NMath.Min(frame.Width, frame.Height);
                frame.Width = width;
                frame.Height = height;
            }
            return frame;
        }

        void SetLocation(CGRect frame)
        {
            mainView.Layer.AnchorPoint = new CGPoint(.5f, .5f);
            frame.Y = 0;
            if (mainView.Frame.Location == frame.Location)
                return;
            frame.Size = mainView.Frame.Size;
            var center = new CGPoint(frame.Left + frame.Width / 2,
                frame.Top + frame.Height / 2);
            mainView.Center = center;
            shadowView.Center = center;
            closeButton.Frame = mainView.Frame;
            DisplayMenuBorder(frame);

        }

        private void DisplayMenuBorder(CGRect frame)
        {
            if (ShowMenuBorder && menuBorder == null)
            {
                menuBorder = new UIView();
                menuBorder.BackgroundColor = menuBorderColor;

                View.InsertSubviewAbove(menuBorder, mainView);
            }

            if (ShowMenuBorder)
            {
                CGRect borderFrame = new CGRect();
                // MDR 29/08/2014 - Prevent bottom part of border missing momentarily after rotate from landscape to portrait

                borderFrame.Height = UIScreen.MainScreen.Bounds.Height;
                borderFrame.Width = 1f;
                borderFrame.X = frame.X - 1f;
                //borderFrame.X = navigation.View.Frame.Right + 1f;
                borderFrame.Y = 0;
                menuBorder.Frame = borderFrame;
            }
        }

        public void HideMenu()
        {
            if (mainView == null || mainView.Frame.X == 0 || ShouldStayOpen)
            {
                closeButton.RemoveFromSuperview();
                return;
            }

            EnsureInvokedOnMainThread(delegate
            {
                closeButton.RemoveFromSuperview();
                UIView.Animate(.2, () =>
                {
                    UIView.SetAnimationCurve(UIViewAnimationCurve.EaseInOut);
                    CGRect frame = GetViewBounds();
                    frame.X = 0;
                    setViewSize();
                    SetLocation(frame);
                    shadowView.Frame = frame;
                }, hideComplete);
            });
        }

        [Export("animationEnded")]
        void hideComplete()
        {
            shadowView.RemoveFromSuperview();
            NavigationViewController.View.Hidden = true;
            CheckRaiseOpenChanged();
        }

        public void ResignFirstResponders(UIView view)
        {
            if (view.Subviews == null)
                return;
            foreach (UIView subview in view.Subviews)
            {
                if (subview.IsFirstResponder)
                    subview.ResignFirstResponder();
                ResignFirstResponders(subview);
            }
        }

        public void ToggleMenu()
        {
            EnsureInvokedOnMainThread(delegate
            {
                if (!IsOpen && CurrentViewController != null && CurrentViewController.IsViewLoaded)
                    ResignFirstResponders(CurrentViewController.View);
                if (IsOpen)
                {
                    HideMenu();
                    NavigationOpenedByLandscapeRotation = false;
                }
                else
                    ShowMenu();
            });
        }

        public override bool ShouldAutorotateToInterfaceOrientation(UIInterfaceOrientation toInterfaceOrientation)
        {
            if (DisableRotation)
                return toInterfaceOrientation == InterfaceOrientation;

            UIInterfaceOrientationMask mask = CurrentViewController.GetSupportedInterfaceOrientations();
            UIInterfaceOrientation orientation = CurrentViewController.PreferredInterfaceOrientationForPresentation();

            bool theReturn = CurrentViewController == null
                ? true
                : CurrentViewController.ShouldAutorotateToInterfaceOrientation(toInterfaceOrientation);

            if (CurrentViewController != null)
                Debug.WriteLine("Should auto rotate: " + toInterfaceOrientation.ToString() + ": " + theReturn);
            else
                Debug.WriteLine("Should auto rotate: View is null");

            return theReturn;
        }

        public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
        {
            if (CurrentViewController != null)
                return CurrentViewController.GetSupportedInterfaceOrientations();
            return UIInterfaceOrientationMask.All;
        }

        public override void WillRotate(UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            base.WillRotate(toInterfaceOrientation, duration);
        }

        public override void DidRotate(UIInterfaceOrientation fromInterfaceOrientation)
        {
            base.DidRotate(fromInterfaceOrientation);

            // mribbons@github - 28/08/2014 - Fix menu width size chunk of shadowView missing when rotating to portait mode
            shadowView.Frame = mainView.Frame;

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
                return;

            // mribbons@github - 28/08/2014 - Only do this is should stay open is false. 
            // Note that this doesn't seem to work well anyway, menu shows and hides, or doesn't hide when switching to portrait (can't recall which but depends how AlwaysShowLandscapeMenu and ForceMenuOpen are set)
            if (AlwaysShowLandscapeMenu)
            {
                switch (InterfaceOrientation)
                {
                    case UIInterfaceOrientation.LandscapeLeft:
                    case UIInterfaceOrientation.LandscapeRight:
                        if (!IsOpen)
                        {
                            NavigationOpenedByLandscapeRotation = true;
                            ShowMenu();
                        }
                        return;
                    default:
                        // mribbons@github - 28/08/2014 - Only close the menu if it was opened by rotating
                        if (NavigationOpenedByLandscapeRotation)
                        {
                            NavigationOpenedByLandscapeRotation = false;
                            HideMenu();
                        }
                        else
                        {
                            DisplayMenuBorder(mainView.Frame);
                        }
                        return;
                }
            }
        }

        protected void EnsureInvokedOnMainThread(Action action)
        {
            if (IsMainThread())
            {
                action();
                return;
            }
            BeginInvokeOnMainThread(() =>
                action()
            );
        }

        static bool IsMainThread()
        {
            return NSThread.Current.IsMainThread;
            //return Messaging.bool_objc_msgSend(GetClassHandle("NSThread"), new Selector("isMainThread").Handle);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (ShouldReceiveTouch != null)
                foreach (var d in ShouldReceiveTouch.GetInvocationList())
                    ShouldReceiveTouch -= (UITouchEventArgs)d;

            View.RemoveGestureRecognizer(closeGesture);
            closeButton.TouchUpInside -= CloseButtonTapped;
            closeButton = null;

            if (this.CurrentViewController != null)
            {
                this.CurrentViewController.View.RemoveFromSuperview();
            }
        }

        class UAUIView : UIView
        {
            [Export("accessibilityIdentifier")]
            public string AccessibilityId { get; set; }
        }

        public class OpenChangedEventArgs : EventArgs
        {
            public OpenChangedEventArgs(bool isOpen) { IsOpen = isOpen; }
            public bool IsOpen { get; set; }
        }
    }
}