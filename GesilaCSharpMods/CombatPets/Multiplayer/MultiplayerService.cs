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

        // playerId, petId
        public event Action<long, string>? ToggleFollowRequested;

        public MultiplayerService(ModEntry mod)
        {
            Mod = mod;
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
                "capacity" => Mod.Helper.Translation.Get("follow.capacity-reached"),
                "owned" => Mod.Helper.Translation.Get("follow.already-following", 
                new { petName = result.PetName, farmerName = result.OwnerName }),
                _ => Mod.Helper.Translation.Get("follow.pet-not-found")
            };

            Game1.showRedMessage(message);
        }

    }
}
