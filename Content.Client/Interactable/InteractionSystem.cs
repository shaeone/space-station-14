using Content.Client.Storage;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Client.Interactable
{
    public sealed class InteractionSystem : SharedInteractionSystem
    {
        public override bool CanAccessViaStorage(EntityUid user, EntityUid target)
        {
            if (!EntityManager.TryGetEntity(target, out var entity))
                return false;

            if (!entity.TryGetContainer(out var container))
                return false;

            if (!EntityManager.TryGetComponent(container.Owner.Uid, out ClientStorageComponent storage))
                return false;

            // we don't check if the user can access the storage entity itself. This should be handed by the UI system.
            return storage.UIOpen;
        }
    }
}
