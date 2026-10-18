using Darkest.Domain.Combat;
using Darkest.Domain.Core;

namespace Darkest.Domain.Content
{
    /// <summary>순차 ID 발급기. seed와 함께 쓰면 재현 가능한 ID가 나온다.</summary>
    public sealed class SequentialIdGenerator
    {
        private int _next;

        public SequentialIdGenerator(int start = 1)
        {
            _next = start;
        }

        public int PeekNext => _next;

        public string Next(string prefix) => prefix + "-" + (_next++);

        /// <summary>세이브 복원 후 ID 충돌 방지.</summary>
        public void EnsureAbove(int value)
        {
            if (_next <= value) _next = value + 1;
        }
    }

    /// <summary>정의(Definition) → 런타임 인스턴스 생성 Factory.</summary>
    public sealed class CombatantFactory
    {
        private readonly IContentCatalog _catalog;
        private readonly SequentialIdGenerator _ids;
        private readonly int _maxStress;

        public CombatantFactory(IContentCatalog catalog, SequentialIdGenerator ids, int maxStress)
        {
            _catalog = Guard.NotNull(catalog, nameof(catalog));
            _ids = Guard.NotNull(ids, nameof(ids));
            _maxStress = maxStress;
        }

        public SequentialIdGenerator Ids => _ids;

        public Hero CreateHero(HeroClassDefinition heroClass, string name, string id = null)
        {
            Guard.NotNull(heroClass, nameof(heroClass));
            return new Hero(id ?? _ids.Next("hero"), name, heroClass.Id, heroClass.Stats, heroClass.Skills,
                heroClass.PortraitKey, _maxStress);
        }

        public Hero CreateRandomHero(IRandom random)
        {
            var heroClass = random.Pick(_catalog.HeroClasses);
            var name = random.Pick(_catalog.HeroNames);
            return CreateHero(heroClass, name);
        }

        public Monster CreateMonster(string monsterId)
        {
            var def = _catalog.GetMonster(monsterId);
            return new Monster(_ids.Next("mon"), def.DisplayName, def.Id, def.Stats, def.Skills, def.PortraitKey);
        }

        public Formation CreateMonsterFormation(MonsterGroupDefinition group)
        {
            Guard.NotNull(group, nameof(group));
            var formation = new Formation(Team.Monsters);
            for (int i = 0; i < group.MonsterIds.Count; i++) formation.Add(CreateMonster(group.MonsterIds[i]));
            return formation;
        }
    }
}
