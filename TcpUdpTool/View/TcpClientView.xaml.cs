using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TcpUdpTool.View
{
    /// <summary>
    /// Interaction logic for TcpClientView.xaml
    /// </summary>
    public partial class TcpClientView : UserControl
    {
        public TcpClientView()
        {
            InitializeComponent();
        }

        private void AutoSendInterval_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                e.Handled = true;
                return;
            }

            e.Handled = !IsValidPositiveIntegerInput(textBox, e.Text);
        }

        private void AutoSendInterval_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                e.CancelCommand();
                return;
            }

            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var pasteText = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
            if (!IsValidPositiveIntegerInput(textBox, pasteText))
            {
                e.CancelCommand();
            }
        }

        private static bool IsValidPositiveIntegerInput(TextBox textBox, string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            for (int i = 0; i < input.Length; i++)
            {
                if (!char.IsDigit(input[i]))
                {
                    return false;
                }
            }

            var proposedText = GetProposedText(textBox, input);
            return int.TryParse(proposedText, out var value) && value > 0;
        }

        private static string GetProposedText(TextBox textBox, string input)
        {
            var currentText = textBox.Text ?? string.Empty;
            var selectionStart = textBox.SelectionStart;
            var selectionLength = textBox.SelectionLength;

            if (selectionLength > 0 && selectionStart >= 0 && selectionStart + selectionLength <= currentText.Length)
            {
                currentText = currentText.Remove(selectionStart, selectionLength);
            }

            if (selectionStart < 0 || selectionStart > currentText.Length)
            {
                selectionStart = currentText.Length;
            }

            return currentText.Insert(selectionStart, input);
        }
    }
}
