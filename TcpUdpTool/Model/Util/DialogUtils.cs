using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TcpUdpTool.Model.Util
{
    public class DialogUtils
    {

        public static void ShowErrorDialog(string message)
        {
            MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

    }
}
