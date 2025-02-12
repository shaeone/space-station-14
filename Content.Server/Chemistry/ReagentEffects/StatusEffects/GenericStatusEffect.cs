﻿using System;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.StatusEffect;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReagentEffects.StatusEffects
{
    /// <summary>
    ///     Adds a generic status effect to the entity,
    ///     not worrying about things like how to affect the time it lasts for
    ///     or component fields or anything. Just adds a component to an entity
    ///     for a given time. Easy.
    /// </summary>
    /// <remarks>
    ///     Can be used for things like adding accents or something. I don't know. Go wild.
    /// </remarks>
    [UsedImplicitly]
    public class GenericStatusEffect : ReagentEffect
    {
        [DataField("key", required: true)]
        public string Key = default!;

        [DataField("component")]
        public string Component = String.Empty;

        [DataField("time")]
        public float Time = 2.0f;

        /// <summary>
        ///     Should this effect add the status effect, remove time from it, or set its cooldown?
        /// </summary>
        [DataField("type")]
        public StatusEffectMetabolismType Type = StatusEffectMetabolismType.Add;

        public override void Effect(ReagentEffectArgs args)
        {
            var statusSys = args.EntityManager.EntitySysManager.GetEntitySystem<StatusEffectsSystem>();
            if (Type == StatusEffectMetabolismType.Add && Component != String.Empty)
            {
                statusSys.TryAddStatusEffect(args.SolutionEntity, Key, TimeSpan.FromSeconds(Time), Component);
            }
            else if (Type == StatusEffectMetabolismType.Remove)
            {
                statusSys.TryRemoveTime(args.SolutionEntity, Key, TimeSpan.FromSeconds(Time));
            }
            else if (Type == StatusEffectMetabolismType.Set)
            {
                statusSys.TrySetTime(args.SolutionEntity, Key, TimeSpan.FromSeconds(Time));
            }
        }
    }

    public enum StatusEffectMetabolismType
    {
        Add,
        Remove,
        Set
    }
}
