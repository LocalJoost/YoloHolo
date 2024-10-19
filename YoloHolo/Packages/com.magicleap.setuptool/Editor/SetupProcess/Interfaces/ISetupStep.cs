
using System;

namespace MagicLeap.SetupTool.Editor.Interfaces
{
    /// <summary>
    /// Interface for each setup step
    /// </summary>
    public interface ISetupStep
    {
        Action OnExecuteFinished { get; set; }
        bool  Block { get; }
        bool Busy { get; }
        bool IsComplete { get; }
        bool CanExecute { get; }
        bool Required { get; }
        /// <summary>
        /// Called when the setup window updates and when a step competes
        /// </summary>
        void Refresh();
        
        /// <summary>
        /// How to draw the Step
        /// </summary>
        /// <returns>Whether the step was executed. (returns true when the fix button is pressed).</returns>
        bool Draw();

        /// <summary>
        /// Action during step execution
        /// </summary>
        void Execute();

        /// <summary>
        /// Debuggable label with stats /// <inheritdoc cref="object.ToString"/>
        /// </summary>
        /// <returns></returns>
        string ToString();

    }
}