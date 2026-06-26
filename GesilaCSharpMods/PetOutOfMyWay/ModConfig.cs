
using System.Runtime.InteropServices;

namespace PetOutOfMyWay
{
    public sealed class ModConfig
    {
        public bool PetOutOfMyWayEnabled { get; set; } = true;
        public int TimeFarmerMustPushBeforeStartShaking { get; set; } = 0; // vanilla: 300
        public int TimeFarmerMustPushBeforePassingThrough { get; set;  } = 0; // vanilla: 750
    }
}
