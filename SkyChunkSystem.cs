using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

[assembly: ModInfo("SkyChunk", Side = "Server",
	Description = "Removes most chunks in the world to create a unique challenge",
	Website = "https://github.com/copygirl/SkyChunk",
	Authors = new[] { "copygirl" })]

namespace SkyChunk
{
	public class SkyChunkSystem : ModSystem
	{
		public const string MOD_ID = "skychunk";
		public const double GEN_CHANCE = 0.15;

		public Harmony Harmony { get; } = new(MOD_ID);

		public override void StartServerSide(ICoreServerAPI api)
		{
			api.Event.SaveGameLoaded += () => {
				var config = api.WorldManager.SaveGame.WorldConfiguration;
				// Set a flag on world creation so this mod only ever affects
				// worlds created when the mod is present, never existing ones.
				if (api.WorldManager.SaveGame.IsNew) config.SetBool(MOD_ID + ":enabled", true);
				else if (!config.GetBool(MOD_ID + ":enabled")) return;

				// Only apply patch when world is new or flag was set.
				Harmony.PatchAll();
			};
		}

		public override void Dispose()
			=> Harmony.UnpatchAll(MOD_ID);
	}

	[HarmonyPatch(typeof(GenTerra), "OnChunkColumnGen")]
	class GenTerra_OnChunkColumnGen_Patch
	{
		static Random Random { get; } = new();

		static bool Prefix(ICoreServerAPI ___api, IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams)
		{
			var world = ___api.WorldManager;
			// Always generate the 4 spawn chunks around the default spawn position.
			if ((chunkX / 2 == world.MapSizeX / world.ChunkSize / 4) &&
			    (chunkZ / 2 == world.MapSizeZ / world.ChunkSize / 4))
				return true;

			// Seeding the Random like this seems to generate patterns, so non-deterministic it is!
			// var seed = (int)world.Seed ^ chunkX ^ (chunkZ << 16 | chunkZ >> 16);
			return (Random.NextDouble() < SkyChunkSystem.GEN_CHANCE);
		}
	}
}
