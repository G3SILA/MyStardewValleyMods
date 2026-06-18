using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatPets
{
    public sealed class ModConfig
    {
        public bool EnablePetFollowing { get; set; } = true; 
        public int FollowDistance { get; set; } = 1; // tiles away the pet should follow
        public int FollowSpeed { get; set; } = 4; // Default 4
    }
}
