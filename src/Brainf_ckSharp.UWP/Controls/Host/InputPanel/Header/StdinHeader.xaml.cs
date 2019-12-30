﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Brainf_ckSharp.Uwp.Constants;
using Brainf_ckSharp.Uwp.Messages.InputPanel;
using Brainf_ckSharp.Uwp.Services.Keyboard;
using Brainf_ckSharp.Uwp.Services.Settings;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;

namespace Brainf_ckSharp.Uwp.Controls.Host.InputPanel.Header
{
    public sealed partial class StdinHeader : UserControl
    {
        // Constants for the visual states
        private const string KeyboardSelectedVisualStateName = "KeyboardSelected";
        private const string MemoryViewerSelectedVisualStateName = "MemoryViewerSelected";

        public StdinHeader()
        {
            this.InitializeComponent();

            Messenger.Default.Register<StdinRequestMessage>(this, ExtractStdinBuffer);
        }

        /// <summary>
        /// Gets or sets the currently selected index for the shell
        /// </summary>
        public int ShellSelectedIndex
        {
            get => (int)GetValue(ShellSelectedIndexProperty);
            set => SetValue(ShellSelectedIndexProperty, value);
        }

        /// <summary>
        /// The dependency property for <see cref="ShellSelectedIndex"/>.
        /// </summary>
        public static readonly DependencyProperty ShellSelectedIndexProperty = DependencyProperty.Register(
            nameof(ShellSelectedIndex),
            typeof(int),
            typeof(StdinHeader),
            new PropertyMetadata(default(int)));

        /// <summary>
        /// Gets or sets the currently selected index for the stdin header buttons
        /// </summary>
        public int StdinSelectedIndex
        {
            get => (int)GetValue(StdinSelectedIndexProperty);
            set => SetValue(StdinSelectedIndexProperty, value);
        }

        /// <summary>
        /// The dependency property for <see cref="StdinSelectedIndex"/>.
        /// </summary>
        public static readonly DependencyProperty StdinSelectedIndexProperty = DependencyProperty.Register(
            nameof(StdinSelectedIndex),
            typeof(int),
            typeof(StdinHeader),
            new PropertyMetadata(default(int), OnStdinSelectedIndexPropertyChanged));

        /// <summary>
        /// Updates the UI when <see cref="StdinSelectedIndex"/> changes
        /// </summary>
        /// <param name="d">The source <see cref="DependencyObject"/> instance</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> info for the current update</param>
        private static void OnStdinSelectedIndexPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StdinHeader @this = (StdinHeader)d;
            int index = (int)e.NewValue;
            VisualStateManager.GoToState(@this, index == 0 ? KeyboardSelectedVisualStateName : MemoryViewerSelectedVisualStateName, false);
        }

        // Sets the selected index to 0 when the keyboard button is clicked
        private void VirtualKeyboardHeaderSelected(object sender, EventArgs e) => StdinSelectedIndex = 0;

        // Sets the selected index to 1 when the memory viewer button is clicked
        private void MemoryViewerHeaderSelected(object sender, EventArgs e) => StdinSelectedIndex = 1;

        // Sets the selected index to 0 when the memory viewer button is deselected
        private void MemoryViewerHeaderDeselected(object sender, EventArgs e) => StdinSelectedIndex = 0;

        /// <summary>
        /// The <see cref="ISettingsService"/> currently in use
        /// </summary>
        private readonly ISettingsService SettingsService = SimpleIoc.Default.GetInstance<ISettingsService>();

        /// <summary>
        /// Handles a request for the current stdin buffer
        /// </summary>
        /// <param name="request">The input request message for the stdin buffer</param>
        private void ExtractStdinBuffer(StdinRequestMessage request)
        {
            request.ReportResult(StdinBox.Text);

            // Clear the buffer if requested
            if (SettingsService.GetValue<bool>(SettingsKeys.ClearStdinBufferOnRequest))
            {
                StdinBox.Text = string.Empty;
            }
        }

        // Disables the keyboard listener service while the stdin buffer box is focused
        private void StdinBox_OnGotFocus(object sender, RoutedEventArgs e) => SimpleIoc.Default.GetInstance<IKeyboardListenerService>().IsEnabled = false;

        // Ensures the keyboard listener service is enabled when the stdin buffer box loses focus
        private void StdinBox_OnLostFocus(object sender, RoutedEventArgs e) => SimpleIoc.Default.GetInstance<IKeyboardListenerService>().IsEnabled = true;
    }
}
