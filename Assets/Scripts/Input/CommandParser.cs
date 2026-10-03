using System;
using UnityEngine;

namespace Babodayo.Input
{
    [Serializable]
    public sealed class CommandParserSettings
    {
        [Range(2, 28), Tooltip("Maximum frames between same-direction presses, with neutral between them.")]
        public int DoubleTapFrames = 10;
        [Min(1), Tooltip("Hold duration for Shift+S+LMB, in combat frames.")] public int HoldFrames = 10;
        [Min(1), Tooltip("LMB grace after the second direction tap.")] public int DashAttackFrames = 6;
    }

    public sealed class CommandParser
    {
        private readonly CommandParserSettings settings;
        private int lastParsedFrame = -1;
        private int holdStart=-1, holdX, holdY, dashFrame=-1000, dashDirection;
        public CommandParser(CommandParserSettings settings)
            => this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

        public void Parse(InputHistory history, CommandBuffer output)
        {
            if (!history.TryGetRecent(0, out var input) || input.Frame <= lastParsedFrame) return;
            lastParsedFrame = input.Frame;
            bool doubleTap = IsDoubleTap(history, input);
            if (input.Horizontal == -dashDirection) dashDirection=0;
            if (doubleTap) { dashFrame=input.Frame; dashDirection=input.Horizontal; }
            if (input.WasPressed(InputButtons.LMB))
            {
                CombatCommand command;
                if (input.LmbShiftHeld && input.LmbVertical<0)
                {
                    holdStart=input.Frame; holdX=input.LmbHorizontal; holdY=input.LmbVertical;
                    command=CombatCommand.None;
                }
                else if (input.LmbShiftHeld)
                    command=input.LmbHorizontal!=0 ? CombatCommand.Shift_Direction_LMB : CombatCommand.Shift_LMB;
                else if (input.LmbVertical>0) command=CombatCommand.W_LMB;
                else if (dashDirection!=0 && input.Frame-dashFrame<settings.DashAttackFrames)
                {
                    command=dashDirection<0 ? CombatCommand.DoubleTapA_LMB : CombatCommand.DoubleTapD_LMB;
                    output.TryConsume(dashDirection<0 ? CombatCommand.DoubleTapA : CombatCommand.DoubleTapD,input.Frame,out _);
                    dashDirection=0;
                }
                else command=input.LmbHorizontal!=0 ? CombatCommand.Direction_LMB : CombatCommand.LMB;
                output.Push(command,input,input.LmbHorizontal,input.LmbVertical);
            }
            else if (doubleTap)
                output.Push(input.Horizontal<0 ? CombatCommand.DoubleTapA : CombatCommand.DoubleTapD,input);
            if (holdStart>=0)
            {
                bool held=input.IsHeld(InputButtons.LMB);
                if (!held || input.Frame-holdStart>=Math.Max(1,settings.HoldFrames))
                {
                    output.Push(held ? CombatCommand.Shift_S_LMB_Hold : CombatCommand.Shift_S_LMB,input,holdX,holdY);
                    holdStart=-1;
                }
            }
            if (input.WasPressed(InputButtons.RMB)) output.Push(CombatCommand.RMB, input);
            if (input.WasPressed(InputButtons.Jump)) output.Push(CombatCommand.Space, input);
        }

        private bool IsDoubleTap(InputHistory history, InputFrame current)
        {
            int direction = current.Horizontal;
            if (direction == 0 || !history.TryGetRecent(1, out var previous) || previous.Horizontal != 0)
                return false;
            for (int age = 2; history.TryGetRecent(age, out var candidate); age++)
            {
                if (current.Frame - candidate.Frame > settings.DoubleTapFrames) return false;
                if (candidate.Horizontal == -direction) return false;
                if (candidate.Horizontal != direction) continue;
                // Find the start of the earlier press, not the end of a long hold.
                if (history.TryGetRecent(age + 1, out var before))
                {
                    if (before.Horizontal != direction) return true;
                }
                else return false;
            }
            return false;
        }
        public void Reset() { lastParsedFrame=-1; holdStart=-1; dashFrame=-1000; dashDirection=0; }
    }
}
