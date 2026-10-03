using Babodayo.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Babodayo.Tests
{
    // Runtime component lifecycle requires PlayMode (OnEnable is not invoked by EditMode AddComponent).
    public sealed class PlayerInputReaderTests : InputTestFixture
    {
        private GameObject owner;
        private PlayerInputReader reader;
        private Mouse mouse;
        private Keyboard keyboard;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            owner = new GameObject("Input reader test");
            reader = owner.AddComponent<PlayerInputReader>();
        }
        public override void TearDown()
        {
            Object.DestroyImmediate(owner);
            base.TearDown();
        }

        [Test]
        public void CursorPositionIsCapturedAndDisabledReaderHasNoPointer()
        {
            Set(mouse.position,new Vector2(320,180));
            var input=reader.Capture(1);
            Assert.That(input.HasPointer,Is.True);
            Assert.That(input.PointerPosition,Is.EqualTo(new Vector2(320,180)));
            reader.enabled=false;
            Assert.That(reader.Capture(2).HasPointer,Is.False);
        }

        [Test]
        public void ShortClickIsRetainedOnceAcrossSeveralCaptures()
        {
            Press(mouse.leftButton); Release(mouse.leftButton);
            var input = reader.Capture(1);
            Assert.That(input.WasPressed(InputButtons.LMB), Is.True);
            Assert.That(input.IsHeld(InputButtons.LMB), Is.False);
            Assert.That(input.Released & InputButtons.LMB, Is.EqualTo(InputButtons.LMB));
            Assert.That(reader.Capture(2).Pressed, Is.EqualTo(InputButtons.None));
            Assert.That(reader.Capture(3).Released, Is.EqualTo(InputButtons.None));
        }

        [Test]
        public void PrimaryChordKeepsPressTimeModifiersAfterRelease()
        {
            Press(keyboard.leftShiftKey); Press(keyboard.sKey); Press(mouse.leftButton);
            Release(mouse.leftButton); Release(keyboard.leftShiftKey); Release(keyboard.sKey);
            var history = new InputHistory();
            history.Add(reader.Capture(1));
            var buffer = new CommandBuffer(new CommandBufferSettings());
            new CommandParser(new CommandParserSettings()).Parse(history, buffer);
            Assert.That(buffer.TryConsume(CombatCommand.Shift_S_LMB, 1, out var command), Is.True);
            Assert.That(command.Vertical, Is.EqualTo(-1));
        }

        [Test]
        public void DisableClearsPendingEdgesAndReenableDoesNotDuplicateSubscriptions()
        {
            Press(mouse.leftButton);
            reader.enabled = false;
            Release(mouse.leftButton);
            reader.enabled = true;
            Assert.That(reader.Capture(1).Pressed, Is.EqualTo(InputButtons.None));
            Press(mouse.leftButton);
            Assert.That(reader.Capture(2).WasPressed(InputButtons.LMB), Is.True);
            Assert.That(reader.Capture(3).WasPressed(InputButtons.LMB), Is.False);
        }
    }
}
