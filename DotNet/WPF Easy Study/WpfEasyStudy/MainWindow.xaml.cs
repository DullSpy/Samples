using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Aquarium_OnNeedsCleaning(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(e.OriginalSource + " invoke Acquarium Event");
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            btn?.RaiseEvent(new RoutedEventArgs(Aquarium.NeedsCleaningEvent));
        }
    }

    public class Aquarium
    {
        public static readonly RoutedEvent NeedsCleaningEvent = EventManager.RegisterRoutedEvent("NeedsCleaning", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Aquarium));
        public static void AddNeedsCleaningHandler(DependencyObject d, RoutedEventHandler handler)
        {
            UIElement uie = d as UIElement;
            if (uie != null)
            {
                uie.AddHandler(Aquarium.NeedsCleaningEvent, handler);
            }
        }
        public static void RemoveNeedsCleaningHandler(DependencyObject d, RoutedEventHandler handler)
        {
            UIElement uie = d as UIElement;
            if (uie != null)
            {
                uie.RemoveHandler(Aquarium.NeedsCleaningEvent, handler);
            }
        }
    }
}
