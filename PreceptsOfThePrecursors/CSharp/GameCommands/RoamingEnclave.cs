﻿using System.Collections.Generic;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;

namespace PreceptsOfThePrecursors.GameCommands
{
    public class MarkUpUnits : BaseGameCommand
    {
        public override void Execute(GameCommand command, ArcenSimContext context)
        {
            for (int x = 0; x < command.RelatedEntityIDs.Count; x++)
            {
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad(command.RelatedEntityIDs[x]);
                if (entity == null)
                    continue;

                entity.SetCurrentMarkLevel((byte)(entity.CurrentMarkLevel + 1), context);
            }
        }
    }

    public class PopulateEnclavesList : BaseGameCommand
    {
        public override void Execute(GameCommand command, ArcenSimContext context)
        {
            Faction faction = command.GetRelatedFaction();
            if (faction == null)
                return;
            BaseRoamingEnclave REFaction = faction.Implementation as BaseRoamingEnclave;
            REFaction.Enclaves = new List<GameEntity_Squad>();
            for (int x = 0; x < command.RelatedEntityIDs.Count; x++)
            {
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad(command.RelatedEntityIDs[x]);
                if (entity != null)
                    REFaction.Enclaves.Add(entity);
            }
        }
    }

    public class PopulateHivesList : BaseGameCommand
    {
        public override void Execute(GameCommand command, ArcenSimContext context)
        {
            BaseRoamingEnclave faction = command.GetRelatedFaction().Implementation as BaseRoamingEnclave;
            faction.Hives = new List<GameEntity_Squad>();
            for (int x = 0; x < command.RelatedEntityIDs.Count; x++)
            {
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad(command.RelatedEntityIDs[x]);
                if (entity != null)
                    faction.Hives.Add(entity);
            }
        }
    }

    public class PopulateEnclavePlanetList : BaseGameCommand
    {
        public override void Execute(GameCommand command, ArcenSimContext context)
        {
            BaseRoamingEnclave faction = command.GetRelatedFaction().Implementation as BaseRoamingEnclave;
            faction.EnclavePlanets = new List<Planet>();
            for (int x = 0; x < command.RelatedIntegers.Count; x++)
            {
                Planet planet = World_AIW2.Instance.GetPlanetByIndex((short)command.RelatedIntegers[x]);
                if (planet != null)
                    faction.EnclavePlanets.Add(planet);
            }
        }
    }

    public class PopulateHivePlanetsList : BaseGameCommand
    {
        public override void Execute(GameCommand command, ArcenSimContext context)
        {
            BaseRoamingEnclave faction = command.GetRelatedFaction().Implementation as BaseRoamingEnclave;
            faction.HivePlanets = new List<Planet>();
            for (int x = 0; x < command.RelatedIntegers.Count; x++)
            {
                Planet planet = World_AIW2.Instance.GetPlanetByIndex((short)command.RelatedIntegers[x]);
                if (planet != null)
                    faction.HivePlanets.Add(planet);
            }
        }
    }

    public class SetOrClearEnclaveOwnership : BaseGameCommand
    {
        public override void Execute(GameCommand command, ArcenSimContext context)
        {
            for (int x = 0; x < command.RelatedIntegers.Count; x++)
            {
                GameEntity_Squad unit = World_AIW2.Instance.GetEntityByID_Squad(command.RelatedIntegers[x]);
                if (unit == null)
                    continue;
                GameEntity_Squad enclave = World_AIW2.Instance.GetEntityByID_Squad(command.RelatedIntegers2[x]);
                if (enclave == null)
                    unit.MinorFactionStackingID = -1;
                else
                {
                    unit.MinorFactionStackingID = enclave.PrimaryKeyID;
                }
            }
        }
    }

    public class LoadYounglingsIntoEnclaves : BaseGameCommand
    {
        public override void Execute(GameCommand command, ArcenSimContext context)
        {
            for (int x = 0; x < command.RelatedIntegers.Count; x++)
            {
                GameEntity_Squad unit = World_AIW2.Instance.GetEntityByID_Squad(command.RelatedIntegers[x]);
                if (unit == null)
                    continue;
                GameEntity_Squad enclave = World_AIW2.Instance.GetEntityByID_Squad(command.RelatedIntegers2[x]);
                if (enclave == null)
                    continue;
                if (unit.Planet != enclave.Planet)
                    continue;
                if (unit.GetDistanceTo_VeryCheapButExtremelyRough(enclave.WorldLocation, true) < 2500)
                    enclave.StoreYoungling(unit, context);
                else
                {
                    unit.Orders.ClearOrders(ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans);
                    unit.Orders.QueueOrder(unit, EntityOrder.Create_Move_Normal(unit, enclave.WorldLocation, true, OrderSource.Other));
                }
            }
        }
    }

    public class UnloadYounglingsFromEnclaves : BaseGameCommand
    {
        public override void Execute(GameCommand command, ArcenSimContext context)
        {
            for (int x = 0; x < command.RelatedEntityIDs.Count; x++)
            {
                GameEntity_Squad enclave = World_AIW2.Instance.GetEntityByID_Squad(command.RelatedEntityIDs[x]);
                if (enclave == null)
                    continue;
                enclave.UnloadYounglings(context);
            }
        }
    }

    public class AddHivesToBuildList : BaseGameCommand
    {
        public override void Execute(GameCommand command, ArcenSimContext context)
        {
            for (int x = 0; x < command.RelatedEntityIDs.Count; x++)
            {
                GameEntity_Squad commandStation = World_AIW2.Instance.GetEntityByID_Squad(command.RelatedEntityIDs[x]);
                if (commandStation != null)
                    commandStation.FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates(GameEntityTypeDataTable.Instance.GetRowByName(BaseRoamingEnclave.HUMAN_HIVE_NAME)).ExplicitBaseSquadCap = 1;
            }
        }
    }

    public class ClaimHivesFromHumanAllies : BaseGameCommand
    {
        public override void Execute(GameCommand command, ArcenSimContext context)
        {
            Faction faction = command.GetRelatedFaction();
            if (faction == null)
                return;
            for (int x = 0; x < command.RelatedEntityIDs.Count; x++)
            {
                GameEntity_Squad hive = World_AIW2.Instance.GetEntityByID_Squad(command.RelatedEntityIDs[x]);

                GameEntity_Squad.CreateNew(hive.Planet.GetPlanetFactionForFaction(faction), GameEntityTypeDataTable.Instance.GetRandomRowWithTag(context, BaseRoamingEnclave.HIVE_TAG), 1,
                    hive.Planet.GetPlanetFactionForFaction(faction).FleetUsedAtPlanet, 0, hive.WorldLocation, context);

                hive.Despawn(context, true, InstancedRendererDeactivationReason.IAmTransforming);
            }
        }
    }

    public class AddPlayerRoamingYounglingToBuildList : BaseGameCommand
    {
        public override void Execute(GameCommand command, ArcenSimContext context)
        {
            for (int x = 0; x < command.RelatedEntityIDs.Count; x++)
            {
                GameEntity_Squad station = World_AIW2.Instance.GetEntityByID_Squad(command.RelatedEntityIDs[x]);

                station.FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates(GameEntityTypeDataTable.Instance.GetRowByName("PlayerRoamingEnclaveConstructor")).ExplicitBaseSquadCap = 1;

                World_AIW2.Instance.QueueLogJournalEntryToSidebar("PlayerRoamingEnclaveAvailable", string.Empty, station.Planet, context);
            }
        }
    }

    public class ActivatePlayerRoamingYoungling : BaseGameCommand
    {
        public override void Execute(GameCommand command, ArcenSimContext context)
        {
            if (command.RelatedEntityIDs.Count <= 0)
                return;
            GameEntity_Squad enclaveConstructor = World_AIW2.Instance.GetEntityByID_Squad(command.RelatedEntityIDs[0]);
            if (enclaveConstructor == null)
                return;

            enclaveConstructor.FleetMembership.ExplicitBaseSquadCap--;

            GameEntity_Squad enclave = enclaveConstructor.Planet.Mapgen_SeedEntity(context, enclaveConstructor.PlanetFaction.Faction, GameEntityTypeDataTable.Instance.GetRowByName(BaseRoamingEnclave.PLAYER_ENCLAVE_TAG), PlanetSeedingZone.MostAnywhere);

            enclave.SetWorldLocation(enclaveConstructor.WorldLocation);

            enclave.FleetMembership.RemoveEntity(enclave, false);

            Fleet fleet = Fleet.Create(FleetCategory.PlayerMobile, enclaveConstructor.PlanetFaction.Faction, enclaveConstructor.PlanetFaction, enclave);

            fleet.NameRaw = "Pearl Enclave's Gift";

            enclaveConstructor.Despawn(context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType);

        }
    }

    public class PopulatePlayerEnclavesList : BaseGameCommand
    {
        public override void Execute(GameCommand command, ArcenSimContext context)
        {
            Faction faction = command.GetRelatedFaction();
            if (faction == null)
                return;
            RoamingEnclavePlayerTeam REFaction = faction.Implementation as RoamingEnclavePlayerTeam;
            REFaction.PlayerEnclaves = new List<GameEntity_Squad>();
            for (int x = 0; x < command.RelatedEntityIDs.Count; x++)
            {
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad(command.RelatedEntityIDs[x]);
                if (entity != null)
                    REFaction.PlayerEnclaves.Add(entity);
            }
        }
    }
}
