﻿// Copyright (c) 2016 The btcsuite developers
// Copyright (c) 2016 The Decred developers
// Licensed under the ISC license.  See LICENSE file in the project root for full license information.

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

namespace Paymetheus
{
    /// <summary>
    /// Interaction logic for CreateAccountDialogView.xaml
    /// </summary>
    public partial class CreateAccountDialogView : UserControl
    {
        public CreateAccountDialogView()
        {
            InitializeComponent();
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = (TextBox)sender;
                var prop = TextBox.TextProperty;

                var binding = BindingOperations.GetBindingExpression(textBox, prop);
                binding?.UpdateSource();
            }
        }

        private void passphraseTextBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext != null)
            {
                ((dynamic)DataContext).Passphrase = ((PasswordBox)sender).Password;
            }
        }
    }
}
