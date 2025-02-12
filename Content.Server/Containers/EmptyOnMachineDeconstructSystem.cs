using Content.Server.Construction.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Content.Shared.Containers.ItemSlots;

namespace Content.Server.Containers
{
    /// <summary>
    /// Implements functionality of EmptyOnMachineDeconstructComponent.
    /// </summary>
    [UsedImplicitly]
    public class EmptyOnMachineDeconstructSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EmptyOnMachineDeconstructComponent, MachineDeconstructedEvent>(OnDeconstruct);
            SubscribeLocalEvent<ItemSlotsComponent, MachineDeconstructedEvent>(OnSlotsDeconstruct);
        }

        // really this should be handled by ItemSlotsSystem, but for whatever reason MachineDeconstructedEvent is server-side? So eh.
        private void OnSlotsDeconstruct(EntityUid uid, ItemSlotsComponent component, MachineDeconstructedEvent args)
        {
            foreach (var slot in component.Slots.Values)
            {
                if (slot.EjectOnDeconstruct && slot.Item != null)
                    slot.ContainerSlot.Remove(slot.Item);
            }
        }

        private void OnDeconstruct(EntityUid uid, EmptyOnMachineDeconstructComponent component, MachineDeconstructedEvent ev)
        {
            if (!EntityManager.TryGetComponent<IContainerManager>(uid, out var mComp))
                return;
            var baseCoords = component.Owner.Transform.Coordinates;
            foreach (var v in component.Containers)
            {
                if (mComp.TryGetContainer(v, out var container))
                {
                    container.EmptyContainer(true, baseCoords);
                }
            }
        }
    }
}
