using System;
using System.Collections.Generic;
using UnityEngine;

namespace Babodayo.Input
{
    [Serializable]
    public sealed class CommandBufferSettings
    {
        [Min(1), Tooltip("Attack input lifetime in combat frames, including creation frame.")]
        public int AttackFrames = 6;
        [Min(1), Tooltip("Jump input lifetime in combat frames, including creation frame.")]
        public int JumpFrames = 8;
        [Min(1), Tooltip("Other input lifetime in combat frames.")]
        public int OtherFrames = 6;
        [Min(1), Tooltip("Maximum queued commands. Overflow discards the oldest command.")]
        public int Capacity = 16;
        public int Lifetime(CombatCommand command) => Math.Max(1,
            command == CombatCommand.Space ? JumpFrames :
            command == CombatCommand.DoubleTapA || command == CombatCommand.DoubleTapD
                ? OtherFrames : AttackFrames);
    }

    public readonly struct BufferedCommand
    {
        public readonly CombatCommand Command;
        public readonly int CreatedFrame, ExpireFrame, Horizontal, Vertical;
        public BufferedCommand(CombatCommand command, int frame, int lifetime, int horizontal, int vertical)
        {
            Command = command; CreatedFrame = frame; ExpireFrame = frame + lifetime;
            Horizontal = horizontal; Vertical = vertical;
        }
    }

    public sealed class CommandBuffer
    {
        private readonly CommandBufferSettings settings;
        private readonly List<BufferedCommand> commands;
        public int Count => commands.Count;
        public CommandBuffer(CommandBufferSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            commands = new List<BufferedCommand>(Math.Max(1, settings.Capacity));
        }
        public void Push(CombatCommand command, InputFrame input, int? horizontal = null, int? vertical = null)
        {
            if (command == CombatCommand.None) return;
            Expire(input.Frame);
            while (commands.Count >= Math.Max(1, settings.Capacity)) commands.RemoveAt(0);
            commands.Add(new BufferedCommand(command, input.Frame, settings.Lifetime(command),
                horizontal ?? input.Horizontal, vertical ?? input.Vertical));
        }
        public void Expire(int frame) => commands.RemoveAll(command => frame >= command.ExpireFrame);
        public bool Peek(int frame, out BufferedCommand command)
        {
            Expire(frame);
            command = commands.Count > 0 ? commands[0] : default;
            return commands.Count > 0;
        }
        public bool TryConsume(CombatCommand requested, int frame, out BufferedCommand command)
        {
            Expire(frame);
            for (int i = 0; i < commands.Count; i++)
            {
                if (commands[i].Command != requested) continue;
                command = commands[i]; commands.RemoveAt(i); return true;
            }
            command = default; return false;
        }
        public void Clear() => commands.Clear();
        public void DeferExpiration(int frames)
        {
            if(frames<=0) return;
            for(int i=0;i<commands.Count;i++)
            {
                var c=commands[i];
                commands[i]=new BufferedCommand(c.Command,c.CreatedFrame,c.ExpireFrame-c.CreatedFrame+frames,c.Horizontal,c.Vertical);
            }
        }
        public int Remaining(CombatCommand requested,int frame)
        {
            Expire(frame);
            foreach(var command in commands) if(command.Command==requested) return command.ExpireFrame-frame;
            return 0;
        }
    }
}
