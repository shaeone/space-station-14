using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Body.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    class AddHandCommand : IConsoleCommand
    {
        public const string DefaultHandPrototype = "LeftHandHuman";

        public string Command => "addhand";
        public string Description => "Adds a hand to your entity.";
        public string Help => $"Usage: {Command} <entityUid> <handPrototypeId> / {Command} <entityUid> / {Command} <handPrototypeId> / {Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (args.Length > 1)
            {
                shell.WriteLine(Help);
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            EntityUid entity;
            EntityUid hand;

            switch (args.Length)
            {
                case 0:
                {
                    if (player == null)
                    {
                        shell.WriteLine("Only a player can run this command without arguments.");
                        return;
                    }

                    if (player.AttachedEntityUid == null)
                    {
                        shell.WriteLine("You don't have an entity to add a hand to.");
                        return;
                    }

                    entity = player.AttachedEntityUid.Value;
                    hand = entityManager.SpawnEntity(DefaultHandPrototype, entityManager.GetComponent<TransformComponent>(entity).Coordinates).Uid;
                    break;
                }
                case 1:
                {
                    if (EntityUid.TryParse(args[0], out var uid))
                    {
                        if (!entityManager.EntityExists(uid))
                        {
                            shell.WriteLine($"No entity found with uid {uid}");
                            return;
                        }

                        entity = uid;
                        hand = entityManager.SpawnEntity(DefaultHandPrototype, entityManager.GetComponent<TransformComponent>(entity).Coordinates).Uid;
                    }
                    else
                    {
                        if (player == null)
                        {
                            shell.WriteLine("You must specify an entity to add a hand to when using this command from the server terminal.");
                            return;
                        }

                        if (player.AttachedEntityUid == null)
                        {
                            shell.WriteLine("You don't have an entity to add a hand to.");
                            return;
                        }

                        entity = player.AttachedEntityUid.Value;
                        hand = entityManager.SpawnEntity(args[0], entityManager.GetComponent<TransformComponent>(entity).Coordinates).Uid;
                    }

                    break;
                }
                case 2:
                {
                    if (!EntityUid.TryParse(args[0], out var uid))
                    {
                        shell.WriteLine($"{args[0]} is not a valid entity uid.");
                        return;
                    }

                    if (!entityManager.EntityExists(uid))
                    {
                        shell.WriteLine($"No entity exists with uid {uid}.");
                        return;
                    }

                    entity = uid;

                    if (!prototypeManager.HasIndex<EntityPrototype>(args[1]))
                    {
                        shell.WriteLine($"No hand entity exists with id {args[1]}.");
                        return;
                    }

                    hand = entityManager.SpawnEntity(args[1], entityManager.GetComponent<TransformComponent>(entity).Coordinates).Uid;

                    break;
                }
                default:
                {
                    shell.WriteLine(Help);
                    return;
                }
            }

            if (!entityManager.TryGetComponent(entity, out SharedBodyComponent? body))
            {
                var random = IoCManager.Resolve<IRobustRandom>();
                var text = $"You have no body{(random.Prob(0.2f) ? " and you must scream." : ".")}";

                shell.WriteLine(text);
                return;
            }

            if (!entityManager.TryGetComponent(hand, out SharedBodyPartComponent? part))
            {
                shell.WriteLine($"Hand entity {hand} does not have a {nameof(SharedBodyPartComponent)} component.");
                return;
            }

            var slot = part.GetHashCode().ToString();
            body.SetPart(slot, part);

            shell.WriteLine($"Added hand to entity {entityManager.GetComponent<MetaDataComponent>(entity).EntityName}");
        }
    }
}
