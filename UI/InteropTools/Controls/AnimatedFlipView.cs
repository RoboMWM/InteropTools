﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace InteropTools.Controls
{
    public class AnimatedFlipView : FlipView
    {
        private ScrollViewer _scrollViewer;
        //private ScrollViewerOffsetMediator _mediator;

        private bool _isRunningAnimation;
        private bool _reverseAnimation;

        public AnimatedFlipView()
        {
            DefaultStyleKey = typeof(AnimatedFlipView);

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_scrollViewer != null)
            {
                DispatcherTimer timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(3)
                };
                timer.Tick += (o, o1) =>
                {
                    if (!_isRunningAnimation)
                    {
                        Storyboard sb = new Storyboard();

                        DoubleAnimation nextItemAnimation = new DoubleAnimation
                        {
                            EnableDependentAnimation = true,
                            Duration = new Duration(TimeSpan.FromSeconds(10)),
                            From = _scrollViewer.HorizontalOffset,
                            To = _reverseAnimation ? _scrollViewer.HorizontalOffset - 1 : _scrollViewer.HorizontalOffset + 1,
                            FillBehavior = FillBehavior.HoldEnd,
                            EasingFunction = new ExponentialEase
                            {
                                EasingMode = EasingMode.EaseOut
                            }
                        };

                        Storyboard.SetTarget(nextItemAnimation, _scrollViewer);
                        Storyboard.SetTargetProperty(nextItemAnimation, "HorizontalOffset");

                        sb.Children.Add(nextItemAnimation);

                        sb.Completed += (sender1, o2) =>
                        {
                            if (_reverseAnimation)
                            {
                                if (SelectedIndex > 0)
                                {
                                    SelectedIndex--;
                                }

                                if (SelectedIndex == 0)
                                {
                                    _reverseAnimation = false;
                                }
                            }
                            else
                            {
                                if (Items != null && SelectedIndex < Items.Count - 1)
                                {
                                    SelectedIndex++;
                                }

                                if (Items != null && SelectedIndex == Items.Count - 1)
                                {
                                    _reverseAnimation = true;
                                }
                            }

                            _isRunningAnimation = false;
                        };

                        sb.Begin();

                        _isRunningAnimation = true;
                    }
                };
                timer.Start();
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _scrollViewer = GetTemplateChild("ScrollingHost") as ScrollViewer;
            if (_scrollViewer == null)
            {
                throw new NullReferenceException("ScrollingHost and Mediator must not be null.");
            }

            SelectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            _scrollViewer.ScrollToHorizontalOffset(_reverseAnimation
                                                                    ? (_scrollViewer.HorizontalOffset - 1)
                                                                    : (_scrollViewer.HorizontalOffset + 1));
        }
    }

    /// <summary>
    /// Mediator that forwards Offset property changes on to a ScrollViewer
    /// instance to enable the animation of Horizontal/VerticalOffset.
    /// </summary>
    public class ScrollViewerOffsetMediator : FrameworkElement
    {
        /// <summary>
        /// ScrollViewer instance to forward Offset changes on to.
        /// </summary>
        public ScrollViewer ScrollViewer
        {
            get => (ScrollViewer)GetValue(ScrollViewerProperty);
            set => SetValue(ScrollViewerProperty, value);
        }
        public static readonly DependencyProperty ScrollViewerProperty =
            DependencyProperty.Register(
                "ScrollViewer",
                typeof(ScrollViewer),
                typeof(ScrollViewerOffsetMediator),
                new PropertyMetadata(null, OnScrollViewerChanged));
        private static void OnScrollViewerChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ScrollViewerOffsetMediator mediator = (ScrollViewerOffsetMediator)o;
            ScrollViewer scrollViewer = (ScrollViewer)(e.NewValue);
            if (null != scrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset(mediator.VerticalOffset);
            }
        }

        /// <summary>
        /// VerticalOffset property to forward to the ScrollViewer.
        /// </summary>
        public double VerticalOffset
        {
            get => (double)GetValue(VerticalOffsetProperty);
            set => SetValue(VerticalOffsetProperty, value);
        }
        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.Register(
                "VerticalOffset",
                typeof(double),
                typeof(ScrollViewerOffsetMediator),
                new PropertyMetadata(null, OnVerticalOffsetChanged));

        public static void OnVerticalOffsetChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ScrollViewerOffsetMediator mediator = (ScrollViewerOffsetMediator)o;
            if (null != mediator.ScrollViewer)
            {
                mediator.ScrollViewer.ScrollToVerticalOffset((double)(e.NewValue));
            }
        }
        /// <summary>
        /// Multiplier for ScrollableHeight property to forward to the ScrollViewer.
        /// </summary>
        /// <remarks>
        /// 0.0 means "scrolled to top"; 1.0 means "scrolled to bottom".
        /// </remarks>
        public double ScrollableHeightMultiplier
        {
            get => (double)GetValue(ScrollableHeightMultiplierProperty);
            set => SetValue(ScrollableHeightMultiplierProperty, value);
        }

        public static readonly DependencyProperty ScrollableHeightMultiplierProperty =
            DependencyProperty.Register(
                "ScrollableHeightMultiplier",
                typeof(double),
                typeof(ScrollViewerOffsetMediator),
                new PropertyMetadata(null, OnScrollableHeightMultiplierChanged));

        public static void OnScrollableHeightMultiplierChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ScrollViewerOffsetMediator mediator = (ScrollViewerOffsetMediator)o;
            ScrollViewer scrollViewer = mediator.ScrollViewer;
            if (null != scrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset((double)(e.NewValue) * scrollViewer.ScrollableHeight);
            }
        }

        /// <summary>
        /// HorizontalOffset property to forward to the ScrollViewer.
        /// </summary>
        public double HorizontalOffset
        {
            get => (double)GetValue(HorizontalOffsetProperty);
            set => SetValue(HorizontalOffsetProperty, value);
        }
        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.Register(
                "HorizontalOffset",
                typeof(double),
                typeof(ScrollViewerOffsetMediator),
                new PropertyMetadata(null, OnHorizontalOffsetChanged));

        public static void OnHorizontalOffsetChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ScrollViewerOffsetMediator mediator = (ScrollViewerOffsetMediator)o;
            if (null != mediator.ScrollViewer)
            {
                mediator.ScrollViewer.ScrollToHorizontalOffset((double)(e.NewValue));
            }
        }
        /// <summary>
        /// Multiplier for ScrollableWidth property to forward to the ScrollViewer.
        /// </summary>
        /// <remarks>
        /// 0.0 means "scrolled to left"; 1.0 means "scrolled to right".
        /// </remarks>
        public double ScrollableWidthMultiplier
        {
            get => (double)GetValue(ScrollableWidthMultiplierProperty);
            set => SetValue(ScrollableWidthMultiplierProperty, value);
        }

        public static readonly DependencyProperty ScrollableWidthMultiplierProperty =
            DependencyProperty.Register(
                "ScrollableWidthMultiplier",
                typeof(double),
                typeof(ScrollViewerOffsetMediator),
                new PropertyMetadata(null, OnScrollableWidthMultiplierChanged));

        public static void OnScrollableWidthMultiplierChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ScrollViewerOffsetMediator mediator = (ScrollViewerOffsetMediator)o;
            ScrollViewer scrollViewer = mediator.ScrollViewer;
            if (null != scrollViewer)
            {
                scrollViewer.ScrollToHorizontalOffset((double)(e.NewValue) * scrollViewer.ScrollableWidth);
            }
        }
    }
}