namespace GDGame.Zone4
{
    public sealed class Zone4ButtonPressedEvent
    {
        public string ButtonName { get; }

        public Zone4ButtonPressedEvent(string buttonName)
        {
            ButtonName = buttonName;
        }
    }

    public sealed class Zone4StateRequestEvent
    {
        public string Reason { get; }

        public Zone4StateRequestEvent(string reason)
        {
            Reason = reason;
        }
    }

    public sealed class Zone4PulseImpulse
    {
        public float Strength { get; }

        public Zone4PulseImpulse(float strength)
        {
            Strength = strength;
        }
    }
}