using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;

namespace CombatPets
{
    internal class MultiplayerService
    {
        private readonly ModEntry Mod;
        private PetRegister PetRegister;
        private readonly AnimationManager Animation;

        // playerId, petId
        public event Action<long, string>? ToggleFollowRequested;

        public MultiplayerService(ModEntry mod)
        {
            Mod = mod;
            Animation = new AnimationManager(Mod.Helper);
        }

        public void SetPetRegister(PetRegister register)
        {
            PetRegister = register;
        }

        // mod messages are not sent back to sender, so must from someone else
        public void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != Mod.ModManifest.UniqueID) return;

            switch (e.Type)
            {
                case MultiplayerMessageType.ToggleFollowRequest when Context.IsMainPlayer:
                    {
                        ToggleFollowRequestMessage request = e.ReadAs<ToggleFollowRequestMessage>();
                        ToggleFollowRequested?.Invoke(e.FromPlayerID, request.PetId);
                        break;
                    }

                case MultiplayerMessageType.ToggleFollowResult when IsMessageFromHost(e):
                    ShowToggleFollowResult(e.ReadAs<ToggleFollowResultMessage>());
                    break;

                case MultiplayerMessageType.AttackEffect when IsMessageFromHost(e):
                    ApplyAttackEffect(e.ReadAs<AttackEffectMessage>());
                    break;

                case MultiplayerMessageType.PetHitEffect when IsMessageFromHost(e):
                    ApplyPetHitEffect(e.ReadAs<PetHitEffectMessage>());
                    break;

                case MultiplayerMessageType.RefreshRegistry when IsMessageFromHost(e):
                    PetRegister.Refresh();
                    break;
            }
        }

        public void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
        {
            if (!Context.IsMainPlayer) return;
            if (!e.Peer.HasSmapi)
            {
                Mod.Monitor.Log(
                    $"Player {e.Peer.PlayerID} joined without SMAPI. "
                    + "Vanilla pet movement may still appear, but Combat Pets controls and effects won't work for them. Latency may exist on pets.",
                    LogLevel.Warn
                );
                return;
            }
              

            if (e.Peer.GetMod(Mod.ModManifest.UniqueID) is null)
            {
                Mod.Monitor.Log(
                    $"Player {e.Peer.PlayerID} joined without {Mod.ModManifest.UniqueID}. "
                    + "Vanilla pet movement may still appear, but Combat Pets controls and effects won't work for them. Latency may exist on pets.",
                    LogLevel.Warn
                );
            }
        }

        private bool IsMessageFromHost(ModMessageReceivedEventArgs e)
        {
            return Context.IsWorldReady
                && Game1.MasterPlayer is not null
                && e.FromPlayerID == Game1.MasterPlayer.UniqueMultiplayerID;
        }

        public GameLocation? FindActiveLocation(string nameOrUniqueName)
        {
            return Mod.Helper.Multiplayer.GetActiveLocations()
                .FirstOrDefault(location => location.NameOrUniqueName == nameOrUniqueName);
        }

        public void SendRefreshRegistry()
        {
            Mod.Helper.Multiplayer.SendMessage(
                new RefreshRegistryMessage(),
                MultiplayerMessageType.RefreshRegistry,
                modIDs: new[] { Mod.ModManifest.UniqueID }
            );
        }

        public void SendToggleFollowRequest(string petId)
        {
            // only host receives
            Mod.Helper.Multiplayer.SendMessage(
                new ToggleFollowRequestMessage { PetId = petId },
                MultiplayerMessageType.ToggleFollowRequest,
                modIDs: new[] { Mod.ModManifest.UniqueID },
                playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID }
            );
        }

        public void SendToggleFollowResult(long playerId, ToggleFollowResultMessage result)
        {
            Mod.Helper.Multiplayer.SendMessage(
                result,
                MultiplayerMessageType.ToggleFollowResult,
                modIDs: new[] { Mod.ModManifest.UniqueID },
                playerIDs: new[] { playerId }
            );
        }

        // for render
        public void ShowToggleFollowResult(ToggleFollowResultMessage result)
        {
            if (result.Success)
            {
                string key = result.IsFollowing ? "follow.started" : "follow.stopped";
                Game1.showGlobalMessage(Mod.Helper.Translation.Get(key, new { petName = result.PetName }));
                return;
            }

            string message = result.ErrorCode switch
            {
                "disabled-in-mines" => Mod.Helper.Translation.Get("follow.disabled-in-mines", new { petName = result.PetName}),
                "capacity" => Mod.Helper.Translation.Get("follow.capacity-reached"),
                "owned" => Mod.Helper.Translation.Get("follow.already-following", 
                new { petName = result.PetName, farmerName = result.OwnerName }),
                _ => Mod.Helper.Translation.Get("follow.pet-not-found")
            };

            Game1.showRedMessage(message);
        }

        /// <summary>
        /// Apply to the effect first for host, then broadcast to all clients. Clients will apply the effect when they receive the message.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="area"></param>
        /// <param name="flipped"></param>
        public void BroadcastAttackEffect(GameLocation location, Rectangle area, bool flipped)
        {
            AttackEffectMessage message = new()
            {
                LocationName = location.NameOrUniqueName,
                X = area.X,
                Y = area.Y,
                Width = area.Width,
                Height = area.Height,
                Flipped = flipped
            };

            ApplyAttackEffect(message);

            if (Context.IsMultiplayer)
            {
                Mod.Helper.Multiplayer.SendMessage(
                    message,
                    MultiplayerMessageType.AttackEffect,
                    modIDs: new[] { Mod.ModManifest.UniqueID }
                );
            }
        }

        /// <summary>
        /// Apply to the effect first for host, then broadcast to all clients. Clients will apply the effect when they receive the message.
        /// </summary>
        /// <param name="pet"></param>
        /// <param name="damage"></param>
        /// <param name="invincibleTicks"></param>
        /// <param name="attackedTicks"></param>
        /// <param name="defeated"></param>
        public void BroadcastPetHit(Pet pet, int damage, int invincibleTicks, int attackedTicks, bool defeated)
        {
            string? petId = PetRegister.GetPetId(pet);
            if (petId is null || pet.currentLocation is null) return;

            Point standingPixel = pet.StandingPixel;
            PetHitEffectMessage message = new()
            {
                PetId = petId,
                PetName = pet.Name,
                LocationName = pet.currentLocation.NameOrUniqueName,
                Damage = damage,
                X = standingPixel.X + 8,
                Y = standingPixel.Y,
                InvincibleTicks = invincibleTicks,
                AttackedTicks = attackedTicks,
                Defeated = defeated
            };

            ApplyPetHitEffect(message);

            if (Context.IsMultiplayer)
            {
                Mod.Helper.Multiplayer.SendMessage(
                    message,
                    MultiplayerMessageType.PetHitEffect,
                    modIDs: new[] { Mod.ModManifest.UniqueID }
                );
            }
        }



        public void ApplyAttackEffect(AttackEffectMessage message)
        {
            GameLocation? location = FindActiveLocation(message.LocationName);
            if (location is null) return;

            Animation.DrawAttack(
                location,
                new Rectangle(message.X, message.Y, message.Width, message.Height),
                message.Flipped
            );
        }

        public void ApplyPetHitEffect(PetHitEffectMessage message)
        {
            PetManager? manager = PetRegister.getManager(message.PetId);
            if (manager is null) return;

            manager.PetState.Attacked();

            Mod.Monitor.VerboseLog($"Apply Hit Effect on {manager.pet.name}");

            GameLocation? location = FindActiveLocation(message.LocationName);
            if (location is null || location != Game1.currentLocation)
                return;

            if (message.Damage > 0 && Context.IsMainPlayer && manager is not null)
            {
                location.debris.Add(new Debris(message.Damage, new Vector2(message.X, message.Y), Color.Yellow,
                    1f, manager.pet));
            }

            Game1.playSound(message.Damage > 0 ? "ow" : "yoba");

            if (message.Defeated)
            {
                Game1.showGlobalMessage(Mod.Helper.Translation.Get("combat-service.pet-defeat-announcement",
                        new { petName = message.PetName }));
            }
        }

    }
}
