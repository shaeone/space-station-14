using System.Linq;
using System.Threading.Tasks;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using static Content.IntegrationTests.Tests.Destructible.DestructibleTestPrototypes;

namespace Content.IntegrationTests.Tests.Destructible
{
    [TestFixture]
    [TestOf(typeof(DestructibleComponent))]
    [TestOf(typeof(DamageThreshold))]
    public class DestructibleThresholdActivationTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var server = StartServer(new ServerContentIntegrationOption
            {
                ExtraPrototypes = Prototypes
            });

            await server.WaitIdleAsync();

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sMapManager = server.ResolveDependency<IMapManager>();
            var sPrototypeManager = server.ResolveDependency<IPrototypeManager>();
            var sEntitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            IEntity sDestructibleEntity = null; ;
            DamageableComponent sDamageableComponent = null;
            DestructibleComponent sDestructibleComponent = null;
            TestDestructibleListenerSystem sTestThresholdListenerSystem = null;
            DamageableSystem sDamageableSystem = null;

            await server.WaitPost(() =>
            {
                var gridId = GetMainGrid(sMapManager).GridEntityId;
                var coordinates = new EntityCoordinates(gridId, 0, 0);

                sDestructibleEntity = sEntityManager.SpawnEntity(DestructibleEntityId, coordinates);
                sDamageableComponent = sDestructibleEntity.GetComponent<DamageableComponent>();
                sDestructibleComponent = sDestructibleEntity.GetComponent<DestructibleComponent>();

                sTestThresholdListenerSystem = sEntitySystemManager.GetEntitySystem<TestDestructibleListenerSystem>();
                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                sDamageableSystem = sEntitySystemManager.GetEntitySystem<DamageableSystem>();
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);
            });

            await server.WaitAssertion(() =>
            {
                var bluntDamage = new DamageSpecifier(sPrototypeManager.Index<DamageTypePrototype>("TestBlunt"), 10);

                sDamageableSystem.TryChangeDamage(sDestructibleEntity.Uid, bluntDamage, true);

                // No thresholds reached yet, the earliest one is at 20 damage
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                sDamageableSystem.TryChangeDamage(sDestructibleEntity.Uid, bluntDamage, true);

                // Only one threshold reached, 20
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached.Count, Is.EqualTo(1));

                // Threshold 20
                var msg = sTestThresholdListenerSystem.ThresholdsReached[0];
                var threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Is.Empty);
                Assert.NotNull(threshold.Trigger);
                Assert.That(threshold.Triggered, Is.True);

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                sDamageableSystem.TryChangeDamage(sDestructibleEntity.Uid, bluntDamage*3, true);

                // One threshold reached, 50, since 20 already triggered before and it has not been healed below that amount
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached.Count, Is.EqualTo(1));

                // Threshold 50
                msg = sTestThresholdListenerSystem.ThresholdsReached[0];
                threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Has.Count.EqualTo(3));

                var soundThreshold = (PlaySoundBehavior) threshold.Behaviors[0];
                var spawnThreshold = (SpawnEntitiesBehavior) threshold.Behaviors[1];
                var actsThreshold = (DoActsBehavior) threshold.Behaviors[2];

                Assert.That(actsThreshold.Acts, Is.EqualTo(ThresholdActs.Breakage));
                Assert.That(soundThreshold.Sound.GetSound(), Is.EqualTo("/Audio/Effects/woodhit.ogg"));
                Assert.That(spawnThreshold.Spawn, Is.Not.Null);
                Assert.That(spawnThreshold.Spawn.Count, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Key, Is.EqualTo(SpawnedEntityId));
                Assert.That(spawnThreshold.Spawn.Single().Value.Min, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Value.Max, Is.EqualTo(1));
                Assert.NotNull(threshold.Trigger);
                Assert.That(threshold.Triggered, Is.True);

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Damage for 50 again, up to 100 now
                sDamageableSystem.TryChangeDamage(sDestructibleEntity.Uid, bluntDamage*5, true);

                // No thresholds reached as they weren't healed below the trigger amount
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Set damage to 0
                sDamageableSystem.SetAllDamage(sDamageableComponent, 0);

                // Damage for 100, up to 100
                sDamageableSystem.TryChangeDamage(sDestructibleEntity.Uid, bluntDamage*10, true);

                // Two thresholds reached as damage increased past the previous, 20 and 50
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached.Count, Is.EqualTo(2));

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Heal the entity for 40 damage, down to 60
                sDamageableSystem.TryChangeDamage(sDestructibleEntity.Uid, bluntDamage*-4, true);

                // Thresholds don't work backwards
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Damage for 10, up to 70
                sDamageableSystem.TryChangeDamage(sDestructibleEntity.Uid, bluntDamage, true);

                // Not enough healing to de-trigger a threshold
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Heal by 30, down to 40
                sDamageableSystem.TryChangeDamage(sDestructibleEntity.Uid, bluntDamage*-3, true);

                // Thresholds don't work backwards
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Damage up to 50 again
                sDamageableSystem.TryChangeDamage(sDestructibleEntity.Uid, bluntDamage, true);

                // The 50 threshold should have triggered again, after being healed
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached.Count, Is.EqualTo(1));

                msg = sTestThresholdListenerSystem.ThresholdsReached[0];
                threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Has.Count.EqualTo(3));

                soundThreshold = (PlaySoundBehavior) threshold.Behaviors[0];
                spawnThreshold = (SpawnEntitiesBehavior) threshold.Behaviors[1];
                actsThreshold = (DoActsBehavior) threshold.Behaviors[2];

                // Check that it matches the YAML prototype
                Assert.That(actsThreshold.Acts, Is.EqualTo(ThresholdActs.Breakage));
                Assert.That(soundThreshold.Sound.GetSound(), Is.EqualTo("/Audio/Effects/woodhit.ogg"));
                Assert.That(spawnThreshold.Spawn, Is.Not.Null);
                Assert.That(spawnThreshold.Spawn.Count, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Key, Is.EqualTo(SpawnedEntityId));
                Assert.That(spawnThreshold.Spawn.Single().Value.Min, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Value.Max, Is.EqualTo(1));
                Assert.NotNull(threshold.Trigger);
                Assert.That(threshold.Triggered, Is.True);

                // Reset thresholds reached
                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Heal all damage
                sDamageableSystem.SetAllDamage(sDamageableComponent, 0);

                // Damage up to 50
                sDamageableSystem.TryChangeDamage(sDestructibleEntity.Uid, bluntDamage*5, true);

                // Check that the total damage matches
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(FixedPoint2.New(50)));

                // Both thresholds should have triggered
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Has.Exactly(2).Items);

                // Verify the first one, should be the lowest one (20)
                msg = sTestThresholdListenerSystem.ThresholdsReached[0];
                var trigger = (DamageTrigger) msg.Threshold.Trigger;
                Assert.NotNull(trigger);
                Assert.That(trigger.Damage, Is.EqualTo(20));

                threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Is.Empty);

                // Verify the second one, should be the highest one (50)
                msg = sTestThresholdListenerSystem.ThresholdsReached[1];
                trigger = (DamageTrigger) msg.Threshold.Trigger;
                Assert.NotNull(trigger);
                Assert.That(trigger.Damage, Is.EqualTo(50));

                threshold = msg.Threshold;

                Assert.That(threshold.Behaviors, Has.Count.EqualTo(3));

                soundThreshold = (PlaySoundBehavior) threshold.Behaviors[0];
                spawnThreshold = (SpawnEntitiesBehavior) threshold.Behaviors[1];
                actsThreshold = (DoActsBehavior) threshold.Behaviors[2];

                // Check that it matches the YAML prototype
                Assert.That(actsThreshold.Acts, Is.EqualTo(ThresholdActs.Breakage));
                Assert.That(soundThreshold.Sound.GetSound(), Is.EqualTo("/Audio/Effects/woodhit.ogg"));
                Assert.That(spawnThreshold.Spawn, Is.Not.Null);
                Assert.That(spawnThreshold.Spawn.Count, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Key, Is.EqualTo(SpawnedEntityId));
                Assert.That(spawnThreshold.Spawn.Single().Value.Min, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Value.Max, Is.EqualTo(1));
                Assert.NotNull(threshold.Trigger);
                Assert.That(threshold.Triggered, Is.True);

                // Reset thresholds reached
                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Heal the entity completely
                sDamageableSystem.SetAllDamage(sDamageableComponent, 0);

                // Check that the entity has 0 damage
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(FixedPoint2.Zero));

                // Set both thresholds to only trigger once
                foreach (var destructibleThreshold in sDestructibleComponent.Thresholds)
                {
                    Assert.NotNull(destructibleThreshold.Trigger);
                    destructibleThreshold.TriggersOnce = true;
                }

                // Damage the entity up to 50 damage again
                sDamageableSystem.TryChangeDamage(sDestructibleEntity.Uid, bluntDamage*5, true);

                // Check that the total damage matches
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(FixedPoint2.New(50)));

                // No thresholds should have triggered as they were already triggered before, and they are set to only trigger once
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Set both thresholds to trigger multiple times
                foreach (var destructibleThreshold in sDestructibleComponent.Thresholds)
                {
                    Assert.NotNull(destructibleThreshold.Trigger);
                    destructibleThreshold.TriggersOnce = false;
                }

                // Check that the total damage matches
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(FixedPoint2.New(50)));

                // They shouldn't have been triggered by changing TriggersOnce
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);
            });
        }
    }
}
