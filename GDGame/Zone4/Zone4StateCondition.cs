using System;
using GDEngine.Core.Gameplay;

namespace GDGame.Zone4
{
    public sealed class Zone4StateCondition : IGameCondition
    {
        private readonly Func<bool> _condition;

        public string Description { get; }

        public Zone4StateCondition(
            string description,
            Func<bool> condition)
        {
            Description = description;
            _condition = condition;
        }

        public bool IsSatisfied()
        {
            return _condition();
        }
    }
}