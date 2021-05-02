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
        public TabItem tab = new TabItem();
        public ScrollViewer sView = new ScrollViewer();
        public Viewbox vBox = new Viewbox();
        public List<Layer> layers = new List<Layer>();
        public Layer activeLayer;
        public String CurrentTool = "Brush";
        public System.Windows.Point prevMLocation;

        public int brushSize = 25;
        public int opacity = 255;
        public string blendMode = "Normal";
        public System.Windows.Media.Color brushColor = System.Windows.Media.Color.FromArgb(255, 255, 255, 255);

        // Transform Group Info for adding Zoom functionality to the Viewbox
        ScaleTransform trZoom = new ScaleTransform(1, 1);
        TranslateTransform trTransform = new TranslateTransform(1, 1);
        TransformGroup TGrp = new TransformGroup();

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
                    mPos = activeLayer.ToBmpLocation(mPos, (int)vBox.Width, (int)vBox.Height);
                    if (mPos != prevMLocation)
                    {
                        prevMLocation = mPos;
                        if (CurrentTool == "Brush")
                        {
                            activeLayer.Draw(mPos, brushSize, blendMode, System.Windows.Media.Color.FromArgb((byte)opacity, brushColor.R, brushColor.G, brushColor.B));
                        }
                        else if (CurrentTool == "Eraser")
                        {
                            activeLayer.Draw(mPos, brushSize, CurrentTool, System.Windows.Media.Color.FromArgb((byte)opacity, brushColor.R, brushColor.G, brushColor.B));
                        }
                        else if (CurrentTool == "EyeDropper")
                        {
                            // Change brush color to the color under the cursor
                            System.Windows.Media.Color tempColor = activeLayer.GetPixelColor((int)mPos.X, (int)mPos.Y);
                            brushColor = System.Windows.Media.Color.FromArgb(255, tempColor.R, tempColor.G, tempColor.B);
                        }
                    }
                }
            }
            else if (e.LeftButton == MouseButtonState.Released)
            {
                activeLayer.NewPass();
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
            activeLayer = newLayer;
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
    }
}
