using System;
using GDEngine.Core.Components;
using GDEngine.Core.Events;
using GDEngine.Core.Services;

namespace GDGame.Zone3
{
    public sealed class Zone3CameraTriggerController : Component
    {
        public Zone3CameraMode Mode { get; set; }

        private IDisposable? _triggerSubscription;

        protected override void Awake()
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

            System.Diagnostics.Debug.WriteLine(
                $"TRIGGER EVENT: " +
                $"Trigger={evt.TriggerBody?.GameObject?.Name}, " +
                $"Other={evt.OtherBody?.GameObject?.Name}");

            // Only react when THIS GameObject is the trigger volume.
            if (evt.TriggerBody?.GameObject != GameObject)
                return;

            System.Diagnostics.Debug.WriteLine(
                $"ZONE 3 CAMERA TRIGGER ACTIVATED: {Mode}");

            EngineContext.Instance.Events.Publish(
                new Zone3CameraTriggerEvent(Mode));
        }

        protected override void OnDestroy()
        {
            _triggerSubscription?.Dispose();
            _triggerSubscription = null;

            base.OnDestroy();
        }
    }
}