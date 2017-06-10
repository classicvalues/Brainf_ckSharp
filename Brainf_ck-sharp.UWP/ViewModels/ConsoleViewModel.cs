﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Brainf_ck_sharp;
using Brainf_ck_sharp.ReturnTypes;
using Brainf_ck_sharp_UWP.DataModels.ConsoleModels;
using Brainf_ck_sharp_UWP.Helpers;
using Brainf_ck_sharp_UWP.Messages;
using Brainf_ck_sharp_UWP.Messages.Actions;
using Brainf_ck_sharp_UWP.Messages.IDEStatus;
using GalaSoft.MvvmLight.Messaging;

namespace Brainf_ck_sharp_UWP.ViewModels
{
    public class ConsoleViewModel : ItemsCollectionViewModelBase<ConsoleCommandModelBase>
    {
        public ConsoleViewModel()
        {
            Source.Add(new ConsoleUserCommand());
        }

        /// <summary>
        /// Raised whenever a new console line is added to the source collection or edited
        /// </summary>
        public event EventHandler ConsoleLineAddedOrModified;

        private bool _IsEnabled;

        /// <summary>
        /// Gets or sets whether or not the instance is enabled and it is processing incoming messages
        /// </summary>
        public bool IsEnabled
        {
            get => _IsEnabled;
            set
            {
                if (Set(ref _IsEnabled, value))
                {
                    if (value)
                    {
                        Messenger.Default.Register<OperatorAddedMessage>(this, op => TryAddCommandCharacter(op.Operator));
                        Messenger.Default.Register<PlayScriptMessage>(this, m => ExecuteCommand().Forget());
                        Messenger.Default.Register<ClearConsoleLineMessage>(this, m => TryResetCommand());
                        Messenger.Default.Register<UndoConsoleCharacterMessage>(this, m => TryUndoLastCommandCharacter());
                        Messenger.Default.Register<RestartConsoleMessage>(this, m => Restart());
                    }
                    else Messenger.Default.Unregister(this);
                }
            }
        }

        /// <summary>
        /// The current machine state to use to process the scripts
        /// </summary>
        private TouringMachineState _State = new TouringMachineState(64);

        private bool _CanRestart;

        /// <summary>
        /// Gets whether or not the console can be restarted from its current state
        /// </summary>
        public bool CanRestart
        {
            get => _CanRestart;
            private set
            {
                if (Set(ref _CanRestart, value))
                    Messenger.Default.Send(new ConsoleAvailableActionStatusChangedMessage(ConsoleAction.Restart, value));
            }
        }

        /// <summary>
        /// Restarts the console and resets the current state
        /// </summary>
        public void Restart()
        {
            if (!CanRestart) return;
            CanRestart = false;
            Source.Add(new ConsoleRestartCommand());
            _State = new TouringMachineState(64);
            Source.Add(new ConsoleUserCommand());
        }

        /// <summary>
        /// Gets whether or not there is an available user command to execute
        /// </summary>
        public bool CommandAvailable => Source.LastOrDefault() is ConsoleUserCommand command &&
                                        command.Command.Length > 0;

        // Sends both the undo and clear messages
        private void SendCommandAvailableMessages(bool status)
        {
            Messenger.Default.Send(new ConsoleAvailableActionStatusChangedMessage(ConsoleAction.Play, status));
            Messenger.Default.Send(new ConsoleAvailableActionStatusChangedMessage(ConsoleAction.Undo, status));
            Messenger.Default.Send(new ConsoleAvailableActionStatusChangedMessage(ConsoleAction.Clear, status));
            if (status && 
                Source.Last() is ConsoleUserCommand command &&
                command.Command.Length > 0)
            {
                (bool valid, int error) = Brainf_ckInterpreter.CheckSourceSyntax(command.Command);
                Messenger.Default.Send(valid 
                    ? new ConsoleStatusUpdateMessage(IDEStatus.Console, "Ready", command.Command.Length, 0) 
                    : new ConsoleStatusUpdateMessage(IDEStatus.FaultedConsole, "Warning", command.Command.Length, error));
            }
            else Messenger.Default.Send(new ConsoleStatusUpdateMessage(IDEStatus.Console, "Ready", 0, 0));
        }

        /// <summary>
        /// Tries to delete the last character in the active command line
        /// </summary>
        public void TryUndoLastCommandCharacter()
        {
            if (!CommandAvailable) return;
            ConsoleUserCommand command = (ConsoleUserCommand)Source.Last();
            command.UpdateCommand(command.Command.Substring(0, command.Command.Length - 1));
            if (!CommandAvailable) SendCommandAvailableMessages(false);
        }

        /// <summary>
        /// Tries to reset the text in the active command
        /// </summary>
        public void TryResetCommand()
        {
            if (!CommandAvailable) return;
            ConsoleUserCommand command = (ConsoleUserCommand)Source.Last();
            command.UpdateCommand(String.Empty);
            SendCommandAvailableMessages(false);
        }

        /// <summary>
        /// Executes the current user command, if possible
        /// </summary>
        public async Task ExecuteCommand()
        {
            if (!CommandAvailable) return;
            CanRestart = true;
            SendCommandAvailableMessages(false);
            String command = ((ConsoleUserCommand)Source.LastOrDefault()).Command;
            InterpreterResult result = await Task.Run(() => Brainf_ckInterpreter.Run(command, String.Empty, _State, 1000));
            if (result.HasFlag(InterpreterExitCode.Success) &&
                result.HasFlag(InterpreterExitCode.TextOutput))
            {
                // Text output
                Source.Add(new ConsoleCommandResult(result.Output));
            }
            else if (result.HasFlag(InterpreterExitCode.MismatchedParentheses))
            {
                // Syntax error
                Source.Add(new ConsoleExceptionResult(ConsoleExceptionType.SyntaxError, LocalizationManager.GetResource("WrongBrackets")));
            }
            else if (result.HasFlag(InterpreterExitCode.InternalException))
            {
                // Interpreter error
                Source.Add(new ConsoleExceptionResult(ConsoleExceptionType.InternalError, LocalizationManager.GetResource("InterpreterError")));
            }
            else if (result.HasFlag(InterpreterExitCode.ThresholdExceeded))
            {
                // Possible infinite loop
                Source.Add(new ConsoleExceptionResult(ConsoleExceptionType.RuntimeError, LocalizationManager.GetResource("ThresholdExceeded")));
            }
            else if (result.HasFlag(InterpreterExitCode.ExceptionThrown))
            {
                // Handled exception
                String message;
                if (result.HasFlag(InterpreterExitCode.LowerBoundExceeded)) message = LocalizationManager.GetResource("ExLowerBound");
                else if (result.HasFlag(InterpreterExitCode.UpperBoundExceeded)) message = LocalizationManager.GetResource("ExUpperBound");
                else if (result.HasFlag(InterpreterExitCode.NegativeValue)) message = LocalizationManager.GetResource("ExNegativeValue");
                else if (result.HasFlag(InterpreterExitCode.MaxValueExceeded)) message = LocalizationManager.GetResource("ExMaxValue");
                else if (result.HasFlag(InterpreterExitCode.StdinBufferExhausted)) message = LocalizationManager.GetResource("ExEmptyStdin");
                else if (result.HasFlag(InterpreterExitCode.StdoutBufferLimitExceeded)) message = LocalizationManager.GetResource("ExMaxStdout");
                else throw new InvalidOperationException("The interpreter exception type isn't valid");
                Source.Add(new ConsoleExceptionResult(ConsoleExceptionType.RuntimeError, message));
            }
            _State = result.MachineState;

            // New user command
            Source.Add(new ConsoleUserCommand());
            ConsoleLineAddedOrModified?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Tries to add a new operator to the active user command line
        /// </summary>
        /// <param name="c">The new operator to add</param>
        public void TryAddCommandCharacter(char c)
        {
            if (!Brainf_ckInterpreter.Operators.Contains(c)) throw new ArgumentException("The input character is invalid");
            if (Source.LastOrDefault() is ConsoleUserCommand command)
            {
                command.UpdateCommand($"{command.Command}{c}");
                SendCommandAvailableMessages(true);
            }
            ConsoleLineAddedOrModified?.Invoke(this, EventArgs.Empty);
        }
    }
}
