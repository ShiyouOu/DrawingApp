using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DrawingApp
{
    class View
    {
        public readonly TabItem tab = new TabItem();
        private List<Layer> layers = new List<Layer>();
        private ScrollViewer sView = new ScrollViewer();
        private Viewbox vBox = new Viewbox();
        private System.Windows.Point prevMLocation;

        private Layer _activeLayer;
        private string _currentTool = "Brush";
        private int _brushSize;
        private int _opacity = 255;
        private string _blendMode = "Normal";
        private System.Windows.Media.Color _brushColor = System.Windows.Media.Color.FromArgb(255, 255, 255, 255);

        // Transform Group Info for adding Zoom functionality to the Viewbox
        ScaleTransform trZoom = new ScaleTransform(1, 1);
        TranslateTransform trTransform = new TranslateTransform(1, 1);
        TransformGroup TGrp = new TransformGroup();

        public int BrushSize{
            get { return _brushSize; }
            set { _brushSize = value; }
        }

        public int Opacity
        {
            get { return _opacity; }
            set { 
                if(value >= 0 && value <= 255)
                {
                    _opacity = value;
                }
            }
        }
        public string BlendMode
        {
            get { return _blendMode; }
            set { _blendMode = value; }
        }
        public System.Windows.Media.Color BrushColor
        {
            get { return _brushColor; }
            set { _brushColor = value; }
        }
        public string CurrentTool
        {
            get { return _currentTool; }
            set { _currentTool = value; }
        }
        public Layer ActiveLayer
        {
            get { return _activeLayer; }
            set { _activeLayer = value; }
        }

        // Constructor
        public View()
        {
            // Setting the correct properties for the ScrollViewer and the Viewbox
            sView.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            vBox.Height = 600;
            vBox.Width = 600;
            vBox.Stretch = System.Windows.Media.Stretch.Uniform;
            vBox.StretchDirection = StretchDirection.Both;

            // Adding MouseScroll Event to the new ScrollViewer
            sView.PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(ViewMouseWheel);
            vBox.MouseMove += new MouseEventHandler(MouseMove);

            // Setting up Transform Group for adding Zoom Capabilities to the ViewBox
            TGrp.Children.Add(trZoom);
            TGrp.Children.Add(trTransform);
            vBox.LayoutTransform = TGrp;

            sView.Content = vBox;
            tab.Content = sView;
        }

        // Zoom In
        public void ZIn()
        {
            trZoom.ScaleX += trZoom.ScaleX * 0.05;
            trZoom.ScaleY += trZoom.ScaleY * 0.05;
        }

        // Zoom Out
        public void ZOut()
        {
            trZoom.ScaleX -= trZoom.ScaleX * 0.05;
            trZoom.ScaleY -= trZoom.ScaleY * 0.05;
        }

        // Mouse has moved, use the selected tool
        private void MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Point mPos = GetMouseLocation();
                if (mPos != null)
                {
                    mPos = ActiveLayer.ToBmpLocation(mPos, (int)vBox.Width, (int)vBox.Height);
                    if (mPos != prevMLocation)
                    {
                        prevMLocation = mPos;
                        if (CurrentTool == "Brush")
                        {
                            ActiveLayer.Draw(mPos, BrushSize, BlendMode, System.Windows.Media.Color.FromArgb((byte)Opacity, BrushColor.R, BrushColor.G, BrushColor.B));
                        }
                        else if (CurrentTool == "Eraser")
                        {
                            ActiveLayer.Draw(mPos, BrushSize, CurrentTool, System.Windows.Media.Color.FromArgb((byte)Opacity, BrushColor.R, BrushColor.G, BrushColor.B));
                        }
                        else if (CurrentTool == "EyeDropper")
                        {
                            // Change brush color to the color under the cursor
                            System.Windows.Media.Color tempColor = ActiveLayer.GetPixelColor((int)mPos.X, (int)mPos.Y);
                            BrushColor = System.Windows.Media.Color.FromArgb(255, tempColor.R, tempColor.G, tempColor.B);
                        }
                    }
                }
            }
            else if (e.LeftButton == MouseButtonState.Released)
            {
                ActiveLayer.NewPass();
            }
        }

        // MouseWheel was Scrolled
        private void ViewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            // Making sure Ctrl button is being held
            if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                return;
            }
            if (e.Delta > 0)
            {
                ZIn();
            }
            else if (e.Delta < 0)
            {
                ZOut();
            }
        }

        // Add a new Layer to the view
        public void AddLayer(WriteableBitmap wrBmp)
        {
            Layer newLayer = new Layer(wrBmp);
            ActiveLayer = newLayer;
            layers.Add(newLayer);
            vBox.Child = newLayer.img;
            vBox.Width = wrBmp.Width;
            vBox.Height = wrBmp.Height;
        }

        // Set the header of the Tabitem for the View
        public void SetHeader(string s)
        {
            tab.Header = s;
        }

        // returns the mouse location relative to the View Box
        public System.Windows.Point GetMouseLocation()
        {
            System.Windows.Point mPos = Mouse.GetPosition(vBox);
            return mPos;
        }

        // Make aLayer the active Layer for the current View/Tab
        public void SetActiveLayer(int layerNum)
        {
            ActiveLayer = layers[layerNum];
        }
    }
}
