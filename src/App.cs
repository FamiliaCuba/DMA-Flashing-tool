using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using IoPath = System.IO.Path;

namespace DmaTool
{
    internal static class EntryPoint
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [STAThread]
        public static void Main()
        {
            try
            {
                try { Payload.Ensure(); }
                catch (Exception ex)
                {
                    try
                    {
                        File.WriteAllText(IoPath.Combine(IoPath.GetTempPath(), "familiacuba_payload.txt"), ex.ToString());
                    }
                    catch { }
                }
                try { SetDllDirectory(AppPaths.Runtime); }
                catch { }
                Application app = new Application();
                MainWindow w = new MainWindow();
                app.Run(w);
            }
            catch (Exception ex)
            {
                try
                {
                    File.WriteAllText(IoPath.Combine(IoPath.GetTempPath(), "familiacuba_crash.txt"), ex.ToString());
                }
                catch { }
                MessageBox.Show(ex.ToString(), "FM Flashing Tool - Error");
            }
        }
    }

    internal sealed class MainWindow : Window
    {
        private const string DiscordUrl = "https://discord.gg/WecrFkBGGx";

        private readonly ContentControl _host = new ContentControl();
        private readonly List<NavItem> _nav = new List<NavItem>();
        private TextBlock _flashStatus;
        private TextBlock _flashWait;
        private Border _flashStatusBox;
        private TextBox _flashConsole;
        private DispatcherTimer _flashTimer;
        private int _flashSecs;
        private int _flashTicks;
        private bool _flashBusy;
        private bool _flashWritePhase;
        private int _flashOflPct;
        private int _flashWriteAt;
        private double _flashShown;
        private Border _flashBarFill;
        private ScaleTransform _flashBarScale;
        private TextBlock _flashPct;
        private GradientStop _flashShine;
        private GradientStop _flashFillA;
        private GradientStop _flashFillB;
        private DropShadowEffect _flashBarGlow;
        private TextBlock _drvStatus;
        private Border _drvStatusBox;
        private string _pageKey = "home";
        private TextBlock _menuCaption;
        private TextBlock _helpTitle;
        private TextBlock _helpBody;
        private Button _helpJoin;
        private FrameworkElement _helpDetails;
        private TextBlock _headerSub;
        private TextBlock _footerSub;
        private TextBlock _brandSub;
        private TextBlock _chromeTitle;
        private Button _langBtn;
        private Ellipse _dmaRing;
        private RotateTransform _dmaRot;
        private DropShadowEffect _dmaGlow;
        private int _dmaSpinMode;
        private TextBlock _dmaStatusText;
        private Border _dmaStatusBox;
        private DmaInfo _dmaLast;
        private TextBlock _spdMbps;
        private TextBlock _spdReads;
        private TextBlock _spdTotal;
        private TextBlock _spdConn;
        private Border _spdDot;
        private TextBlock _spdHint;
        private TextBlock _spdFail;
        private Border _spdConnBox;
        private Button _spdRun;
        private DispatcherTimer _spdTimer;
        private bool _speedStop;
        private bool _speedBusy;
        private bool _aboutDone;
        private string _aboutFull;
        private int _aboutIdx;
        private bool _aboutCursorOn;
        private TextBlock _aboutTerm;
        private DispatcherTimer _aboutType;
        private DispatcherTimer _aboutBlink;

        private sealed class NavItem
        {
            public string Key;
            public Button Btn;
            public Border Badge;
            public TextBlock Title;
            public TextBlock Subtitle;
        }

        public MainWindow()
        {
            Loc.Init();
            Title = Loc.T("app.name");
            Width = 1100;
            Height = 740;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanMinimize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            UseLayoutRounding = true;
            FontFamily = new FontFamily(Theme.FontFamily);
            LoadAppIcon();

            Grid root = new Grid();
            root.RowDefinitions.Add(Row(GridLength.Auto));
            root.RowDefinitions.Add(Row(new GridLength(1, GridUnitType.Star)));
            root.Children.Add(BuildStarfield());
            root.Children.Add(BuildTitleBar());

            Grid body = new Grid();
            Grid.SetRow(body, 1);
            body.ColumnDefinitions.Add(Col(new GridLength(286)));
            body.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));
            body.Children.Add(BuildSidebar());
            body.Children.Add(BuildContent());
            root.Children.Add(body);

            // Clip redondeado del contenido para respetar las esquinas del chrome.
            root.SizeChanged += delegate
            {
                root.Clip = new RectangleGeometry(
                    new Rect(0, 0, root.ActualWidth, root.ActualHeight), 13, 13);
            };

            // Glow en una placa aparte para no rasterizar el starfield cada frame.
            Grid shell = new Grid();
            Border glowPlate = new Border();
            glowPlate.Margin = new Thickness(14);
            glowPlate.CornerRadius = new CornerRadius(15);
            glowPlate.Background = Theme.BgDarkest;
            glowPlate.IsHitTestVisible = false;
            DropShadowEffect glow = Theme.RedGlow(16, 0.32);
            glowPlate.Effect = glow;
            shell.Children.Add(glowPlate);

            Border chrome = new Border();
            chrome.Margin = new Thickness(14);
            chrome.CornerRadius = new CornerRadius(15);
            chrome.Background = Brushes.Transparent;
            chrome.BorderBrush = Theme.AccentBorder;
            chrome.BorderThickness = new Thickness(1.1);
            chrome.Child = root;
            shell.Children.Add(chrome);
            Content = shell;

            Loaded += delegate { StartGlowPulse(glow); };
            Closing += delegate { _speedStop = true; };

            string startKey = "home";
            string[] a = Environment.GetCommandLineArgs();
            if (a.Length > 1)
            {
                string k = a[1].ToLowerInvariant();
                if (k == "flash" || k == "dna" || k == "drivers" || k == "speed" || k == "about")
                    startKey = k;
            }
            Navigate(startKey);
            RefreshDmaStatus(null);
        }

        private void LoadAppIcon()
        {
            try
            {
                string path = IoPath.Combine(AppPaths.BaseDir, "familiacuba.ico");
                if (File.Exists(path))
                    Icon = BitmapFrame.Create(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
            catch { }
        }

        // Starfield tipo proud-kangaroo-29: cielo oscuro + 3 capas de puntos que suben muy lento.
        private UIElement BuildStarfield()
        {
            Grid g = new Grid();
            g.IsHitTestVisible = false;
            Grid.SetRowSpan(g, 2);
            g.ClipToBounds = true;

            Border sky = new Border();
            sky.Background = Theme.NightSky();
            g.Children.Add(sky);

            bool built = false;
            g.SizeChanged += delegate
            {
                if (built || g.ActualWidth < 8 || g.ActualHeight < 8) return;
                built = true;
                double w = g.ActualWidth;
                double h = Math.Max(g.ActualHeight, 640);
                g.Children.Add(StarLayer(w, h, 1.2, 52, 0.22, 80, 1));
                g.Children.Add(StarLayer(w, h, 1.9, 28, 0.32, 120, 2));
                g.Children.Add(StarLayer(w, h, 2.6, 14, 0.42, 170, 3));
            };
            return g;
        }

        private static UIElement StarLayer(double w, double h, double size, int count,
            double opacity, double seconds, int seed)
        {
            Canvas c = new Canvas();
            c.IsHitTestVisible = false;
            int s = seed * 9973;
            int copy;
            for (copy = 0; copy < 2; copy++)
            {
                double yOff = (double)copy * h;
                int i;
                for (i = 0; i < count; i++)
                {
                    s = s * 1103515245 + 12345;
                    double x = (double)((s >> 16) & 0x7fff) / 32767.0 * w;
                    s = s * 1103515245 + 12345;
                    double y = (double)((s >> 16) & 0x7fff) / 32767.0 * h;
                    Ellipse dot = new Ellipse();
                    dot.Width = size;
                    dot.Height = size;
                    dot.Fill = Brushes.White;
                    dot.Opacity = opacity;
                    Canvas.SetLeft(dot, x);
                    Canvas.SetTop(dot, y + yOff);
                    c.Children.Add(dot);
                }
            }

            TranslateTransform t = new TranslateTransform();
            c.RenderTransform = t;
            DoubleAnimation anim = new DoubleAnimation(0, -h, new Duration(TimeSpan.FromSeconds(seconds)));
            anim.RepeatBehavior = RepeatBehavior.Forever;
            t.BeginAnimation(TranslateTransform.YProperty, anim);
            return c;
        }

        private static void StartGlowPulse(DropShadowEffect glow)
        {
            DoubleAnimation blur = new DoubleAnimation(12, 20, new Duration(TimeSpan.FromSeconds(2.4)));
            blur.AutoReverse = true;
            blur.RepeatBehavior = RepeatBehavior.Forever;
            blur.EasingFunction = new SineEase();
            glow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blur);

            DoubleAnimation op = new DoubleAnimation(0.22, 0.42, new Duration(TimeSpan.FromSeconds(2.4)));
            op.AutoReverse = true;
            op.RepeatBehavior = RepeatBehavior.Forever;
            op.EasingFunction = new SineEase();
            glow.BeginAnimation(DropShadowEffect.OpacityProperty, op);
        }

        // ---------- Title bar ----------
        private UIElement BuildTitleBar()
        {
            Border bar = new Border();
            bar.Background = Theme.BgTitleGlass;
            bar.Height = 38;
            bar.CornerRadius = new CornerRadius(13, 13, 0, 0);
            Grid.SetRow(bar, 0);

            DockPanel dp = new DockPanel();
            dp.LastChildFill = false;

            TextBlock title = new TextBlock();
            title.Text = Loc.T("app.name");
            title.Foreground = Theme.TextMuted;
            title.FontSize = 12;
            title.FontWeight = FontWeights.SemiBold;
            title.Margin = new Thickness(14, 0, 0, 0);
            title.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(title, Dock.Left);
            _chromeTitle = title;

            Button close = WindowButton("\u2715", true);
            close.Click += delegate { Close(); };
            DockPanel.SetDock(close, Dock.Right);
            Button min = WindowButton("\u2013", false);
            min.Click += delegate { WindowState = WindowState.Minimized; };
            DockPanel.SetDock(min, Dock.Right);

            dp.Children.Add(title);
            dp.Children.Add(close);
            dp.Children.Add(min);
            bar.Child = dp;

            bar.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    DragMove();
            };
            return bar;
        }

        private Button WindowButton(string glyph, bool danger)
        {
            Button b = new Button();
            b.Content = glyph;
            b.Width = 46;
            b.Height = 38;
            b.Foreground = Theme.TextMuted;
            b.FontSize = 13;
            b.BorderThickness = new Thickness(0);
            b.Background = Brushes.Transparent;
            b.Cursor = Cursors.Hand;
            Brush hover = danger ? Theme.Danger : Theme.BgHover;
            CornerRadius cr = danger ? new CornerRadius(0, 13, 0, 0) : new CornerRadius(0);
            b.Template = Theme.RoundedButtonTemplate(Brushes.Transparent, hover, cr);
            return b;
        }

        // ---------- Sidebar ----------
        private UIElement BuildSidebar()
        {
            Border side = new Border();
            side.Background = Theme.BgSidebarGlass;
            side.BorderBrush = Theme.Stroke;
            side.BorderThickness = new Thickness(0, 0, 1, 0);
            Grid.SetColumn(side, 0);

            DockPanel dp = new DockPanel();
            dp.LastChildFill = true;

            UIElement brand = BuildBrandPlate();
            DockPanel.SetDock(brand, Dock.Top);
            dp.Children.Add(brand);

            TextBlock menu = new TextBlock();
            menu.Text = Loc.T("nav.menu");
            menu.Foreground = Theme.TextFaint;
            menu.FontSize = 11;
            menu.FontWeight = FontWeights.Bold;
            menu.Margin = new Thickness(20, 8, 0, 4);
            _menuCaption = menu;
            DockPanel.SetDock(menu, Dock.Top);
            dp.Children.Add(menu);

            TextBlock footer = new TextBlock();
            footer.Text = "MIT \u2022 v1.0";
            footer.Foreground = Theme.TextFaint;
            footer.FontSize = 11;
            footer.Margin = new Thickness(20, 0, 0, 10);
            DockPanel.SetDock(footer, Dock.Bottom);
            dp.Children.Add(footer);

            UIElement help = BuildHelpCard();
            DockPanel.SetDock(help, Dock.Bottom);
            dp.Children.Add(help);

            UIElement dma = BuildDmaStatus();
            DockPanel.SetDock(dma, Dock.Bottom);
            dp.Children.Add(dma);

            StackPanel items = new StackPanel();
            items.Margin = new Thickness(10, 0, 10, 4);
            items.VerticalAlignment = VerticalAlignment.Top;
            items.Children.Add(NavButton("home", "01"));
            items.Children.Add(NavButton("drivers", "02"));
            items.Children.Add(NavButton("flash", "03"));
            items.Children.Add(NavButton("dna", "04"));
            items.Children.Add(NavButton("speed", "05"));
            items.Children.Add(NavButton("about", "06"));
            dp.Children.Add(items);

            side.Child = dp;
            return side;
        }

        // Marca: anillo de 0/1 girando (SelfMadeSystem/pink-dog-71) en rojo + nombre al lado.
        private UIElement BuildBrandPlate()
        {
            Grid row = new Grid();
            row.Margin = new Thickness(10, 10, 12, 4);
            row.Cursor = Cursors.Hand;
            row.MouseLeftButtonDown += delegate { Navigate("home"); };
            row.ColumnDefinitions.Add(Col(GridLength.Auto));
            row.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));

            UIElement mark = SpinningNumbers(64);
            Grid.SetColumn(mark, 0);
            row.Children.Add(mark);

            StackPanel tx = new StackPanel();
            tx.Margin = new Thickness(10, 0, 0, 0);
            tx.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(tx, 1);

            Viewbox vb = new Viewbox();
            vb.Stretch = Stretch.Uniform;
            vb.StretchDirection = StretchDirection.Both;
            vb.HorizontalAlignment = HorizontalAlignment.Left;
            vb.Height = 30;
            vb.Child = OutlineBrandName("Familia Cuba", 28);
            tx.Children.Add(vb);

            TextBlock sub = new TextBlock();
            sub.Text = Loc.T("brand.sub");
            sub.Foreground = Theme.TextFaint;
            sub.FontSize = 11;
            sub.Margin = new Thickness(0, 2, 0, 0);
            _brandSub = sub;
            tx.Children.Add(sub);
            row.Children.Add(tx);
            return row;
        }

        // Contorno con trazo que recorre las letras (Lissy2019/fat-wasp-24), muy lento y en rojo.
        private static UIElement OutlineBrandName(string text, double em)
        {
            FormattedText ft = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily(Theme.FontFamily), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                em,
                Brushes.White);
            Geometry geo = ft.BuildGeometry(new Point(1, 1));

            LinearGradientBrush stroke = new LinearGradientBrush();
            stroke.StartPoint = new Point(0, 0.5);
            stroke.EndPoint = new Point(1, 0.5);
            stroke.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0x8A, 0x8A), 0));
            stroke.GradientStops.Add(new GradientStop(Color.FromRgb(0xF0, 0x3A, 0x3A), 0.45));
            stroke.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0x4D, 0x4D), 1));
            stroke.Freeze();

            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
            path.Data = geo;
            path.Fill = new SolidColorBrush(Color.FromRgb(0xEC, 0xEC, 0xF0));
            path.Stroke = stroke;
            path.StrokeThickness = 1.2;
            path.StrokeLineJoin = PenLineJoin.Round;
            path.StrokeStartLineCap = PenLineCap.Round;
            path.StrokeEndLineCap = PenLineCap.Round;
            path.StrokeDashCap = PenLineCap.Round;
            DoubleCollection dash = new DoubleCollection();
            dash.Add(18);
            dash.Add(420);
            path.StrokeDashArray = dash;

            DoubleAnimation off = new DoubleAnimation(0, 438, new Duration(TimeSpan.FromSeconds(16)));
            off.RepeatBehavior = RepeatBehavior.Forever;
            path.BeginAnimation(System.Windows.Shapes.Shape.StrokeDashOffsetProperty, off);

            Canvas box = new Canvas();
            box.Width = ft.Width + 4;
            box.Height = ft.Height + 4;
            box.Children.Add(path);
            return box;
        }

        private UIElement SpinningNumbers(double size)
        {
            Grid host = new Grid();
            host.Width = size;
            host.Height = size;
            host.IsHitTestVisible = false;
            List<TextBlock> bits = new List<TextBlock>();
            AddSpinWheel(host, bits, size, 0.19, 16, 16, false, Color.FromRgb(0x9A, 0x18, 0x20), 6.4, 0.55, 1);
            AddSpinWheel(host, bits, size, 0.28, 22, 22, true, Color.FromRgb(0xC0, 0x22, 0x2A), 6.0, 0.70, 2);
            AddSpinWheel(host, bits, size, 0.36, 28, 28, true, Color.FromRgb(0xE0, 0x32, 0x3A), 5.6, 0.82, 3);
            AddSpinWheel(host, bits, size, 0.44, 34, 36, false, Color.FromRgb(0xF0, 0x3A, 0x3A), 5.3, 0.90, 4);
            AddSpinWheel(host, bits, size, 0.52, 40, 44, true, Color.FromRgb(0xFF, 0x5C, 0x5C), 5.0, 0.72, 5);

            DispatcherTimer flip = new DispatcherTimer();
            flip.Interval = TimeSpan.FromMilliseconds(720);
            int seed = 41;
            flip.Tick += delegate
            {
                if (bits.Count == 0) return;
                int n = 0;
                while (n < 7)
                {
                    seed = seed * 1103515245 + 12345;
                    int ix = ((seed >> 16) & 0x7fff) % bits.Count;
                    bits[ix].Text = bits[ix].Text == "1" ? "0" : "1";
                    n++;
                }
            };
            flip.Start();
            return host;
        }

        private static void AddSpinWheel(Grid host, List<TextBlock> bits, double size,
            double radiusFrac, int count, double seconds, bool reverse,
            Color color, double font, double opacity, int seed)
        {
            Grid wheel = new Grid();
            wheel.Width = size;
            wheel.Height = size;
            wheel.RenderTransformOrigin = new Point(0.5, 0.5);
            RotateTransform rot = new RotateTransform();
            wheel.RenderTransform = rot;
            DoubleAnimation spin = new DoubleAnimation(
                reverse ? 360 : 0,
                reverse ? 0 : 360,
                new Duration(TimeSpan.FromSeconds(seconds)));
            spin.RepeatBehavior = RepeatBehavior.Forever;
            rot.BeginAnimation(RotateTransform.AngleProperty, spin);

            SolidColorBrush brush = new SolidColorBrush(color);
            brush.Opacity = opacity;
            brush.Freeze();

            double radius = size * radiusFrac;
            int s = seed * 7919;
            int i;
            for (i = 0; i < count; i++)
            {
                double ang = 360.0 * (double)i / (double)count;
                Grid slot = new Grid();
                slot.Width = size;
                slot.Height = size;
                slot.RenderTransformOrigin = new Point(0.5, 0.5);
                slot.RenderTransform = new RotateTransform(ang);

                TextBlock tb = new TextBlock();
                s = s * 1103515245 + 12345;
                tb.Text = ((s >> 16) & 1) == 0 ? "0" : "1";
                tb.FontFamily = new FontFamily("Consolas");
                tb.FontSize = font;
                tb.Foreground = brush;
                tb.HorizontalAlignment = HorizontalAlignment.Center;
                tb.VerticalAlignment = VerticalAlignment.Center;
                tb.RenderTransform = new TranslateTransform(0, -radius);
                slot.Children.Add(tb);
                wheel.Children.Add(slot);
                bits.Add(tb);
            }
            host.Children.Add(wheel);
        }

        private Button NavButton(string key, string number)
        {
            Grid g = new Grid();
            g.ColumnDefinitions.Add(Col(new GridLength(34)));
            g.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));
            g.Margin = new Thickness(8, 0, 8, 0);

            Border badge = Theme.Badge(number, Theme.BgHover);
            badge.VerticalAlignment = VerticalAlignment.Center;
            badge.HorizontalAlignment = HorizontalAlignment.Left;
            Grid.SetColumn(badge, 0);
            g.Children.Add(badge);

            StackPanel tx = new StackPanel();
            tx.Margin = new Thickness(10, 0, 0, 0);
            tx.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(tx, 1);
            TextBlock tt = new TextBlock();
            tt.Text = Loc.T("nav." + key);
            tt.Foreground = Theme.TextMuted;
            tt.FontSize = 15;
            tt.FontWeight = FontWeights.SemiBold;
            tt.TextWrapping = TextWrapping.NoWrap;
            tt.TextTrimming = TextTrimming.CharacterEllipsis;
            tx.Children.Add(tt);
            TextBlock ts = new TextBlock();
            ts.Text = Loc.T("nav." + key + ".sub");
            ts.Foreground = Theme.TextFaint;
            ts.FontSize = 12;
            ts.TextWrapping = TextWrapping.NoWrap;
            ts.TextTrimming = TextTrimming.CharacterEllipsis;
            tx.Children.Add(ts);
            g.Children.Add(tx);

            Button b = new Button();
            b.Content = g;
            b.Tag = key;
            b.Height = 50;
            b.Margin = new Thickness(0, 1, 0, 1);
            b.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            b.VerticalContentAlignment = VerticalAlignment.Center;
            b.Padding = new Thickness(0);
            b.BorderThickness = new Thickness(0);
            b.Background = Brushes.Transparent;
            b.Cursor = Cursors.Hand;
            b.Template = Theme.RoundedButtonTemplate(Brushes.Transparent, Theme.BgHover, new CornerRadius(8), HorizontalAlignment.Stretch);
            b.Click += delegate { Navigate(key); };

            NavItem item = new NavItem();
            item.Key = key;
            item.Btn = b;
            item.Badge = badge;
            item.Title = tt;
            item.Subtitle = ts;
            _nav.Add(item);
            return b;
        }

        private UIElement BuildHelpCard()
        {
            Border card = new Border();
            card.Background = Theme.BgCard;
            card.BorderBrush = Theme.Stroke;
            card.BorderThickness = new Thickness(1);
            card.CornerRadius = new CornerRadius(10);
            card.Padding = new Thickness(12, 10, 12, 10);
            card.Margin = new Thickness(12, 0, 12, 10);
            card.Cursor = Cursors.Hand;

            StackPanel s = new StackPanel();
            TextBlock h = new TextBlock();
            h.Text = Loc.T("help.title");
            h.Foreground = Theme.TextPrimary;
            h.FontSize = 13;
            h.FontWeight = FontWeights.SemiBold;
            _helpTitle = h;
            s.Children.Add(h);

            StackPanel details = new StackPanel();
            details.MaxHeight = 0;
            details.Opacity = 0;
            details.IsHitTestVisible = false;
            _helpDetails = details;

            TextBlock t = new TextBlock();
            t.Text = Loc.T("help.body");
            t.Foreground = Theme.TextFaint;
            t.FontSize = 11;
            t.TextWrapping = TextWrapping.Wrap;
            t.Margin = new Thickness(0, 6, 0, 10);
            _helpBody = t;
            details.Children.Add(t);

            Button join = Theme.DiscordButton(Loc.T("help.join"));
            join.MinWidth = 0;
            join.Height = 34;
            join.HorizontalAlignment = HorizontalAlignment.Stretch;
            join.Click += delegate { OpenUrl(DiscordUrl); };
            _helpJoin = join;
            details.Children.Add(join);
            s.Children.Add(details);

            card.Child = s;
            card.MouseEnter += delegate { SetHelpOpen(true); };
            card.MouseLeave += delegate { SetHelpOpen(false); };
            return card;
        }

        private void SetHelpOpen(bool open)
        {
            if (_helpDetails == null) return;
            QuadraticEase ease = new QuadraticEase();
            ease.EasingMode = open ? EasingMode.EaseOut : EasingMode.EaseIn;

            DoubleAnimation mh = new DoubleAnimation();
            mh.To = open ? 108 : 0;
            mh.Duration = new Duration(TimeSpan.FromMilliseconds(open ? 280 : 200));
            mh.EasingFunction = ease;
            _helpDetails.BeginAnimation(FrameworkElement.MaxHeightProperty, mh);

            DoubleAnimation op = new DoubleAnimation();
            op.To = open ? 1 : 0;
            op.Duration = new Duration(TimeSpan.FromMilliseconds(open ? 240 : 160));
            _helpDetails.BeginAnimation(UIElement.OpacityProperty, op);
            _helpDetails.IsHitTestVisible = open;
        }

        // ---------- Content + cabecera + pie ----------
        private UIElement BuildContent()
        {
            DockPanel g = new DockPanel();
            Grid.SetColumn(g, 1);
            g.Margin = new Thickness(18, 12, 18, 10);
            g.LastChildFill = true;

            UIElement header = BuildHeader();
            DockPanel.SetDock(header, Dock.Top);
            g.Children.Add(header);

            UIElement footer = BuildFooter();
            DockPanel.SetDock(footer, Dock.Bottom);
            g.Children.Add(footer);

            ScrollViewer sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            sv.Background = Brushes.Transparent;
            sv.BorderThickness = new Thickness(0);
            sv.Padding = new Thickness(0);
            sv.Margin = new Thickness(0, 10, 0, 8);
            sv.Content = _host;
            g.Children.Add(sv);
            return g;
        }

        private UIElement BuildHeader()
        {
            DockPanel g = new DockPanel();
            g.LastChildFill = false;
            g.Height = 36;
            UIElement lang = BuildLangSwitch();
            DockPanel.SetDock(lang, Dock.Right);
            g.Children.Add(lang);
            _headerSub = null;
            return g;
        }

        private UIElement BuildLangSwitch()
        {
            Button b = new Button();
            b.MinWidth = 64;
            b.Height = 28;
            b.Padding = new Thickness(10, 0, 10, 0);
            b.Foreground = Theme.TextPrimary;
            b.FontSize = 12;
            b.FontWeight = FontWeights.Bold;
            b.BorderThickness = new Thickness(0);
            b.Cursor = Cursors.Hand;
            b.Template = Theme.RoundedButtonTemplate(Theme.BgHover, Theme.Accent, new CornerRadius(7));
            _langBtn = b;

            Popup pop = new Popup();
            StackPanel list = new StackPanel();
            list.Margin = new Thickness(4);
            int i;
            for (i = 0; i < Loc.Codes.Length; i++)
            {
                string code = Loc.Codes[i];
                Button item = new Button();
                item.Content = Loc.Labels[i];
                item.Tag = code;
                item.Height = 26;
                item.MinWidth = 56;
                item.Margin = new Thickness(0, 1, 0, 1);
                item.Foreground = Theme.TextPrimary;
                item.FontSize = 12;
                item.FontWeight = FontWeights.Bold;
                item.Cursor = Cursors.Hand;
                item.BorderThickness = new Thickness(0);
                item.Template = Theme.RoundedButtonTemplate(Theme.BgInput, Theme.Accent, new CornerRadius(6));
                item.Click += delegate(object s, RoutedEventArgs e)
                {
                    Button src = (Button)s;
                    string c = (string)src.Tag;
                    pop.IsOpen = false;
                    if (c != Loc.Lang) SetLang(c);
                };
                list.Children.Add(item);
            }

            Border panel = new Border();
            panel.Background = Theme.BgCard;
            panel.BorderBrush = Theme.Stroke;
            panel.BorderThickness = new Thickness(1);
            panel.CornerRadius = new CornerRadius(8);
            panel.Child = list;

            pop.Child = panel;
            pop.PlacementTarget = b;
            pop.Placement = PlacementMode.Bottom;
            pop.StaysOpen = false;
            pop.AllowsTransparency = true;

            b.Click += delegate { pop.IsOpen = !pop.IsOpen; };
            StyleLangChips();
            return b;
        }

        private void StyleLangChips()
        {
            if (_langBtn != null)
                _langBtn.Content = Loc.LabelOf(Loc.Lang) + "  \u25BE";
        }

        private void SetLang(string lang)
        {
            Loc.Set(lang);
            ApplyLanguage();
            Navigate(_pageKey);
        }

        private void ApplyLanguage()
        {
            foreach (NavItem it in _nav)
            {
                it.Title.Text = Loc.T("nav." + it.Key);
                if (it.Subtitle != null) it.Subtitle.Text = Loc.T("nav." + it.Key + ".sub");
            }
            if (_menuCaption != null) _menuCaption.Text = Loc.T("nav.menu");
            if (_helpTitle != null) _helpTitle.Text = Loc.T("help.title");
            if (_helpBody != null) _helpBody.Text = Loc.T("help.body");
            if (_helpJoin != null) _helpJoin.Content = "\uD83D\uDCAC  " + Loc.T("help.join");
            if (_headerSub != null) _headerSub.Text = Loc.T("header.tagline");
            if (_footerSub != null) _footerSub.Text = Loc.T("footer.tagline");
            if (_brandSub != null) _brandSub.Text = Loc.T("brand.sub");
            if (_chromeTitle != null) _chromeTitle.Text = Loc.T("app.name");
            Title = Loc.T("app.name");
            StyleLangChips();
            ApplyDmaStatus(_dmaLast);
        }

        private UIElement BuildFooter()
        {
            StackPanel left = new StackPanel();
            left.Orientation = Orientation.Horizontal;
            left.VerticalAlignment = VerticalAlignment.Center;
            left.Margin = new Thickness(0, 10, 0, 0);
            TextBlock a = new TextBlock();
            a.Text = "Familia Cuba";
            a.Foreground = Theme.TextMuted;
            a.FontSize = 12;
            a.FontWeight = FontWeights.SemiBold;
            left.Children.Add(a);
            TextBlock b = new TextBlock();
            b.Text = Loc.T("footer.tagline");
            b.Foreground = Theme.TextFaint;
            b.FontSize = 12;
            _footerSub = b;
            left.Children.Add(b);
            return left;
        }

        // ---------- Navegacion ----------
        private void Navigate(string key)
        {
            if (key != "flash") StopFlashProgress();
            StopSpeedUi();
            if (key != "about") StopAboutType(true);
            _pageKey = key;
            foreach (NavItem it in _nav)
            {
                bool sel = string.Equals(it.Key, key);
                it.Btn.Template = Theme.RoundedButtonTemplate(
                    sel ? Theme.AccentSoft : Brushes.Transparent,
                    sel ? Theme.AccentSoft : Theme.BgHover,
                    new CornerRadius(8),
                    HorizontalAlignment.Stretch);
                it.Badge.Background = sel ? Theme.Accent : Theme.BgHover;
                it.Title.Foreground = sel ? Theme.TextPrimary : Theme.TextMuted;
            }

            if (key == "flash") _host.Content = ViewFlash();
            else if (key == "dna") _host.Content = ViewDna();
            else if (key == "drivers") _host.Content = ViewDrivers();
            else if (key == "speed") _host.Content = ViewSpeed();
            else if (key == "about") _host.Content = ViewAbout();
            else _host.Content = ViewHome();
        }

        // ---------- Vista: Inicio ----------
        private FrameworkElement ViewHome()
        {
            StackPanel sp = new StackPanel();

            Border hero = Theme.Card();
            StackPanel h = new StackPanel();
            h.Children.Add(Theme.Eyebrow(Loc.T("home.eyebrow")));
            TextBlock t = Theme.Title(Loc.T("home.title"));
            t.Margin = new Thickness(0, 4, 0, 4);
            t.FontSize = 24;
            h.Children.Add(t);
            h.Children.Add(Theme.Label(Loc.T("home.body")));
            WrapPanel btns = new WrapPanel();
            btns.Margin = new Thickness(0, 12, 0, 0);
            Button start = Theme.Button(Loc.T("home.start"), true);
            start.Margin = new Thickness(0, 0, 10, 0);
            start.Click += delegate { Navigate("drivers"); };
            btns.Children.Add(start);
            h.Children.Add(btns);
            hero.Child = h;
            sp.Children.Add(hero);

            UniformGrid grid = new UniformGrid();
            grid.Columns = 2;
            FrameworkElement f1 = FeatureCard("1", Loc.T("home.c1.title"), Loc.T("home.c1.body"), "drivers");
            FrameworkElement f2 = FeatureCard("2", Loc.T("home.c2.title"), Loc.T("home.c2.body"), "flash");
            FrameworkElement f3 = FeatureCard("3", Loc.T("home.c3.title"), Loc.T("home.c3.body"), "dna");
            FrameworkElement f4 = FeatureCard("4", Loc.T("home.c4.title"), Loc.T("home.c4.body"), "speed");
            f1.Margin = new Thickness(0, 0, 8, 16);
            f2.Margin = new Thickness(8, 0, 0, 16);
            f3.Margin = new Thickness(0, 0, 8, 16);
            f4.Margin = new Thickness(8, 0, 0, 16);
            grid.Children.Add(f1);
            grid.Children.Add(f2);
            grid.Children.Add(f3);
            grid.Children.Add(f4);
            sp.Children.Add(grid);
            return sp;
        }

        private FrameworkElement FeatureCard(string number, string title, string desc, string navKey)
        {
            Border card = Theme.Card();
            card.Margin = new Thickness(0);

            StackPanel s = new StackPanel();
            StackPanel top = new StackPanel();
            top.Orientation = Orientation.Horizontal;
            top.Children.Add(Theme.Badge(number, Theme.Accent));
            TextBlock tt = new TextBlock();
            tt.Text = title;
            tt.Foreground = Theme.TextPrimary;
            tt.FontSize = 15;
            tt.FontWeight = FontWeights.SemiBold;
            tt.Margin = new Thickness(10, 0, 0, 0);
            tt.VerticalAlignment = VerticalAlignment.Center;
            top.Children.Add(tt);
            s.Children.Add(top);

            TextBlock d = Theme.Label(desc);
            d.Margin = new Thickness(0, 8, 0, 8);
            d.MinHeight = 36;
            s.Children.Add(d);

            Button link = Theme.Link(Loc.T("home.open"));
            link.Click += delegate { Navigate(navKey); };
            s.Children.Add(link);
            card.Child = s;
            return card;
        }

        // ---------- Vista: Flash ----------
        private FrameworkElement ViewFlash()
        {
            StackPanel sp = new StackPanel();
            sp.Children.Add(PageHead(Loc.T("flash.title"), Loc.T("flash.sub")));

            Grid cols = new Grid();
            cols.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));
            cols.ColumnDefinitions.Add(Col(new GridLength(18)));
            cols.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));

            // Estado seleccionable
            string[] cable = { "ch347_jtag" };
            string[] part = { "xc7a75t" };

            // --- Columna izquierda ---
            StackPanel left = new StackPanel();
            Grid.SetColumn(left, 0);

            Border cPort = Theme.Card();
            StackPanel sPort = new StackPanel();
            sPort.Children.Add(SectionTitle(Loc.T("flash.port")));
            Border tCh = Tile("CH347", "USB-JTAG");
            Border tRs = Tile("RS232", "FTDI HS");
            tCh.MouseLeftButtonUp += delegate { cable[0] = "ch347_jtag"; StyleTile(tCh, true); StyleTile(tRs, false); };
            tRs.MouseLeftButtonUp += delegate { cable[0] = "ft2232"; StyleTile(tCh, false); StyleTile(tRs, true); };
            StyleTile(tCh, true); StyleTile(tRs, false);
            sPort.Children.Add(TileRow(tCh, tRs));
            Button detect = Theme.Button(Loc.T("flash.detect"), false);
            detect.Margin = new Thickness(0, 6, 0, 0);
            detect.MinWidth = 0;
            detect.Height = 32;
            detect.HorizontalAlignment = HorizontalAlignment.Stretch;
            sPort.Children.Add(detect);
            cPort.Padding = new Thickness(12, 8, 12, 8);
            cPort.Margin = new Thickness(0, 0, 0, 8);
            cPort.Child = sPort;
            left.Children.Add(cPort);

            Border cModel = Theme.Card();
            StackPanel sModel = new StackPanel();
            sModel.Children.Add(SectionTitle(Loc.T("flash.model")));
            Border m35 = Tile("35T", "XC7A35T");
            Border m75 = Tile("75T", "XC7A75T");
            Border m100 = Tile("100T", "XC7A100T");
            m35.MouseLeftButtonUp += delegate { part[0] = "xc7a35t"; StyleTile(m35, true); StyleTile(m75, false); StyleTile(m100, false); };
            m75.MouseLeftButtonUp += delegate { part[0] = "xc7a75t"; StyleTile(m35, false); StyleTile(m75, true); StyleTile(m100, false); };
            m100.MouseLeftButtonUp += delegate { part[0] = "xc7a100t"; StyleTile(m35, false); StyleTile(m75, false); StyleTile(m100, true); };
            StyleTile(m35, false); StyleTile(m75, true); StyleTile(m100, false);
            sModel.Children.Add(TileRow(m35, m75, m100));
            cModel.Padding = new Thickness(12, 8, 12, 8);
            cModel.Margin = new Thickness(0, 0, 0, 8);
            cModel.Child = sModel;
            left.Children.Add(cModel);

            Border cFile = Theme.Card();
            StackPanel sFile = new StackPanel();
            sFile.Children.Add(SectionTitle(Loc.T("flash.file")));
            Grid fileRow = new Grid();
            fileRow.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));
            fileRow.ColumnDefinitions.Add(Col(new GridLength(8)));
            fileRow.ColumnDefinitions.Add(Col(GridLength.Auto));
            TextBox fileBox = Theme.TextBox(true);
            fileBox.Text = Loc.T("flash.placeholder");
            fileBox.Foreground = Theme.TextFaint;
            fileBox.Padding = new Thickness(10, 6, 10, 6);
            Grid.SetColumn(fileBox, 0);
            fileRow.Children.Add(fileBox);
            Button pick = Theme.Button(Loc.T("flash.pick"), false);
            pick.MinWidth = 0;
            pick.Height = 32;
            pick.Padding = new Thickness(12, 0, 12, 0);
            Grid.SetColumn(pick, 2);
            fileRow.Children.Add(pick);
            sFile.Children.Add(fileRow);
            TextBlock hash = Theme.Label("");
            hash.Margin = new Thickness(0, 4, 0, 0);
            sFile.Children.Add(hash);
            cFile.Padding = new Thickness(12, 8, 12, 8);
            cFile.Margin = new Thickness(0);
            cFile.Child = sFile;
            left.Children.Add(cFile);

            cols.Children.Add(left);

            // --- Columna derecha ---
            StackPanel right = new StackPanel();
            Grid.SetColumn(right, 2);

            Border cStatus = Theme.Card();
            StackPanel sStatus = new StackPanel();
            sStatus.Children.Add(SectionTitle(Loc.T("flash.status")));
            _flashStatusBox = new Border();
            _flashStatusBox.Background = Theme.BgInput;
            _flashStatusBox.BorderBrush = Theme.Accent;
            _flashStatusBox.BorderThickness = new Thickness(4, 0, 0, 0);
            _flashStatusBox.CornerRadius = new CornerRadius(6);
            _flashStatusBox.Padding = new Thickness(10, 8, 10, 8);
            StackPanel st = new StackPanel();
            _flashStatus = new TextBlock();
            _flashStatus.Text = Loc.T("flash.ready");
            _flashStatus.Foreground = Theme.TextPrimary;
            _flashStatus.FontSize = 14;
            _flashStatus.FontWeight = FontWeights.Bold;
            _flashStatus.TextWrapping = TextWrapping.Wrap;
            st.Children.Add(_flashStatus);
            _flashWait = new TextBlock();
            _flashWait.Text = "";
            _flashWait.Foreground = Theme.TextMuted;
            _flashWait.FontSize = 12;
            _flashWait.Margin = new Thickness(0, 6, 0, 0);
            _flashWait.TextWrapping = TextWrapping.Wrap;
            st.Children.Add(_flashWait);
            _flashStatusBox.Child = st;
            sStatus.Children.Add(_flashStatusBox);
            cStatus.Padding = new Thickness(12, 8, 12, 8);
            cStatus.Margin = new Thickness(0, 0, 0, 8);
            cStatus.Child = sStatus;
            right.Children.Add(cStatus);

            Border cOut = Theme.Card();
            StackPanel sOut = new StackPanel();
            sOut.Children.Add(SectionTitle(Loc.T("flash.output")));
            TextBox console = NewConsole(90);
            _flashConsole = console;
            sOut.Children.Add(console);
            Button clear = Theme.Button(Loc.T("flash.clear"), false);
            clear.Margin = new Thickness(0, 6, 0, 0);
            clear.MinWidth = 0;
            clear.Height = 32;
            clear.Click += delegate { console.Clear(); };
            sOut.Children.Add(clear);
            cOut.Padding = new Thickness(12, 8, 12, 8);
            cOut.Margin = new Thickness(0);
            cOut.Child = sOut;
            right.Children.Add(cOut);

            cols.Children.Add(right);

            Grid actions = new Grid();
            actions.Margin = new Thickness(0, 0, 0, 10);
            actions.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));
            actions.ColumnDefinitions.Add(Col(new GridLength(10)));
            actions.ColumnDefinitions.Add(Col(GridLength.Auto));
            Button start = Theme.Button(Loc.T("flash.start"), true);
            start.Height = 40;
            start.MinWidth = 0;
            start.HorizontalAlignment = HorizontalAlignment.Stretch;
            Grid.SetColumn(start, 0);
            Button cancel = Theme.Button(Loc.T("flash.cancel"), false);
            cancel.Height = 40;
            Grid.SetColumn(cancel, 2);
            actions.Children.Add(start);
            actions.Children.Add(cancel);

            sp.Children.Add(BuildFlashBar());
            sp.Children.Add(actions);
            sp.Children.Add(cols);

            Action<string> log = Logger(console);

            pick.Click += delegate
            {
                Microsoft.Win32.OpenFileDialog d = new Microsoft.Win32.OpenFileDialog();
                d.Filter = Loc.T("flash.filter");
                if (d.ShowDialog() == true)
                {
                    fileBox.Text = d.FileName;
                    fileBox.Foreground = Theme.TextPrimary;
                    hash.Text = FileShortHash(d.FileName);
                }
            };
            detect.Click += delegate
            {
                RunBackground(delegate
                {
                    string found = OpenFpga.AutoDetect(log);
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        if (found == "ft2232")
                        {
                            cable[0] = "ft2232";
                            StyleTile(tCh, false); StyleTile(tRs, true);
                        }
                        else if (found == "ch347_jtag")
                        {
                            cable[0] = "ch347_jtag";
                            StyleTile(tCh, true); StyleTile(tRs, false);
                        }
                    }));
                }, new Control[] { detect, start, pick });
            };
            start.Click += delegate
            {
                string f = fileBox.Text.Trim();
                if (f.Length == 0 || !File.Exists(f)) { log(Loc.T("flash.needfile")); return; }
                StartFlashProgress();
                Action<string> flashLog = delegate(string line)
                {
                    log(line);
                    NoteFlashLine(line);
                };
                RunBackground(delegate
                {
                    int rc = OpenFpga.FlashWithBackup(f, cable[0], part[0], flashLog);
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        StopFlashProgress();
                        if (rc == 0) SetFlashStatus(Loc.T("flash.done"), Theme.Success);
                        else SetFlashStatus(Loc.T("flash.err") + rc + ")", Theme.Danger);
                    }));
                }, new Control[] { start, pick, detect });
            };
            cancel.Click += delegate
            {
                ProcRunner.KillCurrent();
                StopFlashProgress();
                SetFlashStatus(Loc.T("flash.cancelled"), Theme.TextMuted);
                log(Loc.T("flash.user.cancel"));
            };

            return sp;
        }

        private void SetFlashStatus(string text, Brush color)
        {
            if (_flashStatus == null) return;
            _flashStatus.Text = text;
            _flashStatus.Foreground = Theme.TextPrimary;
            if (color == Theme.Success) _flashStatus.Foreground = Theme.Success;
            if (color == Theme.Danger) _flashStatus.Foreground = Theme.Danger;
            if (color == Theme.Accent) _flashStatus.Foreground = Theme.Accent;
            if (_flashStatusBox != null)
            {
                _flashStatusBox.BorderBrush = color;
                _flashStatusBox.Background = (color == Theme.Success) ? Theme.AccentSoft : Theme.BgInput;
            }
            if (_flashWait != null)
            {
                if (color == Theme.Success) _flashWait.Text = Loc.T("flash.done.hint");
                else if (color == Theme.Accent) _flashWait.Text = Loc.T("flash.wait.hint");
                else _flashWait.Text = "";
            }
            if (color == Theme.Success)
            {
                StopFlashShine();
                ColorFlashBar(true, false);
                ApplyFlashPercent(100, true);
            }
            else if (color == Theme.Danger)
            {
                StopFlashShine();
                ColorFlashBar(false, true);
            }
            else if (color != Theme.Accent)
            {
                StopFlashShine();
                ColorFlashBar(false, false);
                ApplyFlashPercent(0, true);
            }
        }

        private FrameworkElement BuildFlashBar()
        {
            StackPanel wrap = new StackPanel();
            wrap.Margin = new Thickness(0, 0, 0, 8);
            TextBlock lab = Theme.Eyebrow(Loc.T("flash.progress"));
            lab.Margin = new Thickness(0, 0, 0, 8);
            wrap.Children.Add(lab);

            DockPanel row = new DockPanel();
            row.LastChildFill = true;
            row.Height = 28;

            TextBlock pct = new TextBlock();
            pct.Text = "0%";
            pct.Foreground = Theme.TextFaint;
            pct.FontSize = 22;
            pct.FontWeight = FontWeights.Bold;
            pct.Margin = new Thickness(14, 0, 0, 0);
            pct.VerticalAlignment = VerticalAlignment.Center;
            pct.MinWidth = 62;
            pct.TextAlignment = TextAlignment.Right;
            _flashPct = pct;
            DockPanel.SetDock(pct, Dock.Right);
            row.Children.Add(pct);

            Border outer = new Border();
            outer.VerticalAlignment = VerticalAlignment.Center;
            DropShadowEffect glow = Theme.RedGlow(12, 0);
            outer.Effect = glow;
            _flashBarGlow = glow;

            Border track = new Border();
            track.Height = 16;
            track.CornerRadius = new CornerRadius(8);
            track.Background = Theme.BgInput;
            track.BorderBrush = Theme.Stroke;
            track.BorderThickness = new Thickness(1);
            track.ClipToBounds = true;

            LinearGradientBrush br = new LinearGradientBrush();
            br.StartPoint = new Point(0, 0.5);
            br.EndPoint = new Point(1, 0.5);
            _flashFillA = new GradientStop(Color.FromRgb(0xB4, 0x16, 0x22), 0);
            _flashFillB = new GradientStop(Color.FromRgb(0xFF, 0x52, 0x52), 0.45);
            _flashShine = new GradientStop(Color.FromArgb(90, 255, 230, 230), -0.2);
            GradientStop end = new GradientStop(Color.FromRgb(0xF0, 0x3A, 0x3A), 1);
            br.GradientStops.Add(_flashFillA);
            br.GradientStops.Add(_flashFillB);
            br.GradientStops.Add(_flashShine);
            br.GradientStops.Add(end);

            Border fill = new Border();
            fill.Background = br;
            fill.CornerRadius = new CornerRadius(7);
            fill.HorizontalAlignment = HorizontalAlignment.Stretch;
            fill.VerticalAlignment = VerticalAlignment.Stretch;
            ScaleTransform sc = new ScaleTransform(0, 1);
            fill.RenderTransform = sc;
            fill.RenderTransformOrigin = new Point(0, 0.5);
            _flashBarScale = sc;
            _flashBarFill = fill;
            track.Child = fill;
            outer.Child = track;
            row.Children.Add(outer);
            wrap.Children.Add(row);
            return wrap;
        }

        private void ApplyFlashPercent(double pct, bool animate)
        {
            if (pct < 0) pct = 0;
            if (pct > 100) pct = 100;
            _flashShown = pct;
            if (_flashBarScale != null)
            {
                double x = pct / 100.0;
                if (animate)
                {
                    DoubleAnimation a = new DoubleAnimation(x, TimeSpan.FromMilliseconds(420));
                    QuadraticEase ease = new QuadraticEase();
                    ease.EasingMode = EasingMode.EaseOut;
                    a.EasingFunction = ease;
                    _flashBarScale.BeginAnimation(ScaleTransform.ScaleXProperty, a);
                }
                else
                {
                    _flashBarScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    _flashBarScale.ScaleX = x;
                }
            }
            if (_flashPct != null)
            {
                int n = (int)Math.Floor(pct + 0.0001);
                if (n > 100) n = 100;
                _flashPct.Text = n.ToString() + "%";
            }
        }

        private void ColorFlashBar(bool ok, bool fail)
        {
            if (_flashFillA == null) return;
            if (ok)
            {
                _flashFillA.Color = Color.FromRgb(0x18, 0x9A, 0x52);
                _flashFillB.Color = Color.FromRgb(0x3B, 0xE0, 0x7D);
                if (_flashShine != null) _flashShine.Color = Color.FromArgb(50, 220, 255, 230);
                if (_flashBarGlow != null)
                {
                    _flashBarGlow.Color = Color.FromRgb(0x3B, 0xE0, 0x7D);
                    _flashBarGlow.Opacity = 0.55;
                }
                if (_flashPct != null) _flashPct.Foreground = Theme.Success;
            }
            else if (fail)
            {
                _flashFillA.Color = Color.FromRgb(0x8A, 0x14, 0x18);
                _flashFillB.Color = Color.FromRgb(0xFF, 0x4D, 0x50);
                if (_flashShine != null) _flashShine.Color = Color.FromArgb(0, 255, 255, 255);
                if (_flashBarGlow != null) _flashBarGlow.Color = Color.FromRgb(0xFF, 0x4D, 0x50);
                if (_flashPct != null) _flashPct.Foreground = Theme.Danger;
            }
            else
            {
                _flashFillA.Color = Color.FromRgb(0xB4, 0x16, 0x22);
                _flashFillB.Color = Color.FromRgb(0xFF, 0x52, 0x52);
                if (_flashShine != null) _flashShine.Color = Color.FromArgb(80, 255, 230, 230);
                if (_flashBarGlow != null) _flashBarGlow.Color = Color.FromRgb(0xFF, 0x2A, 0x2A);
                if (_flashPct != null)
                    _flashPct.Foreground = _flashBusy ? Theme.Accent : Theme.TextFaint;
            }
        }

        private void StartFlashShine()
        {
            if (_flashShine != null)
            {
                DoubleAnimation sh = new DoubleAnimation(-0.2, 1.2, TimeSpan.FromSeconds(1.45));
                sh.RepeatBehavior = RepeatBehavior.Forever;
                _flashShine.BeginAnimation(GradientStop.OffsetProperty, sh);
            }
            if (_flashBarGlow != null)
            {
                _flashBarGlow.BeginAnimation(DropShadowEffect.OpacityProperty, null);
                DoubleAnimation gop = new DoubleAnimation(0.32, 0.68, TimeSpan.FromSeconds(1.15));
                gop.AutoReverse = true;
                gop.RepeatBehavior = RepeatBehavior.Forever;
                _flashBarGlow.BeginAnimation(DropShadowEffect.OpacityProperty, gop);
            }
        }

        private void StopFlashShine()
        {
            if (_flashShine != null)
                _flashShine.BeginAnimation(GradientStop.OffsetProperty, null);
            if (_flashBarGlow != null)
            {
                _flashBarGlow.BeginAnimation(DropShadowEffect.OpacityProperty, null);
                _flashBarGlow.Opacity = _flashBusy ? 0.4 : 0.18;
            }
        }

        private void NoteFlashLine(string line)
        {
            if (line == null) return;
            string low = line.ToLowerInvariant();
            if (low.IndexOf("grabando") >= 0 || low.IndexOf("verif") >= 0 ||
                low.IndexOf("writ") >= 0 || low.IndexOf("eras") >= 0)
            {
                if (!_flashWritePhase)
                {
                    _flashWritePhase = true;
                    _flashWriteAt = _flashTicks;
                }
            }
            int p = ParseOflPercent(line);
            if (p >= 0) _flashOflPct = p;
        }

        private static int ParseOflPercent(string line)
        {
            if (string.IsNullOrEmpty(line)) return -1;
            int p = line.LastIndexOf('%');
            if (p <= 0) return -1;
            int i = p - 1;
            while (i >= 0 && line[i] == ' ') i--;
            int end = i;
            while (i >= 0 && (char.IsDigit(line[i]) || line[i] == '.')) i--;
            if (end <= i) return -1;
            string num = line.Substring(i + 1, end - i);
            double d;
            if (!double.TryParse(num, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d))
                return -1;
            if (d < 0 || d > 100) return -1;
            return (int)Math.Round(d);
        }

        private void StartFlashProgress()
        {
            _flashSecs = 0;
            _flashTicks = 0;
            _flashBusy = true;
            _flashWritePhase = false;
            _flashWriteAt = 0;
            _flashOflPct = -1;
            _flashShown = 0;
            ColorFlashBar(false, false);
            ApplyFlashPercent(0, false);
            StartFlashShine();
            SetFlashStatus(Loc.T("flash.wait.backup") + "  0 s", Theme.Accent);
            if (_flashWait != null) _flashWait.Text = Loc.T("flash.wait.hint");
            if (_flashTimer == null)
            {
                _flashTimer = new DispatcherTimer();
                _flashTimer.Tick += delegate
                {
                    if (!_flashBusy) return;
                    _flashTicks++;
                    _flashSecs = _flashTicks / 10;
                    if (!_flashWritePhase && _flashSecs >= 25)
                    {
                        _flashWritePhase = true;
                        _flashWriteAt = _flashTicks;
                    }
                    double t = _flashTicks / 10.0;
                    double target;
                    if (!_flashWritePhase)
                        target = 36.0 * (1.0 - Math.Exp(-t / 12.0));
                    else
                    {
                        double tw = (_flashTicks - _flashWriteAt) / 10.0;
                        if (tw < 0) tw = 0;
                        target = 38.0 + 56.0 * (1.0 - Math.Exp(-tw / 28.0));
                    }
                    if (_flashOflPct >= 0)
                    {
                        double mapped = _flashWritePhase
                            ? (40.0 + _flashOflPct * 0.55)
                            : (_flashOflPct * 0.38);
                        if (mapped > target) target = mapped;
                    }
                    if (target > 96) target = 96;
                    if (target < 1) target = 1;
                    _flashShown += (target - _flashShown) * 0.18;
                    ApplyFlashPercent(_flashShown, false);
                    if (_flashTicks % 10 == 0)
                    {
                        string phase = _flashWritePhase ? Loc.T("flash.wait.write") : Loc.T("flash.wait.backup");
                        SetFlashStatus(phase + "  " + _flashSecs + " s", Theme.Accent);
                        if (_flashWait != null) _flashWait.Text = Loc.T("flash.wait.hint");
                        if (_flashSecs > 0 && _flashSecs % 15 == 0 && _flashConsole != null)
                            AppendLine(_flashConsole, Loc.T("flash.wait.tick") + _flashSecs + " s");
                    }
                };
            }
            _flashTimer.Interval = TimeSpan.FromMilliseconds(100);
            _flashTimer.Start();
        }

        private void StopFlashProgress()
        {
            _flashBusy = false;
            if (_flashTimer != null) _flashTimer.Stop();
            StopFlashShine();
        }

        // ---------- Vista: DNA / Info ----------
        private FrameworkElement ViewDna()
        {
            StackPanel sp = new StackPanel();
            sp.Children.Add(PageHead(Loc.T("dna.title"), Loc.T("dna.sub")));

            string[] cable = { "ch347_jtag" };
            string[] copyBuf = { "" };

            Border cCab = Theme.Card();
            StackPanel sCab = new StackPanel();
            sCab.Children.Add(SectionTitle(Loc.T("dna.cable")));
            Border tCh = Tile("CH347", "USB-JTAG");
            Border tRs = Tile("RS232", "FTDI HS");
            tCh.MouseLeftButtonUp += delegate { cable[0] = "ch347_jtag"; StyleTile(tCh, true); StyleTile(tRs, false); };
            tRs.MouseLeftButtonUp += delegate { cable[0] = "ft2232"; StyleTile(tCh, false); StyleTile(tRs, true); };
            StyleTile(tCh, true); StyleTile(tRs, false);
            sCab.Children.Add(TileRow(tCh, tRs));
            cCab.Child = sCab;
            sp.Children.Add(cCab);

            Border cVal = Theme.Card();
            StackPanel sVal = new StackPanel();
            sVal.Children.Add(SectionTitle(Loc.T("dna.value")));
            Border box = new Border();
            box.Background = Theme.BgInput;
            box.BorderBrush = Theme.Accent;
            box.BorderThickness = new Thickness(3, 0, 0, 0);
            box.CornerRadius = new CornerRadius(8);
            box.Padding = new Thickness(12, 8, 12, 8);
            StackPanel vx = new StackPanel();
            TextBlock dnaVal = new TextBlock();
            dnaVal.Text = Loc.T("dna.waiting");
            dnaVal.Foreground = Theme.TextPrimary;
            dnaVal.FontFamily = new FontFamily(Theme.FontFamily);
            dnaVal.FontSize = 13;
            dnaVal.FontWeight = FontWeights.SemiBold;
            dnaVal.TextWrapping = TextWrapping.Wrap;
            vx.Children.Add(dnaVal);
            TextBlock dnaMeta = new TextBlock();
            dnaMeta.Text = "";
            dnaMeta.Foreground = Theme.TextMuted;
            dnaMeta.FontSize = 12;
            dnaMeta.Margin = new Thickness(0, 8, 0, 0);
            dnaMeta.TextWrapping = TextWrapping.Wrap;
            vx.Children.Add(dnaMeta);
            box.Child = vx;
            sVal.Children.Add(box);

            Grid actions = new Grid();
            actions.Margin = new Thickness(0, 10, 0, 0);
            actions.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));
            actions.ColumnDefinitions.Add(Col(new GridLength(10)));
            actions.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));
            Button read = Theme.Button(Loc.T("dna.read"), true);
            read.MinWidth = 0;
            read.HorizontalAlignment = HorizontalAlignment.Stretch;
            Grid.SetColumn(read, 0);
            Button copy = Theme.Button(Loc.T("dna.copy"), false);
            copy.MinWidth = 0;
            copy.HorizontalAlignment = HorizontalAlignment.Stretch;
            Grid.SetColumn(copy, 2);
            actions.Children.Add(read);
            actions.Children.Add(copy);
            sVal.Children.Add(actions);
            Button info = Theme.Button(Loc.T("dna.info"), false);
            info.Margin = new Thickness(0, 8, 0, 0);
            info.MinWidth = 0;
            info.HorizontalAlignment = HorizontalAlignment.Stretch;
            sVal.Children.Add(info);
            cVal.Child = sVal;
            sp.Children.Add(cVal);

            Border cOut = Theme.Card();
            StackPanel sOut = new StackPanel();
            sOut.Children.Add(SectionTitle(Loc.T("dna.output")));
            TextBox console = NewConsole(110);
            sOut.Children.Add(console);
            cOut.Child = sOut;
            sp.Children.Add(cOut);

            Action<string> log = Logger(console);
            Action<FpgaDna> showDna = delegate(FpgaDna d)
            {
                if (d != null && d.Ok)
                {
                    string primary = d.Primary();
                    dnaVal.Text = primary;
                    dnaVal.FontFamily = new FontFamily("Consolas");
                    dnaVal.FontSize = 18;
                    dnaVal.FontWeight = FontWeights.Bold;
                    dnaVal.Foreground = Theme.Success;
                    dnaMeta.Text = d.Summary();
                    copyBuf[0] = d.CopyBlock();
                    if (d.Cable == "ft2232")
                    {
                        cable[0] = "ft2232";
                        StyleTile(tCh, false); StyleTile(tRs, true);
                    }
                    else if (d.Cable == "ch347_jtag")
                    {
                        cable[0] = "ch347_jtag";
                        StyleTile(tCh, true); StyleTile(tRs, false);
                    }
                }
                else
                {
                    dnaVal.Text = Loc.T("dna.none");
                    dnaVal.FontFamily = new FontFamily(Theme.FontFamily);
                    dnaVal.FontSize = 13;
                    dnaVal.FontWeight = FontWeights.SemiBold;
                    dnaVal.Foreground = Theme.Danger;
                    dnaMeta.Text = "";
                    copyBuf[0] = "";
                }
            };

            Action doRead = delegate
            {
                dnaVal.Text = Loc.T("dna.waiting");
                dnaVal.FontFamily = new FontFamily(Theme.FontFamily);
                dnaVal.FontSize = 13;
                dnaVal.FontWeight = FontWeights.SemiBold;
                dnaVal.Foreground = Theme.TextPrimary;
                string c = cable[0];
                RunBackground(delegate
                {
                    FpgaDna d = OpenFpga.ReadIdentity(c, log);
                    Dispatcher.BeginInvoke(new Action(delegate { showDna(d); }));
                }, new Control[] { read, copy, info });
            };

            read.Click += delegate { doRead(); };
            copy.Click += delegate
            {
                if (copyBuf[0].Length == 0)
                {
                    log(Loc.T("dna.nocopy"));
                    return;
                }
                try
                {
                    Clipboard.SetText(copyBuf[0]);
                    copy.Content = Loc.T("dna.copied");
                    DispatcherTimer t = new DispatcherTimer();
                    t.Interval = TimeSpan.FromSeconds(1.4);
                    t.Tick += delegate { copy.Content = Loc.T("dna.copy"); t.Stop(); };
                    t.Start();
                }
                catch (Exception ex) { log("ERROR: " + ex.Message); }
            };
            info.Click += delegate
            {
                RunBackground(delegate
                {
                    DmaInfo inf = Leech.Probe(log);
                    Dispatcher.BeginInvoke(new Action(delegate { ApplyDmaStatus(inf); }));
                }, new Control[] { read, copy, info });
            };
            doRead();
            return sp;
        }

        // ---------- Vista: Drivers ----------
        private FrameworkElement ViewDrivers()
        {
            StackPanel sp = new StackPanel();
            string stZ = Drivers.HasZadig() ? Loc.T("drv.ok") : Loc.T("drv.missing");
            string stW = Drivers.HasPackage("wch") ? Loc.T("drv.ok") : Loc.T("drv.missing");
            string stF = Drivers.HasPackage("ftdi") ? Loc.T("drv.ok") : Loc.T("drv.missing");
            sp.Children.Add(PageHead(Loc.T("drv.title"),
                Loc.T("drv.sub") + "  ·  Zadig: " + stZ + "  ·  CH347: " + stW + "  ·  FTDI: " + stF));

            Grid cols = new Grid();
            cols.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));
            cols.ColumnDefinitions.Add(Col(new GridLength(12)));
            cols.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));

            Border c1 = Theme.Card();
            Grid.SetColumn(c1, 0);
            StackPanel s1 = new StackPanel();
            s1.Children.Add(SectionTitle(Loc.T("drv.ch.title")));
            s1.Children.Add(Theme.Label(Loc.T("drv.ch.body")));
            Button ch = Theme.Button(Loc.T("drv.ch.btn"), true);
            ch.Margin = new Thickness(0, 10, 0, 0);
            ch.MinWidth = 0;
            ch.HorizontalAlignment = HorizontalAlignment.Stretch;
            s1.Children.Add(ch);
            Button zadig = Theme.Button(Loc.T("drv.zadig"), false);
            zadig.Margin = new Thickness(0, 8, 0, 0);
            zadig.MinWidth = 0;
            zadig.HorizontalAlignment = HorizontalAlignment.Stretch;
            s1.Children.Add(zadig);
            c1.Child = s1;
            cols.Children.Add(c1);

            Border c2 = Theme.Card();
            Grid.SetColumn(c2, 2);
            StackPanel s2 = new StackPanel();
            s2.Children.Add(SectionTitle(Loc.T("drv.ftdi.title")));
            s2.Children.Add(Theme.Label(Loc.T("drv.ftdi.body")));
            Button ftdi = Theme.Button(Loc.T("drv.ftdi.btn"), true);
            ftdi.Margin = new Thickness(0, 10, 0, 0);
            ftdi.MinWidth = 0;
            ftdi.HorizontalAlignment = HorizontalAlignment.Stretch;
            s2.Children.Add(ftdi);
            c2.Child = s2;
            cols.Children.Add(c2);
            sp.Children.Add(cols);

            Border cOut = Theme.Card();
            StackPanel sOut = new StackPanel();
            sOut.Children.Add(SectionTitle(Loc.T("drv.output")));

            _drvStatusBox = new Border();
            _drvStatusBox.Background = Theme.BgInput;
            _drvStatusBox.BorderBrush = Theme.Accent;
            _drvStatusBox.BorderThickness = new Thickness(4, 0, 0, 0);
            _drvStatusBox.CornerRadius = new CornerRadius(6);
            _drvStatusBox.Padding = new Thickness(14, 12, 14, 12);
            _drvStatusBox.Margin = new Thickness(0, 0, 0, 10);
            _drvStatus = new TextBlock();
            _drvStatus.Text = Loc.T("drv.ready");
            _drvStatus.Foreground = Theme.TextPrimary;
            _drvStatus.FontSize = 16;
            _drvStatus.FontWeight = FontWeights.Bold;
            _drvStatus.TextWrapping = TextWrapping.Wrap;
            _drvStatusBox.Child = _drvStatus;
            sOut.Children.Add(_drvStatusBox);

            TextBlock logBox = new TextBlock();
            logBox.Foreground = Theme.TextMuted;
            logBox.FontFamily = new FontFamily("Consolas");
            logBox.FontSize = 13;
            logBox.TextWrapping = TextWrapping.Wrap;
            logBox.LineHeight = 20;
            Border logFrame = new Border();
            logFrame.Background = Theme.BgInput;
            logFrame.BorderBrush = Theme.Stroke;
            logFrame.BorderThickness = new Thickness(1);
            logFrame.CornerRadius = new CornerRadius(6);
            logFrame.Padding = new Thickness(12);
            logFrame.MinHeight = 140;
            logFrame.Child = logBox;
            sOut.Children.Add(logFrame);
            cOut.Child = sOut;
            sp.Children.Add(cOut);

            Action<string> log = LoggerBlock(logBox);
            ch.Click += delegate { InstallInfFrom("wch", log, new Control[] { ch, ftdi, zadig }); };
            ftdi.Click += delegate { InstallInfFrom("ftdi", log, new Control[] { ch, ftdi, zadig }); };
            zadig.Click += delegate { Drivers.LaunchZadig(log); };
            return sp;
        }

        // ---------- Vista: Speed Test ----------
        private FrameworkElement ViewSpeed()
        {
            StackPanel sp = new StackPanel();
            sp.Children.Add(PageHead(Loc.T("spd.title"), Loc.T("spd.sub")));

            Border c1 = Theme.Card();
            StackPanel s1 = new StackPanel();
            s1.Children.Add(SectionTitle(Loc.T("spd.result")));
            Grid metrics = new Grid();
            metrics.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));
            metrics.ColumnDefinitions.Add(Col(new GridLength(10)));
            metrics.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));
            metrics.ColumnDefinitions.Add(Col(new GridLength(10)));
            metrics.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));
            Border m1 = MetricCard(Loc.T("spd.mbps"), "0.0", out _spdMbps, Theme.Success);
            Border m2 = MetricCard(Loc.T("spd.reads"), "0", out _spdReads, Theme.Accent);
            Border m3 = MetricCard(Loc.T("spd.total"), "0 MB", out _spdTotal, Theme.TextPrimary);
            Grid.SetColumn(m1, 0);
            Grid.SetColumn(m2, 2);
            Grid.SetColumn(m3, 4);
            metrics.Children.Add(m1);
            metrics.Children.Add(m2);
            metrics.Children.Add(m3);
            s1.Children.Add(metrics);
            _spdFail = new TextBlock();
            _spdFail.Text = "";
            _spdFail.Foreground = Theme.TextFaint;
            _spdFail.FontSize = 12;
            _spdFail.Margin = new Thickness(0, 8, 0, 0);
            _spdFail.Visibility = Visibility.Collapsed;
            s1.Children.Add(_spdFail);
            Button run = Theme.Button(Loc.T("spd.run"), true);
            run.Margin = new Thickness(0, 12, 0, 0);
            run.MinWidth = 0;
            run.HorizontalAlignment = HorizontalAlignment.Stretch;
            _spdRun = run;
            s1.Children.Add(run);
            c1.Child = s1;
            sp.Children.Add(c1);

            Button check = Theme.Button(Loc.T("spd.check"), false);
            check.Height = 30;
            check.MinWidth = 0;
            check.FontSize = 12;
            check.Padding = new Thickness(12, 0, 12, 0);
            check.Margin = new Thickness(8, 0, 0, 0);

            _spdConnBox = new Border();
            _spdConnBox.Background = Theme.BgInput;
            _spdConnBox.BorderBrush = Theme.Stroke;
            _spdConnBox.BorderThickness = new Thickness(1);
            _spdConnBox.CornerRadius = new CornerRadius(8);
            _spdConnBox.Padding = new Thickness(10, 4, 6, 4);
            _spdConnBox.Margin = new Thickness(0, 0, 0, 10);
            _spdHint = null;

            Grid connRow = new Grid();
            connRow.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));
            connRow.ColumnDefinitions.Add(Col(GridLength.Auto));

            Grid line = new Grid();
            line.VerticalAlignment = VerticalAlignment.Center;
            line.ColumnDefinitions.Add(Col(GridLength.Auto));
            line.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));
            Border dot = new Border();
            dot.Width = 8;
            dot.Height = 8;
            dot.CornerRadius = new CornerRadius(4);
            dot.Background = Theme.TextFaint;
            dot.VerticalAlignment = VerticalAlignment.Center;
            dot.Margin = new Thickness(0, 0, 8, 0);
            _spdDot = dot;
            Grid.SetColumn(dot, 0);
            line.Children.Add(dot);
            _spdConn = new TextBlock();
            _spdConn.Text = Loc.T("dma.checking");
            _spdConn.Foreground = Theme.TextMuted;
            _spdConn.FontSize = 13;
            _spdConn.FontWeight = FontWeights.SemiBold;
            _spdConn.VerticalAlignment = VerticalAlignment.Center;
            _spdConn.TextWrapping = TextWrapping.NoWrap;
            _spdConn.TextTrimming = TextTrimming.CharacterEllipsis;
            Grid.SetColumn(_spdConn, 1);
            line.Children.Add(_spdConn);
            Grid.SetColumn(line, 0);
            connRow.Children.Add(line);
            Grid.SetColumn(check, 1);
            connRow.Children.Add(check);
            _spdConnBox.Child = connRow;
            sp.Children.Add(_spdConnBox);

            Border cOut = Theme.Card();
            StackPanel sOut = new StackPanel();
            sOut.Children.Add(SectionTitle(Loc.T("spd.output")));
            TextBox console = NewConsole(100);
            sOut.Children.Add(console);
            cOut.Child = sOut;
            sp.Children.Add(cOut);

            Action<string> log = Logger(console);
            check.Click += delegate { RefreshDmaStatus(log); };

            ApplyDmaStatus(_dmaLast);
            RefreshDmaStatus(log);

            run.Click += delegate
            {
                if (_speedBusy)
                {
                    _speedStop = true;
                    return;
                }
                _speedStop = false;
                _speedBusy = true;
                run.Content = Loc.T("spd.stop");
                if (_spdHint != null) _spdHint.Text = Loc.T("spd.hint.run");
                StartSpeedUiTimer();
                RunBackground(delegate
                {
                    Leech.BenchmarkLive(log, delegate { return !_speedStop; });
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        _speedBusy = false;
                        if (_spdRun != null) _spdRun.Content = Loc.T("spd.run");
                        UpdateSpeedUi();
                        RefreshDmaStatus(null);
                    }));
                }, new Control[] { check });
            };
            return sp;
        }

        // ---------- Vista: Acerca de ----------
        private FrameworkElement ViewAbout()
        {
            StopAboutType(false);
            StackPanel sp = new StackPanel();

            Border card = new Border();
            card.Background = Theme.BgCard;
            card.BorderBrush = Theme.Stroke;
            card.BorderThickness = new Thickness(1);
            card.CornerRadius = new CornerRadius(12);
            card.Padding = new Thickness(10);
            card.Margin = new Thickness(0, 0, 0, 10);

            Border wrap = new Border();
            wrap.Background = Theme.BgInput;
            wrap.BorderBrush = Theme.Stroke;
            wrap.BorderThickness = new Thickness(1);
            wrap.CornerRadius = new CornerRadius(8);
            wrap.ClipToBounds = true;

            DockPanel inner = new DockPanel();
            inner.LastChildFill = true;

            Border head = new Border();
            head.Background = FrozenBrush(0x20, 0x24, 0x25);
            head.Height = 36;
            head.Padding = new Thickness(12, 0, 12, 0);
            DockPanel.SetDock(head, Dock.Top);
            DockPanel hd = new DockPanel();
            hd.LastChildFill = true;
            TextBlock tag = new TextBlock();
            tag.Text = ">_  Terminal";
            tag.Foreground = Theme.TextMuted;
            tag.FontSize = 12;
            tag.FontWeight = FontWeights.SemiBold;
            tag.VerticalAlignment = VerticalAlignment.Center;
            hd.Children.Add(tag);
            head.Child = hd;
            inner.Children.Add(head);

            Border body = new Border();
            body.Background = FrozenBrush(0x0A, 0x0A, 0x0C);
            body.Padding = new Thickness(14, 12, 14, 14);
            body.MinHeight = 260;
            TextBlock term = new TextBlock();
            term.FontFamily = new FontFamily("Consolas");
            term.FontSize = 13;
            term.Foreground = FrozenBrush(0xC8, 0xD0, 0xC8);
            term.TextWrapping = TextWrapping.Wrap;
            term.LineHeight = 20;
            _aboutTerm = term;
            body.Child = term;
            inner.Children.Add(body);
            wrap.Child = inner;
            card.Child = wrap;
            sp.Children.Add(card);

            _aboutFull = BuildAboutScript();
            _aboutIdx = 0;
            _aboutCursorOn = true;
            if (_aboutDone)
            {
                PaintAboutTerm();
                StartAboutBlink();
            }
            else StartAboutType();
            return sp;
        }

        private string BuildAboutScript()
        {
            return "$  cat about.txt\n" +
                "#  " + Loc.T("about.sub") + "\n" +
                Loc.T("about.heading") + "\n" +
                Loc.T("about.ver") + "\n" +
                "\n" +
                Loc.T("about.body") + "\n" +
                "\n" +
                "##  " + Loc.T("about.inc.title") + "\n" +
                Loc.T("about.inc.body") + "\n" +
                "\n" +
                "##  " + Loc.T("about.lic.title") + "\n" +
                Loc.T("about.lic.body") + "\n" +
                "\n" +
                "$  ";
        }

        private void PaintAboutTerm()
        {
            if (_aboutTerm == null) return;
            string shown = "";
            if (_aboutFull != null)
            {
                if (_aboutDone) shown = _aboutFull;
                else if (_aboutIdx > 0 && _aboutIdx <= _aboutFull.Length)
                    shown = _aboutFull.Substring(0, _aboutIdx);
            }
            if (_aboutCursorOn) shown += "\u2588";
            _aboutTerm.Text = shown;
        }

        private void StartAboutType()
        {
            _aboutIdx = 0;
            PaintAboutTerm();
            if (_aboutType == null)
            {
                _aboutType = new DispatcherTimer();
                _aboutType.Tick += delegate { AboutTypeTick(); };
            }
            _aboutType.Interval = TimeSpan.FromMilliseconds(12);
            _aboutType.Start();
            StartAboutBlink();
        }

        private void AboutTypeTick()
        {
            if (_aboutFull == null || _aboutTerm == null)
            {
                StopAboutType(true);
                return;
            }
            if (_aboutIdx >= _aboutFull.Length)
            {
                _aboutDone = true;
                if (_aboutType != null) _aboutType.Stop();
                PaintAboutTerm();
                return;
            }
            char ch = _aboutFull[_aboutIdx];
            _aboutIdx++;
            PaintAboutTerm();
            if (_aboutType == null) return;
            if (ch == '\n') _aboutType.Interval = TimeSpan.FromMilliseconds(85);
            else _aboutType.Interval = TimeSpan.FromMilliseconds(11);
        }

        private void StartAboutBlink()
        {
            if (_aboutBlink == null)
            {
                _aboutBlink = new DispatcherTimer();
                _aboutBlink.Interval = TimeSpan.FromMilliseconds(530);
                _aboutBlink.Tick += delegate
                {
                    _aboutCursorOn = !_aboutCursorOn;
                    PaintAboutTerm();
                };
            }
            _aboutBlink.Start();
        }

        private void StopAboutType(bool markDone)
        {
            if (_aboutType != null) _aboutType.Stop();
            if (_aboutBlink != null) _aboutBlink.Stop();
            if (markDone && _aboutIdx > 0) _aboutDone = true;
        }

        private static SolidColorBrush FrozenBrush(byte r, byte g, byte b)
        {
            SolidColorBrush br = new SolidColorBrush(Color.FromRgb(r, g, b));
            br.Freeze();
            return br;
        }

        // ---------- Helpers UI ----------
        private UIElement BuildDmaStatus()
        {
            Border box = new Border();
            box.Background = Theme.BgCard;
            box.BorderBrush = Theme.Stroke;
            box.BorderThickness = new Thickness(1);
            box.CornerRadius = new CornerRadius(10);
            box.Padding = new Thickness(12, 8, 12, 8);
            box.Margin = new Thickness(12, 6, 12, 8);
            box.MinHeight = 40;
            box.HorizontalAlignment = HorizontalAlignment.Stretch;
            _dmaStatusBox = box;

            Grid row = new Grid();
            row.ColumnDefinitions.Add(Col(GridLength.Auto));
            row.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));

            UIElement arc = BuildDmaArc(16);
            Grid.SetColumn(arc, 0);
            row.Children.Add(arc);

            TextBlock t = new TextBlock();
            t.Text = Loc.T("dma.checking");
            t.Foreground = Theme.TextMuted;
            t.FontSize = 12;
            t.FontWeight = FontWeights.SemiBold;
            t.VerticalAlignment = VerticalAlignment.Center;
            t.TextWrapping = TextWrapping.NoWrap;
            t.TextTrimming = TextTrimming.CharacterEllipsis;
            Grid.SetColumn(t, 1);
            _dmaStatusText = t;
            row.Children.Add(t);
            box.Child = row;
            _dmaSpinMode = -1;
            ApplyDmaArc(false);
            return box;
        }

        // Arco giratorio (Victan78/serious-gecko-84), pequeño. Rojo si no hay DMA, verde si conectado.
        private UIElement BuildDmaArc(double size)
        {
            Grid host = new Grid();
            host.Width = size;
            host.Height = size;
            host.VerticalAlignment = VerticalAlignment.Center;
            host.Margin = new Thickness(0, 0, 10, 0);

            Ellipse ring = new Ellipse();
            ring.Width = size;
            ring.Height = size;
            ring.Fill = Brushes.Transparent;
            ring.StrokeThickness = 2.2;
            ring.StrokeStartLineCap = PenLineCap.Round;
            ring.StrokeEndLineCap = PenLineCap.Round;
            ring.RenderTransformOrigin = new Point(0.5, 0.5);

            RotateTransform rot = new RotateTransform();
            ring.RenderTransform = rot;

            DropShadowEffect glow = new DropShadowEffect();
            glow.ShadowDepth = 0;
            glow.BlurRadius = 7;
            glow.Opacity = 0.55;
            glow.Color = Color.FromRgb(0xF1, 0x44, 0x44);
            ring.Effect = glow;

            host.Children.Add(ring);
            _dmaRing = ring;
            _dmaRot = rot;
            _dmaGlow = glow;
            return host;
        }

        private DoubleCollection DmaArcDash()
        {
            double size = _dmaRing.Width;
            double th = _dmaRing.StrokeThickness;
            double circ = Math.PI * Math.Max(1, size - th);
            double units = circ / th;
            DoubleCollection dash = new DoubleCollection();
            dash.Add(units * 0.28);
            dash.Add(units * 0.72);
            return dash;
        }

        private void ApplyDmaArc(bool connected)
        {
            if (_dmaRing == null || _dmaRot == null || _dmaGlow == null) return;
            int mode = connected ? 1 : 0;
            if (_dmaSpinMode == mode) return;
            _dmaSpinMode = mode;

            Color c = connected
                ? Color.FromRgb(0x3B, 0xE0, 0x7D)
                : Color.FromRgb(0xF1, 0x44, 0x44);
            SolidColorBrush stroke = new SolidColorBrush(c);
            stroke.Freeze();
            _dmaRing.Stroke = stroke;
            _dmaGlow.Color = c;

            _dmaRot.BeginAnimation(RotateTransform.AngleProperty, null);
            _dmaGlow.BeginAnimation(DropShadowEffect.OpacityProperty, null);
            _dmaGlow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, null);

            if (connected)
            {
                _dmaRot.Angle = 0;
                _dmaRing.StrokeDashArray = null;
                _dmaGlow.BlurRadius = 8;
                DoubleAnimation pulse = new DoubleAnimation(0.28, 0.72, new Duration(TimeSpan.FromSeconds(1.4)));
                pulse.AutoReverse = true;
                pulse.RepeatBehavior = RepeatBehavior.Forever;
                pulse.EasingFunction = new SineEase();
                _dmaGlow.BeginAnimation(DropShadowEffect.OpacityProperty, pulse);
            }
            else
            {
                _dmaRing.StrokeDashArray = DmaArcDash();
                DoubleAnimation spin = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(1.2)));
                spin.RepeatBehavior = RepeatBehavior.Forever;
                _dmaRot.BeginAnimation(RotateTransform.AngleProperty, spin);

                DoubleAnimation blur = new DoubleAnimation(4, 11, new Duration(TimeSpan.FromSeconds(1.2)));
                blur.AutoReverse = true;
                blur.RepeatBehavior = RepeatBehavior.Forever;
                _dmaGlow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blur);

                DoubleAnimation op = new DoubleAnimation(0.35, 0.7, new Duration(TimeSpan.FromSeconds(1.2)));
                op.AutoReverse = true;
                op.RepeatBehavior = RepeatBehavior.Forever;
                _dmaGlow.BeginAnimation(DropShadowEffect.OpacityProperty, op);
            }
        }

        private void ApplyDmaStatus(DmaInfo inf)
        {
            _dmaLast = inf;
            string label = Loc.T("dma.checking");
            Brush color = Theme.TextFaint;
            string hint = Loc.T("spd.hint.off");
            if (inf != null)
            {
                if (inf.DmaReadOk)
                {
                    label = Loc.T("dma.on");
                    color = Theme.Success;
                    hint = Loc.T("spd.hint.on") + "  v" + inf.FwMajor + "." + inf.FwMinor +
                        "  ID 0x" + inf.DeviceId.ToString("X");
                }
                else if (inf.FpgaOk)
                {
                    label = Loc.T("dma.fpgaonly");
                    color = Theme.Accent;
                    hint = Loc.T("spd.hint.fpga");
                }
                else
                {
                    label = Loc.T("dma.off");
                    color = Theme.Danger;
                    hint = Loc.T("spd.hint.off");
                }
            }
            if (_dmaStatusText != null) { _dmaStatusText.Text = label; _dmaStatusText.Foreground = color; }
            ApplyDmaArc(inf != null && inf.DmaReadOk);
            if (_spdConn != null) { _spdConn.Text = label; _spdConn.Foreground = color; }
            if (_spdDot != null) _spdDot.Background = color;
            if (_spdConnBox != null) _spdConnBox.BorderBrush = color;
            if (_spdHint != null && !_speedBusy) _spdHint.Text = hint;
        }

        private void RefreshDmaStatus(Action<string> log)
        {
            if (_speedBusy) return;
            Thread t = new Thread(delegate()
            {
                DmaInfo inf = Leech.Probe(log);
                Dispatcher.BeginInvoke(new Action(delegate { ApplyDmaStatus(inf); }));
            });
            t.IsBackground = true;
            t.Start();
        }

        private Border MetricCard(string label, string initial, out TextBlock value, Brush color)
        {
            Border b = new Border();
            b.Background = Theme.BgInput;
            b.BorderBrush = Theme.Stroke;
            b.BorderThickness = new Thickness(1);
            b.CornerRadius = new CornerRadius(10);
            b.Padding = new Thickness(12, 10, 12, 10);
            StackPanel s = new StackPanel();
            TextBlock v = new TextBlock();
            v.Text = initial;
            v.Foreground = color;
            v.FontSize = 32;
            v.FontWeight = FontWeights.Bold;
            v.TextWrapping = TextWrapping.Wrap;
            value = v;
            s.Children.Add(v);
            TextBlock l = new TextBlock();
            l.Text = label;
            l.Foreground = Theme.TextFaint;
            l.FontSize = 12;
            l.Margin = new Thickness(0, 4, 0, 0);
            s.Children.Add(l);
            b.Child = s;
            return b;
        }

        private void StartSpeedUiTimer()
        {
            if (_spdTimer == null)
            {
                _spdTimer = new DispatcherTimer();
                _spdTimer.Interval = TimeSpan.FromMilliseconds(120);
                _spdTimer.Tick += delegate { UpdateSpeedUi(); };
            }
            _spdTimer.Start();
        }

        private void StopSpeedUi()
        {
            _speedStop = true;
            if (_spdTimer != null) _spdTimer.Stop();
        }

        private void UpdateSpeedUi()
        {
            SpeedSnap s = Leech.CopyLive();
            CultureInfo ci = CultureInfo.InvariantCulture;
            if (_spdMbps != null) _spdMbps.Text = s.Mbps.ToString("F1", ci);
            if (_spdReads != null) _spdReads.Text = ((int)s.ReadsPerSec).ToString("N0", ci);
            if (_spdTotal != null) _spdTotal.Text = FmtData(s.TotalBytes);
            if (_spdFail != null)
            {
                if (s.Running || s.TotalReads > 0)
                {
                    _spdFail.Visibility = Visibility.Visible;
                    _spdFail.Text = Loc.T("spd.fail") + s.Failed.ToString("N0", ci) +
                        "   \u00b7   0x" + s.Address.ToString("X") + "  " + (s.Chunk / 1024) + " KB";
                }
                else
                {
                    _spdFail.Text = "";
                    _spdFail.Visibility = Visibility.Collapsed;
                }
            }
            if (!s.Running && _spdTimer != null && !_speedBusy) _spdTimer.Stop();
        }

        private static string FmtData(long n)
        {
            if (n >= 1073741824L)
                return (n / 1073741824.0).ToString("F2", CultureInfo.InvariantCulture) + " GB";
            if (n >= 1048576L)
                return (n / 1048576.0).ToString("F1", CultureInfo.InvariantCulture) + " MB";
            return (n / 1024.0).ToString("F0", CultureInfo.InvariantCulture) + " KB";
        }

        private FrameworkElement PageHead(string title, string subtitle)
        {
            StackPanel sp = new StackPanel();
            sp.Margin = new Thickness(0, 0, 0, 8);
            TextBlock t = Theme.Title(title);
            t.FontSize = 20;
            sp.Children.Add(t);
            TextBlock s = Theme.Label(subtitle);
            s.Margin = new Thickness(0, 4, 0, 0);
            sp.Children.Add(s);
            return sp;
        }

        private FrameworkElement SectionTitle(string text)
        {
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            sp.Margin = new Thickness(0, 0, 0, 8);
            Border dot = new Border();
            dot.Background = Theme.Accent;
            dot.CornerRadius = new CornerRadius(2);
            dot.Width = 8;
            dot.Height = 8;
            dot.VerticalAlignment = VerticalAlignment.Center;
            dot.Margin = new Thickness(0, 0, 8, 0);
            sp.Children.Add(dot);
            TextBlock t = new TextBlock();
            t.Text = text;
            t.Foreground = Theme.TextPrimary;
            t.FontSize = 15;
            t.FontWeight = FontWeights.SemiBold;
            t.VerticalAlignment = VerticalAlignment.Center;
            sp.Children.Add(t);
            return sp;
        }

        private Border Tile(string title, string sub)
        {
            Border b = new Border();
            b.Background = Theme.BgInput;
            b.BorderBrush = Theme.Stroke;
            b.BorderThickness = new Thickness(1);
            b.CornerRadius = new CornerRadius(8);
            b.Padding = new Thickness(10, 8, 10, 8);
            b.Cursor = Cursors.Hand;
            StackPanel s = new StackPanel();
            TextBlock t = new TextBlock();
            t.Text = title;
            t.Foreground = Theme.TextPrimary;
            t.FontSize = 14;
            t.FontWeight = FontWeights.Bold;
            s.Children.Add(t);
            TextBlock u = new TextBlock();
            u.Text = sub;
            u.Foreground = Theme.TextFaint;
            u.FontSize = 11;
            s.Children.Add(u);
            b.Child = s;
            return b;
        }

        private void StyleTile(Border b, bool sel)
        {
            b.BorderBrush = sel ? Theme.Accent : Theme.Stroke;
            b.Background = sel ? Theme.AccentSoft : Theme.BgInput;
        }

        private Grid TileRow(params Border[] tiles)
        {
            Grid g = new Grid();
            int col = 0;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (i > 0) { g.ColumnDefinitions.Add(Col(new GridLength(10))); col++; }
                g.ColumnDefinitions.Add(Col(new GridLength(1, GridUnitType.Star)));
                Grid.SetColumn(tiles[i], col);
                g.Children.Add(tiles[i]);
                col++;
            }
            return g;
        }

        private TextBox NewConsole(int height)
        {
            TextBox tb = new TextBox();
            tb.Background = Theme.BgInput;
            tb.Foreground = Theme.TextMuted;
            tb.BorderBrush = Theme.Stroke;
            tb.BorderThickness = new Thickness(1);
            tb.IsReadOnly = true;
            tb.FontFamily = new FontFamily("Consolas");
            tb.FontSize = 12;
            tb.Padding = new Thickness(10, 12, 10, 10);
            tb.Height = height;
            tb.AcceptsReturn = true;
            tb.VerticalContentAlignment = VerticalAlignment.Top;
            tb.TextWrapping = TextWrapping.Wrap;
            tb.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            tb.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            return tb;
        }

        private Action<string> Logger(TextBox box)
        {
            return delegate(string line) { AppendLine(box, line); };
        }

        private Action<string> LoggerBlock(TextBlock box)
        {
            return delegate(string line) { AppendBlock(box, line); };
        }

        private void AppendLine(TextBox box, string line)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action<TextBox, string>(AppendLine), box, line);
                return;
            }
            box.AppendText(line + "\r\n");
            box.ScrollToEnd();
        }

        private void AppendBlock(TextBlock box, string line)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action<TextBlock, string>(AppendBlock), box, line);
                return;
            }
            if (line == null) line = "";
            box.Text = box.Text + line + "\r\n";
        }

        private void SetDrvStatus(string text, Brush color)
        {
            if (_drvStatus == null) return;
            _drvStatus.Text = text;
            _drvStatus.Foreground = Theme.TextPrimary;
            if (_drvStatusBox != null)
            {
                _drvStatusBox.BorderBrush = color;
                _drvStatusBox.Background = (color == Theme.Success) ? Theme.AccentSoft : Theme.BgInput;
                if (color == Theme.Danger) _drvStatus.Foreground = Theme.Danger;
                if (color == Theme.Success) _drvStatus.Foreground = Theme.Success;
            }
        }

        private void InstallInfFrom(string subfolder, Action<string> log, Control[] disable)
        {
            SetDrvStatus(Loc.T("flash.running"), Theme.Accent);
            RunBackground(delegate
            {
                int rc = Drivers.InstallFromFolder(subfolder, log);
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (rc == 0)
                    {
                        log("");
                        log(Loc.T("drv.success"));
                        SetDrvStatus(Loc.T("drv.success"), Theme.Success);
                    }
                    else
                    {
                        log("");
                        log(Loc.T("drv.fail"));
                        SetDrvStatus(Loc.T("drv.fail"), Theme.Danger);
                    }
                }));
            }, disable);
        }

        private void RunBackground(Action work, Control[] disableWhileRunning)
        {
            foreach (Control c in disableWhileRunning) c.IsEnabled = false;
            Thread t = new Thread(delegate()
            {
                try { work(); }
                catch (Exception) { }
                finally
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        foreach (Control c in disableWhileRunning) c.IsEnabled = true;
                    }));
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        private static string FileShortHash(string path)
        {
            try
            {
                FileStream fs = File.OpenRead(path);
                SHA256 sha = SHA256.Create();
                byte[] h = sha.ComputeHash(fs);
                fs.Close();
                sha.Clear();
                string hex = "";
                int n = h.Length < 8 ? h.Length : 8;
                for (int i = 0; i < n; i++) hex += h[i].ToString("x2");
                long kb = new FileInfo(path).Length / 1024;
                return Loc.T("flash.hash") + kb + " KB   SHA256 " + hex + "...";
            }
            catch
            {
                return "";
            }
        }

        private void OpenUrl(string url)
        {
            try { System.Diagnostics.Process.Start(url); }
            catch { }
        }

        private static RowDefinition Row(GridLength h)
        {
            RowDefinition r = new RowDefinition();
            r.Height = h;
            return r;
        }

        private static ColumnDefinition Col(GridLength w)
        {
            ColumnDefinition c = new ColumnDefinition();
            c.Width = w;
            return c;
        }
    }
}
