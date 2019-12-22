﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Brainf_ck_sharp.Legacy.UWP.ViewModels.Abstract;
using JetBrains.Annotations;

namespace Brainf_ck_sharp.Legacy.UWP.ViewModels.FlyoutsViewModels
{
    public class UnicodeCharactersGuideFlyoutViewModel : ItemsCollectionViewModelBase<int>
    {
        private ObservableCollection<int> _SecondSource = new ObservableCollection<int>();

        /// <summary>
        /// Gets the second items collection for the current instance
        /// </summary>
        public ObservableCollection<int> SecondSource
        {
            get => _SecondSource;
            private set => Set(ref _SecondSource, value);
        }

        // An actions that animates the first fixed UI element
        private readonly Action DisplayFirstControlGroup;

        // An actions that animates the second fixed UI element
        private readonly Action DisplaySecondControlGroup;

        /// <summary>
        /// Creates a new instance with the given functions from the related UI view
        /// </summary>
        /// <param name="first">An action that shows the first group of control characters</param>
        /// <param name="second">An action that shows the second group of control characters</param>
        public UnicodeCharactersGuideFlyoutViewModel([NotNull] Action first, [NotNull] Action second)
        {
            DisplayFirstControlGroup = first;
            DisplaySecondControlGroup = second;
        }

        /// <summary>
        /// Raised when the characters loading completes
        /// </summary>
        public event EventHandler LoadingCompleted;

        /// <summary>
        /// Initializes the list of characters to display
        /// </summary>
        public async Task LoadAsync()
        {
            Tuple<IEnumerable<int>, IEnumerable<int>> sources = await Task.Run(() => Tuple.Create<IEnumerable<int>, IEnumerable<int>>(
                Enumerable.Range(32, 96).ToArray(), Enumerable.Range(160, 96).ToArray()));
            DisplayFirstControlGroup();
            Source = new ObservableCollection<int>(sources.Item1);
            DisplaySecondControlGroup();
            SecondSource = new ObservableCollection<int>(sources.Item2);
            LoadingCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        public override void Cleanup()
        {
            base.Cleanup();
            SecondSource.Clear();
            SecondSource = null;
            LoadingCompleted = null;
        }
    }
}