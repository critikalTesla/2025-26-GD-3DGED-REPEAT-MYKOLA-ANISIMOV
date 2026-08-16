using GDEngine;
using GDEngine.Core;
using GDEngine.Core.Events;
using GDEngine.Core.Physics;

namespace GDGame.Zone3
{
    public sealed class Zone3CameraTriggerController :
        Component
    {
        public Zone3CameraMode Mode { get; set; }

        private bool _triggered;

        public override void Awake()
        {
            base.Awake();

            _triggered = false;
        }

        public override void Update()
        {
            base.Update();

            // Trigger collision handling will be connected
            // through the collision event system.
        }

        public void TriggerCameraSwitch()
        {
            if (_triggered)
                return;

            _triggered = true;

            EngineContext.Instance.Events.Publish(
                new Zone3CameraTriggerEvent(
                    Mode));
        }

        public void ResetTrigger()
        {
            _triggered = false;
        }
    }
}