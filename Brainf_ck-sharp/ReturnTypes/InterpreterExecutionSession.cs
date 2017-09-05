﻿using System;
using Brainf_ck_sharp.Enums;
using Brainf_ck_sharp.Helpers;
using JetBrains.Annotations;

namespace Brainf_ck_sharp.ReturnTypes
{
    /// <summary>
    /// A class that represents an execution session for a script
    /// </summary>
    public sealed class InterpreterExecutionSession
    {
        #region Public APIs

        /// <summary>
        /// Gets the current result of the session
        /// </summary>
        [NotNull]
        public InterpreterResult CurrentResult { get; }

        /// <summary>
        /// Gets whether or not the instance can continue its execution from its current state
        /// </summary>
        public bool CanContinue => CurrentResult.ExitCode.HasFlag(InterpreterExitCode.Success) &&
                                   CurrentResult.ExitCode.HasFlag(InterpreterExitCode.BreakpointReached);

        /// <summary>
        /// Gets the overflow mode being used in the current session
        /// </summary>
        public OverflowMode Mode { get; }

        /// <summary>
        /// Continues the script execution from the current state
        /// </summary>
        [PublicAPI]
        [Pure, NotNull]
        public InterpreterExecutionSession Continue()
        {
            if (!CanContinue) throw new InvalidOperationException("The current session can't be continued");
            return Brainf_ckInterpreter.ContinueSession(Clone());
        }

        /// <summary>
        /// Continues the execution of the instance from the current state to the end of the code
        /// </summary>
        [PublicAPI]
        [Pure, NotNull]
        public InterpreterExecutionSession RunToCompletion()
        {
            if (!CanContinue) throw new InvalidOperationException("The current session can't continue from this state");
            return Brainf_ckInterpreter.RunSessionToCompletion(Clone());
        }

        #endregion

        #region Tools

        /// <summary>
        /// Gets the debug data associated with the current instance
        /// </summary>
        [NotNull]
        internal SessionDebugData DebugData { get; }

        /// <summary>
        /// Creates a new execution session
        /// </summary>
        /// <param name="result">The current result</param>
        /// <param name="data">The debug data associated with the current state</param>
        /// <param name="mode">Indicates the desired overflow mode for the script to run</param>
        internal InterpreterExecutionSession(InterpreterResult result, SessionDebugData data, OverflowMode mode)
        {
            CurrentResult = result;
            DebugData = data;
            Mode = mode;
        }

        /// <summary>
        /// Creates a new instance with a copy of the current values
        /// </summary>
        [Pure, NotNull]
        internal InterpreterExecutionSession Clone()
        {
            return new InterpreterExecutionSession(CurrentResult.Clone(), DebugData.Clone(), Mode);
        }

        #endregion
    }
}
