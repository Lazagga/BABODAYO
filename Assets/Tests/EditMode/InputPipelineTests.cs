using System;
using Babodayo.Input;
using NUnit.Framework;

namespace Babodayo.Tests
{
    public sealed class InputPipelineTests
    {
        private static InputFrame Frame(int frame, int x = 0, int y = 0,
            InputButtons pressed = InputButtons.None, InputButtons held = InputButtons.None)
            => new InputFrame(frame, x, y, pressed, held, InputButtons.None);

        [TestCase(1,0,false,CombatCommand.Direction_LMB)]
        [TestCase(1,0,true,CombatCommand.Shift_Direction_LMB)]
        [TestCase(1,1,false,CombatCommand.W_LMB)]
        public void ExtendedChordsAreExclusive(int x,int y,bool shift,CombatCommand expected)
        {
            var h=new InputHistory(); var b=new CommandBuffer(new CommandBufferSettings());
            h.Add(Frame(0,x,y,InputButtons.LMB,shift ? InputButtons.Shift : InputButtons.None));
            new CommandParser(new CommandParserSettings()).Parse(h,b);
            Assert.That(b.Count,Is.EqualTo(1)); Assert.That(b.TryConsume(expected,0,out _),Is.True);
        }
        [TestCase(false)] [TestCase(true)]
        public void HoldChordEmitsExactlyOneCommandOnReleaseOrThreshold(bool hold)
        {
            var h=new InputHistory(); var b=new CommandBuffer(new CommandBufferSettings());
            var p=new CommandParser(new CommandParserSettings { HoldFrames=10 });
            h.Add(Frame(0,0,-1,InputButtons.LMB,InputButtons.LMB|InputButtons.Shift)); p.Parse(h,b);
            Assert.That(b.Count,Is.Zero);
            for(int f=1;f<=10;f++)
            {
                h.Add(Frame(f,held: hold || f<3 ? InputButtons.LMB : InputButtons.None)); p.Parse(h,b);
                if(!hold && f==3) { Assert.That(b.TryConsume(CombatCommand.Shift_S_LMB,f,out _),Is.True); }
            }
            if(hold) Assert.That(b.TryConsume(CombatCommand.Shift_S_LMB_Hold,10,out _),Is.True);
            h.Add(Frame(11)); p.Parse(h,b); Assert.That(b.Count,Is.Zero);
        }
        [TestCase(-1)] [TestCase(1)]
        public void DashAttackAcceptsDelayedClickAndRemovesQueuedDash(int direction)
        {
            var h=new InputHistory(); var b=new CommandBuffer(new CommandBufferSettings());
            var p=new CommandParser(new CommandParserSettings());
            int[] axis={0,direction,0,direction,direction};
            for(int f=0;f<axis.Length;f++)
            { h.Add(Frame(f,axis[f],pressed:f==4 ? InputButtons.LMB : InputButtons.None)); p.Parse(h,b); }
            Assert.That(b.Count,Is.EqualTo(1));
            Assert.That(b.TryConsume(direction<0 ? CombatCommand.DoubleTapA_LMB : CombatCommand.DoubleTapD_LMB,4,out _),Is.True);
        }

        [Test]
        public void HistoryWrapsAndRejectsDuplicateFrames()
        {
            var history = new InputHistory(3);
            for (int frame = 0; frame < 5; frame++) history.Add(Frame(frame));
            Assert.That(history.Count, Is.EqualTo(3));
            Assert.That(history.TryGetRecent(0, out var newest), Is.True);
            Assert.That(newest.Frame, Is.EqualTo(4));
            history.TryGetRecent(2, out var oldest);
            Assert.That(oldest.Frame, Is.EqualTo(2));
            Assert.That(history.TryGetRecent(3, out _), Is.False);
            Assert.Throws<ArgumentException>(() => history.Add(Frame(4)));
        }

        [TestCase(0, InputButtons.None, CombatCommand.LMB)]
        [TestCase(0, InputButtons.Shift, CombatCommand.Shift_LMB)]
        [TestCase(-1, InputButtons.Shift, CombatCommand.Shift_S_LMB)]
        public void PrimaryPressProducesOnlyMostSpecificCommand(int y, InputButtons held, CombatCommand expected)
        {
            var history = new InputHistory();
            var buffer = new CommandBuffer(new CommandBufferSettings());
            var parser = new CommandParser(new CommandParserSettings());
            history.Add(Frame(1, y: y, pressed: InputButtons.LMB, held: held));
            parser.Parse(history, buffer);
            parser.Parse(history, buffer);
            Assert.That(buffer.Count, Is.EqualTo(1));
            Assert.That(buffer.TryConsume(expected, 1, out _), Is.True);
            Assert.That(buffer.TryConsume(expected, 1, out _), Is.False);
        }

        [TestCase(1, CombatCommand.DoubleTapD)]
        [TestCase(-1, CombatCommand.DoubleTapA)]
        public void DoubleTapRequiresNeutralAndPreservesDirection(int direction, CombatCommand expected)
        {
            var history = new InputHistory();
            history.Add(Frame(0)); history.Add(Frame(1, direction));
            history.Add(Frame(2)); history.Add(Frame(11, direction));
            var buffer = new CommandBuffer(new CommandBufferSettings());
            new CommandParser(new CommandParserSettings()).Parse(history, buffer);
            Assert.That(buffer.TryConsume(expected, 11, out var command), Is.True);
            Assert.That(command.Horizontal, Is.EqualTo(direction));
        }

        [TestCase(new int[] { 0, 1, 1 })]
        [TestCase(new int[] { 0, 1, 0, -1, 0, 1 })]
        [TestCase(new int[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1 })]
        public void HoldOppositeAndExpiredFirstPressDoNotDoubleTap(int[] directions)
        {
            var history = new InputHistory();
            for (int i = 0; i < directions.Length; i++) history.Add(Frame(i, directions[i]));
            var buffer = new CommandBuffer(new CommandBufferSettings());
            new CommandParser(new CommandParserSettings()).Parse(history, buffer);
            Assert.That(buffer.Count, Is.Zero);
        }

        [Test]
        public void BufferLifetimeIncludesCreationFrameAndExpiresExclusively()
        {
            var buffer = new CommandBuffer(new CommandBufferSettings());
            buffer.Push(CombatCommand.LMB, Frame(10));
            buffer.Push(CombatCommand.Space, Frame(10));
            Assert.That(buffer.Peek(15, out var attack), Is.True);
            Assert.That(attack.ExpireFrame, Is.EqualTo(16));
            Assert.That(buffer.TryConsume(CombatCommand.LMB, 16, out _), Is.False);
            Assert.That(buffer.TryConsume(CombatCommand.Space, 17, out _), Is.True);
            buffer.Push(CombatCommand.Space, Frame(20));
            Assert.That(buffer.Peek(28, out _), Is.False);
        }

        [Test]
        public void FullBufferDropsOldestAndConsumptionSkipsUnrelatedCommands()
        {
            var buffer = new CommandBuffer(new CommandBufferSettings { Capacity = 2 });
            buffer.Push(CombatCommand.LMB, Frame(1));
            buffer.Push(CombatCommand.Space, Frame(2));
            buffer.Push(CombatCommand.RMB, Frame(3));
            Assert.That(buffer.TryConsume(CombatCommand.LMB, 3, out _), Is.False);
            Assert.That(buffer.TryConsume(CombatCommand.RMB, 3, out _), Is.True);
            Assert.That(buffer.Peek(3, out var remaining), Is.True);
            Assert.That(remaining.Command, Is.EqualTo(CombatCommand.Space));
        }

        [Test]
        public void ReleasedChordUsesPressSnapshotInsteadOfCurrentDirectionAndHeld()
        {
            var history = new InputHistory();
            history.Add(new InputFrame(1, 1, 0, InputButtons.LMB, InputButtons.None,
                InputButtons.LMB | InputButtons.Shift, -1, -1, true));
            var buffer = new CommandBuffer(new CommandBufferSettings());
            new CommandParser(new CommandParserSettings()).Parse(history, buffer);
            Assert.That(buffer.Count, Is.EqualTo(1));
            Assert.That(buffer.TryConsume(CombatCommand.Shift_S_LMB, 1, out var command), Is.True);
            Assert.That(command.Horizontal, Is.EqualTo(-1));
            Assert.That(command.Vertical, Is.EqualTo(-1));
        }

        [Test]
        public void IndependentButtonsSurviveAndHeldPrimaryDoesNotRepeat()
        {
            var history = new InputHistory();
            var buffer = new CommandBuffer(new CommandBufferSettings());
            var parser = new CommandParser(new CommandParserSettings());
            history.Add(Frame(1, pressed: InputButtons.LMB | InputButtons.RMB | InputButtons.Jump));
            parser.Parse(history, buffer);
            history.Add(Frame(2, held: InputButtons.LMB));
            parser.Parse(history, buffer);
            Assert.That(buffer.Count, Is.EqualTo(3));
        }
    }
}
