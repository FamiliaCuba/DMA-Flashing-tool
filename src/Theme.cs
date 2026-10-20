using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace DmaTool
{
    // Paleta y helpers visuales: tema casi negro con acento ROJO (estilo "RED DMA").
    // Todo por codigo porque compilamos con csc (sin XAML).
    internal static class Theme
    {
        public static readonly Brush BgDarkest = Frozen(Rgb(0x0B, 0x0B, 0x0D)); // titlebar / chrome
        public static readonly Brush BgSidebar = Frozen(Rgb(0x08, 0x08, 0x0A)); // barra lateral (mas oscura)
        public static readonly Brush BgContent = Frozen(Rgb(0x16, 0x16, 0x1A)); // panel principal
        public static readonly Brush BgTitleGlass = Frozen(Color.FromArgb(0xB8, 0x0B, 0x0B, 0x0D));
        public static readonly Brush BgSidebarGlass = Frozen(Color.FromArgb(0xD4, 0x08, 0x08, 0x0A));
        public static readonly Brush BgCard = Frozen(Rgb(0x20, 0x20, 0x26));    // tarjetas
        public static readonly Brush BgInput = Frozen(Rgb(0x0D, 0x0D, 0x10));   // cajas / consola
        public static readonly Brush BgHover = Frozen(Rgb(0x2C, 0x2C, 0x34));
        public static readonly Brush Stroke = Frozen(Rgb(0x32, 0x32, 0x3C));    // bordes sutiles

        public static readonly Brush Accent = Frozen(Rgb(0xF0, 0x3A, 0x3A));    // rojo vivo
        public static readonly Brush AccentHover = Frozen(Rgb(0xFF, 0x4D, 0x4D));
        public static readonly Brush AccentBright = Frozen(Rgb(0xFF, 0x2A, 0x2A)); // glow
        public static readonly Brush AccentBorder = Frozen(Rgb(0xB8, 0x2E, 0x2E)); // borde ventana (menos neon)
        public static readonly Brush AccentSoft = Frozen(Rgb(0x33, 0x15, 0x17)); // fondo rojo tenue (nav seleccionado)

        public static readonly Brush Blurple = Frozen(Rgb(0x58, 0x65, 0xF2));   // Discord (azul vivo)
        public static readonly Brush BlurpleHover = Frozen(Rgb(0x6E, 0x7B, 0xFF));

        public static readonly Brush Success = Frozen(Rgb(0x3B, 0xE0, 0x7D));
        public static readonly Brush Danger = Frozen(Rgb(0xFF, 0x4D, 0x50));

        public static readonly Brush TextPrimary = Frozen(Rgb(0xF5, 0xF5, 0xF5));
        public static readonly Brush TextMuted = Frozen(Rgb(0xA8, 0xA8, 0xA8));
        public static readonly Brush TextFaint = Frozen(Rgb(0x6E, 0x6E, 0x6E));

        public const string FontFamily = "Segoe UI";

        private static Color Rgb(byte r, byte g, byte b)
        {
            return Color.FromRgb(r, g, b);
        }

        private static SolidColorBrush Frozen(Color c)
        {
            SolidColorBrush b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        // Tarjeta redondeada con borde sutil, sombra y acento al pasar el raton.
        public static Border Card()
        {
            LinearGradientBrush bg = new LinearGradientBrush();
            bg.StartPoint = new Point(0, 0);
            bg.EndPoint = new Point(0, 1);
            bg.GradientStops.Add(new GradientStop(Color.FromRgb(0x28, 0x28, 0x30), 0));
            bg.GradientStops.Add(new GradientStop(Color.FromRgb(0x1C, 0x1C, 0x22), 1));
            bg.Freeze();

            Border b = new Border();
            b.Background = bg;
            b.BorderBrush = Stroke;
            b.BorderThickness = new Thickness(1);
            b.CornerRadius = new CornerRadius(12);
            b.Padding = new Thickness(14);
            b.Margin = new Thickness(0, 0, 0, 10);
            DropShadowEffect sh = new DropShadowEffect();
            sh.Color = Colors.Black;
            sh.BlurRadius = 16;
            sh.ShadowDepth = 0;
            sh.Opacity = 0.35;
            b.Effect = sh;
            b.MouseEnter += delegate { b.BorderBrush = Accent; };
            b.MouseLeave += delegate { b.BorderBrush = Stroke; };
            return b;
        }

        public static TextBlock Title(string text)
        {
            TextBlock t = new TextBlock();
            t.Text = text;
            t.Foreground = TextPrimary;
            t.FontFamily = new FontFamily(FontFamily);
            t.FontSize = 26;
            t.FontWeight = FontWeights.Bold;
            return t;
        }

        public static TextBlock Heading(string text)
        {
            TextBlock t = new TextBlock();
            t.Text = text;
            t.Foreground = TextPrimary;
            t.FontFamily = new FontFamily(FontFamily);
            t.FontSize = 15;
            t.FontWeight = FontWeights.SemiBold;
            t.Margin = new Thickness(0, 0, 0, 8);
            return t;
        }

        public static TextBlock Label(string text)
        {
            TextBlock t = new TextBlock();
            t.Text = text;
            t.Foreground = TextMuted;
            t.FontFamily = new FontFamily(FontFamily);
            t.FontSize = 13;
            t.TextWrapping = TextWrapping.Wrap;
            return t;
        }

        // Etiqueta pequena en mayusculas tipo "GETTING STARTED".
        public static TextBlock Eyebrow(string text)
        {
            TextBlock t = new TextBlock();
            t.Text = text.ToUpperInvariant();
            t.Foreground = Accent;
            t.FontFamily = new FontFamily(FontFamily);
            t.FontSize = 11;
            t.FontWeight = FontWeights.Bold;
            return t;
        }

        // Boton: estilo rojo (primary), gris (secondary) o azul Discord (discord).
        public static Button Button(string text, bool primary)
        {
            return MakeButton(text, primary ? Accent : BgHover,
                primary ? AccentHover : Frozen(Rgb(0x35, 0x35, 0x35)));
        }

        public static Button DiscordButton(string text)
        {
            return MakeButton("\uD83D\uDCAC  " + text, Blurple, BlurpleHover);
        }

        private static Button MakeButton(string text, Brush normal, Brush hover)
        {
            Button btn = new Button();
            btn.Content = text;
            btn.Foreground = TextPrimary;
            btn.FontFamily = new FontFamily(FontFamily);
            btn.FontSize = 14;
            btn.FontWeight = FontWeights.SemiBold;
            btn.Height = 40;
            btn.MinWidth = 150;
            btn.Padding = new Thickness(18, 0, 18, 0);
            btn.Cursor = System.Windows.Input.Cursors.Hand;
            btn.BorderThickness = new Thickness(0);
            btn.HorizontalAlignment = HorizontalAlignment.Left;
            btn.Background = normal;
            btn.Template = RoundedButtonTemplate(normal, hover, new CornerRadius(8));
            return btn;
        }

        // Enlace tipo texto "Abrir pagina ->".
        public static Button Link(string text)
        {
            Button b = new Button();
            b.Content = text;
            b.Foreground = Accent;
            b.FontFamily = new FontFamily(FontFamily);
            b.FontSize = 13;
            b.FontWeight = FontWeights.SemiBold;
            b.Background = Brushes.Transparent;
            b.BorderThickness = new Thickness(0);
            b.Cursor = System.Windows.Input.Cursors.Hand;
            b.HorizontalAlignment = HorizontalAlignment.Left;
            b.Padding = new Thickness(0);
            b.Template = RoundedButtonTemplate(Brushes.Transparent, Brushes.Transparent, new CornerRadius(0));
            return b;
        }

        public static ControlTemplate RoundedButtonTemplate(Brush normal, Brush hover, CornerRadius radius)
        {
            return RoundedButtonTemplate(normal, hover, radius, HorizontalAlignment.Center);
        }

        public static ControlTemplate RoundedButtonTemplate(Brush normal, Brush hover, CornerRadius radius, HorizontalAlignment contentAlign)
        {
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "root";
            border.SetValue(Border.BackgroundProperty, normal);
            border.SetValue(Border.CornerRadiusProperty, radius);
            border.SetValue(Border.PaddingProperty, new Thickness(0));

            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, contentAlign);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);

            ControlTemplate tpl = new ControlTemplate(typeof(Button));
            tpl.VisualTree = border;

            Trigger over = new Trigger();
            over.Property = UIElement.IsMouseOverProperty;
            over.Value = true;
            over.Setters.Add(new Setter(Border.BackgroundProperty, hover, "root"));
            tpl.Triggers.Add(over);

            Trigger disabled = new Trigger();
            disabled.Property = UIElement.IsEnabledProperty;
            disabled.Value = false;
            disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45));
            tpl.Triggers.Add(disabled);

            return tpl;
        }

        public static TextBox TextBox(bool readOnly)
        {
            TextBox tb = new TextBox();
            tb.Background = BgInput;
            tb.Foreground = TextPrimary;
            tb.CaretBrush = TextPrimary;
            tb.BorderBrush = Stroke;
            tb.BorderThickness = new Thickness(1);
            tb.FontFamily = new FontFamily(FontFamily);
            tb.FontSize = 13;
            tb.Padding = new Thickness(10, 8, 10, 8);
            tb.IsReadOnly = readOnly;
            return tb;
        }

        // Insignia numerada (01, 1, 2...) en un cuadro redondeado.
        public static Border Badge(string text, Brush bg)
        {
            Border b = new Border();
            b.Background = bg;
            b.CornerRadius = new CornerRadius(7);
            b.Width = 30;
            b.Height = 30;
            TextBlock t = new TextBlock();
            t.Text = text;
            t.Foreground = TextPrimary;
            t.FontSize = 13;
            t.FontWeight = FontWeights.Bold;
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.VerticalAlignment = VerticalAlignment.Center;
            b.Child = t;
            return b;
        }

        public static DropShadowEffect SoftShadow()
        {
            DropShadowEffect e = new DropShadowEffect();
            e.Color = Colors.Black;
            e.BlurRadius = 14;
            e.ShadowDepth = 0;
            e.Opacity = 0.4;
            return e;
        }

        public static DropShadowEffect RedGlow(double blur, double opacity)
        {
            DropShadowEffect e = new DropShadowEffect();
            e.Color = Color.FromRgb(0xFF, 0x2A, 0x2A);
            e.BlurRadius = blur;
            e.ShadowDepth = 0;
            e.Opacity = opacity;
            return e;
        }

        // Cielo del starfield (proud-kangaroo-29): elipse muy oscura abajo, casi negra arriba.
        public static Brush NightSky()
        {
            RadialGradientBrush rg = new RadialGradientBrush();
            rg.Center = new Point(0.5, 1.08);
            rg.GradientOrigin = new Point(0.5, 1.08);
            rg.RadiusX = 1.15;
            rg.RadiusY = 0.9;
            rg.GradientStops.Add(new GradientStop(Color.FromRgb(0x1A, 0x0A, 0x10), 0));
            rg.GradientStops.Add(new GradientStop(Color.FromRgb(0x09, 0x0A, 0x0F), 0.7));
            rg.GradientStops.Add(new GradientStop(Color.FromRgb(0x07, 0x07, 0x0A), 1));
            rg.Freeze();
            return rg;
        }
    }
}
