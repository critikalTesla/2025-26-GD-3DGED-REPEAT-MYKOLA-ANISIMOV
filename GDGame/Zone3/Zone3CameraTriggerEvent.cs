namespace GDGame.Zone3
{
    public enum Zone3CameraMode
    {
        FirstPerson,
        Orbit,
        Cinematic
    }

    public sealed class Zone3CameraTriggerEvent
    {
        public Zone3CameraMode Mode { get; }

        public Zone3CameraTriggerEvent(
            Zone3CameraMode mode)
        {
            Mode = mode;
        }
    }
}