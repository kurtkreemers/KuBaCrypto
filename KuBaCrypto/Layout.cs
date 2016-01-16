using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;




namespace KuBaCrypto
{
    public class Layout 
    {
        public static void ClearTextbox(Grid grid)
        {         
            foreach (var item in grid.Children.OfType<TextBox>())
            {
                item.Clear();  
            }
        }

        public static void ButtonEnable(Grid grid, bool enable)
        {
            foreach (var item in grid.Children.OfType<Button>().Where(b => b.Name.Contains("bt_")))
            {
                if (enable)
                    item.IsEnabled = true;
                else
                    item.IsEnabled = false;
            }
        }
        public static void LabelClear(Grid grid)
        {
            foreach (var item in grid.Children.OfType<Label>().Where(l => l.Name != ""))
            {
                item.Content = "";
            }
        }
    
      public static bool CheckFile(string btTag, string dirPath)
        {
            string filePriv = dirPath + @"\private_" + btTag + ".xml";
            string filePubl = dirPath + @"\public_" + btTag + ".xml";
            if (File.Exists(filePriv) && File.Exists(filePubl))
            {
                return true;
            }
            else
                return false;
        }

        public static void ControlsHidden(Grid grid,bool hidden, string name)
      {
          var result = new List<Control>();
          for (int x = 0; x < VisualTreeHelper.GetChildrenCount(grid); x++)
          {
              DependencyObject child = VisualTreeHelper.GetChild(grid, x);
              var instance = child as Control;

              if (null != instance)
                  result.Add(instance);
          }
          var controls = result.Where(t => t.Name.Contains(name));
          foreach (var item in controls)
          {
              if (hidden)
                  item.Visibility = Visibility.Hidden;
              else
                  item.Visibility = Visibility.Visible;
          }         
      }
        public static OpenFileDialog OpenXMLFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = ".xml";
            ofd.Filter = "Xml files (.xml)|*.xml";
            return ofd;
        }
    }
}
