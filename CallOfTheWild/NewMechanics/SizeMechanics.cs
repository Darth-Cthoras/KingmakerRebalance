﻿using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallOfTheWild.SizeMechanics
{
    public class UnitPartSizeOverride : AdditiveUnitPart
    {
        public new void addBuff(Fact buff)
        {
            base.addBuff(buff);
            this.Owner?.Ensure<UnitPartSizeModifier>()?.Remove(null);
        }


        public new void removeBuff(Fact buff)
        {
            base.removeBuff(buff);
            this.Owner?.Ensure<UnitPartSizeModifier>()?.Remove(null);
        }


        public Size getSize()
        {
            if (buffs.Empty())
            {
                return this.Owner.OriginalSize;
            }
            else
            {                
                return buffs.Last().Blueprint.GetComponent<PermanentSizeOverride>().getSize();
            }
        }
    }



    [AllowedOn(typeof(BlueprintUnitFact))]
    public class PermanentSizeOverride : OwnedGameLogicComponent<UnitDescriptor>
    {
        public Size size;
        public override void OnFactActivate()
        {
            this.Owner.Ensure<UnitPartSizeOverride>().addBuff(this.Fact);
        }

        /*public override void OnTurnOn()
        {
            this.Owner.Ensure<UnitPartSizeOverride>().addBuff(this.Fact);
            //this.Owner.State.Size = this.Owner.Ensure<UnitPartSizeOverride>().getSize();
        }*/


        /*public override void OnTurnOff()
        {
            this.Owner.Ensure<UnitPartSizeOverride>().removeBuff(this.Fact);
            //this.Owner.State.Size = this.Owner.Ensure<UnitPartSizeOverride>().getSize();
        }*/

        public override void OnFactDeactivate()
        {
            this.Owner.Ensure<UnitPartSizeOverride>().removeBuff(this.Fact);
        }

        public Size getSize()
        {
            return size;
        }
    }


    [AllowMultipleComponents]
    [AllowedOn(typeof(BlueprintUnitFact))]
    public class AddFeatureBasedOnOriginalSize : OwnedGameLogicComponent<UnitDescriptor>, IGlobalSubscriber
    {
        public BlueprintFeature small_feature;
        public BlueprintFeature medium_feature;
        [JsonProperty]
        private Fact m_AppliedFact;

        public override void OnFactActivate()
        {
            this.Apply();
        }

        public override void OnFactDeactivate()
        {
            this.Owner.RemoveFact(this.m_AppliedFact);
            this.m_AppliedFact = (Fact)null;
        }


        private void Apply()
        {
            this.Owner.RemoveFact(this.m_AppliedFact);
            this.m_AppliedFact = (Fact)null;

            if (this.Owner.OriginalSize == Size.Small)
            {
                if (small_feature != null)
                {
                    this.m_AppliedFact = this.Owner.AddFact(this.small_feature, null, null);
                }
            }
            else if (medium_feature != null)
            {
                this.m_AppliedFact = this.Owner.AddFact(this.medium_feature, null, null);
            }

        }
    }


    class PrerequisiteCharacterSize : Prerequisite
    {
        public Size value;
        public bool or_smaller;
        public bool or_larger;

        public override bool Check([CanBeNull] FeatureSelectionState selectionState, [NotNull] UnitDescriptor unit, [NotNull] LevelUpState state)
        {
            return CheckUnit(unit);
        }

        public bool CheckUnit(UnitDescriptor unit)
        {
            if (unit.OriginalSize == value)
                return true;

            if (or_smaller && unit.OriginalSize < value)
                return true;

            if (or_larger && unit.OriginalSize > value)
                return true;

            return false;
        }

        public override string GetUIText()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string text = $"Size: {Kingmaker.Blueprints.Root.LocalizedTexts.Instance.Sizes.GetText(value)}";
            stringBuilder.Append(text);
            if (or_smaller)
                stringBuilder.Append(" or smaller");
            if (or_larger)
                stringBuilder.Append(" or larger");
            return stringBuilder.ToString();
        }
    }


    [Harmony12.HarmonyPatch(typeof(UnitPartSizeModifier), "UpdateSize")]
    class UnitPartSizeModifier_UnitPartSizeModifier_Patch
    {
        static void Postfix(UnitPartSizeModifier __instance, List<Fact> ___m_SizeChangeFacts)
        {
            Fact fact = ___m_SizeChangeFacts?.LastItem<Fact>();
            var part = __instance?.Owner?.Get<UnitPartSizeOverride>();
            if (fact == null && part != null)
            {
                __instance.Owner.State.Size = part.getSize();
            }
        }
    }


    [Harmony12.HarmonyPatch(typeof(ChangeUnitSize), "GetSize")]
    class ChangeUnitSize_GetSize_Patch
    {
        static void Postfix(ChangeUnitSize __instance, ref Size __result)
        {
            var change_type = Helpers.GetField<int>(__instance, "m_Type");
            var part = __instance?.Owner?.Get<UnitPartSizeOverride>();
            if (change_type == 0 && part != null)
            {
                __result = __instance.Owner.Get<UnitPartSizeOverride>().getSize().Shift(__instance.SizeDelta);
            }
        }
    }


}
