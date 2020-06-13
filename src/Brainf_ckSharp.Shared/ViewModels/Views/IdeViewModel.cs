﻿using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Brainf_ckSharp.Constants;
using Brainf_ckSharp.Services;
using Brainf_ckSharp.Shared.Messages.Ide;
using Brainf_ckSharp.Shared.Messages.InputPanel;
using Brainf_ckSharp.Shared.Models.Ide;
using Brainf_ckSharp.Shared.ViewModels.Views.Abstract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;

namespace Brainf_ckSharp.Shared.ViewModels.Views
{
    /// <summary>
    /// A view model for a Brainf*ck/PBrain IDE
    /// </summary>
    public sealed class IdeViewModel : WorkspaceViewModelBase
    {
        /// <summary>
        /// The <see cref="IAnalyticsService"/> instance currently in use
        /// </summary>
        private readonly IAnalyticsService AnalyticsService = Ioc.Default.GetRequiredService<IAnalyticsService>();

        /// <summary>
        /// The <see cref="IFilesService"/> instance currently in use
        /// </summary>
        private readonly IFilesService FilesService = Ioc.Default.GetRequiredService<IFilesService>();

        /// <summary>
        /// The <see cref="IFilesManagerService"/> instance currently in use
        /// </summary>
        private readonly IFilesManagerService FilesManagerService = Ioc.Default.GetRequiredService<IFilesManagerService>();

        /// <summary>
        /// Creates a new <see cref="IdeViewModel"/> instance
        /// </summary>
        public IdeViewModel()
        {
            Messenger.Register<RunIdeScriptRequestMessage>(this, _ => ScriptRunRequested?.Invoke(this, EventArgs.Empty));
            Messenger.Register<DebugIdeScriptRequestMessage>(this, _ => ScriptDebugRequested?.Invoke(this, EventArgs.Empty));
            Messenger.Register<InsertNewLineRequestMessage>(this, _ => CharacterAdded?.Invoke(this, Characters.CarriageReturn));
            Messenger.Register<DeleteCharacterRequestMessage>(this, _ => CharacterDeleted?.Invoke(this, EventArgs.Empty));
            Messenger.Register<PickOpenFileRequestMessage>(this, m => _ = TryLoadTextFromFileAsync(m.Favorite));
            Messenger.Register<LoadSourceCodeRequestMessage>(this, m => LoadSourceCode(m.Value));
            Messenger.Register<NewFileRequestMessage>(this, _ => LoadNewFile());
            Messenger.Register<SaveFileRequestMessage>(this, m => _ = TrySaveTextAsync());
            Messenger.Register<SaveFileAsRequestMessage>(this, m => _ = TrySaveTextAsAsync());
            Messenger.Register<SaveIdeStateRequestMessage>(this, m => m.ReportResult(SaveStateAsync()));
        }

        /// <summary>
        /// Raised whenever a script is requested to be run
        /// </summary>
        public event EventHandler? ScriptRunRequested;

        /// <summary>
        /// Raised whenever a script is requested to be debugged
        /// </summary>
        public event EventHandler? ScriptDebugRequested;

        /// <summary>
        /// Raised whenever a new character is requested to be added to the current text
        /// </summary>
        public event EventHandler<char>? CharacterAdded;

        /// <summary>
        /// Raised whenever a character is requested to be deleted
        /// </summary>
        public event EventHandler? CharacterDeleted;

        /// <summary>
        /// Raised whenever a new source code is loaded and used as a reference
        /// </summary>
        public event EventHandler<string>? CodeLoaded;

        /// <summary>
        /// Raised whenever the current source code is saved by the user
        /// </summary>
        public event EventHandler? CodeSaved;

        /// <summary>
        /// Raised whenever the state is restored from a serialized one
        /// </summary>
        public event EventHandler<IdeState>? StateRestored;

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            Messenger.Register<OperatorKeyPressedNotificationMessage>(this, m => CharacterAdded?.Invoke(this, m.Value));
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            Messenger.Unregister<OperatorKeyPressedNotificationMessage>(this);
        }

        /// <inheritdoc/>
        protected override void OnCodeChanged(SourceCode code)
        {
            FilesManagerService.RegisterFile(code.File);
        }

        /// <summary>
        /// Loads a specific <see cref="SourceCode"/> instance
        /// </summary>
        /// <param name="code">The source code to load</param>
        private void LoadSourceCode(SourceCode code)
        {
            AnalyticsService.Log(Constants.Events.LoadLibrarySourceCode);

            if (!(code.File is null) &&
                FilesManagerService.TrySwitchTo(code.File))
            {
                AnalyticsService.Log(Constants.Events.SwitchToFile);

                return;
            }

            Code = code;

            CodeLoaded?.Invoke(this, Code.Content);
        }

        /// <summary>
        /// Loads an empty source code
        /// </summary>
        private void LoadNewFile()
        {
            Code = SourceCode.CreateEmpty();

            CodeLoaded?.Invoke(this, Code.Content);
        }

        /// <summary>
        /// Tries to open and load a source code file
        /// </summary>
        /// <param name="favorite">Whether to immediately mark the item as favorite</param>
        private async Task TryLoadTextFromFileAsync(bool favorite)
        {
            AnalyticsService.Log(Constants.Events.PickFileRequest);

            if (!(await FilesService.TryPickOpenFileAsync(".bfs") is IFile file)) return;

            if (FilesManagerService.TrySwitchTo(file))
            {
                AnalyticsService.Log(Constants.Events.SwitchToFile);

                return;
            }

            AnalyticsService.Log(Constants.Events.LoadPickedFile, (nameof(CodeMetadata.IsFavorited), favorite.ToString()));

            if (await SourceCode.TryLoadFromEditableFileAsync(file) is SourceCode code)
            {
                // Set the favorite state, if requested
                if (favorite)
                {
                    code.Metadata.IsFavorited = true;

                    await code.TrySaveAsync();
                }

                Code = code;

                CodeLoaded?.Invoke(this, Code.Content);
            }
        }

        /// <summary>
        /// Tries to open and load a source code file
        /// </summary>
        /// <param name="file">The file to open</param>
        private async Task TryLoadTextFromFileAsync(IFile file)
        {
            AnalyticsService.Log(Constants.Events.LoadProtocolFile);

            if (await SourceCode.TryLoadFromEditableFileAsync(file) is SourceCode code)
            {
                Code = code;

                CodeLoaded?.Invoke(this, Code.Content);
            }
        }

        /// <summary>
        /// Tries to save the current text to the current file, if possible
        /// </summary>
        private async Task TrySaveTextAsync()
        {
            if (Code.File == null) await TrySaveTextAsAsync();
            else
            {
                Code.Content = Text.ToString();

                await Code.TrySaveAsync();

                CodeSaved?.Invoke(this, EventArgs.Empty);

                ReportCodeSaved();
            }
        }

        /// <summary>
        /// Tries to save the current text to a new file
        /// </summary>
        private async Task TrySaveTextAsAsync()
        {
            if (!(await FilesService.TryPickSaveFileAsync(string.Empty, (string.Empty, ".bfs")) is IFile file)) return;

            if (FilesManagerService.TrySwitchTo(file))
            {
                AnalyticsService.Log(Constants.Events.SwitchToFile);

                return;
            }

            Code.Content = Text.ToString();

            await Code.TrySaveAsAsync(file);

            CodeSaved?.Invoke(this, EventArgs.Empty);

            ReportCodeSaved();
        }

        /// <summary>
        /// Serializes and saves the state of the current instance
        /// </summary>
        private async Task SaveStateAsync()
        {
            IdeState state = new IdeState
            {
                FilePath = Code.File?.Path,
                Text = Text.ToString(),
                Row = Row,
                Column = Column
            };

            string
                json = JsonSerializer.Serialize(state),
                temporaryPath = FilesService.TemporaryFilesPath,
                statePath = Path.Combine(temporaryPath, "state.json");

            IFile file = await FilesService.CreateOrOpenFileFromPathAsync(statePath);

            using Stream stream = await file.OpenStreamForWriteAsync();

            stream.SetLength(0);

            using StreamWriter writer = new StreamWriter(stream);

            await writer.WriteAsync(json);
        }

        /// <summary>
        /// Loads and restores the serialized state of the current instance, if available
        /// </summary>
        /// <param name="file">The optional file to load, if present</param>
        public async Task RestoreStateAsync(IFile? file)
        {
            if (file is null)
            {
                string
                    temporaryPath = FilesService.TemporaryFilesPath,
                    statePath = Path.Combine(temporaryPath, "state.json");

                if (!(await FilesService.GetFileFromPathAsync(statePath) is IFile jsonFile))
                    return;

                using Stream stream = await jsonFile.OpenStreamForReadAsync();
                using StreamReader reader = new StreamReader(stream);

                string json = await reader.ReadToEndAsync();

                IdeState state = JsonSerializer.Deserialize<IdeState>(json);

                if (state.FilePath is null) Code = SourceCode.CreateEmpty();
                else
                {
                    IFile? sourceFile = await FilesService.TryGetFileFromPathAsync(state.FilePath);

                    if (sourceFile is null) Code = SourceCode.CreateEmpty();
                    else Code = await SourceCode.TryLoadFromEditableFileAsync(sourceFile) ?? SourceCode.CreateEmpty();
                }

                Text = state.Text.AsMemory();
                Row = state.Row;
                Column = state.Column;

                StateRestored?.Invoke(this, state);
            }
            else await TryLoadTextFromFileAsync(file);
        }
    }
}