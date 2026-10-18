using Darkest.Domain.Combat;
using Darkest.Domain.Core;
using Darkest.Domain.Items;

namespace Darkest.Domain.Hamlet
{
    /// <summary>마을에서의 장신구 장착/해제/판매. 장착 중인 장신구는 보관함에 없고, 해제하면 보관함으로 돌아온다.</summary>
    public sealed class TrinketService
    {
        private readonly Town _town;
        private readonly IEventBus _events;

        public TrinketService(Town town, IEventBus events)
        {
            _town = Guard.NotNull(town, nameof(town));
            _events = Guard.NotNull(events, nameof(events));
        }

        /// <summary>보관함의 장신구를 빈 슬롯 중 앞쪽에 낀다.</summary>
        public string TryEquip(Hero hero, TrinketDefinition trinket)
        {
            if (hero == null || _town.FindHero(hero.Id) == null) return "명부에 없는 영웅입니다.";
            if (trinket == null || !Contains(trinket)) return "보관함에 없는 장신구입니다.";
            int slot = FirstEmptySlot(hero);
            if (slot < 0) return "장신구 슬롯이 가득 찼습니다. 먼저 하나를 빼세요.";

            _town.RemoveTrinket(trinket);
            hero.SetTrinket(slot, trinket);
            _events.Publish(new TownChanged(hero.DisplayName + " " + trinket.DisplayName + " 장착"));
            return null;
        }

        public string TryUnequip(Hero hero, int slot)
        {
            if (hero == null || _town.FindHero(hero.Id) == null) return "명부에 없는 영웅입니다.";
            if (slot < 0 || slot >= Hero.TrinketSlots) return "잘못된 슬롯입니다.";
            var trinket = hero.GetTrinket(slot);
            if (trinket == null) return "빈 슬롯입니다.";

            hero.SetTrinket(slot, null);
            _town.AddTrinket(trinket);
            _events.Publish(new TownChanged(hero.DisplayName + " " + trinket.DisplayName + " 해제"));
            return null;
        }

        public string TrySell(TrinketDefinition trinket)
        {
            if (trinket == null || !_town.RemoveTrinket(trinket)) return "보관함에 없는 장신구입니다.";
            _town.Earn(trinket.SellValue);
            _events.Publish(new TownChanged(trinket.DisplayName + " 판매"));
            return null;
        }

        private bool Contains(TrinketDefinition trinket)
        {
            for (int i = 0; i < _town.Trinkets.Count; i++)
            {
                if (_town.Trinkets[i] == trinket) return true;
            }
            return false;
        }

        private static int FirstEmptySlot(Hero hero)
        {
            for (int slot = 0; slot < Hero.TrinketSlots; slot++)
            {
                if (hero.GetTrinket(slot) == null) return slot;
            }
            return -1;
        }
    }
}
