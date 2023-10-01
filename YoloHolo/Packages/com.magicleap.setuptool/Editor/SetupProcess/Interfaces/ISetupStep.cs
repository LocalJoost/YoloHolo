
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
        /// <summary>
        /// How to draw the Step
        /// </summary>
        /// <returns>Whether or not the item drew correctly.</returns>
        bool Draw();

        /// <summary>
        /// Action during step execution
        /// </summary>
        void Execute();

        void Refresh();
    }
}