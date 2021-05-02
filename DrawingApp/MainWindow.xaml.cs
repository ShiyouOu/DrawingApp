using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Drawing;

namespace DrawingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer timer = new DispatcherTimer();
        List<View> views = new List<View>();
        int backgroundColor = 0;
        View activeView;

        System.Windows.Media.Brush transparentBackgroundBrush;

        public MainWindow()
        {
            InitializeComponent();
        }

        // A Tool was selected
        private void Tool_Selected(object sender, MouseButtonEventArgs e)
        {
            if (sender != null && activeView != null)
            {
                sender = (System.Windows.Controls.Image)sender;
                // Reset all the tool locations
                Brush.HorizontalAlignment = HorizontalAlignment.Center;
                Pointer.HorizontalAlignment = HorizontalAlignment.Center;
                Eraser.HorizontalAlignment = HorizontalAlignment.Center;
                Eye_Dropper.HorizontalAlignment = HorizontalAlignment.Center;
                if (sender == Brush)
                {
                    activeView.CurrentTool = "Brush";
                    Brush.HorizontalAlignment = HorizontalAlignment.Right;
                }
                else if (sender == Pointer)
                {
                    activeView.CurrentTool = "Pointer";
                    Pointer.HorizontalAlignment = HorizontalAlignment.Right;
                }
                else if (sender == Eraser)
                {
                    activeView.CurrentTool = "Eraser";
                    Eraser.HorizontalAlignment = HorizontalAlignment.Right;
                }
                else if (sender == Eye_Dropper)
                {
                    activeView.CurrentTool = "EyeDropper";
                    Eye_Dropper.HorizontalAlignment = HorizontalAlignment.Right;
                }
            }
        }

        private void UpdateToolSettings()
        {
            activeView.brushSize = int.Parse(Brush_Size.Text);
            activeView.opacity = (int)Color_Opacity.Value;
            activeView.blendMode = (string)Blend_Mode.SelectedValue;
            BrushColorViewer.Fill = new SolidColorBrush(activeView.brushColor);
        }

        // For testing purposes
        private void DPrint(string output)
        {
            string[] debugContent = DebugConsole.Text.Split("\n");
            if (debugContent.Length >= 4)
            {
                int end = debugContent[1].Length;
                DebugConsole.Text = DebugConsole.Text.Remove(0, end+1);
            }
            DebugConsole.Text = DebugConsole.Text + "\n " + output;
        }

        // Make aView the current active View/Tab
        private void SetActiveView(View aView)
        {
            activeView = aView;
            MainTabControl.SelectedItem = aView.tab;
            Reference_Image.Source = aView.activeLayer.BMap;
        }

        // Make aLayer the active Layer for the current View/Tab
        private void SetActiveLayer(Layer aLayer)
        {
            activeView.activeLayer = aLayer;
        }

        // Opening a File
        private void File_Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            // Only .png and .jpg files allowed as of right now
            fd.Filter = "Image files (*.png;*.jpg)|*.png;*.jpg|All files (*.*)|*.*";
            Nullable<bool> openedFile = fd.ShowDialog();

            // Open the Image file as a WriteableBitmap to allow us to make changes to it
            try
            {
                if (fd.FileName != string.Empty)
                {
                    WriteableBitmap wrBmp = new WriteableBitmap(new BitmapImage(new Uri(fd.FileName)));
                    if (openedFile == true)
                    {
                        View currTab = NewTab();
                        currTab.SetHeader(fd.FileName);
                        currTab.AddLayer(wrBmp);
                        SetActiveView(currTab);
                        SetActiveLayer(currTab.layers[0]);
                        DPrint("done");
                    }
                }
            }
            catch(Exception ex)
            {
                DPrint(ex.ToString());
            }
        }


        // Add a new Tab to the Main Tabcontrol by creating a new view
        private View NewTab()
        {
            View newView = new View();
            Tool_Selected(Brush, null);
            views.Add(newView);
            MainTabControl.Items.Add(newView.tab);
            return newView;
        }

        // Program has been started
        private void Window_Initialized(object sender, EventArgs e)
        {
            DPrint("Program Started");
            transparentBackgroundBrush = MainTabControl.Background;
            timer.Tick += timer_Tick;
            // timer ticks every 5 milliseconds
            timer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            // Clear all tabs from MainTabControl, all tabs in designer were for reference and testing
            MainTabControl.Items.Clear();
            // Starting the timer
            timer.Start();
        }

        // User clicked the Zoom In Button
        private void ZoomIn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (activeView != null)
            {
                activeView.ZIn();
            }
        }

        // User clicked the Zoom Out Button
        private void ZoomOut_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (activeView != null)
            {
                activeView.ZOut();
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (activeView != null)
            {
                UpdateToolSettings();
            }
        }

        // Open a save dialog so allow user to save the image (currently only pngs)
        private void File_Save_As_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fd = new SaveFileDialog();
            // Only .png and .jpg files allowed as of right now
            fd.Filter = "Image files (*.png)|*.png";
            fd.ShowDialog();
            if (fd.FileName != string.Empty && activeView != null)
            {
                activeView.activeLayer.SaveBitmap(fd.FileName);
            }
        }

        // Make sure the brush size is an integer
        private void Validate_For_Int(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Change brush color
        private void ColorPicker_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (activeView != null)
            {
                // It is an image so must convert to bitmap and find pixel color
                Bitmap b = new Bitmap(System.Drawing.Image.FromFile("../../../Resources/ColorPicker.png"));
                int bWidth = b.Width;
                int bHeight = b.Height;
                System.Windows.Point mPos = e.GetPosition(ColorPicker);
                int x = (int)Math.Round(bWidth * (mPos.X / ColorPicker.Width));
                int y = (int)Math.Round(bHeight * (mPos.Y / ColorPicker.Height));
                // Convert from System.Drawing.Color to System.Windows.Media.Color
                System.Drawing.Color tempColor = b.GetPixel(x, y);
                System.Windows.Media.Color selectedColor = System.Windows.Media.Color.FromArgb(255, tempColor.R, tempColor.G, tempColor.B);
                activeView.brushColor = selectedColor;
            }
        }

        // Changes the background of the tabcontrol makes, good when working images with transparent backgrounds
        private void Change_Background_MouseDown(object sender, MouseButtonEventArgs e)
        {
            backgroundColor++;
            switch (backgroundColor % 4)
            {
                case 0:
                    MainTabControl.Background = transparentBackgroundBrush;
                    break;
                case 1:
                    MainTabControl.Background = System.Windows.Media.Brushes.Black;
                    break;
                case 2:
                    MainTabControl.Background = System.Windows.Media.Brushes.White;
                    break;
                case 3:
                    MainTabControl.Background = System.Windows.Media.Brushes.SkyBlue;
                    break;
            }
        }

        // Create new file
        private void File_New_Click(object sender, RoutedEventArgs e)
        {
            WriteableBitmap wrBmp = new WriteableBitmap(1024, 1024, 96, 96, PixelFormats.Bgra32, null);
            View currTab = NewTab();
            currTab.SetHeader("New");
            currTab.AddLayer(wrBmp);
            SetActiveView(currTab);
            SetActiveLayer(currTab.layers[0]);
            DPrint("done");
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (MainTabControl.SelectedIndex != -1)
                {
                    SetActiveView(views[MainTabControl.SelectedIndex]);
                    Tool_Selected(Brush, null);
                }
            }
        }
    }
}
