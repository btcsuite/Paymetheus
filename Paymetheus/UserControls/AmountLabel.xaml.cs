﻿using Paymetheus.Decred;
using Paymetheus.Framework.TypeConverters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for AmountLabel.xaml
    /// </summary>
    public partial class AmountLabel : UserControl
    {
        public AmountLabel()
        {
            InitializeComponent();
        }

        [TypeConverter(typeof(AmountTypeConverter))]
        public Amount Value
        {
            get { return (Amount)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public Denomination Denomination
        {
            get { return (Denomination)GetValue(DenominationProperty); }
            set { SetValue(DenominationProperty, value); }
        }

        public double FontSizeWhole
        {
            get { return (double)GetValue(FontSizeWholeProperty); }
            set { SetValue(FontSizeWholeProperty, value); }
        }

        public double FontSizeDecimal
        {
            get { return (double)GetValue(FontSizeDecimalProperty); }
            set { SetValue(FontSizeDecimalProperty, value); }
        }

        public Brush ForegroundWhole
        {
            get { return (Brush)GetValue(ForegroundWholeProperty); }
            set { SetValue(ForegroundWholeProperty, value); }
        }

        public Brush ForegroundDecimal
        {
            get { return (Brush)GetValue(ForegroundDecimalProperty); }
            set { SetValue(ForegroundDecimalProperty, value); }
        }

        public FontWeight FontWeightWhole
        {
            get { return (FontWeight)GetValue(FontWeightWholeProperty); }
            set { SetValue(FontWeightWholeProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(Amount), typeof(AmountLabel), new PropertyMetadata((Amount)0, new PropertyChangedCallback(OnValueChanged)));

        public static readonly DependencyProperty DenominationProperty =
            DependencyProperty.Register(nameof(Denomination), typeof(Denomination), typeof(AmountLabel), new PropertyMetadata(Denomination.Decred, new PropertyChangedCallback(OnDenominationChanged)));

        public static readonly DependencyProperty FontSizeWholeProperty =
            DependencyProperty.Register(nameof(FontSizeWhole), typeof(double), typeof(AmountLabel), new PropertyMetadata((double)12, new PropertyChangedCallback(OnFontSizeWholeChanged)));

        public static readonly DependencyProperty FontSizeDecimalProperty =
            DependencyProperty.Register(nameof(FontSizeDecimal), typeof(double), typeof(AmountLabel), new PropertyMetadata((double)12, new PropertyChangedCallback(OnFontSizeDecimalChanged)));

        public static readonly DependencyProperty ForegroundWholeProperty =
            DependencyProperty.Register(nameof(ForegroundWhole), typeof(Brush), typeof(AmountLabel), new PropertyMetadata(Brushes.Black, new PropertyChangedCallback(OnForegroundWholeChanged)));

        public static readonly DependencyProperty ForegroundDecimalProperty =
            DependencyProperty.Register(nameof(ForegroundDecimal), typeof(Brush), typeof(AmountLabel), new PropertyMetadata(Brushes.Black, new PropertyChangedCallback(OnForegroundDecimalChanged)));

        public static readonly DependencyProperty FontWeightWholeProperty =
            DependencyProperty.Register(nameof(FontWeightWhole), typeof(FontWeight), typeof(AmountLabel), new PropertyMetadata(FontWeights.Normal, new PropertyChangedCallback(OnFontWeightWholeChanged)));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (AmountLabel)d;
            var value = (Amount)e.NewValue;

            var splitValue = control.Denomination.Split(value);
            var negativeSign = value < 0 ? "-" : "";

            control.WholePartRun.Text = string.Format("{0}{1:#,0}", negativeSign, splitValue.Item1);
            control.DecimalPartRun.Text = string.Format("{0:.0#######}", splitValue.Item2);
        }

        private static void OnDenominationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (AmountLabel)d;
            var denomination = (Denomination)e.NewValue;
            control.TickerRun.Text = " " + denomination.Ticker;
        }

        private static void OnFontSizeWholeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (AmountLabel)d;
            var fontSize = (double)e.NewValue;
            control.WholePartRun.FontSize = fontSize;
        }

        private static void OnFontSizeDecimalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (AmountLabel)d;
            var fontSize = (double)e.NewValue;
            control.DecimalPartRun.FontSize = fontSize;
            control.TickerRun.FontSize = fontSize;
        }

        private static void OnForegroundWholeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (AmountLabel)d;
            var brush = (Brush)e.NewValue;
            control.WholePartRun.Foreground = brush;
            control.TickerRun.Foreground = brush;
        }

        private static void OnForegroundDecimalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (AmountLabel)d;
            var brush = (Brush)e.NewValue;
            control.DecimalPartRun.Foreground = brush;
        }

        private static void OnFontWeightWholeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (AmountLabel)d;
            control.WholePartRun.FontWeight = (FontWeight)e.NewValue;
        }
    }
}
