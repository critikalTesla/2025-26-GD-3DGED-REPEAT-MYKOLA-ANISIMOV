using System;
using GDEngine.Core;
using GDEngine.Core.Events;

namespace GDGame.Zone3
{
    public sealed class Zone3CameraTriggerController : Component
    {
        public Zone3CameraMode Mode { get; set; }

        private IDisposable _triggerSubscription;

        public override void Awake()
        {
            base.Awake();

            _triggerSubscription =
                EngineContext.Instance.Events.Subscribe<TriggerEvent>(
                    OnTriggerEvent);
        }

        private void OnTriggerEvent(TriggerEvent evt)
        {
            if (GameObject == null)
                return;

            // Only react if THIS object is the trigger.
            if (evt.Trigger.GameObject != GameObject)
                return;

            // Only the FPS player can activate it.
            if (evt.Other.GameObject.Name !=
                AppData.CAMERA_NAME_FIRST_PERSON_PARENT)
            {
                return;
            }

            System.Diagnostics.Debug.WriteLine(
                $"ZONE 3 TRIGGER HIT: {Mode}");

            EngineContext.Instance.Events.Publish(
                new Zone3CameraTriggerEvent(Mode));
        }

        public override void OnDestroy()
        {
            _triggerSubscription?.Dispose();
            _triggerSubscription = null;

            base.OnDestroy();
        }
    }
}